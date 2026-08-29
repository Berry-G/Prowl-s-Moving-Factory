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

        private SelectionController _selection;
        private DeploymentController _deployment;
        private AllyUnit _unit;
        private Font _font;
        private float _timer;

        private GameObject _panel;
        private Text _title;
        private Text _combatLine;
        private Text _dpsLine;
        private Text _stateLine;
        private Button _moveButton;
        private Text _moveLabel;
        private Button _retireButton;
        private Text _retireLabel;
        private Button _upgradeButton;
        private Text _upgradeLabel;

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _deployment = FindAnyObjectByType<DeploymentController>();
            _selection = FindAnyObjectByType<SelectionController>();

            Build();
            Show(null);

            if (_selection != null) _selection.OnSelectionChanged += Show;
        }

        private void OnDestroy()
        {
            // OnEnable/OnDisable 이 아니라 Start/OnDestroy 쌍이다 — 이 패널은 껐다 켜지 않는다.
            if (_selection != null) _selection.OnSelectionChanged -= Show;
        }

        private void Show(AllyUnit unit)
        {
            _unit = unit;
            _panel.SetActive(unit != null);

            // 선택이 풀리면 [이동] 으로 켜 둔 목적지 하이라이트도 같이 끈다.
            if (unit == null) _deployment?.EndRedeployTargeting();
            else Refresh();
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

            // 업그레이드 — 최대 티어면 비활성, 자원이 모자라도 비활성
            bool canUpgrade = _unit.CanUpgrade;
            int cost = _unit.NextUpgradeCost;
            var wallet = GameSession.Instance != null ? GameSession.Instance.Wallet : null;
            bool affordable = canUpgrade && wallet != null && wallet.CanAfford(cost);
            SetButton(_upgradeButton, _upgradeLabel, affordable,
                      canUpgrade ? $"강화  -{cost}" : "최대 티어");
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

        private void OnUpgrade()
        {
            if (_unit != null) _deployment?.RequestUpgrade(_unit);
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
            rt.sizeDelta = new Vector2(300f, 264f);
            _panel.GetComponent<Image>().color = new Color(0.10f, 0.12f, 0.16f, 0.94f);

            _title = MakeText("Title", "", 22, -14f, 30f);
            _combatLine = MakeText("Combat", "", 17, -50f, 24f);
            _dpsLine = MakeText("Dps", "", 17, -76f, 24f);
            _stateLine = MakeText("State", "", 17, -102f, 24f);
            _stateLine.color = new Color(0.78f, 0.82f, 0.9f);

            _moveButton = MakeButton("Btn_Move", -140f, out _moveLabel);
            _moveButton.onClick.AddListener(OnMove);
            _retireButton = MakeButton("Btn_Retire", -186f, out _retireLabel);
            _retireButton.onClick.AddListener(OnRetire);
            _upgradeButton = MakeButton("Btn_Upgrade", -232f, out _upgradeLabel);
            _upgradeButton.onClick.AddListener(OnUpgrade);
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
            rt.sizeDelta = new Vector2(268f, 40f);
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
