using UnityEngine;
using PMF.Combat;
using PMF.Data;
using PMF.Grid;
using PMF.Pathing;
using PMF.Session;

namespace PMF.Actors
{
    /// <summary>
    /// 아군 유닛. 상태머신: Queued → Marching → Deployed.
    /// 행군 중에는 공격 불가 (Attacker.Enabled = false).
    /// D-03 토글이 꺼져 있으면(기본) 행군 중 TargetRegistry 에 등록되지 않는다.
    /// </summary>
    public sealed class AllyUnit : MonoBehaviour
    {
        private enum State { Queued, Marching, Deployed }

        private State _state = State.Queued;
        private readonly PathFollower _follower = new PathFollower();
        private readonly System.Collections.Generic.List<PathNode> _route =
            new System.Collections.Generic.List<PathNode>();
        private readonly System.Collections.Generic.List<PathNode> _lineBuffer =
            new System.Collections.Generic.List<PathNode>();

        private GameSession _session;
        private GridSystem _grid;
        private PathGraph _graph;
        private UnitDefinition _def;
        private StageDefinition _stage;
        private Health _health;
        private Attacker _attacker;
        private SpriteRenderer _sprite;
        private LineRenderer _marchLine;
        private UI.ShotLine _shotLine;

        private GridCoord _targetSlot;
        private Village _homeVillage;      // 재배치 때 도로를 우회하는 경유점
        private Vector3 _wayPoint;         // 목적지 전에 먼저 들르는 지점
        private bool _hasWayPoint;
        private bool _onStraightLeg;          // 마지막 노드 → 슬롯 직선 구간
        private float _marchStartTime;

        [SerializeField] private Color _marchingColor = new Color(0.55f, 0.75f, 1f);   // 연한 파랑
        [SerializeField] private Color _deployedColor = new Color(0.15f, 0.3f, 0.8f);  // 진한 파랑

        public bool IsDeployed => _state == State.Deployed;

        /// <summary>배치 전 행군 중인가. 정보 패널(G-15) 상태 표시용.</summary>
        public bool IsMarching => _state == State.Marching;

        /// <summary>이 유닛의 정의(SO). <b>읽기 전용으로만 써라</b> — 런타임 수정 금지 (CLAUDE.md §4).</summary>
        public UnitDefinition Definition => _def;

        /// <summary>현재 티어가 반영된 전투 능력치. 정보 패널(G-15)이 사거리·공격력·간격을 읽는다.</summary>
        public Attacker Attacker => _attacker;

        /// <summary>현재 점유 중(또는 행군 목표)인 슬롯. 슬롯 예약 해제(DeploymentController)에 쓴다.</summary>
        public GridCoord TargetSlot => _targetSlot;

        /// <summary>재배치 쿨다운이 끝났는가 (ADR-0008 B안).</summary>
        public bool CanRedeployNow => Time.time >= _nextRedeployTime;

        /// <summary>재배치 쿨다운 잔여 시간. 화면 표시용.</summary>
        public float RedeployCooldownRemaining => Mathf.Max(0f, _nextRedeployTime - Time.time);

        /// <summary>이 유닛이 슬롯을 떠났을 때 (재배치 명령 또는 사망). DeploymentController 가 예약 해제에 쓴다.</summary>
        public event System.Action<AllyUnit, GridCoord> OnLeftSlot;

        /// <summary>회수되었을 때. 환불액을 전달한다. DeploymentController 가 자원 반환에 쓴다.</summary>
        public event System.Action<AllyUnit, int> OnRetired;

        private float _nextRedeployTime;

        // --- 회수 (G-04) ---
        private const float RetireFadeDuration = 0.35f;   // 후퇴 연출 길이 — 격파 연출(G-08)과 달라야 한다 (톤 유지)
        private float _investedAmount;                    // 투입 총액 (고용비 + 업그레이드비) — 환불 기준. G-05 가 누적한다.
        private bool _retiring;
        private float _retireTimer;
        private Vector3 _initialScale;

        /// <summary>이 유닛에 지금까지 투입한 총액 (고용비 + 지불한 업그레이드비).</summary>
        public int InvestedAmount => Mathf.RoundToInt(_investedAmount);

        /// <summary>회수 시 돌려받는 자원 = 투입 총액 × 환불률 (ADR-0008 확정 기준).</summary>
        public int RefundAmount => Mathf.FloorToInt(_investedAmount * (_def != null ? _def.RefundRatio : 0.5f));

        // --- 업그레이드 (G-05) ---
        // 가변 상태는 이 인스턴스만 갖는다. SO(_def)는 절대 런타임에 수정하지 않는다 (CLAUDE.md §4).
        private int _tierLevel;   // 0 = Lv1. Tiers[_tierLevel] = 다음 단계 정의.

        /// <summary>현재 티어 (0 = Lv1). 이동해도 유지된다.</summary>
        public int TierLevel => _tierLevel;

        /// <summary>다음 단계로 강화 가능한가 (최대 티어 = false).</summary>
        public bool CanUpgrade => _def != null && _tierLevel < _def.Tiers.Count;

        /// <summary>다음 단계 강화 비용. 최대 티어면 0.</summary>
        public int NextUpgradeCost => CanUpgrade ? _def.Tiers[_tierLevel].Cost : 0;

        /// <summary>강화 적용. 자원 차감은 호출자(DeploymentController)가 하고, 여기선 티어 상승 + Attacker 갱신만.</summary>
        public void ApplyUpgrade()
        {
            if (!CanUpgrade) return;

            var tier = _def.Tiers[_tierLevel];
            _investedAmount += tier.Cost;   // 투입 총액 누적 — 회수 환불 기준 (ADR-0008)
            _tierLevel++;

            if (_attacker != null)
            {
                _attacker.Damage = tier.AttackDamage;
                _attacker.Range = tier.AttackRange;
            }

            // 티어를 크기로 구분 (코드 보간 — 파티클/애니메이션 금지). 사거리 원(G-06)은 Attacker.Range 를 따라 크진다.
            transform.localScale = _initialScale * (1f + 0.12f * _tierLevel);
        }

        private void Awake()
        {
            _session = GameSession.Instance;
            _grid = GridSystem.Instance;
            _graph = PathGraph.Instance;

            _health = GetComponent<Health>();
            _attacker = GetComponent<Attacker>();
            _sprite = GetComponent<SpriteRenderer>();
            _marchLine = GetComponent<LineRenderer>();
            _initialScale = transform.localScale;
            if (_marchLine != null) _marchLine.enabled = false;

            // 기본은 미등록. BeginMarch(D-03 on) 또는 Deploy 시 등록.
            if (_health != null) _health.SetRegistryEnabled(false);

            if (_attacker != null) _attacker.Enabled = false;
        }

        private void Start()
        {
            if (_def == null || _session == null || _health == null) return;
            ConfigureCombat();
            _health.Initialize(_def.MaxHealth, Team.Ally);
            _health.OnDied += OnDied;

            // 사격 연출 (G-07) — 발사 이벤트 구독. UI 는 이벤트로만 분리.
            if (_attacker != null)
            {
                _attacker.OnFired += HandleFired;
                var go = new GameObject("ShotLine");
                go.transform.SetParent(transform, false);
                _shotLine = go.AddComponent<UI.ShotLine>();
            }
        }

        /// <summary>발사 순간 연출. 근접(고양이)은 짧은 호, 원거리(쥐)는 직선 — 사거리 3 기준 판정 (G-07 현장 결정).</summary>
        private void HandleFired(IDamageable target)
        {
            if (target == null) return;
            bool melee = _attacker != null && _attacker.Range < 3f;
            float seconds = _stage != null ? _stage.ShotLineSeconds : 0.07f;

            if (melee)
            {
                // 고양이 수인 — 짧은 호. 붙어서 때리는 것이 보여야 한다.
                if (_shotLine != null)
                    _shotLine.Show(transform.position, target, new Color(0.55f, 0.75f, 1f, 0.95f), true, seconds);
            }
            else
            {
                // 쥐 수인은 마법사다 — 직선 대신 매직 미사일이 날아간다.
                // 피해는 여전히 즉시 들어간다 (매직 미사일은 빗나가지 않는 주문이라 설정과도 맞는다).
                float speed = _stage != null ? _stage.MagicMissileSpeed : 8f;
                UI.MagicMissile.Spawn(transform.position, target,
                                      new Color(0.72f, 0.60f, 1f, 0.95f), speed, transform.parent);
            }

            // 효과음 (G-11) — 종족별로 다르게: 고양이=높고 짧게, 쥐=낮고 약간 길게.
            if (_session.Sfx != null)
                _session.Sfx.Play(melee ? Audio.SfxPlayer.SfxId.CatHit : Audio.SfxPlayer.SfxId.RatShot);
        }

        private void OnDisable()
        {
            if (_health != null) _health.OnDied -= OnDied;
            if (_attacker != null) _attacker.OnFired -= HandleFired;
        }

        /// <summary>고용 확정 즉시 호출. village.DepartureNode 에서 slot 까지 걷는다.</summary>
        public void BeginMarch(Village village, UnitDefinition definition, GridCoord slot)
        {
            _def = definition;
            _targetSlot = slot;
            _homeVillage = village;
            _hasWayPoint = false;
            _state = State.Marching;
            _marchStartTime = Time.time;
            _investedAmount = definition.HireCost;   // 투입 총액 기록 — 회수 환불 기준 (G-04). 업그레이드비는 G-05 가 누적.
            _stage = _session.Definition;

            BeginMarchCommon();

            Vector3 slotWorld = _grid.CellToWorld(slot);

            // 막힌 것이 없으면 그래프를 타지 않고 곧장 걸어간다.
            //
            // 경로 그래프는 <b>도로</b>다 — 적과 보호대상의 것이다 (ADR-0004).
            // 아군까지 도로로 우회시키면 마을 바로 옆 칸에 배치하는데도 도로까지 내려갔다
            // 되돌아온다. 실측(2026-08-29): 직선 1.0칸 목적지를 7.7칸 행군, 마을×배치칸
            // 291쌍의 평균 우회율 2.23배, 65%가 2배 넘게 돌아갔다.
            // 아군은 도로에 묶이지 않는다. 막힌 칸만 피하면 된다.
            if (IsStraightWalkClear(transform.position, slotWorld))
            {
                _route.Clear();
                _follower.Clear();
                _onStraightLeg = true;
                UpdateMarchLine();
                return;
            }

            // 여기에 오면 도로나 벽이 가로막은 것이다. 아군은 도로를 건널 수 없으므로 갈 방법이 없다.
            // 도로 그래프로 우회시키지 않는다 — 그게 바로 "길을 건너는" 행위다.
            // DeploymentController 가 CanWalkTo 로 미리 걸러야 하므로 여기까지 오면 배선 문제다.
            Debug.LogError($"[{nameof(AllyUnit)}] {slot} 까지 갈 수 없다 (도로/벽이 가로막음). " +
                           $"DeploymentController 가 먼저 걸렀어야 한다.", this);
            Deploy();
        }

        /// <summary>행군 시작 시 공통 처리 (등록 정책·색).</summary>
        private void BeginMarchCommon()
        {
            // 행군 중 등록 정책 (D-03)
            if (_stage != null && _stage.AlliesCanDieWhileMarching && _health != null)
                _health.SetRegistryEnabled(true);

            if (_sprite != null) _sprite.color = _marchingColor;
        }

        /// <summary>두 지점을 잇는 직선을 아군이 걸어갈 수 있는가.
        ///
        /// <b>아군은 도로를 건널 수 없다</b> (2026-08-29 확정). 도로는 보호대상·모체·적의 통행로이고,
        /// 아군은 자기 쪽 구역 안에서만 움직인다. 그래서 Road 와 Blocked 를 똑같이 막힌 칸으로 본다.
        ///
        /// 격자 탐색(A*)이 아니다 — 선분 위를 샘플링해 막힌 칸만 확인한다 (ADR-0004 준수).
        /// 맵은 "마을에서 자기 담당 사각형 안 어느 칸으로도 직선이 닿는다"를 만족하도록 설계되어 있다
        /// (GreyboxMapData 참조). 그래서 우회 경로 탐색이 애초에 필요 없다.</summary>
        private bool IsStraightWalkClear(Vector3 from, Vector3 to) => CanWalk(from, to);

        /// <summary>아군이 <paramref name="from"/> 에서 <paramref name="to"/> 까지 걸어갈 수 있는가.
        /// 배치·재배치를 받아들일지 판단할 때 DeploymentController 도 이걸 쓴다.</summary>
        public static bool CanWalk(Vector3 from, Vector3 to)
        {
            var grid = GridSystem.Instance;
            if (grid == null) return true;

            float distance = Vector3.Distance(from, to);
            int steps = Mathf.CeilToInt(distance / (grid.CellSize * 0.5f));
            for (int i = 0; i <= steps; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, i / (float)Mathf.Max(steps, 1));
                var cell = grid.GetCell(grid.WorldToCell(p));
                if (cell == CellType.Blocked || cell == CellType.Road) return false;
            }
            return true;
        }

        /// <summary>이 유닛을 고용한 마을. 직선이 막혔을 때 경유점으로 쓴다.</summary>
        public Village HomeVillage => _homeVillage;

        /// <summary>이 위치에서 목적지 칸까지 아군이 갈 수 있는가 (직선 또는 마을 경유).
        /// DeploymentController 가 배치·재배치를 받아들일지 판단할 때 쓴다.</summary>
        public bool CanWalkTo(GridCoord slot)
        {
            Vector3 to = _grid.CellToWorld(slot);
            if (CanWalk(transform.position, to)) return true;
            return _homeVillage != null && CanWalkVia(transform.position, to, _homeVillage.transform.position);
        }

        /// <summary>직선이 막히면 <paramref name="via"/> 를 한 번 거쳐서 갈 수 있는가.
        ///
        /// 격자 탐색을 도입하지 않고 도로를 우회하는 방법이다 (ADR-0004 유지).
        /// 맵이 "마을에서 자기 구역 안 어느 칸으로도 직선이 닿는다"를 보장하므로,
        /// 같은 구역 안의 두 지점은 <b>마을을 한 번 거치면 반드시 이어진다.</b>
        /// 최단은 아니지만 도로를 밟지 않는다 — 직선이 뚫려 있으면 그쪽이 먼저다.</summary>
        public static bool CanWalkVia(Vector3 from, Vector3 to, Vector3 via)
            => CanWalk(from, via) && CanWalk(via, to);

        /// <summary>재배치 (ADR-0008). Deployed·Marching 어느 상태에서든 재명령 가능.
        /// 새 슬롯까지 실제로 걸어간다 (순간이동 금지). 성공하면 이전 슬롯 예약 해제 이벤트를 발행한다.</summary>
        public bool BeginRedeploy(GridCoord newSlot)
        {
            if (_state == State.Queued)
            {
                Debug.LogError($"[{nameof(AllyUnit)}] 아직 행군을 시작하지 않은 유닛은 재배치할 수 없다.", this);
                return false;
            }

            // 이전 슬롯 예약 해제 — DeploymentController 가 구독해 지운다. (이동·사망 공통 경로)
            OnLeftSlot?.Invoke(this, _targetSlot);

            // 이동 쿨다운 (ADR-0008 B안) — 명령 시점부터 다음 명령까지.
            _nextRedeployTime = Time.time + (_def != null ? _def.RedeployCooldown : 3f);

            _targetSlot = newSlot;
            _state = State.Marching;
            _marchStartTime = Time.time;

            if (_attacker != null) _attacker.Enabled = false;   // 행군 중 사격 금지 (GDD §8) — 이동의 실질적 비용

            // 행군 중 등록 정책 (D-03) — Deployed 동안 켜져 있던 등록을 행군 규칙으로 되돌린다.
            if (_health != null)
                _health.SetRegistryEnabled(_stage != null && _stage.AlliesCanDieWhileMarching);
            if (_sprite != null) _sprite.color = _marchingColor;

            Vector3 newSlotWorld = _grid.CellToWorld(newSlot);
            _route.Clear();
            _follower.Clear();
            _onStraightLeg = true;

            // 1) 곧장 갈 수 있으면 최단으로 간다.
            if (CanWalk(transform.position, newSlotWorld))
            {
                _hasWayPoint = false;
                UpdateMarchLine();
                return true;
            }

            // 2) 도로가 가로막으면 <b>도로를 피해서</b> 마을을 한 번 거쳐 돌아간다.
            //    격자 탐색을 쓰지 않는다 — 맵이 "마을 ↔ 자기 구역 전 칸" 직선을 보장하므로 이 한 번이면 된다.
            if (_homeVillage != null && CanWalkVia(transform.position, newSlotWorld, _homeVillage.transform.position))
            {
                _wayPoint = _homeVillage.transform.position;
                _hasWayPoint = true;
                UpdateMarchLine();
                return true;
            }

            // 도로/벽이 가로막았다 = 아군이 갈 수 없는 칸이다. 우회시키지 않고 거절한다.
            Debug.LogError($"[{nameof(AllyUnit)}] {newSlot} 로 재배치할 수 없다 (도로/벽이 가로막음). " +
                           $"DeploymentController 가 먼저 걸렀어야 한다.", this);
            Deploy();
            return false;
        }

        /// <summary>회수 (G-04). 행군 중에도 가능. 슬롯 해제 + 레지스트리 해제 + 환불 이벤트 발행 후
        /// 후퇴 연출(축소+페이드 — 격파와 다른 톤, GDD §8 "사망이 아니라 후퇴/탈진") 뒤 사라진다.</summary>
        public void Retire()
        {
            if (_retiring) return;
            _retiring = true;

            OnLeftSlot?.Invoke(this, _targetSlot);   // 슬롯 예약 해제 (행군 중이면 목표 슬롯)
            if (_health != null) _health.SetRegistryEnabled(false);   // TargetRegistry 해제
            if (_attacker != null) _attacker.Enabled = false;
            if (_marchLine != null) _marchLine.enabled = false;

            OnRetired?.Invoke(this, RefundAmount);
            _retireTimer = RetireFadeDuration;
        }

        private void ConfigureCombat()
        {
            if (_attacker == null) return;
            _attacker.Range = _def.AttackRange;
            _attacker.Damage = _def.AttackDamage;
            _attacker.Interval = _def.AttackInterval;
            _attacker.TargetTeam = Team.Enemy;
        }

        private void Update()
        {
            // 후퇴 연출은 배속·승패와 무관하게 진행된다 (unscaled).
            if (_retiring)
            {
                _retireTimer -= Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(_retireTimer / RetireFadeDuration);
                if (_sprite != null)
                {
                    var c = _sprite.color;
                    c.a = k;
                    _sprite.color = c;
                }
                transform.localScale = _initialScale * k;   // 축소 = 후퇴/탈진 (사망이 아니다, GDD §8)
                if (_retireTimer <= 0f) Destroy(gameObject);
                return;
            }

            if (_session.Result != GameResult.InProgress)
                return;   // 행군 중에 게임이 끝나면 그대로 멈춘다.

            switch (_state)
            {
                case State.Marching:
                    MarchStep();
                    break;
                case State.Deployed:
                    if (_attacker != null) _attacker.Anchor = EscortAnchor();
                    break;
            }
        }

        private Vector3 EscortAnchor()
        {
            var escortee = _session.Escortee;
            return escortee != null ? escortee.transform.position : transform.position;
        }

        private void MarchStep()
        {
            float step = _def.MoveSpeed * Time.deltaTime;

            // 경유점이 있으면 거기 먼저 들른다 (도로를 우회하는 구간).
            if (_hasWayPoint)
            {
                transform.position = Vector3.MoveTowards(transform.position, _wayPoint, step);
                UpdateMarchLine();
                if ((transform.position - _wayPoint).sqrMagnitude < 0.0001f) _hasWayPoint = false;
                return;
            }

            Vector3 target = _grid.CellToWorld(_targetSlot);
            transform.position = Vector3.MoveTowards(transform.position, target, step);
            UpdateMarchLine();   // 두세 점짜리 선이라 매 프레임 갱신해도 싸다
            if ((transform.position - target).sqrMagnitude < 0.0001f)
                Deploy();
        }

        private void Deploy()
        {
            _state = State.Deployed;

            // 슬롯 중심에 정확히 스냅.
            transform.position = _grid.CellToWorld(_targetSlot);

            if (_marchLine != null) _marchLine.enabled = false;
            if (_sprite != null) _sprite.color = _deployedColor;

            // 상태 전환 시점에 정확히 한 번만 바꾼다 (Update 에서 매 프레임 대입 금지).
            if (_attacker != null) _attacker.Enabled = true;

            // 배치 시점에 등록 (D-03 off 기본).
            if (_health != null) _health.SetRegistryEnabled(true);

            // 배치 완료 확인음 (G-11).
            if (_session != null && _session.Sfx != null)
                _session.Sfx.Play(Audio.SfxPlayer.SfxId.DeployDone);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Diagnostics.DebugOverlay.NotifyAllyDeployed(Time.time - _marchStartTime);
#endif
        }

        private void OnDied(Health health)
        {
            if (_retiring) return;   // 회수 연출 중 사망 처리 중복 방지

            if (_state == State.Marching &&
                (_stage == null || _stage.AlliesCanDieWhileMarching))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Diagnostics.DebugOverlay.NotifyAllyLostWhileMarching();
#endif
            }

            // 죽은 자리의 슬롯 예약을 푼다 (이동·사망 공통 해제 경로).
            if (_state == State.Deployed)
                OnLeftSlot?.Invoke(this, _targetSlot);

            Destroy(gameObject);
        }

        /// <summary>행군 경로를 선으로 그린다 — "얼마나 걸리는지"가 보여야 트레이드오프가 성립.</summary>
        private void UpdateMarchLine()
        {
            if (_marchLine == null) return;
            if (_state != State.Marching) { _marchLine.enabled = false; return; }

            Vector3 target = _grid.CellToWorld(_targetSlot);
            if (_hasWayPoint)
            {
                // 우회 구간이 보이게 꺾인 선으로 그린다.
                _marchLine.positionCount = 3;
                _marchLine.SetPosition(0, transform.position);
                _marchLine.SetPosition(1, _wayPoint);
                _marchLine.SetPosition(2, target);
            }
            else
            {
                _marchLine.positionCount = 2;
                _marchLine.SetPosition(0, transform.position);
                _marchLine.SetPosition(1, target);
            }
            _marchLine.enabled = true;
        }
    }
}
