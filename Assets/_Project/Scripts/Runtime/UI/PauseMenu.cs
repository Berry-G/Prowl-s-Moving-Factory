using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using PMF.Session;

namespace PMF.UI
{
    /// <summary>일시정지 메뉴 (G-12, 우상단). uGUI — 이 컴포넌트가 Canvas 에서 자기 UI 를 스스로 만든다.
    /// 계속하기(직전 배속 복원) / 설정(볼륨·음소거·다시 시작) / 게임 종료.
    /// 메뉴가 열려 있는 동안 게임 입력은 전부 차단된다 (DeploymentController·SelectionController 가 IsOpen 을 본다).
    ///
    /// 맵 클릭 입력 우선순위 1단계 — 일시정지 메뉴가 떠 있으면 게임 입력 전부 차단 (TASKS G-02/G-12).</summary>
    public sealed partial class PauseMenu : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        // 도메인 리로드 OFF — static 이 다음 플레이로 샌다 (CLAUDE.md §3).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => IsOpen = false;

        private DeploymentController _deployment;
        private Audio.SfxPlayer _sfx;
        private GameObject _pauseButton;
        private GameObject _blocker;
        private GameObject _panel;
        private GameObject _settingsPanel;
        private Text _muteLabel;
        private Font _font;

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _deployment = FindAnyObjectByType<DeploymentController>();
            _sfx = FindAnyObjectByType<Audio.SfxPlayer>();

            _pauseButton = FindOrCreatePauseButton();
            BuildBlocker();
            BuildPanel();
            BuildSettingsPanel();
            ApplyOpenState(false);
        }

        private void Update()
        {
            // 판이 끝났는지는 세션 결과로 판정한다.
            // ResultPanel 은 SetActive 가 아니라 CanvasGroup 알파로 숨으므로 activeSelf 로는 알 수 없다.
            var session = GameSession.Instance;
            bool gameOver = session != null && session.Result != GameResult.InProgress;

            // 판이 끝나면 중지 버튼을 숨긴다 — 결과 패널과 겹치지 않게 (함정 목록).
            if (_pauseButton != null && _pauseButton.activeSelf == gameOver)
                _pauseButton.SetActive(!gameOver);

            // Esc 로 열고 닫기. 단, 배치 모드 중 Esc 는 배치 취소가 우선이다 (G-02 입력 우선순위).
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                // 배치 중이면 배치 취소가 우선. DeploymentController 가 우리보다 먼저 돌아
                // 이미 취소를 끝냈을 수도 있으므로 CancelledThisFrame 까지 본다.
                bool deployAte = _deployment != null && (_deployment.IsBusy || _deployment.CancelledThisFrame);
                if (!deployAte && !gameOver)
                    Toggle();
            }
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        private void Open()
        {
            // 판이 끝났으면 열지 않는다 — 결과 패널과 겹치지 않게.
            var session = GameSession.Instance;
            if (session != null && session.Result != GameResult.InProgress) return;

            // GameClock.Pause 로만 멈춘다 — Time.timeScale 직접 대입 금지 (CLAUDE.md §4).
            GameClock.Instance?.Pause();
            ApplyOpenState(true);
        }

        private void Close()
        {
            GameClock.Instance?.Resume();   // 직전 배속 그대로 복귀 (1x 초기화 없음)
            ApplyOpenState(false);
        }

        private void ApplyOpenState(bool open)
        {
            IsOpen = open;
            if (_blocker != null) _blocker.SetActive(open);
            if (_panel != null) _panel.SetActive(open);
            if (_settingsPanel != null) _settingsPanel.SetActive(false);   // 열릴 때는 항상 메인 패널부터
        }

        private void OnContinue()
        {
            GameClock.Instance?.Resume();
            ApplyOpenState(false);
        }

        private void OnRestart()
        {
            GameSession.Instance?.RestartStage();
            IsOpen = false;   // 씬이 리로드되며 이 컴포넌트도 새로 Start 한다
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;   // 에디터에서 Application.Quit 는 아무 일도 안 한다
#else
            Application.Quit();
#endif
        }
    }
}