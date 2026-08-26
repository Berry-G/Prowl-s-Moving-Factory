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

        /// <summary>false 면 TargetRegistry 에 등록하지 않는다 (행군 중 아군 등).</summary>
        private bool _registryEnabled = true;

        public float Max => _maxHealth;
        public float Current => _current;
        public Team Team => _team;
        public Vector3 Position => transform.position;
        public bool IsAlive => _isAlive;

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

        public void Initialize(float max, Team team)
        {
            _maxHealth = max;
            _current = max;
            _team = team;
            _isAlive = true;
            _initialized = true;
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
