using UnityEngine;
using UnityEngine.UI;
using PMF.Session;

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
                RefreshDifficultyUI();   // 잠금 상태는 열 때마다 다시 판정한다 (G-23)
            });

            var quit = MakeButton(_panel.transform, "Btn_Quit", "게임 종료", TextAnchor.MiddleCenter,
                                  new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -222f), new Vector2(260f, 44f));
            quit.GetComponent<Button>().onClick.AddListener(OnQuit);
        }

        private void BuildSettingsPanel() => BuildTabbedSettings();

        /// <summary>난이도 3버튼 (G-23).
        ///
        /// <b>고르는 순간 지금 판이 그 난이도로 처음부터 다시 시작된다.</b>
        /// 판 도중에 수입 규칙만 슬쩍 바꾸면 그 판의 밸런스를 무엇으로 읽어야 할지 알 수 없고,
        /// 불리할 때 쉬움으로 내려 위기를 넘기는 우회로가 생긴다. 그래서 "바꾼다 = 다시 한다" 로 묶었다.
        ///
        /// 되돌릴 수 없는 조작이므로 <b>반드시 확인 단계를 거친다</b> — 지름길 구매(ShortcutPanel)와 같은 이유다.</summary>
        private void BuildDifficultyRow()
        {
            _difficultyLabel = MakeText(_settingsPanel.transform, "DifficultyLabel", "난이도", 20, TextAnchor.MiddleLeft,
                                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                        new Vector2(0f, -222f), new Vector2(280f, 30f)).GetComponent<Text>();

            // 260 폭에 84짜리 3개 + 간격 4 두 번 = 260. 가운데 정렬이라 x 는 -88 / 0 / +88.
            var order = new[] { PMF.Data.Difficulty.Easy, PMF.Data.Difficulty.Normal, PMF.Data.Difficulty.Hard };
            var fallbackNames = new[] { "쉬움", "보통", "어려움" };
            float[] xs = { -88f, 0f, 88f };

            var stage = GameSession.Instance != null ? GameSession.Instance.Definition : null;

            for (int i = 0; i < order.Length; i++)
            {
                var value = order[i];
                string label = fallbackNames[i];
                if (stage != null && stage.HasTierFor(value))
                {
                    string fromAsset = stage.TierFor(value).DisplayName;
                    if (!string.IsNullOrEmpty(fromAsset)) label = fromAsset;
                }

                var go = MakeButton(_settingsPanel.transform, "Btn_Difficulty_" + value, label, TextAnchor.MiddleCenter,
                                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                    new Vector2(xs[i], -256f), new Vector2(84f, 36f));
                var captured = value;
                go.GetComponent<Button>().onClick.AddListener(() => RequestDifficultyChange(captured));
                _difficultyButtons.Add(go.GetComponent<Button>());
                _difficultyImages.Add(go.GetComponent<Image>());
                _difficultyValues.Add(value);
            }

            _difficultyNotice = MakeText(_settingsPanel.transform, "DifficultyNotice", "", 14, TextAnchor.UpperCenter,
                                         new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                         new Vector2(0f, -300f), new Vector2(300f, 44f)).GetComponent<Text>();
            _difficultyNotice.color = new Color(0.95f, 0.75f, 0.35f);

            BuildDifficultyConfirm();
            RefreshDifficultyUI();
        }

        /// <summary>난이도 변경 확인 패널. 설정 패널을 통째로 덮어 다른 버튼을 못 누르게 한다.</summary>
        private void BuildDifficultyConfirm()
        {
            _difficultyConfirm = new GameObject("DifficultyConfirm", typeof(RectTransform), typeof(Image));
            _difficultyConfirm.transform.SetParent(_settingsPanel.transform, false);
            Rect(_difficultyConfirm, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _difficultyConfirm.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.11f, 0.99f);
            _difficultyConfirm.SetActive(false);

            MakeText(_difficultyConfirm.transform, "ConfirmTitle", "난이도를 바꿀까요?", 24, TextAnchor.MiddleCenter,
                     new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(300f, 40f));

            _difficultyConfirmBody = MakeText(_difficultyConfirm.transform, "ConfirmBody", "", 17, TextAnchor.UpperCenter,
                                              new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                              new Vector2(0f, -190f), new Vector2(300f, 200f)).GetComponent<Text>();

            var ok = MakeButton(_difficultyConfirm.transform, "Btn_ConfirmDifficulty", "바꾸고 다시 시작", TextAnchor.MiddleCenter,
                                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -350f), new Vector2(260f, 44f));
            ok.GetComponent<Image>().color = new Color(0.62f, 0.32f, 0.24f);   // 되돌릴 수 없는 조작은 붉은 계열
            ok.GetComponent<Button>().onClick.AddListener(() =>
            {
                GameSession.SelectedDifficulty = _pendingDifficulty;
                _difficultyConfirm.SetActive(false);
                OnRestart();   // 씬 리로드 — 새 GameSession.Awake 가 바뀐 난이도를 읽는다
            });

            var cancel = MakeButton(_difficultyConfirm.transform, "Btn_CancelDifficulty", "취소", TextAnchor.MiddleCenter,
                                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -406f), new Vector2(260f, 36f));
            cancel.GetComponent<Button>().onClick.AddListener(() => _difficultyConfirm.SetActive(false));
        }

        /// <summary>난이도 버튼을 눌렀을 때. 같은 난이도면 아무 일도 하지 않는다 —
        /// 이미 그 난이도로 돌고 있는데 판을 날릴 이유가 없다.</summary>
        private void RequestDifficultyChange(PMF.Data.Difficulty value)
        {
            var session = GameSession.Instance;
            var current = session != null ? session.ActiveDifficulty : GameSession.SelectedDifficulty;
            if (value == current) return;

            _pendingDifficulty = value;
            if (_difficultyConfirmBody != null)
                _difficultyConfirmBody.text =
                    DifficultyName(current) + "  \u2192  " + DifficultyName(value) + "\n\n" +
                    "지금 판을 처음부터 다시 시작합니다." + "\n" +
                    "배치한 유닛과 모은 자원이 모두 사라집니다." + "\n" +
                    "되돌릴 수 없습니다." + "\n\n" +
                    DifficultyEconomyLine(value);

            if (_difficultyConfirm != null) _difficultyConfirm.SetActive(true);
        }

        /// <summary>확인 화면에 띄울 경제 요약. 무엇이 달라지는지 숫자로 보여야 경고가 경고 구실을 한다.</summary>
        private string DifficultyEconomyLine(PMF.Data.Difficulty value)
        {
            var stage = GameSession.Instance != null ? GameSession.Instance.Definition : null;
            if (stage == null || !stage.HasTierFor(value)) return string.Empty;

            var tier = stage.TierFor(value);
            string perSecond = tier.ResourcePerSecond > 0f
                ? "초당 " + tier.ResourcePerSecond.ToString("0.##") + "G"
                : "시간 수입 없음";
            return "처치당 " + tier.KillReward.ToString("0.##") + "G · " + perSecond;
        }

        private string DifficultyName(PMF.Data.Difficulty value)
        {
            var stage = GameSession.Instance != null ? GameSession.Instance.Definition : null;
            if (stage != null && stage.HasTierFor(value))
            {
                string fromAsset = stage.TierFor(value).DisplayName;
                if (!string.IsNullOrEmpty(fromAsset)) return fromAsset;
            }
            return value == PMF.Data.Difficulty.Easy ? "쉬움"
                 : value == PMF.Data.Difficulty.Hard ? "어려움" : "보통";
        }

        /// <summary>선택 표시를 다시 그린다. 설정 패널을 열 때마다 부른다.</summary>
        private void RefreshDifficultyUI()
        {
            var session = GameSession.Instance;
            var current = session != null ? session.ActiveDifficulty : GameSession.SelectedDifficulty;

            for (int i = 0; i < _difficultyButtons.Count; i++)
            {
                bool isSelected = _difficultyValues[i] == current;
                _difficultyImages[i].color = isSelected
                    ? new Color(0.35f, 0.6f, 0.9f)
                    : new Color(0.25f, 0.3f, 0.4f);
            }

            if (_difficultyNotice != null)
                _difficultyNotice.text = "바꾸면 지금 판을 처음부터 다시 시작합니다.";

            if (_difficultyConfirm != null) _difficultyConfirm.SetActive(false);
        }


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