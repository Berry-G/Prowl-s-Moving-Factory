using System;
using UnityEngine;

namespace PMF.Combat
{
    /// <summary>체력. IDamageable 구현체. OnEnable/OnDisable 에서 TargetRegistry 등록/해제.</summary>
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private float _maxHealth = 10f;

        private float _current;
        private Team _team = Team.Enemy;
        private bool _isAlive = true;
        private bool _invincible;
        private bool _initialized;
        private bool _isBoss;

        /// <summary>false 면 TargetRegistry 에 등록하지 않는다 (행군 중 아군 등).</summary>
        private bool _registryEnabled = true;

        public float Max => _maxHealth;
        public float Current => _current;
        public Team Team => _team;
        public Vector3 Position => transform.position;
        public bool IsAlive => _isAlive;

        /// <summary>보스인가. 처형(ADR-0020)의 예외 대상이다. <see cref="Initialize"/> 에서 정해진다.</summary>
        public bool IsBoss => _isBoss;

        public event Action<Health> OnDied;
        public event Action<Health, float> OnDamaged;   // (self, amount)

        private void Awake()
        {
            if (!_initialized)
            {
                _current = _maxHealth;
                _isAlive = true;
                _initialized = true;
            }
        }

        private void OnEnable()
        {
            if (_registryEnabled && _isAlive)
                TargetRegistry.Register(this);
        }

        private void OnDisable()
        {
            if (_registryEnabled)
                TargetRegistry.Unregister(this);
        }

        /// <summary>최대 체력과 소속 팀을 정한다. 각 액터의 Start 에서 호출된다.
        ///
        /// ⚠️ <b>여기서 반드시 다시 등록해야 한다.</b>
        /// <see cref="OnEnable"/>(등록)은 이 호출보다 <b>먼저</b> 돌고, 그때 <c>_team</c> 은 아직
        /// 기본값이다. 다시 등록하지 않으면 모든 Health 가 기본값 팀 리스트에 눌러앉는다.
        /// 실제로 보호대상이 Enemy 리스트에 남아 있었고, 그 결과
        /// <b>적은 보호대상을 찾지 못하고 아군은 보호대상을 때렸다</b> (2026-08-29).</summary>
        /// <param name="isBoss">보스는 처형되지 않고 치명타를 받는다 (ADR-0020).</param>
        public void Initialize(float max, Team team, bool isBoss = false)
        {
            TargetRegistry.UnregisterFromAll(this);   // 팀을 바꾸기 전에, 어느 리스트에 있든 뺀다

            _maxHealth = max;
            _current = max;
            _team = team;
            _isAlive = true;
            _initialized = true;
            _isBoss = isBoss;

            if (_registryEnabled && isActiveAndEnabled) TargetRegistry.Register(this);
        }

        /// <summary>레지스트리 등록 제어. 행군 중 아군은 false 로 시작하고 배치 시 true.</summary>
        public void SetRegistryEnabled(bool enabled)
        {
            if (_registryEnabled == enabled) return;
            _registryEnabled = enabled;
            if (enabled && _isAlive && isActiveAndEnabled) TargetRegistry.Register(this);
            else if (!enabled) TargetRegistry.Unregister(this);
        }

        /// <summary>F4 치트용 무적 토글.</summary>
        public void SetInvincible(bool invincible) => _invincible = invincible;

        /// <summary>체력 회복. 최대치를 넘지 않고, 죽은 대상은 되살리지 않는다.
        ///
        /// <b>맹독(ADR-0020)이 걸려 있으면 아무 일도 일어나지 않는다.</b> 지금 이 프로젝트에 회복 수단은
        /// 없지만(지속 힐러는 기각, 일회성 회복은 미구현), "맹독 = 회복 불가" 라는 규칙이 성립하려면
        /// 회복이 반드시 이 문을 지나야 한다. 나중에 회복을 만들 때 이 메서드를 쓸 것 —
        /// <c>_current</c> 를 직접 건드리면 맹독이 조용히 무력화된다.</summary>
        /// <returns>실제로 회복된 양. 막혔으면 0.</returns>
        public float Heal(float amount)
        {
            if (!_isAlive || amount <= 0f) return 0f;
            if (StatusEffects.IsHealingBlocked(this)) return 0f;

            float before = _current;
            _current = Mathf.Min(_maxHealth, _current + amount);
            return _current - before;
        }

        public void TakeDamage(float amount, object source)
        {
            if (!_isAlive || _invincible || amount <= 0f) return;

            _current -= amount;
            OnDamaged?.Invoke(this, amount);

            if (_current <= 0f)
            {
                // 재귀 방지: IsAlive 를 먼저 false 로 만들고 이벤트를 발행한다.
                _current = 0f;
                _isAlive = false;
                if (_registryEnabled) TargetRegistry.Unregister(this);
                OnDied?.Invoke(this);
            }
        }
    }
}
