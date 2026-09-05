using UnityEngine;
using UnityEngine.InputSystem;
using PMF.Session;

namespace PMF.Diagnostics
{
    /// <summary>핫키: Space 일시정지 / 1·2·3 배속 / R 재시작.</summary>
    public sealed class DebugHotkeys : MonoBehaviour
    {
        private GameClock _clock;
        private GameSession _session;

        private void Start()
        {
            _clock = FindAnyObjectByType<GameClock>();
            _session = FindAnyObjectByType<GameSession>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;   // Keyboard.current 는 null 일 수 있다.

            if (keyboard.spaceKey.wasPressedThisFrame)
                _clock?.TogglePause();

            // 배속 키. 버튼과 같은 규칙이다 — 배속을 바꾸면 열린 창이 닫힌다 (2026-09-02).
            // 창이 떠 있는 동안은 0.1배속이 강제되므로(ADR-0019) 닫지 않으면 키를 눌러도 아무 일이 없다.
            if (keyboard.digit1Key.wasPressedThisFrame) PickSpeed(1f);
            if (keyboard.digit2Key.wasPressedThisFrame) PickSpeed(2f);
            if (keyboard.digit3Key.wasPressedThisFrame) PickSpeed(4f);
            if (keyboard.digit4Key.wasPressedThisFrame) PickSpeed(0.1f);

            if (keyboard.rKey.wasPressedThisFrame)
                _session?.RestartStage();
        }

        /// <summary>배속을 정하고 열린 창을 닫는다. 순서가 중요하다 —
        /// 창이 닫히면서 "마지막으로 고른 배속"으로 되돌아가므로 배속을 먼저 기록해야 한다.</summary>
        private void PickSpeed(float speed)
        {
            if (_clock == null) return;
            _clock.SetSpeed(speed);
            _clock.RequestDismissUiWindows();
        }
    }
}
