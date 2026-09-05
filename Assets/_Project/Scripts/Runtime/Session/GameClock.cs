using UnityEngine;

namespace PMF.Session
{
    /// <summary>
    /// 시간 배속의 유일한 창구. Time.timeScale 을 다른 곳에서 건드리지 마라.
    /// </summary>
    public sealed class GameClock : MonoBehaviour
    {
        public static GameClock Instance { get; private set; }

        /// <summary>플레이어가 고를 수 있는 배속들. 표시 순서와 같다.
        /// <b>0.1 은 UI 슬로우모션과 같은 값이다</b> — 창이 뜰 때 자동으로 걸리던 속도를
        /// 플레이어가 직접 고를 수도 있게 열어 둔 것이다 (2026-09-02).</summary>
        public static readonly float[] SelectableSpeeds = { 0.1f, 1f, 2f, 4f };

        private float _speed = 1f;
        private bool _paused;

        /// <summary>슬로우모션을 요청한 창의 <b>개수</b>. bool 이 아니라 카운터인 이유는
        /// 창이 겹칠 수 있기 때문이다 — 유닛 정보 패널을 띄운 채 마을을 눌러 고용 패널을 열면 둘이 겹친다.
        /// bool 이면 먼저 닫히는 쪽이 슬로우를 풀어 <b>아직 창이 떠 있는데 게임이 정상 속도로 돌아간다</b>.
        /// 실제로 그렇게 규칙이 새어 나갔다 (2026-09-02).</summary>
        private int _uiSlowDepth;
        private float _uiSlowScale = 0.1f;

        public bool IsPaused => _paused;
        public float Speed => _speed;

        /// <summary>지금 UI 슬로우모션이 걸려 있는가.</summary>
        public bool IsUiSlowMotion => _uiSlowDepth > 0;

        /// <summary>배속이나 일시정지 상태가 바뀌었다. 배속 버튼의 현재 상태 표시가 구독한다.</summary>
        public event System.Action OnStateChanged;

        /// <summary>열려 있는 윈도우형 UI 는 스스로 닫으라는 신호.
        ///
        /// <b>왜 시간 담당인 GameClock 이 이걸 갖는가:</b> 창이 뜨면 0.1배속이 걸리므로(ADR-0019)
        /// 창이 떠 있는 동안 배속을 바꿔 봐야 아무 일도 일어나지 않는다. 즉 <b>배속을 바꾸겠다는 행위는
        /// "이 창은 이제 됐으니 게임을 돌리겠다"는 뜻</b>이고, 그 판단이 일어나는 곳이 여기다.
        /// 슬로우모션 참조 계수를 이미 이 클래스가 들고 있으므로 창구를 둘로 나눌 이유도 없다.
        ///
        /// ⚠️ 구독자는 <b>반드시 OnDisable/OnDestroy 에서 해제</b>하라 (CLAUDE.md §3).</summary>
        public event System.Action OnDismissUiWindows;

        /// <summary>열린 창들에게 닫으라고 알린다. 창이 없으면 아무 일도 일어나지 않는다.</summary>
        public void RequestDismissUiWindows() => OnDismissUiWindows?.Invoke();

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
            if (IsUiSlowMotion && !_paused) Time.timeScale = _uiSlowScale;
        }

        /// <summary>허용값: 1, 2, 4. 그 외는 거부하고 로그.</summary>
        /// <summary>허용값은 <see cref="SelectableSpeeds"/> 뿐. 그 외는 거부하고 로그.</summary>
        public void SetSpeed(float speed)
        {
            if (System.Array.IndexOf(SelectableSpeeds, speed) < 0)
            {
                Debug.LogError($"[{nameof(GameClock)}] 허용되지 않는 배속: {speed} " +
                               $"({string.Join("/", SelectableSpeeds)} 만 허용)", this);
                return;
            }
            _speed = speed;
            if (!_paused && !IsUiSlowMotion) Time.timeScale = _speed;
            OnStateChanged?.Invoke();
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
            OnStateChanged?.Invoke();
        }

        /// <summary>마지막 Speed(또는 UI 슬로우모션 중이면 그 배속)로 복귀.</summary>
        public void Resume()
        {
            _paused = false;
            Time.timeScale = IsUiSlowMotion ? _uiSlowScale : _speed;
            OnStateChanged?.Invoke();
        }

        /// <summary>윈도우형 UI 가 뜨는 동안 정밀 조작을 위해 슬로우모션을 건다.
        /// 일시정지 중에는 아무 효과 없다 (0배속이 더 강한 상태라 그대로 둔다).
        ///
        /// <b>Enter 와 Exit 는 반드시 짝을 이뤄야 한다.</b> 창을 여는 곳에서 Enter,
        /// 닫는 <b>모든</b> 경로(확인·취소·ESC·대상 소멸·OnDisable)에서 Exit 다.
        /// 한쪽이 빠지면 게임이 0.1배속에 갇히거나, 창이 떠 있는데 정상 속도로 돈다.</summary>
        public void EnterUiSlowMotion()
        {
            _uiSlowDepth++;
            if (!_paused) Time.timeScale = _uiSlowScale;
            OnStateChanged?.Invoke();
        }

        /// <summary>창이 닫히면 호출. 마지막 창이 닫힐 때만 원래 Speed 로 복귀한다.</summary>
        public void ExitUiSlowMotion()
        {
            if (_uiSlowDepth <= 0)
            {
                // 짝이 맞지 않는다 = 어딘가에서 Exit 를 두 번 불렀거나 Enter 를 빠뜨렸다.
                // 조용히 넘기면 "창이 떠 있는데 정상 속도" 로 새어 나가므로 소리를 낸다.
                Debug.LogError($"[{nameof(GameClock)}] ExitUiSlowMotion 이 Enter 보다 많이 불렸다. " +
                               $"창을 여닫는 경로에서 짝이 빠졌다.", this);
                _uiSlowDepth = 0;
                return;
            }

            _uiSlowDepth--;
            if (_uiSlowDepth == 0 && !_paused) Time.timeScale = _speed;
            OnStateChanged?.Invoke();
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
