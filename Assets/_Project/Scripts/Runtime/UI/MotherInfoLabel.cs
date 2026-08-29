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

        private Text _text;
        private Escortee _escortee;
        private MotherSpawner _mother;
        private float _timer;

        private void Start()
        {
            _text = GetComponent<Text>();
            _text.text = string.Empty;
        }

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

            _timer -= Time.unscaledDeltaTime;   // 일시정지 중에도 표시가 굳지 않게
            if (_timer > 0f) return;
            _timer = RefreshInterval;

            float distance = Vector3.Distance(_mother.transform.position, _escortee.transform.position);
            string spawn = _mother.IsIdle
                ? $"활동 시작 {_mother.SecondsToNextSpawn:F1}s"
                : $"다음 스폰 {_mother.SecondsToNextSpawn:F1}s";

            _text.text = $"모체 거리 {distance:F1}   {spawn}";
        }
    }
}
