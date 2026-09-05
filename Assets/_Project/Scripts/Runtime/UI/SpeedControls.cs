using UnityEngine;
using UnityEngine.UI;
using PMF.Session;

namespace PMF.UI
{
    /// <summary>배속 버튼(일시정지 · 0.1x · 1x · 2x · 4x). 시각 요소는 SceneParts 가 만들고,
    /// 클릭 동작과 <b>지금 어느 배속인지</b>를 여기서 관리한다.
    ///
    /// 두 가지 규칙이 여기 걸려 있다 (2026-09-02):
    /// <list type="number">
    /// <item><b>현재 배속을 눈에 보이게 한다.</b> 버튼이 다섯 개인데 어느 것이 켜져 있는지 모르면
    /// 누를 때마다 화면을 보고 추측해야 한다.</item>
    /// <item><b>배속을 바꾸면 열린 창이 닫힌다.</b> 창이 떠 있는 동안은 0.1배속이 강제되므로
    /// (ADR-0019) 배속을 눌러도 아무 일이 없다 — 배속을 바꾸겠다는 것은 창을 그만 보겠다는 뜻이다.</item>
    /// </list></summary>
    public sealed class SpeedControls : MonoBehaviour
    {
        // 활성/비활성 색. 그레이박스라 색만으로 구분한다.
        private static readonly Color ActiveColor = new Color(0.36f, 0.62f, 0.95f);
        private static readonly Color IdleColor = new Color(0.25f, 0.3f, 0.4f);
        private static readonly Color ActiveTextColor = Color.white;
        private static readonly Color IdleTextColor = new Color(0.72f, 0.75f, 0.82f);

        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _speed01Button;
        [SerializeField] private Button _speed1Button;
        [SerializeField] private Button _speed2Button;
        [SerializeField] private Button _speed4Button;

        private GameClock _clock;

        private void Awake()
        {
            if (_pauseButton != null) _pauseButton.onClick.AddListener(() => GameClock.Instance?.TogglePause());
            if (_speed01Button != null) _speed01Button.onClick.AddListener(() => Pick(0.1f));
            if (_speed1Button != null) _speed1Button.onClick.AddListener(() => Pick(1f));
            if (_speed2Button != null) _speed2Button.onClick.AddListener(() => Pick(2f));
            if (_speed4Button != null) _speed4Button.onClick.AddListener(() => Pick(4f));
        }

        private void Start()
        {
            // GameClock 은 Awake 에서 자기를 등록하므로 Start 에서 잡는다.
            _clock = GameClock.Instance;
            if (_clock != null) _clock.OnStateChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            // 도메인 리로드가 꺼져 있다 — 구독을 반드시 푼다 (CLAUDE.md §3).
            if (_clock != null) _clock.OnStateChanged -= Refresh;
        }

        /// <summary>배속 선택. <b>먼저 배속을 기록하고 그 다음 창을 닫는다</b> —
        /// 창이 닫히면서 <c>ExitUiSlowMotion</c> 이 "마지막으로 고른 배속"으로 되돌리기 때문에,
        /// 순서가 뒤바뀌면 방금 누른 배속이 무시되고 이전 배속으로 돌아간다.</summary>
        private void Pick(float speed)
        {
            var clock = GameClock.Instance;
            if (clock == null) return;

            clock.SetSpeed(speed);
            clock.RequestDismissUiWindows();
        }

        /// <summary>지금 상태를 버튼에 칠한다. 일시정지 중에는 일시정지 버튼만 켜진다 —
        /// 0배속이 배속보다 강한 상태라 "2x 이면서 멈춰 있다" 고 보여 주면 거짓말이 된다.</summary>
        private void Refresh()
        {
            var clock = _clock != null ? _clock : GameClock.Instance;
            if (clock == null) return;

            bool paused = clock.IsPaused;
            float speed = clock.Speed;

            Paint(_pauseButton, paused);
            Paint(_speed01Button, !paused && speed == 0.1f);
            Paint(_speed1Button, !paused && speed == 1f);
            Paint(_speed2Button, !paused && speed == 2f);
            Paint(_speed4Button, !paused && speed == 4f);
        }

        private static void Paint(Button button, bool active)
        {
            if (button == null) return;

            var image = button.GetComponent<Image>();
            if (image != null) image.color = active ? ActiveColor : IdleColor;

            var label = button.GetComponentInChildren<Text>();
            if (label != null) label.color = active ? ActiveTextColor : IdleTextColor;
        }
    }
}
