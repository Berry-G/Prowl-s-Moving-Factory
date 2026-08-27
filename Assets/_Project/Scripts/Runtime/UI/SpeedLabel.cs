using UnityEngine;
using UnityEngine.UI;
using PMF.Session;

namespace PMF.UI
{
    /// <summary>현재 배속 표시. GameClock 에 변경 이벤트가 없어 값이 바뀔 때만 문자열을 새로 만든다.</summary>
    public sealed class SpeedLabel : MonoBehaviour
    {
        private Text _text;
        private float _lastSpeed = -1f;
        private bool _lastPaused;

        private void Start()
        {
            _text = GetComponent<Text>();
        }

        private void Update()
        {
            var clock = GameClock.Instance;
            if (clock == null) return;
            if (clock.Speed == _lastSpeed && clock.IsPaused == _lastPaused) return;

            _lastSpeed = clock.Speed;
            _lastPaused = clock.IsPaused;
            _text.text = _lastPaused ? "PAUSED" : $"{_lastSpeed:0}x";
        }
    }
}
