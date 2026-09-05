using System.Collections.Generic;
using UnityEngine;

namespace PMF.Combat
{
    /// <summary>
    /// 대상에게 걸린 지속 효과 (ADR-0020). 지금은 <b>독 하나뿐</b>이다.
    ///
    /// 물리 미사용(ADR-0005)이라 트리거도 코루틴도 쓰지 않는다 — 그냥 타이머를 누산한다.
    /// 필요할 때만 <see cref="ApplyPoison"/> 이 컴포넌트를 붙이므로,
    /// 독에 걸린 적 없는 적에게는 이 컴포넌트 자체가 존재하지 않는다.
    ///
    /// ⚠️ 독은 <b>매 프레임이 아니라 틱 간격</b>으로 들어간다. 매 프레임 <see cref="Health.TakeDamage"/> 를
    /// 부르면 <c>OnDamaged</c> 도 매 프레임 발행되어 피격 플래시(G-08)가 계속 터진다.
    ///
    /// ⚠️ 도메인 리로드가 꺼져 있으므로 static 사전은 반드시 리셋한다 (CLAUDE.md §3).
    /// </summary>
    public sealed class StatusEffects : MonoBehaviour
    {
        /// <summary>틱 간격이 지정되지 않았을 때 쓰는 기본값 (초).</summary>
        private const float DefaultTickInterval = 0.5f;

        /// <summary>대상 → 그 대상에 붙은 인스턴스. <see cref="Attacker"/> 가 매 프레임 타겟 우선순위를
        /// 판정하므로 <c>GetComponent</c> 를 쓸 수 없다 (CLAUDE.md §4).</summary>
        private static readonly Dictionary<IDamageable, StatusEffects> _byTarget =
            new Dictionary<IDamageable, StatusEffects>();

        private Health _health;

        private float _poisonRemaining;
        private float _poisonDamagePerSecond;
        private float _poisonTickInterval = DefaultTickInterval;
        private float _tickTimer;
        private bool _poisonBlocksHealing;

        /// <summary>독이 걸려 있는가. 독단검의 "중독되지 않은 적을 우선공격" 판정에 쓴다.</summary>
        public bool IsPoisoned => _poisonRemaining > 0f;

        /// <summary>맹독 — 체력 회복이 막혀 있는가 (Lv4-1).</summary>
        public bool HealingBlocked => _poisonRemaining > 0f && _poisonBlocksHealing;

        // ---------- 정적 조회 ----------

        /// <summary><paramref name="target"/> 이 중독 상태인가. 컴포넌트가 없으면 false.</summary>
        public static bool IsTargetPoisoned(IDamageable target)
        {
            if (target == null) return false;
            return _byTarget.TryGetValue(target, out var fx) && fx != null && fx.IsPoisoned;
        }

        /// <summary><paramref name="target"/> 의 체력 회복이 막혀 있는가 (맹독).</summary>
        public static bool IsHealingBlocked(IDamageable target)
        {
            if (target == null) return false;
            return _byTarget.TryGetValue(target, out var fx) && fx != null && fx.HealingBlocked;
        }

        /// <summary>독을 건다. 컴포넌트가 없으면 그때 붙인다.
        /// 이미 걸려 있으면 <b>갱신</b>이다 — 중첩되지 않고 남은 시간과 세기가 더 강한 쪽으로 덮인다.
        /// (중첩을 허용하면 고양이를 몰아 놓는 것이 정답이 되어 배치 판단이 사라진다.)</summary>
        public static void ApplyPoison(IDamageable target, float damagePerSecond, float duration,
                                       float tickInterval, bool blocksHealing)
        {
            if (target == null || damagePerSecond <= 0f || duration <= 0f) return;
            if (!(target is Health health) || !health.IsAlive) return;

            if (!_byTarget.TryGetValue(target, out var fx) || fx == null)
            {
                fx = health.gameObject.AddComponent<StatusEffects>();
                fx.Bind(health);
            }
            fx.Poison(damagePerSecond, duration, tickInterval, blocksHealing);
        }

        /// <summary>씬 재시작/리셋용 전체 초기화.</summary>
        public static void Clear() => _byTarget.Clear();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Clear();

        // ---------- 인스턴스 ----------

        private void Bind(Health health)
        {
            _health = health;
            _byTarget[health] = this;
        }

        private void Awake()
        {
            // AddComponent 경로가 아니라 프리팹에 미리 붙어 있는 경우를 위한 보정.
            if (_health == null)
            {
                var health = GetComponent<Health>();
                if (health != null) Bind(health);
            }
        }

        private void OnDestroy()
        {
            if (_health != null) _byTarget.Remove(_health);
        }

        private void Poison(float damagePerSecond, float duration, float tickInterval, bool blocksHealing)
        {
            // 더 강한 쪽으로 덮는다. 약한 독이 강한 독을 지우면 안 된다.
            _poisonDamagePerSecond = Mathf.Max(_poisonDamagePerSecond, damagePerSecond);
            _poisonRemaining = Mathf.Max(_poisonRemaining, duration);
            _poisonBlocksHealing |= blocksHealing;
            if (tickInterval > 0f) _poisonTickInterval = tickInterval;
        }

        private void Update()
        {
            if (_poisonRemaining <= 0f) return;

            if (_health == null || !_health.IsAlive)
            {
                ClearPoison();
                return;
            }

            float dt = Time.deltaTime;
            _poisonRemaining -= dt;
            _tickTimer += dt;

            if (_tickTimer < _poisonTickInterval && _poisonRemaining > 0f) return;

            // 마지막 틱은 남은 시간만큼만 — 지속 시간이 틱의 배수가 아니어도 총량이 맞는다.
            float elapsed = _tickTimer;
            _tickTimer = 0f;
            _health.TakeDamage(_poisonDamagePerSecond * elapsed, this);

            if (_poisonRemaining <= 0f) ClearPoison();
        }

        private void ClearPoison()
        {
            _poisonRemaining = 0f;
            _poisonDamagePerSecond = 0f;
            _poisonBlocksHealing = false;
            _tickTimer = 0f;
        }
    }
}
