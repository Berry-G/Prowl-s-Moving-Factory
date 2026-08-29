using System;
using UnityEngine;
using UnityEngine.UI;
using PMF.Session;

namespace PMF.UI
{
    /// <summary>지름길 구매 확인 패널.
    ///
    /// 예전에는 마커를 클릭하는 즉시 자원이 빠져나갔다. 되돌릴 수 없는 지출인데 확인 단계가 없었다.
    /// 이제 클릭하면 이 패널이 뜨고, 여기서 결정한다.
    ///
    /// 다른 배치 UI(고용 패널)와 마찬가지로 <b>떠 있는 동안 0.1배속</b>이다 — 정밀 조작 구간은
    /// 시간이 느려진다는 규칙을 일관되게 지킨다 (GameClock.EnterUiSlowMotion).
    ///
    /// G-12 의 <see cref="PauseMenu"/> 와 같은 방식으로 자기 UI 를 스스로 만든다.</summary>
    public sealed class ShortcutPanel : MonoBehaviour
    {
        /// <summary>패널이 떠 있는 동안 게임판 입력을 막는다. 일시정지 메뉴와 같은 규칙.</summary>
        public static bool IsOpen { get; private set; }

        // 도메인 리로드 OFF — static 이 다음 플레이로 샌다 (CLAUDE.md §3).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => IsOpen = false;

        private Font _font;
        private GameObject _blocker;
        private GameObject _panel;
        private Text _title;
        private Text _costLabel;
        private Button _buyButton;
        private Text _buyLabel;
        private Action _onConfirm;

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
            ApplyOpen(false);
        }

        /// <param name="cost">지불할 자원.</param>
        /// <param name="affordable">지금 살 수 있는가. 못 사면 버튼이 비활성으로 뜬다 — 왜 안 되는지 보여야 한다.</param>
        public void Show(int cost, bool affordable, Action onConfirm)
        {
            _onConfirm = onConfirm;
            _costLabel.text = $"자원 {cost}";
            _buyButton.interactable = affordable;
            _buyLabel.text = affordable ? "개방" : "자원 부족";
            _buyLabel.color = affordable ? Color.white : new Color(0.55f, 0.57f, 0.62f);

            ApplyOpen(true);
            GameClock.Instance?.EnterUiSlowMotion();   // 배치 UI 와 같은 규칙 (0.1배속)
        }

        public void Hide()
        {
            if (!IsOpen) return;
            ApplyOpen(false);
            _onConfirm = null;
            GameClock.Instance?.ExitUiSlowMotion();
        }

        private void ApplyOpen(bool open)
        {
            IsOpen = open;
            if (_blocker != null) _blocker.SetActive(open);
            if (_panel != null) _panel.SetActive(open);
        }

        private void OnBuy()
        {
            var confirm = _onConfirm;
            Hide();
            confirm?.Invoke();
        }

        // ---------- 구성 ----------

        private void Build()
        {
            _blocker = new GameObject("ShortcutBlocker", typeof(RectTransform), typeof(Image));
            _blocker.transform.SetParent(transform, false);
            Stretch((RectTransform)_blocker.transform);
            var img = _blocker.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.45f);
            img.raycastTarget = true;   // 뒤 게임판으로 클릭이 새지 않게

            _panel = new GameObject("ShortcutPanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(transform, false);
            var rt = (RectTransform)_panel.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(320f, 190f);
            _panel.GetComponent<Image>().color = new Color(0.10f, 0.12f, 0.16f, 0.96f);

            _title = MakeText("Title", "지름길 개방", 24, -18f, 32f, TextAnchor.MiddleCenter);
            _costLabel = MakeText("Cost", "", 20, -56f, 26f, TextAnchor.MiddleCenter);
            MakeText("Hint", "보호대상만 지나간다", 15, -84f, 22f, TextAnchor.MiddleCenter)
                .color = new Color(0.72f, 0.76f, 0.84f);

            _buyButton = MakeButton("Btn_Buy", -120f, out _buyLabel);
            _buyButton.onClick.AddListener(OnBuy);
            Text cancelLabel;
            var cancel = MakeButton("Btn_Cancel", -158f, out cancelLabel);
            cancelLabel.text = "취소";
            cancel.onClick.AddListener(Hide);
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private Text MakeText(string name, string content, int fontSize, float y, float height, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_panel.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(288f, height);

            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private Button MakeButton(string name, float y, out Text label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_panel.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(288f, 34f);
            go.GetComponent<Image>().color = new Color(0.25f, 0.3f, 0.4f, 1f);

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            Stretch((RectTransform)textGo.transform);
            label = textGo.AddComponent<Text>();
            label.font = _font;
            label.fontSize = 18;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            return go.GetComponent<Button>();
        }
    }
}
