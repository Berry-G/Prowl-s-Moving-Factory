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
            if (_shotLine == null || target == null) return;
            bool melee = _attacker != null && _attacker.Range < 3f;
            float seconds = _session.Definition != null ? _session.Definition.ShotLineSeconds : 0.07f;
            _shotLine.Show(transform.position, target, new Color(0.55f, 0.75f, 1f, 0.95f), melee, seconds);

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

            var goalNode = _graph.FindNearestNode(slotWorld, PathAgent.Ally);
            if (goalNode == null)
            {
                Debug.LogError($"[{nameof(AllyUnit)}] 목적지 근처 노드 없음", this);
                Deploy();
                return;
            }

            var fromNode = village.DepartureNode ?? _graph.FindNearestNode(transform.position, PathAgent.Ally);
            if (fromNode == null || !_graph.TryFindRoute(fromNode, goalNode, PathAgent.Ally, _route))
            {
                Debug.LogError($"[{nameof(AllyUnit)}] 행군 경로 탐색 실패: {fromNode} -> {goalNode}", this);
                Deploy();
                return;
            }

            _onStraightLeg = false;
            _follower.SetRoute(_route, transform.position);
            UpdateMarchLine();
        }

        /// <summary>행군 시작 시 공통 처리 (등록 정책·색).</summary>
        private void BeginMarchCommon()
        {
            // 행군 중 등록 정책 (D-03)
            if (_stage != null && _stage.AlliesCanDieWhileMarching && _health != null)
                _health.SetRegistryEnabled(true);

            if (_sprite != null) _sprite.color = _marchingColor;
        }

        /// <summary>두 지점을 잇는 직선이 Blocked 칸을 지나지 않는가.
        /// 격자 탐색(A*)이 아니다 — 선분 위를 샘플링해 막힌 칸만 확인한다 (ADR-0004 준수).</summary>
        private bool IsStraightWalkClear(Vector3 from, Vector3 to)
        {
            float distance = Vector3.Distance(from, to);
            int steps = Mathf.CeilToInt(distance / (_grid.CellSize * 0.5f));
            for (int i = 0; i <= steps; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, i / (float)Mathf.Max(steps, 1));
                if (_grid.GetCell(_grid.WorldToCell(p)) == CellType.Blocked) return false;
            }
            return true;
        }

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

            // 배치 때와 같은 규칙 — 막힌 것이 없으면 도로로 우회하지 않고 곧장 걸어간다.
            if (IsStraightWalkClear(transform.position, newSlotWorld))
            {
                _route.Clear();
                _follower.Clear();
                _onStraightLeg = true;
                UpdateMarchLine();
                return true;
            }

            var goalNode = _graph.FindNearestNode(newSlotWorld, PathAgent.Ally);
            if (goalNode == null)
            {
                Debug.LogError($"[{nameof(AllyUnit)}] 재배치 목적지 근처 노드 없음: {newSlot}", this);
                Deploy();
                return false;
            }

            // 재배치는 마을 출발이 아니라 "현재 위치"에서 다시 잡는다.
            // 경로 첫 노드를 현재 위치에서 가장 가까운 노드로 — PathFollower.SetRoute 의 route[0] 근접 규약.
            var fromNode = _graph.FindNearestNode(transform.position, PathAgent.Ally);
            if (fromNode == null || !_graph.TryFindRoute(fromNode, goalNode, PathAgent.Ally, _route))
            {
                Debug.LogError($"[{nameof(AllyUnit)}] 재배치 경로 탐색 실패: {fromNode} -> {goalNode}", this);
                Deploy();
                return false;
            }

            _onStraightLeg = false;
            _follower.SetRoute(_route, transform.position);
            UpdateMarchLine();
            return true;
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

            if (!_onStraightLeg)
            {
                bool passed = _follower.Advance(step);
                transform.position = _follower.Position;
                if (passed) UpdateMarchLine();

                if (_follower.IsFinished)
                {
                    _onStraightLeg = true;
                    // 마지막 직선 구간. Blocked 는 BeginMarch 의 직선 판정에서 이미 걸러졌고,
                    // 그래프를 탄 경우에도 목적지 노드는 슬롯 근처라 짧다.
                }
            }
            else
            {
                Vector3 target = _grid.CellToWorld(_targetSlot);
                transform.position = Vector3.MoveTowards(transform.position, target, step);
                UpdateMarchLine();   // 두 점짜리 선이라 매 프레임 갱신해도 싸다
                if ((transform.position - target).sqrMagnitude < 0.0001f)
                    Deploy();
            }
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

            // 직선 구간(도로를 타지 않는 행군 / 마지막 다가서기)은 목적지까지 한 줄로 긋는다.
            if (_onStraightLeg)
            {
                if (_state != State.Marching) { _marchLine.enabled = false; return; }
                _marchLine.positionCount = 2;
                _marchLine.SetPosition(0, transform.position);
                _marchLine.SetPosition(1, _grid.CellToWorld(_targetSlot));
                _marchLine.enabled = true;
                return;
            }

            if (_follower.IsFinished) { _marchLine.enabled = false; return; }

            _follower.FillRemainingNodes(_lineBuffer);
            int count = 1 + _lineBuffer.Count;
            _marchLine.positionCount = count;
            _marchLine.SetPosition(0, transform.position);
            for (int i = 0; i < _lineBuffer.Count; i++)
                _marchLine.SetPosition(i + 1, _lineBuffer[i].WorldPosition);
            _marchLine.enabled = true;
        }
    }
}
