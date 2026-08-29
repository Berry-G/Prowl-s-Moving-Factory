using UnityEngine;
using UnityEngine.UI;
using PMF.Actors;
using PMF.Session;

namespace PMF.UI
{
    /// <summary>상단 바 보조 정보 (G-14) — 모체까지 거리와 다음 스폰까지 남은 시간.
    /// 이 둘이 "지금 어디를 방어해야 하는가"를 결정하므로 배속과 함께 보조 위계에 둔다.</summary>
    public sealed class MotherInfoLabel : MonoBehaviour
    {
        private const float RefreshInterval = 0.1f;   // 매 프레임 문자열을 새로 만들지 않는다

        private static readonly Color RestColor = new Color(0.45f, 0.65f, 0.95f);   // 휴지 — 배치할 시간
        private static readonly Color FiringColor = new Color(1f, 0.5f, 0.3f);      // 묶음이 나가는 중

        private Text _text;
        private Escortee _escortee;
        private MotherSpawner _mother;
        private Image _gaugeFill;
        private float _timer;

        private void Start()
        {
            _text = GetComponent<Text>();
            _text.text = string.Empty;
        }

        /// <summary>다음 묶음까지 남은 시간 게이지 (G-19 작업 3).
        /// 플레이어가 휴지를 배치에 쓰도록 유도하는 것이 목적이다.</summary>
        internal void BindGauge(Image fill) => _gaugeFill = fill;

        private void Update()
        {
            // 액터들의 Start 순서가 보장되지 않으므로 잡힐 때까지 가볍게 찾는다.
            if (_escortee == null)
            {
                var session = GameSession.Instance;
                _escortee = session != null ? session.Escortee : null;
            }
            if (_mother == null) _mother = FindAnyObjectByType<MotherSpawner>();
            if (_escortee == null || _mother == null) return;

            // 게이지는 매 프레임 — 차오르는 것이 보여야 의미가 있다.
            UpdateGauge();

            _timer -= Time.unscaledDeltaTime;   // 일시정지 중에도 표시가 굳지 않게
            if (_timer > 0f) return;
            _timer = RefreshInterval;

            float distance = Vector3.Distance(_mother.transform.position, _escortee.transform.position);
            string rhythm = _mother.IsIdle
                ? $"활동 시작 {_mother.SecondsToNextVolley:F1}s"
                : _mother.IsVolleyFiring
                    ? "묶음 진행 중"
                    : $"다음 묶음 {_mother.SecondsToNextVolley:F1}s";

            _text.text = $"모체 거리 {distance:F1}   {rhythm}";
        }

        private void UpdateGauge()
        {
            if (_gaugeFill == null) return;

            // 스프라이트가 없어 fillAmount 를 못 쓴다 — 앵커 폭으로 채운다 (G-14 와 같은 방식).
            var rt = (RectTransform)_gaugeFill.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(_mother.VolleyGauge01, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _gaugeFill.color = _mother.IsVolleyFiring ? FiringColor : RestColor;
        }
    }
}
