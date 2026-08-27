using UnityEngine;

namespace PMF.Session
{
    /// <summary>
    /// 시간 배속의 유일한 창구. Time.timeScale 을 다른 곳에서 건드리지 마라.
    /// </summary>
    public sealed class GameClock : MonoBehaviour
    {
        public static GameClock Instance { get; private set; }

        private float _speed = 1f;
        private bool _paused;
        private bool _uiSlowActive;
        private float _uiSlowScale = 0.1f;

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
            // 일시정지/배속 상태로 플레이를 끝내면 도메인 리로드가 꺼져 있어 timeScale 유지됨 → 복구 필수.
            Time.timeScale = 1f;
            if (Instance == this) Instance = null;
        }

        /// <summary>UI 슬로우모션 배속 주입. 밸런스 수치이므로 StageDefinition 이 소유하고 GameSession 이 넣어 준다.</summary>
        public void SetUiSlowScale(float scale)
        {
            if (scale <= 0f || scale > 1f)
            {
                Debug.LogError($"[{nameof(GameClock)}] 잘못된 UI 슬로우모션 배속: {scale} (0 < scale <= 1)", this);
                return;
            }
            _uiSlowScale = scale;
            // 이미 슬로우모션 중이면 즉시 반영한다.
            if (_uiSlowActive && !_paused) Time.timeScale = _uiSlowScale;
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
            if (!_paused && !_uiSlowActive) Time.timeScale = _speed;
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

        /// <summary>마지막 Speed(또는 UI 슬로우모션 중이면 그 배속)로 복귀.</summary>
        public void Resume()
        {
            _paused = false;
            Time.timeScale = _uiSlowActive ? _uiSlowScale : _speed;
        }

        /// <summary>배치 UI 가 뜨는 동안 정밀 조작을 위해 슬로우모션을 건다. 일시정지 중에는 아무 효과 없음.</summary>
        public void EnterUiSlowMotion()
        {
            if (_uiSlowActive) return;
            _uiSlowActive = true;
            if (!_paused) Time.timeScale = _uiSlowScale;
        }

        /// <summary>UI 가 닫히면 원래 Speed 로 복귀.</summary>
        public void ExitUiSlowMotion()
        {
            if (!_uiSlowActive) return;
            _uiSlowActive = false;
            if (!_paused) Time.timeScale = _speed;
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
