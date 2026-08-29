using UnityEngine;
using UnityEngine.UI;

namespace PMF.UI
{
    /// <summary>PauseMenu 의 uGUI 구성부 (G-12). 헬퍼와 패널 빌드만 담당.</summary>
    public sealed partial class PauseMenu
    {
        private GameObject FindOrCreatePauseButton()
        {
            // ⚠️ 이름을 "Btn_Pause" 로 쓰지 마라. 그건 좌하단 배속 컨트롤의 일시정지 버튼
            // (SceneParts.BuildHudBottomLeft → SpeedControls._pauseButton) 이다. 이름이 겹치면
            // transform.Find 가 그쪽을 집어 배속 버튼이 메뉴를 여는 버튼으로 둔갑한다.
            var found = transform.Find("Btn_PauseMenu");
            if (found != null)
            {
                var btn = found.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(Open);
                return found.gameObject;
            }

            var go = MakeButton(transform, "Btn_PauseMenu", "II", TextAnchor.MiddleCenter,
                                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-44f, -36f), new Vector2(56f, 40f));
            go.GetComponent<Button>().onClick.AddListener(Open);
            return go;
        }

        private void BuildBlocker()
        {
            _blocker = new GameObject("PauseBlocker", typeof(RectTransform), typeof(Image));
            _blocker.transform.SetParent(transform, false);
            Rect(_blocker, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var img = _blocker.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.55f);
            img.raycastTarget = true;   // 메뉴 뒤 게임판·HUD 로 클릭이 새는 것을 차단
            // 형제 순서를 건드리지 않는다. 지금 시점의 마지막 자식이라 먼저 만들어진
            // HUD·고용 패널·중지 버튼을 전부 덮고, 뒤에 만드는 메뉴 패널은 이것 위에 온다.
            _blocker.SetActive(false);
        }

        private void BuildPanel()
        {
            _panel = new GameObject("PausePanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(transform, false);
            Rect(_panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 290f));
            _panel.GetComponent<Image>().color = new Color(0.12f, 0.13f, 0.16f, 0.98f);
            _panel.SetActive(false);

            MakeText(_panel.transform, "Title", "일시정지", 30, TextAnchor.MiddleCenter,
                     new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(300f, 44f));

            var resume = MakeButton(_panel.transform, "Btn_Continue", "계속하기", TextAnchor.MiddleCenter,
                                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(260f, 44f));
            resume.GetComponent<Button>().onClick.AddListener(OnContinue);

            var settings = MakeButton(_panel.transform, "Btn_Settings", "설정", TextAnchor.MiddleCenter,
                                      new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -166f), new Vector2(260f, 44f));
            settings.GetComponent<Button>().onClick.AddListener(() =>
            {
                _panel.SetActive(false);
                _settingsPanel.SetActive(true);
            });

            var quit = MakeButton(_panel.transform, "Btn_Quit", "게임 종료", TextAnchor.MiddleCenter,
                                  new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -222f), new Vector2(260f, 44f));
            quit.GetComponent<Button>().onClick.AddListener(OnQuit);
        }

        private void BuildSettingsPanel()
        {
            _settingsPanel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
            _settingsPanel.transform.SetParent(transform, false);
            Rect(_settingsPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 380f));
            _settingsPanel.GetComponent<Image>().color = new Color(0.12f, 0.13f, 0.16f, 0.98f);
            _settingsPanel.SetActive(false);

            MakeText(_settingsPanel.transform, "Title", "설정", 30, TextAnchor.MiddleCenter,
                     new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(300f, 44f));

            MakeText(_settingsPanel.transform, "VolumeLabel", "효과음 볼륨", 20, TextAnchor.MiddleLeft,
                     new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(280f, 30f));

            var slider = MakeSlider(_settingsPanel.transform, "VolumeSlider",
                                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(260f, 24f));
            if (_sfx != null) slider.value = _sfx.MasterVolume;
            slider.onValueChanged.AddListener(v =>
            {
                if (_sfx != null) _sfx.SetMasterVolume(v);
            });

            // MakeText 는 Button 이 없는 순수 라벨을 만든다. 토글은 버튼이어야 하므로 MakeButton 을 쓰고
            // 그 자식 Label 의 Text 를 잡아 둔다.
            var muteBtn = MakeButton(_settingsPanel.transform, "Btn_Mute", MuteText(), TextAnchor.MiddleCenter,
                                     new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -196f), new Vector2(260f, 36f));
            _muteLabel = muteBtn.GetComponentInChildren<Text>();
            muteBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (_sfx != null) _sfx.ToggleMute();
                _muteLabel.text = MuteText();
            });

            var restart = MakeButton(_settingsPanel.transform, "Btn_Restart", "다시 시작", TextAnchor.MiddleCenter,
                                     new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -252f), new Vector2(260f, 44f));
            restart.GetComponent<Button>().onClick.AddListener(OnRestart);

            var close = MakeButton(_settingsPanel.transform, "Btn_CloseSettings", "닫기", TextAnchor.MiddleCenter,
                                   new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -308f), new Vector2(260f, 36f));
            close.GetComponent<Button>().onClick.AddListener(() =>
            {
                _settingsPanel.SetActive(false);
                _panel.SetActive(true);
            });
        }

        private string MuteText() => _sfx != null && _sfx.IsMuted ? "음소거: 켜짐" : "음소거: 꺼짐";

        private static void Rect(GameObject go, Vector2 anchor, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private GameObject MakeButton(Transform parent, string name, string label, TextAnchor align,
                                      Vector2 anchor, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Rect(go, anchor, anchorMax, pos, size);
            go.GetComponent<Image>().color = new Color(0.25f, 0.3f, 0.4f, 1f);

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            Rect(textGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var text = textGo.AddComponent<Text>();
            text.text = label;
            text.font = _font != null ? _font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20;
            text.alignment = align;
            text.color = Color.white;
            return go;
        }

        private GameObject MakeText(Transform parent, string name, string label, int fontSize, TextAnchor align,
                                    Vector2 anchor, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Rect(go, anchor, anchorMax, pos, size);
            var text = go.AddComponent<Text>();
            text.text = label;
            text.font = _font != null ? _font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = Color.white;
            return go;
        }

        private Slider MakeSlider(Transform parent, string name, Vector2 anchor, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            Rect(go, anchor, anchorMax, pos, size);
            var slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.direction = Slider.Direction.LeftToRight;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            Rect(bg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bg.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.35f, 1f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            Rect(fillArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.offsetMin = new Vector2(6f, 0f);
            fillAreaRt.offsetMax = new Vector2(-6f, 0f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            Rect(fill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fill.GetComponent<Image>().color = new Color(0.4f, 0.7f, 1f, 1f);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            Rect(handleArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var handleAreaRt = handleArea.GetComponent<RectTransform>();
            handleAreaRt.offsetMin = new Vector2(8f, 0f);
            handleAreaRt.offsetMax = new Vector2(-8f, 0f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            Rect(handle, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(12f, 0f));
            handle.GetComponent<Image>().color = Color.white;

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = fill.GetComponent<Image>();
            return slider;
        }
    }
}