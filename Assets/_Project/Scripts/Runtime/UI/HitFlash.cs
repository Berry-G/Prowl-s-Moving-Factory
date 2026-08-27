using UnityEngine;
using PMF.Combat;

namespace PMF.UI
{
    /// <summary>피격 플래시 (G-08). 기존 Health.OnDamaged 를 구독한다 (새 이벤트 만들지 않음).
    /// _hitFlashSeconds 동안 흰색 램프 — 게임시간 기준. OnDisable 에서 반드시 구독 해제.</summary>
    public sealed class HitFlash : MonoBehaviour
    {
        private Health _health;
        private SpriteRenderer _sprite;
        private Color _original;
        private float _seconds;
        private float _timer;
        private bool _flashing;

        public void Init(Health health, SpriteRenderer sprite, float seconds)
        {
            _health = health;
            _sprite = sprite;
            _seconds = seconds;
            if (_sprite != null) _original = _sprite.color;
            _health.OnDamaged += HandleDamaged;
        }

        private void OnDisable()
        {
            if (_health != null) _health.OnDamaged -= HandleDamaged;
            Restore();
        }

        private void HandleDamaged(Health health, float amount)
        {
            if (_sprite == null) return;
            if (!_flashing)
            {
                _original = _sprite.color;   // 첫 플래시 시점의 원래 색
                _flashing = true;
            }
            _timer = _seconds;
            _sprite.color = Color.white;
        }

        private void Update()
        {
            if (!_flashing) return;
            _timer -= Time.deltaTime;   // 게임시간 기준 (배속 비율 유지)
            if (_timer <= 0f)
            {
                _flashing = false;
                Restore();
            }
        }

        private void Restore()
        {
            if (_sprite != null && !_flashing) _sprite.color = _original;
        }
    }
}