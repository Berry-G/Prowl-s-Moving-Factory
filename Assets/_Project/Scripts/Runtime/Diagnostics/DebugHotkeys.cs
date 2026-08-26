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

            if (keyboard.digit1Key.wasPressedThisFrame) _clock?.SetSpeed(1f);
            if (keyboard.digit2Key.wasPressedThisFrame) _clock?.SetSpeed(2f);
            if (keyboard.digit3Key.wasPressedThisFrame) _clock?.SetSpeed(4f);

            if (keyboard.rKey.wasPressedThisFrame)
                _session?.RestartStage();
        }
    }
}
