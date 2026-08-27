using UnityEngine;
using UnityEngine.UI;
using PMF.Combat;
using PMF.Session;

namespace PMF.UI
{
    /// <summary>호위대상 체력 표시. Health.OnDamaged/OnDied 구독으로 변경 시에만 갱신.</summary>
    public sealed class EscorteeHealthLabel : MonoBehaviour
    {
        private Text _text;
        private Health _health;

        private void Start()
        {
            _text = GetComponent<Text>();
        }

        private void Update()
        {
            // Escortee.Start() 와 실행 순서가 보장되지 않으므로, 등록될 때까지 가볍게 폴링한다.
            var escortee = GameSession.Instance != null ? GameSession.Instance.Escortee : null;
            if (escortee == null || escortee.Health == null) return;

            _health = escortee.Health;
            _health.OnDamaged += OnHealthChanged;
            _health.OnDied += OnDied;
            Refresh();
            enabled = false;   // 구독 완료 — 더 폴링할 필요 없음.
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnDamaged -= OnHealthChanged;
                _health.OnDied -= OnDied;
            }
        }

        private void OnHealthChanged(Health health, float amount) => Refresh();
        private void OnDied(Health health) => Refresh();

        private void Refresh()
        {
            _text.text = $"HP {Mathf.CeilToInt(_health.Current)}/{Mathf.CeilToInt(_health.Max)}";
        }
    }
}
