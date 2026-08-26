using UnityEngine;
using UnityEngine.UI;
using PMF.Session;

namespace PMF.UI
{
    /// <summary>
    /// 승패 결과 패널. 텍스트 + 재시작 버튼. R 키는 DebugHotkeys 가 담당.
    /// 초기엔 알파 0 으로 숨겨 있고 게임 종료 시 나타난다 (SetActive 를 쓰지 않는다).
    /// </summary>
    public sealed class ResultPanel : MonoBehaviour
    {
        private Text _text;
        private CanvasGroup _group;
        private GameSession _session;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();

            var button = GetComponentInChildren<Button>(true);
            if (button != null)
                button.onClick.AddListener(() =>
                    GameSession.Instance?.RestartStage());

            Hide();
        }

        private void Start()
        {
            _session = FindAnyObjectByType<GameSession>();
            if (_session != null)
                _session.OnGameEnded += HandleGameEnded;
        }

        private void OnDestroy()
        {
            if (_session != null) _session.OnGameEnded -= HandleGameEnded;
        }

        private void Hide()
        {
            if (_group != null)
            {
                _group.alpha = 0f;
                _group.interactable = false;
                _group.blocksRaycasts = false;
            }
        }

        private void HandleGameEnded(GameResult result)
        {
            if (_text == null) _text = GetComponentInChildren<Text>(true);
            if (_text != null)
                _text.text = result == GameResult.Victory ? "VICTORY!" : "DEFEAT...";

            _group.alpha = 1f;
            _group.interactable = true;
            _group.blocksRaycasts = true;
        }
    }
}
