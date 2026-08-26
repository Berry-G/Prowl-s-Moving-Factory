using UnityEngine;

namespace PMF.Session
{
    /// <summary>
    /// 시간 배속의 유일한 창구. Time.timeScale 을 다른 곳에서 건드리지 지라.
    /// </summary>
    public sealed class GameClock : MonoBehaviour
    {
        public static GameClock Instance { get; private set; }

        private float _speed = 1f;
        private bool _paused;

        public bool IsPaused => _paused;
        public float Speed => _speed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError($"[{nameof(GameClock)}] 씬에 두 개 이상 존재합니다.", this);
                enabled = false;
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            // 일시정지/배속 상태로 플레이를 끝내면 도메인 리ロ드가 꺼져 있어 timeScale 유지됨 → 복구 필수.
            Time.timeScale = 1f;
            if (Instance == this) Instance = null;
        }

        /// <summary>허용값: 1, 2, 4. 그 외는 거부하고 로그.</summary>
        public void SetSpeed(float speed)
        {
            if (speed != 1f && speed != 2f && speed != 4f)
            {
                Debug.LogError($"[{nameof(GameClock)}] 허용되지 않는 배속: {speed} (1/2/4 만 허용)", this);
                return;
            }
            _speed = speed;
            if (!_paused) Time.timeScale = _speed;
        }

        public void TogglePause()
        {
            if (_paused) Resume();
            else Pause();
        }

        public void Pause()
        {
            _paused = true;
            Time.timeScale = 0f;
        }

        /// <summary>마지막 Speed 로 복귀.</summary>
        public void Resume()
        {
            _paused = false;
            Time.timeScale = _speed;
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Time.timeScale = 1f;
        }
#endif
    }
}
