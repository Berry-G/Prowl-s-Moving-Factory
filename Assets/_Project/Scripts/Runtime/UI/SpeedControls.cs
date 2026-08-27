using UnityEngine;
using UnityEngine.UI;
using PMF.Session;

namespace PMF.UI
{
    /// <summary>배속 버튼 4개(일시정지/1x/2x/4x). 시각 요소는 SceneParts 가 만들고, 클릭 동작만 여기서 연결한다.</summary>
    public sealed class SpeedControls : MonoBehaviour
    {
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _speed1Button;
        [SerializeField] private Button _speed2Button;
        [SerializeField] private Button _speed4Button;

        private void Awake()
        {
            if (_pauseButton != null) _pauseButton.onClick.AddListener(() => GameClock.Instance?.TogglePause());
            if (_speed1Button != null) _speed1Button.onClick.AddListener(() => GameClock.Instance?.SetSpeed(1f));
            if (_speed2Button != null) _speed2Button.onClick.AddListener(() => GameClock.Instance?.SetSpeed(2f));
            if (_speed4Button != null) _speed4Button.onClick.AddListener(() => GameClock.Instance?.SetSpeed(4f));
        }
    }
}
