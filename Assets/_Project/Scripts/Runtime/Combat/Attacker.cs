using System;
using UnityEngine;

namespace PMF.Combat
{
    /// <summary>
    /// 공격 능력. AllyUnit과 Enemy가 공유. 투사체 없음 — 히트스캔(즉시 데미지).
    /// 타겟 선택: FindNearestTo(내 위치, Range, TargetTeam, Anchor). Anchor는 소유자가 갱신.
    /// </summary>
    public sealed class Attacker : MonoBehaviour
    {
        [SerializeField] private float _range = 1f;
        [SerializeField] private float _damage = 5f;
        [SerializeField] private float _interval = 1f;
        [SerializeField] private Team _targetTeam = Team.Enemy;
        [SerializeField] private bool _enabled = true;

        private float _cooldown;

        public float Range { get => _range; set => _range = value; }
        public float Damage { get => _damage; set => _damage = value; }
        public float Interval { get => _interval; set => _interval = value; }
        public Team TargetTeam { get => _targetTeam; set => _targetTeam = value; }

        /// <summary>행군 중에는 false. 상태 전환 시점에 정확히 한 번만 바꾸는 것을 원칙.</summary>
        public bool Enabled { get => _enabled; set => _enabled = value; }

        /// <summary>타겟 우선순위 기준점 (보통 보호대상 위치). 소유 컴포넌트가 갱신한다.</summary>
        public Vector3 Anchor { get; set; }

        public event Action<IDamageable> OnFired;

        private void Update()
        {
            if (!_enabled) return;

            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;

            var target = TargetRegistry.FindNearestTo(transform.position, _range, _targetTeam, Anchor)
                         ?? TargetRegistry.FindNearest(transform.position, _range, _targetTeam);
            if (target == null) return;

            target.TakeDamage(_damage, this);
            OnFired?.Invoke(target);
            _cooldown = _interval;
        }
    }
}
