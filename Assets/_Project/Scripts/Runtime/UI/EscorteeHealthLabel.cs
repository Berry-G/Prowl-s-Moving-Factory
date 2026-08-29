using UnityEngine;
using UnityEngine.UI;
using PMF.Combat;
using PMF.Session;

namespace PMF.UI
{
    /// <summary>호위대상 체력 표시. Health.OnDamaged/OnDied 구독으로 변경 시에만 갱신.</summary>
    public sealed class EscorteeHealthLabel : MonoBehaviour
    {
        private static readonly Color BarFull = new Color(0.42f, 0.85f, 0.45f);
        private static readonly Color BarLow = new Color(0.9f, 0.32f, 0.3f);

        private Text _text;
        private Health _health;
        private Image _fill;

        private void Start()
        {
            _text = GetComponent<Text>();
        }

        /// <summary>체력 바의 fill 이미지를 연결한다. 상단 바(G-14)가 만들면서 호출한다.
        /// 연결하지 않으면 숫자만 표시된다 — 바 없이도 동작해야 한다.</summary>
        internal void BindFill(Image fill)
        {
            _fill = fill;
            if (_health != null) Refresh();
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
            _text.text = $"{Mathf.CeilToInt(_health.Current)} / {Mathf.CeilToInt(_health.Max)}";

            if (_fill == null) return;
            float ratio = _health.Max > 0f ? Mathf.Clamp01(_health.Current / _health.Max) : 0f;

            // Image.fillAmount 은 스프라이트가 있어야 동작한다. 그레이박스에는 스프라이트가 없으므로
            // 앵커 폭으로 채운다 (fill 은 좌측 정렬 stretch 로 만들어져 있다).
            var rt = (RectTransform)_fill.transform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(ratio, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _fill.color = Color.Lerp(BarLow, BarFull, ratio);
        }
    }
}
