using System;
using System.Collections.Generic;
using UnityEngine;

namespace PMF.Combat
{
    /// <summary>
    /// 공격 능력. AllyUnit과 Enemy가 공유. 투사체 없음 — 히트스캔(즉시 데미지).
    /// 타겟 선택: FindNearestTo(<see cref="RangeOrigin"/>, Range, TargetTeam, Anchor). Anchor는 소유자가 갱신.
    /// 사거리 원의 중심은 자기 위치가 아니라 <see cref="RangeOrigin"/> 이다 — 근접 병종은 원 안에서 움직인다 (G-22).
    /// </summary>
    public sealed class Attacker : MonoBehaviour
    {
        /// <summary>사거리 안의 대상을 <b>장애물이 가로막는가</b> (G-22).
        /// 장애물은 2종이고 이동은 둘 다 막지만 공격 판정은 다르다 — <see cref="Grid.CellType"/> 참조.</summary>
        public enum SightMode
        {
            /// <summary>아무것도 가리지 않는다. 적(로봇)의 기본값 — 밸런스를 건드리지 않으려고 그대로 둔다.</summary>
            Ignore = 0,
            /// <summary>벽 너머는 못 때린다. 물 너머는 쏜다 — 원거리 병종(쥐 마법사).</summary>
            BlockedByWalls = 1,
            /// <summary>벽도 물도 넘어갈 수 없다 — 근접 병종(고양이). 걸어가 붙어야 때리기 때문이다.</summary>
            BlockedByWallsAndWater = 2,
        }

        [SerializeField] private float _range = 1f;
        [SerializeField] private float _damage = 5f;
        [SerializeField] private float _interval = 1f;
        [SerializeField] private Team _targetTeam = Team.Enemy;
        [SerializeField] private bool _enabled = true;
        [Tooltip("장애물에 막힌 대상을 사거리에서 제외할지. 적에게 켜면 밸런스가 바뀐다 — 아군 전용 (G-22).")]
        [SerializeField] private SightMode _sight = SightMode.Ignore;

        private float _cooldown;
        private bool _hasRangeOrigin;
        private Vector3 _rangeOrigin;
        private Func<IDamageable, bool> _sightFilter;        // 매 프레임 할당을 피하려고 캐시한다
        private Func<IDamageable, bool> _unpoisonedFilter;   // 독단검의 "중독 안 된 적 먼저" 판정
        private readonly List<IDamageable> _splashBuffer = new List<IDamageable>();   // 범위 공격 대상 복사본

        public float Range { get => _range; set => _range = value; }
        public float Damage { get => _damage; set => _damage = value; }
        public float Interval { get => _interval; set => _interval = value; }
        public Team TargetTeam { get => _targetTeam; set => _targetTeam = value; }

        /// <summary>장애물이 사거리를 어떻게 깎는가 (G-22).</summary>
        public SightMode Sight { get => _sight; set => _sight = value; }

        /// <summary>이 공격자에게 물이 장애물인가. 근접만 true.</summary>
        public bool WaterBlocks => _sight == SightMode.BlockedByWallsAndWater;

        /// <summary>행군 중에는 false. 상태 전환 시점에 정확히 한 번만 바꾸는 것을 원칙.</summary>
        public bool Enabled { get => _enabled; set => _enabled = value; }

        /// <summary>타겟 우선순위 기준점 (보통 보호대상 위치). 소유 컴포넌트가 갱신한다.</summary>
        public Vector3 Anchor { get; set; }

        /// <summary>사거리 원의 <b>중심</b>. 기본은 자기 위치지만, 근접 병종처럼 몸이 원 안에서 움직이는 경우
        /// <see cref="SetRangeOrigin"/> 로 배치 슬롯에 고정한다.
        /// 고정하지 않으면 유닛이 원 가장자리까지 걸어 나갈 때마다 실효 사거리가 그만큼 늘어난다 (G-22).</summary>
        public Vector3 RangeOrigin => _hasRangeOrigin ? _rangeOrigin : transform.position;

        /// <summary>사거리 원의 중심을 한 지점에 고정한다 (배치 슬롯).</summary>
        public void SetRangeOrigin(Vector3 origin)
        {
            _rangeOrigin = origin;
            _hasRangeOrigin = true;
        }

        /// <summary>고정을 풀어 다시 자기 위치를 중심으로 쓴다 (행군·재배치 중).</summary>
        public void ClearRangeOrigin() => _hasRangeOrigin = false;

        // ---------- 업그레이드가 붙이는 능력 (ADR-0020) ----------
        // SO 가 아니라 런타임 프로퍼티다. 값의 출처는 UnitDefinition 의 UpgradeTier 이고,
        // AllyUnit 이 누적한 결과를 여기에 밀어 넣는다. SO 는 절대 런타임에 수정하지 않는다 (CLAUDE.md §4).

        /// <summary>독 — 초당 피해량. 0 보다 크면 공격이 적중할 때마다 대상에게 독을 건다.</summary>
        public float PoisonDamagePerSecond { get; set; }

        /// <summary>독 지속 시간 (초).</summary>
        public float PoisonDuration { get; set; }

        /// <summary>독 피해 틱 간격 (초). 0 이면 <see cref="StatusEffects"/> 의 기본값.</summary>
        public float PoisonTickInterval { get; set; }

        /// <summary>맹독 — 이 독에 걸린 적은 체력을 회복하지 못한다 (Lv4-1).</summary>
        public bool PoisonBlocksHealing { get; set; }

        /// <summary>중독되지 않은 적을 우선공격하는가. 독을 퍼뜨리는 편이 총 피해가 크기 때문이다.</summary>
        public bool PrefersUnpoisonedTarget { get; set; }

        /// <summary>처형 — 잔여 체력 비율이 이 값 미만인 대상을 즉사시킨다. 0 이면 처형 없음.</summary>
        public float ExecuteThreshold { get; set; }

        /// <summary>보스에게 처형 대신 적용하는 치명타 배율. 1 이하면 그냥 평타가 된다.</summary>
        public float BossCritMultiplier { get; set; } = 1f;

        /// <summary>범위 공격 — 착탄 지점 중심 반경. 0 이면 단일 대상 (ADR-0021).</summary>
        public float SplashRadius { get; set; }

        /// <summary>주 대상 외 대상이 받는 피해 비율. 주 대상은 언제나 100% 다.</summary>
        public float SplashDamageRatio { get; set; }

        /// <summary>저격 — 사거리 안에서 <b>가장 먼</b> 적을 우선한다 (ADR-0021).</summary>
        public bool PrefersFarthestTarget { get; set; }

        public event Action<IDamageable> OnFired;

        /// <summary>이번 타격이 처형이었는가 (연출·로그용). <see cref="OnFired"/> 발행 시점에 유효하다.</summary>
        public bool LastHitWasExecute { get; private set; }

        /// <summary>사거리 안이지만 장애물에 막혀 때릴 수 없는 대상인가. 사거리 원 중심에서 판정한다.</summary>
        public bool CanSee(Vector3 targetPosition)
        {
            if (_sight == SightMode.Ignore) return true;
            var grid = Grid.GridSystem.Instance;
            return grid == null || grid.HasLineOfSight(RangeOrigin, targetPosition, WaterBlocks);
        }

        private void Awake()
        {
            _sightFilter = t => CanSee(t.Position);
            _unpoisonedFilter = t => !StatusEffects.IsTargetPoisoned(t) && CanSee(t.Position);
        }

        private void Update()
        {
            if (!_enabled) return;

            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;

            var target = SelectTarget();
            if (target == null) return;

            // 주 대상. 처형·독은 여기에만 건다 — 범위 피해까지 처형이 번지면 한 방에 판이 정리되고,
            // 무엇이 왜 죽었는지 읽히지 않는다.
            Vector3 impact = target.Position;
            target.TakeDamage(ResolveDamage(target), this);

            if (PoisonDamagePerSecond > 0f && PoisonDuration > 0f)
                StatusEffects.ApplyPoison(target, PoisonDamagePerSecond, PoisonDuration,
                                          PoisonTickInterval, PoisonBlocksHealing);

            if (SplashRadius > 0f && SplashDamageRatio > 0f)
                ApplySplash(impact, target);

            OnFired?.Invoke(target);
            _cooldown = _interval;
        }

        /// <summary>착탄 지점 주변에 감쇠된 피해를 준다 (메테오, ADR-0021).
        ///
        /// 주 대상은 이미 100% 를 받았으므로 건너뛴다. 대상 목록은 복사본이라
        /// 여기서 적이 죽어 레지스트리가 바뀌어도 순회가 깨지지 않는다.</summary>
        private void ApplySplash(Vector3 impact, IDamageable primary)
        {
            TargetRegistry.CollectInRadius(impact, SplashRadius, _targetTeam, _splashBuffer);

            float splashDamage = _damage * SplashDamageRatio;
            for (int i = 0; i < _splashBuffer.Count; i++)
            {
                var t = _splashBuffer[i];
                if (t == primary || !t.IsUsable() || !t.IsAlive) continue;
                t.TakeDamage(splashDamage, this);
            }
        }

        /// <summary>이번에 때릴 대상. 기본은 <see cref="Anchor"/>(보통 보호대상)에 가장 가까운 적이고,
        /// 없으면 자기에게 가장 가까운 적이다.
        ///
        /// 두 가지 우선순위가 이 기본을 덮는다:
        /// <list type="bullet">
        /// <item>독단검(ADR-0020) — <b>중독되지 않은 적</b> 먼저. 이미 독이 도는 적을 또 때리는 것보다
        /// 새 대상에게 옮겨 붙이는 편이 총 피해가 크다.</item>
        /// <item>저격(ADR-0021) — <b>가장 먼 적</b> 먼저. 아직 들어오지 않은 적을 미리 자른다.</item>
        /// </list>
        /// 둘 다 후보가 없으면 평소대로 고른다 — 그래야 손을 놓지 않는다.</summary>
        private IDamageable SelectTarget()
        {
            var filter = _sight == SightMode.Ignore ? null : _sightFilter;
            Vector3 origin = RangeOrigin;

            if (PrefersUnpoisonedTarget)
            {
                var fresh = TargetRegistry.FindNearestTo(origin, _range, _targetTeam, Anchor, _unpoisonedFilter)
                            ?? TargetRegistry.FindNearest(origin, _range, _targetTeam, _unpoisonedFilter);
                if (fresh != null) return fresh;
            }

            if (PrefersFarthestTarget)
            {
                var far = TargetRegistry.FindFarthest(origin, _range, _targetTeam, filter);
                if (far != null) return far;
            }

            return TargetRegistry.FindNearestTo(origin, _range, _targetTeam, Anchor, filter)
                   ?? TargetRegistry.FindNearest(origin, _range, _targetTeam, filter);
        }

        /// <summary>이번 타격의 피해량. 처형(ADR-0020) 판정이 여기서 일어난다.</summary>
        private float ResolveDamage(IDamageable target)
        {
            float damage = ResolveHitDamage(_damage, target.Current, target.Max, target.IsBoss,
                                            ExecuteThreshold, BossCritMultiplier, out bool executed);
            LastHitWasExecute = executed;
            return damage;
        }

        /// <summary>처형 규칙의 순수 계산 (ADR-0020). 부수효과가 없어 EditMode 테스트로 검증한다.
        ///
        /// 잔여 체력 비율이 임계 <b>미만</b>이면 즉사시킨다 — 남은 체력만큼을 그대로 준다.
        /// 보스는 즉사하지 않고 대신 치명타를 받는다: 즉사가 통하면 보스전이 "체력을 임계까지 깎는 경주"로
        /// 변해 버려서 체력 막대의 마지막 구간이 통째로 의미를 잃는다.</summary>
        /// <param name="executed">즉사 처형이 일어났는가. 보스 치명타는 처형이 아니다.</param>
        public static float ResolveHitDamage(float baseDamage, float current, float max, bool isBoss,
                                             float executeThreshold, float bossCritMultiplier,
                                             out bool executed)
        {
            executed = false;
            if (executeThreshold <= 0f || max <= 0f) return baseDamage;

            // 나누지 않고 곱해서 비교한다 — current/max 는 5/100 조차 0.05f 와 정확히 같지 않아서
            // 임계 바로 위아래가 부동소수 오차로 뒤집힌다. 경계값 자체는 규정하지 않는다.
            if (current >= max * executeThreshold) return baseDamage;

            if (isBoss) return baseDamage * Mathf.Max(1f, bossCritMultiplier);

            executed = true;
            return current;
        }
    }
}
