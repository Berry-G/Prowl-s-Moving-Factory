using UnityEngine;
using UnityEngine.UI;
using PMF.Actors;
using PMF.Session;

namespace PMF.UI
{
    /// <summary>유닛 정보 패널 (G-15). 아군을 고르면 뜨고, 해제하면 닫힌다.
    ///
    /// G-12·G-14 와 같은 방식으로 <b>자기 UI 를 스스로 만든다.</b>
    /// 표시 값은 전부 현재 티어가 반영된 런타임 값(<see cref="AllyUnit.Attacker"/>)에서 읽는다.
    /// <b>DPS 는 공격력/간격으로 계산한다 — SO 필드를 만들지 마라</b> (파생값, G-15 작업 4).</summary>
    public sealed class UnitInfoPanel : MonoBehaviour
    {
        private const float RefreshInterval = 0.1f;

        /// <summary>한 단계에서 고를 수 있는 강화의 최대 수. 고양이 Lv3 분기가 둘이라 2다 (ADR-0020).</summary>
        private const int MaxUpgradeOptions = 2;

        // 강화 버튼 줄의 치수. 분기일 때만 좌우 2열이 되므로 줄 자체는 항상 한 칸 높이다.
        private const float ButtonWidth = 268f;        // 패널 안쪽 폭 = 다른 버튼들과 같은 폭
        private const float SplitGap = 8f;             // 좌우로 나눌 때 두 버튼 사이 간격
        private const float UpgradeRowY = -248f;

        private SelectionController _selection;
        private DeploymentController _deployment;
        private AllyUnit _unit;
        private Font _font;
        private GameClock _clock;
        private float _timer;
        private bool _slowMotionHeld;   // GameClock 의 Enter/Exit 짝을 이 패널이 하나만 들고 있게 한다

        private GameObject _panel;
        private Text _title;
        private Text _combatLine;
        private Text _dpsLine;
        private Text _stateLine;
        private Text _hintLine;
        private Button _moveButton;
        private Text _moveLabel;
        private Button _retireButton;
        private Text _retireLabel;

        /// <summary>강화 버튼. Lv2→Lv3 분기(ADR-0020)에서만 둘 다 뜨고, 그 외에는 0번만 쓴다.</summary>
        private readonly Button[] _upgradeButtons = new Button[MaxUpgradeOptions];
        private readonly Text[] _upgradeLabels = new Text[MaxUpgradeOptions];

        /// <summary>지금 강화 버튼이 좌우 2열로 놓여 있는가. 레이아웃을 <b>바뀔 때만</b> 다시 잡으려고 든다 —
        /// Refresh 는 0.1초마다 도는데 매번 RectTransform 에 대입할 이유가 없다.</summary>
        private bool _upgradeRowIsSplit;

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _deployment = FindAnyObjectByType<DeploymentController>();
            _selection = FindAnyObjectByType<SelectionController>();

            Build();
            Show(null);

            if (_selection != null) _selection.OnSelectionChanged += Show;

            // 배속을 바꾸면 창을 닫는다 (2026-09-02). 창이 떠 있는 한 0.1배속이라 배속 변경이 무의미하다.
            _clock = GameClock.Instance;
            if (_clock != null) _clock.OnDismissUiWindows += Dismiss;
        }

        private void OnDestroy()
        {
            // OnEnable/OnDisable 이 아니라 Start/OnDestroy 쌍이다 — 이 패널은 껐다 켜지 않는다.
            if (_selection != null) _selection.OnSelectionChanged -= Show;
            if (_clock != null) _clock.OnDismissUiWindows -= Dismiss;

            // 패널이 열린 채로 파괴되면(씬 리로드 등) 슬로우모션이 걸린 채 남는다. 반드시 되돌린다.
            ReleaseSlowMotion();
        }

        private void Show(AllyUnit unit)
        {
            _unit = unit;
            bool open = unit != null;
            _panel.SetActive(open);

            // 확정 규칙 6 — 윈도우형 UI 가 뜨면 0.1배속 (ADR-0019).
            // 이 패널도 [이동]·[회수]·[강화] 를 누르는 정밀 조작 창이므로 고용 패널·지름길 패널과 같은 규칙이다.
            if (open) AcquireSlowMotion();
            else ReleaseSlowMotion();

            // 선택이 풀리면 [이동] 으로 켜 둔 목적지 하이라이트도 같이 끈다.
            if (unit == null) _deployment?.EndRedeployTargeting();
            else Refresh();
        }

        /// <summary>슬로우모션을 <b>한 번만</b> 잡는다. Show 는 선택이 바뀔 때마다 불리는데
        /// (유닛 A → 유닛 B) 그때마다 Enter 를 걸면 Exit 와 짝이 어긋나 0.1배속에 갇힌다.</summary>
        private void AcquireSlowMotion()
        {
            if (_slowMotionHeld) return;
            _slowMotionHeld = true;
            GameClock.Instance?.EnterUiSlowMotion();
        }

        private void ReleaseSlowMotion()
        {
            if (!_slowMotionHeld) return;
            _slowMotionHeld = false;
            GameClock.Instance?.ExitUiSlowMotion();
        }

        private void Update()
        {
            if (_unit == null)
            {
                // 회수·격파로 파괴되면 Unity null 이 된다. 패널을 닫는다.
                if (_panel.activeSelf) Show(null);
                return;
            }

            _timer -= Time.unscaledDeltaTime;   // 일시정지 중에도 표시가 굳지 않게
            if (_timer > 0f) return;
            _timer = RefreshInterval;
            Refresh();
        }

        private void Refresh()
        {
            var def = _unit.Definition;
            var atk = _unit.Attacker;
            if (def == null || atk == null) return;

            _title.text = $"{def.DisplayName}   Lv{_unit.TierLevel + 1}";
            _combatLine.text = $"사거리 {atk.Range:F1}      공격력 {atk.Damage:F0}";

            // DPS 는 계산값이다. 간격이 0 이하면 나눗셈을 하지 않는다.
            float dps = atk.Interval > 0f ? atk.Damage / atk.Interval : 0f;
            _dpsLine.text = $"공격 간격 {atk.Interval:F2}s      DPS {dps:F1}";

            _stateLine.text = "상태  " + StateText();

            // 이동 — 재배치 쿨다운 중에는 비활성 + 남은 시간 표시 (G-15 DoD)
            bool canMove = _unit.CanRedeployNow;
            SetButton(_moveButton, _moveLabel, canMove,
                      canMove ? "이동" : $"이동 {_unit.RedeployCooldownRemaining:F1}초 후");

            SetButton(_retireButton, _retireLabel, true, $"회수  +{_unit.RefundAmount}");

            RefreshUpgradeButtons();
        }

        /// <summary>강화 버튼들 (ADR-0020). 후보가 둘이면 갈래를 고르는 것이고, 그 선택은 되돌릴 수 없다 —
        /// 확정 결정 17 "플레이어는 선택에 책임을 진다". 그래서 그때만 경고 한 줄을 띄운다.</summary>
        private void RefreshUpgradeButtons()
        {
            var options = _unit.UpgradeOptions;
            var wallet = GameSession.Instance != null ? GameSession.Instance.Wallet : null;
            int shown = Mathf.Min(options.Count, MaxUpgradeOptions);

            // 양자택일은 위아래가 아니라 좌우로 놓는다. 세로로 쌓으면 "다음 단계 두 개"처럼 읽히지만,
            // 나란히 놓으면 한 줄 안에서 <b>둘 중 하나</b>라는 것이 형태만으로 드러난다.
            SetUpgradeRowSplit(shown > 1);

            // 후보가 없어도 0번 버튼은 "최대 티어" 를 보여 주려고 남긴다 — 패널이 갑자기 짧아지지 않게.
            for (int i = 0; i < MaxUpgradeOptions; i++)
            {
                bool used = i < shown || (i == 0 && shown == 0);
                _upgradeButtons[i].gameObject.SetActive(used);
                if (!used) continue;

                if (shown == 0)
                {
                    SetButton(_upgradeButtons[i], _upgradeLabels[i], false, "최대 티어");
                    continue;
                }

                int cost = _unit.UpgradeOptionCost(i);
                bool affordable = wallet != null && wallet.CanAfford(cost);
                SetButton(_upgradeButtons[i], _upgradeLabels[i], affordable,
                          $"{_unit.UpgradeOptionName(i)}  -{cost}");
            }

            bool branching = shown > 1;
            _hintLine.gameObject.SetActive(branching);
            if (branching) _hintLine.text = "갈래는 한 번 고르면 바꿀 수 없다";
        }

        /// <summary>강화 버튼 줄을 좌우 2열(<paramref name="split"/>)과 한 칸 사이에서 전환한다.
        /// 상태가 실제로 바뀔 때만 RectTransform 을 건드린다.</summary>
        private void SetUpgradeRowSplit(bool split)
        {
            // Build 가 버튼을 비분할 모양(전체 폭)으로 만들어 두므로 초기 상태와 어긋나지 않는다.
            if (split == _upgradeRowIsSplit) return;
            _upgradeRowIsSplit = split;

            float half = (ButtonWidth - SplitGap) * 0.5f;
            for (int i = 0; i < MaxUpgradeOptions; i++)
            {
                var rt = (RectTransform)_upgradeButtons[i].transform;
                if (split)
                {
                    // 0번이 왼쪽, 1번이 오른쪽. 후보 순서(= SO 의 배열 순서)가 그대로 좌우 순서다.
                    float x = (i == 0 ? -1f : 1f) * (half + SplitGap) * 0.5f;
                    rt.anchoredPosition = new Vector2(x, UpgradeRowY);
                    rt.sizeDelta = new Vector2(half, 40f);
                }
                else
                {
                    rt.anchoredPosition = new Vector2(0f, UpgradeRowY);
                    rt.sizeDelta = new Vector2(ButtonWidth, 40f);
                }

                // 폭이 절반이 되므로 "처형자 II  -160" 같은 긴 라벨이 잘리지 않게 글자도 같이 줄인다.
                _upgradeLabels[i].fontSize = split ? 16 : 18;
            }
        }

        private string StateText()
        {
            if (_unit.IsMarching) return "행군 중";
            if (!_unit.IsDeployed) return "대기";
            return _unit.CanRedeployNow
                ? "배치됨"
                : $"배치됨 (이동 쿨다운 {_unit.RedeployCooldownRemaining:F1}s)";
        }

        // ---------- 조작 ----------

        private void OnMove()
        {
            if (_unit != null) _deployment?.BeginRedeployTargeting(_unit);
        }

        private void OnRetire()
        {
            if (_unit == null) return;
            _unit.Retire();
            _selection?.Clear();   // 회수한 유닛을 계속 선택해 둘 이유가 없다
        }

        /// <summary>창을 닫는다. X 버튼과 "배속을 바꿨다" 신호가 같이 쓴다.
        /// 이 패널은 <b>선택 상태에 매달려 있으므로</b> 선택을 푸는 것이 곧 닫는 것이다 —
        /// <c>_panel.SetActive(false)</c> 로 직접 끄면 유닛이 선택된 채 창만 사라져
        /// 슬로우모션이 걸린 채 남는다.</summary>
        private void Dismiss()
        {
            if (_unit == null) return;
            _selection?.Clear();
        }

        private void OnUpgrade(int option)
        {
            if (_unit != null) _deployment?.RequestUpgrade(_unit, option);
            Refresh();
        }

        // ---------- 구성 ----------

        private void Build()
        {
            _panel = new GameObject("UnitInfoPanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(transform, false);
            var rt = (RectTransform)_panel.transform;
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-16f, 150f);
            // 강화 버튼이 항상 한 줄이므로(분기는 좌우로 갈라진다) 마지막 줄 아래 여백만 남긴다.
            rt.sizeDelta = new Vector2(300f, 302f);
            _panel.GetComponent<Image>().color = new Color(0.10f, 0.12f, 0.16f, 0.94f);

            _title = MakeText("Title", "", 22, -14f, 30f);
            _combatLine = MakeText("Combat", "", 17, -50f, 24f);
            _dpsLine = MakeText("Dps", "", 17, -76f, 24f);
            _stateLine = MakeText("State", "", 17, -102f, 24f);
            _stateLine.color = new Color(0.78f, 0.82f, 0.9f);
            _hintLine = MakeText("Hint", "", 14, -128f, 20f);
            _hintLine.color = new Color(0.95f, 0.78f, 0.42f);   // 되돌릴 수 없는 선택이라는 신호

            _moveButton = MakeButton("Btn_Move", -156f, out _moveLabel);
            _moveButton.onClick.AddListener(OnMove);
            _retireButton = MakeButton("Btn_Retire", -202f, out _retireLabel);
            _retireButton.onClick.AddListener(OnRetire);

            // 강화 버튼 2개. 둘 다 같은 줄(UpgradeRowY)에 겹쳐 만들고, 분기일 때만 좌우로 갈라진다
            // (→ SetUpgradeRowSplit). 분기가 없는 단계에서는 0번만 켜지므로 겹쳐 있어도 보이지 않는다.
            // 람다가 루프 변수를 잡지 않게 지역 복사를 만든다 — 안 그러면 둘 다 마지막 값으로 붙는다.
            for (int i = 0; i < MaxUpgradeOptions; i++)
            {
                int option = i;
                _upgradeButtons[i] = MakeButton($"Btn_Upgrade{i}", UpgradeRowY, out _upgradeLabels[i]);
                _upgradeButtons[i].onClick.AddListener(() => OnUpgrade(option));
            }

            MakeCloseButton(_panel.transform, _font).onClick.AddListener(Dismiss);
        }

        /// <summary>창 우측 위 모서리의 X 버튼 (2026-09-02).
        ///
        /// 창을 닫는 방법이 ESC·우클릭·다른 곳 클릭처럼 <b>화면에 안 보이는 것들뿐이면</b>
        /// 닫는 법을 배우기 전까지 창이 계속 떠 있고, 창이 떠 있는 동안은 0.1배속이라 판이 느려진다.
        /// 눈에 보이는 닫기 수단을 하나 둔다.
        ///
        /// 윈도우형 UI 를 새로 만들면 이걸 같이 붙여라 — <c>public static</c> 인 이유다.</summary>
        public static Button MakeCloseButton(Transform panel, Font font)
        {
            var go = new GameObject("Btn_Close", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(panel, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-6f, -6f);
            rt.sizeDelta = new Vector2(26f, 26f);
            go.GetComponent<Image>().color = new Color(0.32f, 0.20f, 0.22f, 1f);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var lrt = (RectTransform)labelGo.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            var label = labelGo.AddComponent<Text>();
            label.text = "X";
            label.font = font;
            label.fontSize = 16;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.95f, 0.85f, 0.85f);
            label.raycastTarget = false;

            return go.GetComponent<Button>();
        }

        private Text MakeText(string name, string content, int fontSize, float y, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_panel.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(268f, height);

            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleLeft;
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
            rt.sizeDelta = new Vector2(ButtonWidth, 40f);
            go.GetComponent<Image>().color = new Color(0.25f, 0.3f, 0.4f, 1f);

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var trt = (RectTransform)textGo.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            label = textGo.AddComponent<Text>();
            label.font = _font;
            label.fontSize = 18;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            return go.GetComponent<Button>();
        }

        private static void SetButton(Button button, Text label, bool enabled, string text)
        {
            button.interactable = enabled;
            label.text = text;
            // interactable 만으로는 그레이박스 색에서 차이가 잘 안 보인다 — 글자도 같이 죽인다.
            label.color = enabled ? Color.white : new Color(0.55f, 0.57f, 0.62f);
        }
    }
}
