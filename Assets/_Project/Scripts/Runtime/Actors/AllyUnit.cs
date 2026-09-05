using System.Collections.Generic;
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

        private GameSession _session;
        private GridSystem _grid;
        private UnitDefinition _def;
        private StageDefinition _stage;
        private Health _health;
        private Attacker _attacker;
        private SpriteRenderer _sprite;
        private LineRenderer _marchLine;
        private UI.ShotLine _shotLine;

        private GridCoord _targetSlot;
        private Village _homeVillage;
        private readonly List<Vector3> _walkPath = new List<Vector3>();   // 도로·장애물을 피해 도는 경유점들
        private int _walkIndex;
        private float _marchStartTime;

        private Vector3 _slotPosition;   // 배치 슬롯의 월드 좌표. 근접 병종은 여기를 중심으로 오간다 (G-22).
        private System.Func<IDamageable, bool> _meleeReachFilter;   // 매 프레임 할당을 피하려고 캐시

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

        /// <summary>배치 슬롯의 월드 좌표 = <b>사거리 원의 중심</b> (G-22).
        /// 근접 병종은 적에게 붙으러 몸이 슬롯을 떠나므로, 사거리 원을
        /// <c>transform.position</c> 에 그리면 원이 같이 흔들린다. 표시도 판정도 이 값을 쓴다.
        /// 배치 전(행군 중)에는 아직 도착하지 않았으므로 현재 위치를 준다.</summary>
        public Vector3 RangeCenter => _state == State.Deployed ? _slotPosition : transform.position;

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

        // --- 업그레이드 (G-05, ADR-0020) ---
        // 가변 상태는 이 인스턴스만 갖는다. SO(_def)는 절대 런타임에 수정하지 않는다 (CLAUDE.md §4).
        private int _level = 1;   // Lv1 부터. 티어를 사면 그 티어의 도달 레벨이 된다.
        private int _branch;      // 0 = 아직 분기를 고르지 않음. 한 번 정해지면 되돌릴 수 없다.
        private readonly List<int> _upgradeOptions = new List<int>();   // 지금 살 수 있는 티어 인덱스들

        // 누적된 효과. 티어의 값이 0/false 면 유지되므로, 상위 티어에 하위 효과를 다시 적지 않아도 된다.
        private float _scavengeBonus;
        private float _poisonDamagePerSecond;
        private float _poisonDuration;
        private float _poisonTickInterval;
        private bool _poisonBlocksHealing;
        private float _executeThreshold;
        private float _bossCritMultiplier = 1f;
        private float _splashRadius;
        private float _splashDamageRatio;
        private bool _prefersFarthestTarget;

        /// <summary>현재 레벨 (Lv1 = 1). 이동해도 유지된다.</summary>
        public int Level => _level;

        /// <summary>현재 티어 (0 = Lv1). <see cref="Level"/> - 1 과 같다 — 기존 표시 코드 호환용.</summary>
        public int TierLevel => _level - 1;

        /// <summary>확정된 분기 (0 = 아직 안 고름 · 1 = A · 2 = B). 되돌릴 수 없다 (2026-09-02 확정).</summary>
        public int Branch => _branch;

        /// <summary>지금 살 수 있는 티어들의 인덱스. Lv2→Lv3 에서만 2개가 되고 그 외에는 0 또는 1개다.</summary>
        public IReadOnlyList<int> UpgradeOptions => _upgradeOptions;

        /// <summary>강화할 것이 남아 있는가.</summary>
        public bool CanUpgrade => _upgradeOptions.Count > 0;

        /// <summary>다음 단계 강화 비용 (첫 후보 기준). 분기 중이면 <see cref="UpgradeOptionCost"/> 를 써라.</summary>
        public int NextUpgradeCost => _upgradeOptions.Count > 0 ? _def.Tiers[_upgradeOptions[0]].Cost : 0;

        /// <summary><paramref name="option"/> 번째 후보의 비용.</summary>
        public int UpgradeOptionCost(int option)
        {
            if (option < 0 || option >= _upgradeOptions.Count) return 0;
            return _def.Tiers[_upgradeOptions[option]].Cost;
        }

        /// <summary><paramref name="option"/> 번째 후보의 이름. SO 에 이름이 없으면 "Lv{n} 강화".</summary>
        public string UpgradeOptionName(int option)
        {
            if (option < 0 || option >= _upgradeOptions.Count) return string.Empty;
            int index = _upgradeOptions[option];
            string name = _def.Tiers[index].DisplayName;
            return string.IsNullOrEmpty(name) ? $"Lv{_def.LevelOf(index)} 강화" : name;
        }

        /// <summary>후보 목록을 다시 계산한다. 강화 직후와 정의를 받은 직후에 부른다.</summary>
        private void RefreshUpgradeOptions()
        {
            if (_def == null) { _upgradeOptions.Clear(); return; }
            _def.CollectNextTiers(_level, _branch, _upgradeOptions);
        }

        /// <summary>첫 후보로 강화한다. 분기가 없는 병종(쥐 수인)과 기존 호출부를 위한 편의 오버로드.</summary>
        public void ApplyUpgrade() => ApplyUpgrade(0);

        /// <summary><paramref name="option"/> 번째 후보로 강화한다.
        /// 자원 차감은 호출자(DeploymentController)가 하고, 여기선 레벨·분기 확정과 능력 반영만 한다.</summary>
        public void ApplyUpgrade(int option)
        {
            if (option < 0 || option >= _upgradeOptions.Count) return;

            int index = _upgradeOptions[option];
            var tier = _def.Tiers[index];

            _investedAmount += tier.Cost;   // 투입 총액 누적 — 회수 환불 기준 (ADR-0008)
            _level = _def.LevelOf(index);

            // 분기 확정. 되돌리는 경로는 일부러 만들지 않는다 — 확정 결정 17 "선택에 책임을 진다".
            if (tier.Branch != 0) _branch = tier.Branch;

            AccumulateTier(tier);
            PushCombatUpgrades();
            RefreshScavenge();
            RefreshUpgradeOptions();

            // 레벨을 크기로 구분 (코드 보간 — 파티클/애니메이션 금지). 사거리 원(G-06)은 Attacker.Range 를 따라 크진다.
            transform.localScale = _initialScale * (1f + 0.12f * (_level - 1));
        }

        /// <summary>티어의 값을 누적 상태에 반영한다. <b>0 / false 는 "유지"</b>다 —
        /// 그래서 Lv3 티어에 Lv2 에서 얻은 손기술 값을 다시 적지 않아도 효과가 살아 있다.</summary>
        private void AccumulateTier(UpgradeTier tier)
        {
            if (tier.AttackDamage > 0f) _attackDamageOverride = tier.AttackDamage;
            if (tier.AttackRange > 0f) _attackRangeOverride = tier.AttackRange;

            if (tier.ScavengeBonus > 0f) _scavengeBonus = tier.ScavengeBonus;
            if (tier.PoisonDamagePerSecond > 0f) _poisonDamagePerSecond = tier.PoisonDamagePerSecond;
            if (tier.PoisonDuration > 0f) _poisonDuration = tier.PoisonDuration;
            if (tier.PoisonTickInterval > 0f) _poisonTickInterval = tier.PoisonTickInterval;
            if (tier.PoisonBlocksHealing) _poisonBlocksHealing = true;
            if (tier.ExecuteThreshold > 0f) _executeThreshold = tier.ExecuteThreshold;
            if (tier.BossCritMultiplier > 0f) _bossCritMultiplier = tier.BossCritMultiplier;

            if (tier.SplashRadius > 0f) _splashRadius = tier.SplashRadius;
            if (tier.SplashDamageRatio > 0f) _splashDamageRatio = tier.SplashDamageRatio;
            if (tier.PrefersFarthestTarget) _prefersFarthestTarget = true;
        }

        /// <summary>누적된 능력을 Attacker 에 밀어 넣는다. <see cref="ConfigureCombat"/> 도 이걸 부르므로
        /// 재배치·재설정 후에도 강화 효과가 살아남는다.</summary>
        private void PushCombatUpgrades()
        {
            if (_attacker == null) return;

            if (_attackDamageOverride > 0f) _attacker.Damage = _attackDamageOverride;
            if (_attackRangeOverride > 0f) _attacker.Range = _attackRangeOverride;

            _attacker.PoisonDamagePerSecond = _poisonDamagePerSecond;
            _attacker.PoisonDuration = _poisonDuration;
            _attacker.PoisonTickInterval = _poisonTickInterval;
            _attacker.PoisonBlocksHealing = _poisonBlocksHealing;

            // 독을 부여하는 유닛만 "중독 안 된 적 먼저" 를 쓴다 (독단검 Lv3-1 / Lv4-1).
            _attacker.PrefersUnpoisonedTarget = _poisonDamagePerSecond > 0f;

            _attacker.ExecuteThreshold = _executeThreshold;
            _attacker.BossCritMultiplier = _bossCritMultiplier;

            _attacker.SplashRadius = _splashRadius;
            _attacker.SplashDamageRatio = _splashDamageRatio;
            _attacker.PrefersFarthestTarget = _prefersFarthestTarget;
        }

        /// <summary>손기술(Lv2) 보상 반경을 등록/갱신한다. 중심은 몸이 아니라 배치 슬롯이다 (G-22).
        /// 배치 전이거나 손기술이 없으면 등록을 지운다.</summary>
        private void RefreshScavenge()
        {
            if (_scavengeBonus <= 0f || _state != State.Deployed || _attacker == null)
            {
                ScavengeRegistry.Unregister(this);
                return;
            }
            ScavengeRegistry.Register(this, _attacker.RangeOrigin, _attacker.Range, _scavengeBonus);
        }

        private float _attackDamageOverride;   // 0 = 아직 강화 없음 → SO 기본값을 쓴다
        private float _attackRangeOverride;

        private void Awake()
        {
            _session = GameSession.Instance;
            _grid = GridSystem.Instance;

            _health = GetComponent<Health>();
            _attacker = GetComponent<Attacker>();
            _sprite = GetComponent<SpriteRenderer>();
            _marchLine = GetComponent<LineRenderer>();
            _initialScale = transform.localScale;
            _meleeReachFilter = CanMeleeReach;
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
                // 범위 공격(메테오)이면 착탄 지점에서 스플래시 반경만큼 터진다 (ADR-0021).
                // 반경을 그대로 넘기므로 그림과 실제 피해 범위가 일치한다.
                UI.MagicMissile.Spawn(transform.position, target,
                                      new Color(0.72f, 0.60f, 1f, 0.95f), speed, transform.parent,
                                      _attacker != null ? _attacker.SplashRadius : 0f);
            }

            // 효과음 (G-11) — 종족별로 다르게: 고양이=높고 짧게, 쥐=낮고 약간 길게.
            if (_session.Sfx != null)
                _session.Sfx.Play(melee ? Audio.SfxPlayer.SfxId.CatHit : Audio.SfxPlayer.SfxId.RatShot);
        }

        private void OnDisable()
        {
            if (_health != null) _health.OnDied -= OnDied;
            if (_attacker != null) _attacker.OnFired -= HandleFired;

            // static 레지스트리에 유령이 남지 않게 한다 (CLAUDE.md §3 — 도메인 리로드가 꺼져 있다).
            ScavengeRegistry.Unregister(this);
        }

        /// <summary>고용 확정 즉시 호출. 마을에서 slot 까지 도로·장애물을 피해 걷는다.</summary>
        public void BeginMarch(Village village, UnitDefinition definition, GridCoord slot)
        {
            _def = definition;
            _targetSlot = slot;
            _homeVillage = village;
            _state = State.Marching;
            _marchStartTime = Time.time;
            _investedAmount = definition.HireCost;   // 투입 총액 기록 — 회수 환불 기준 (G-04). 업그레이드비는 G-05 가 누적.
            _stage = _session.Definition;
            RefreshUpgradeOptions();   // 정의를 받은 지금이 강화 후보를 처음 계산할 수 있는 시점이다 (ADR-0020)

            BeginMarchCommon();

            if (SetWalkPath(_grid.CellToWorld(slot))) return;

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

            // 슬롯을 떠났으므로 손기술 보상 반경도 같이 걷힌다 (ADR-0020).
            // 도착해서 자리를 잡아야 효과가 산다 — 걸어가는 동안 판이 유리해지지 않는다.
            ScavengeRegistry.Unregister(this);
        }

        /// <summary>두 지점을 잇는 직선을 아군이 걸어갈 수 있는가.
        ///
        /// <b>아군은 도로를 건널 수 없다</b> (2026-08-29 확정). 도로는 보호대상·모체·적의 통행로이고,
        /// 아군은 자기 쪽 구역 안에서만 움직인다. 그래서 Road 와 Blocked 를 똑같이 막힌 칸으로 본다.
        ///
        /// 격자 탐색(A*)이 아니다 — 선분 위를 샘플링해 막힌 칸만 확인한다 (ADR-0004 준수).
        /// 맵은 "마을에서 자기 담당 사각형 안 어느 칸으로도 직선이 닿는다"를 만족하도록 설계되어 있다
        /// (GreyboxMapData 참조). 그래서 우회 경로 탐색이 애초에 필요 없다.</summary>
        /// <summary>목적지까지 걸어갈 경로를 잡는다. 직선이 뚫려 있으면 그 한 구간,
        /// 막혀 있으면 도로·장애물의 모서리를 도는 경유점들이 들어온다. 못 가면 false.</summary>
        private bool SetWalkPath(Vector3 target)
        {
            _walkIndex = 0;

            if (!AllyWalkGraph.TryFindPath(transform.position, target, _walkPath))
            {
                _walkPath.Clear();
                return false;
            }

            UpdateMarchLine();
            return true;
        }


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
                var coord = grid.WorldToCell(p);
                // 장애물 2종(벽·물)은 이동에 대해서는 똑같이 막힌 칸이다 (G-22).
                if (!grid.IsWalkable(coord) || grid.GetCell(coord) == CellType.Road) return false;
            }
            return true;
        }

        /// <summary>이 유닛을 고용한 마을.</summary>
        public Village HomeVillage => _homeVillage;

        /// <summary>이 위치에서 목적지 칸까지 아군이 갈 수 있는가.
        /// 직선이 막혀도 도로·장애물을 <b>돌아서</b> 갈 수 있으면 갈 수 있는 것이다.
        /// DeploymentController 가 배치·재배치를 받아들일지 판단할 때 쓴다.</summary>
        public bool CanWalkTo(GridCoord slot) => CanReach(transform.position, _grid.CellToWorld(slot));

        /// <summary>도로·장애물을 피해 갈 수 있는가 (우회 포함).</summary>
        public static bool CanReach(Vector3 from, Vector3 to)
        {
            if (CanWalk(from, to)) return true;
            var scratch = new List<Vector3>();
            return AllyWalkGraph.TryFindPath(from, to, scratch);
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

            if (_attacker != null)
            {
                _attacker.Enabled = false;        // 행군 중 사격 금지 (GDD §8) — 이동의 실질적 비용
                _attacker.ClearRangeOrigin();     // 옛 슬롯에 고정돼 있던 사거리 중심을 푼다 (G-22)
            }

            // 행군 중 등록 정책 (D-03) — Deployed 동안 켜져 있던 등록을 행군 규칙으로 되돌린다.
            if (_health != null)
                _health.SetRegistryEnabled(_stage != null && _stage.AlliesCanDieWhileMarching);
            if (_sprite != null) _sprite.color = _marchingColor;

            // 슬롯을 떠났으므로 손기술 보상 반경도 걷는다 (ADR-0020).
            // ⚠️ 이 메서드는 BeginMarchCommon 을 거치지 않고 행군 준비를 직접 인라인한다 —
            //    거기에만 넣으면 재배치 경로에서 옛 자리의 반경이 그대로 남는다 (실측으로 잡은 버그).
            ScavengeRegistry.Unregister(this);

            if (SetWalkPath(_grid.CellToWorld(newSlot))) return true;

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
            ScavengeRegistry.Unregister(this);       // 후퇴 연출이 도는 동안 보상 반경이 남으면 안 된다
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

            // 장애물 2종 규칙 (G-22, ADR-0017). 아군만 켠다 — 적에게 켜면 밸런스가 통째로 바뀐다.
            // 근접은 걸어가 붙어야 하므로 물도 넘지 못하고, 원거리는 물 위로 그냥 쏜다.
            _attacker.Sight = _def.IsMelee
                ? Attacker.SightMode.BlockedByWallsAndWater
                : Attacker.SightMode.BlockedByWalls;

            // SO 기본값으로 되돌린 뒤 강화분을 다시 얹는다 (ADR-0020).
            // 순서가 뒤집히면 재배치할 때마다 업그레이드가 조용히 날아간다.
            PushCombatUpgrades();
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
                    if (_def != null && _def.IsMelee) MeleeStep();
                    break;
            }
        }

        private Vector3 EscortAnchor()
        {
            var escortee = _session.Escortee;
            return escortee != null ? escortee.transform.position : transform.position;
        }

        /// <summary>근접 병종의 교전 이동 (G-22, ADR-0017).
        ///
        /// 배치된 자리에 못박혀 있던 GDD §8 을 근접 병종에 한해 완화한다. 다만 <b>자유 추격이 아니다</b> —
        /// 움직일 수 있는 범위는 <b>사거리 원 안</b>으로 못박혀 있고(원의 중심은 슬롯,
        /// <see cref="Attacker.SetRangeOrigin"/>), 원을 벗어나지 않으므로 실효 사거리는 늘지 않는다.
        /// 슬롯 예약도 그대로 들고 있어 다른 유닛이 그 칸에 들어오지 않는다.
        ///
        /// <b>도로는 여전히 밟지 않는다</b>(2026-08-29 확정 규칙 2). 적은 항상 도로 위를 걷기 때문에
        /// 실제로는 "도로 경계까지 바짝 붙어서 때린다"가 된다 — 그게 이 게임에서 근접의 그림이다.
        /// 물도 마찬가지로 걸어 들어갈 수 없어 그 앞에서 멈춘다.
        ///
        /// 매 프레임 도는 계산이지만 반경이 사거리(고양이 1.6~2.0셀)뿐이라 샘플이 10개 미만이다.</summary>
        private void MeleeStep()
        {
            if (_attacker == null) return;

            Vector3 destination = _slotPosition;   // 때릴 것이 없으면 자기 자리로 돌아간다

            var target = TargetRegistry.FindNearestTo(_slotPosition, _attacker.Range,
                                                      Team.Enemy, EscortAnchor(), _meleeReachFilter)
                         ?? TargetRegistry.FindNearest(_slotPosition, _attacker.Range,
                                                       Team.Enemy, _meleeReachFilter);
            if (target != null) destination = MeleeStandPoint(target.Position);

            float step = _def.MoveSpeed * Time.deltaTime;

            // 목적지가 크게 바뀌어 현재 위치에서 직선이 막혔으면 슬롯을 경유해 되돌아간다.
            // 이동 가능 범위가 사거리 원 하나뿐이라 이 폴백이면 충분하다 (경로 탐색을 붙이지 않는다).
            if (!CanWalk(transform.position, destination)) destination = _slotPosition;

            transform.position = Vector3.MoveTowards(transform.position, destination, step);
        }

        /// <summary>적에게 다가갈 수 있는 가장 가까운 지점. 슬롯에서 적 쪽으로 직선을 뻗되
        /// ① 사거리 원 밖으로 나가지 않고 ② 도로·장애물 직전에서 멈추고 ③ 적과 접촉 거리는 남긴다.</summary>
        private Vector3 MeleeStandPoint(Vector3 targetPosition)
        {
            Vector3 delta = targetPosition - _slotPosition;
            float distance = delta.magnitude;
            if (distance <= Mathf.Epsilon) return _slotPosition;

            Vector3 direction = delta / distance;
            float maxAdvance = Mathf.Min(_attacker.Range, distance - _def.MeleeContactDistance);
            if (maxAdvance <= 0f) return _slotPosition;

            // 밟을 수 있는 마지막 지점까지 전진한다. 셀의 1/4 씩 훑는다 — 사거리가 2셀 남짓이라 8스텝 정도다.
            float probe = _grid.CellSize * 0.25f;
            float reached = 0f;
            for (float d = probe; d <= maxAdvance; d += probe)
            {
                if (!CanWalk(_slotPosition, _slotPosition + direction * d)) break;
                reached = d;
            }
            return _slotPosition + direction * reached;
        }

        /// <summary>근접 병종이 <b>실제로 붙을 수 있는</b> 적인가.
        /// 사거리 안이어도 벽·물 너머면 걸어갈 수가 없으니 후보에서 뺀다 (G-22).
        /// 매 프레임 호출되므로 델리게이트를 필드에 캐시해 쓴다 (람다를 그 자리에서 만들지 않는다).</summary>
        private bool CanMeleeReach(IDamageable target)
            => _attacker != null && _attacker.CanSee(target.Position);

        private void MarchStep()
        {
            float step = _def.MoveSpeed * Time.deltaTime;

            // 경유점을 순서대로 지난다. 칸에 묶이지 않고 모서리를 스치듯 돈다.
            while (step > 0f && _walkIndex < _walkPath.Count)
            {
                Vector3 target = _walkPath[_walkIndex];
                float remaining = Vector3.Distance(transform.position, target);
                if (remaining > step)
                {
                    transform.position = Vector3.MoveTowards(transform.position, target, step);
                    step = 0f;
                    break;
                }

                transform.position = target;
                step -= remaining;
                _walkIndex++;
            }

            UpdateMarchLine();

            if (_walkIndex >= _walkPath.Count) Deploy();
        }

        private void Deploy()
        {
            _state = State.Deployed;

            // 슬롯 중심에 정확히 스냅.
            _slotPosition = _grid.CellToWorld(_targetSlot);
            transform.position = _slotPosition;

            if (_marchLine != null) _marchLine.enabled = false;
            if (_sprite != null) _sprite.color = _deployedColor;

            // 상태 전환 시점에 정확히 한 번만 바꾼다 (Update 에서 매 프레임 대입 금지).
            if (_attacker != null)
            {
                _attacker.Enabled = true;
                // 사거리 원의 중심을 슬롯에 못박는다 (G-22). 근접 병종은 이 원 안에서 몸만 움직인다 —
                // 중심을 몸에 두면 앞으로 나간 만큼 실효 사거리가 늘어나 밸런스가 조용히 무너진다.
                _attacker.SetRangeOrigin(_slotPosition);
            }

            // 손기술(Lv2) 보상 반경은 슬롯 기준이므로 여기서 등록/갱신한다 (ADR-0020).
            RefreshScavenge();

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
            if (_state != State.Marching || _walkIndex >= _walkPath.Count)
            {
                _marchLine.enabled = false;
                return;
            }

            // 현재 위치 + 남은 경유점들 — 우회 구간이 그대로 보인다.
            int remaining = _walkPath.Count - _walkIndex;
            _marchLine.positionCount = remaining + 1;
            _marchLine.SetPosition(0, transform.position);
            for (int i = 0; i < remaining; i++)
                _marchLine.SetPosition(i + 1, _walkPath[_walkIndex + i]);
            _marchLine.enabled = true;
        }
    }
}
