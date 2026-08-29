using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PMF.Data;
using PMF.Session;

namespace PMF.UI
{
    /// <summary>
    /// 고용 패널. 마을 클릭 시 뜨고 유닛 버튼을 동적으로 만든다.
    /// 로직을 갖지 않는다 — 선택 결과를 콜백으로 넘기는 뷰다.
    /// </summary>
    public sealed class HirePanel : MonoBehaviour
    {
        private const float ButtonHeight = 64f;   // 3줄 라벨이 들어간다 (G-16)
        private const float ButtonStride = 72f;

        [SerializeField] private RectTransform _buttonRoot;

        private Action<UnitDefinition> _onPicked;
        private Action<UnitDefinition> _onHover;

        // ClearButtons 가 _buttonRoot 의 자식을 전부 지우면 SceneParts 가 만든 "고용" 제목까지 사라진다.
        // 우리가 만든 버튼만 기억했다가 그것만 지운다.
        private readonly List<GameObject> _buttons = new List<GameObject>();

        /// <param name="onHover">버튼에 포인터가 올라가면 그 정의를, 벗어나면 null 을 넘긴다 (G-16 사거리 미리보기).</param>
        public void Show(IReadOnlyList<UnitDefinition> units, Wallet wallet,
                         Action<UnitDefinition> onPicked, Action<UnitDefinition> onHover = null)
        {
            _onPicked = onPicked;
            _onHover = onHover;
            BuildButtons(units);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _onHover?.Invoke(null);   // 패널이 닫히는데 미리보기 원만 남는 것을 막는다
            gameObject.SetActive(false);
        }

        private void BuildButtons(IReadOnlyList<UnitDefinition> units)
        {
            ClearButtons();

            if (_buttonRoot == null) _buttonRoot = transform as RectTransform;

            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                var buttonGo = new GameObject($"Btn_{unit.DisplayName}",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                buttonGo.transform.SetParent(_buttonRoot, false);
                _buttons.Add(buttonGo);

                var rect = (RectTransform)buttonGo.transform;
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(220f, ButtonHeight);
                rect.anchoredPosition = new Vector2(0f, -40f - i * ButtonStride);

                buttonGo.GetComponent<Image>().color = new Color(0.25f, 0.35f, 0.55f);

                // 라벨이 없으면 AffordabilityTint.Bind 가 GetComponentInChildren<Text> 로 못 찾아 글자가 안 보인다.
                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                labelGo.transform.SetParent(buttonGo.transform, false);
                var labelRect = (RectTransform)labelGo.transform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                var label = labelGo.GetComponent<Text>();
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 16;
                label.lineSpacing = 0.95f;
                label.text = LabelFor(unit);

                var button = buttonGo.GetComponent<Button>();
                var captured = unit;
                button.onClick.AddListener(() => _onPicked?.Invoke(captured));

                // hover → 마을 주변에 이 유닛의 사거리 원 (G-16). 무엇을 그릴지는 호출자가 정한다.
                buttonGo.AddComponent<HoverRelay>()
                        .Bind(entered => _onHover?.Invoke(entered ? captured : null));

                // CanAfford false → 비활성(회색)
                buttonGo.AddComponent<AffordabilityTint>().Bind(unit);
            }
        }

        /// <summary>이름 / 비용 / 사거리·DPS 3줄 (G-16).
        /// <b>DPS 는 공격력÷간격 계산값이다 — SO 에 필드를 만들지 마라.</b></summary>
        private static string LabelFor(UnitDefinition unit)
        {
            float dps = unit.AttackInterval > 0f ? unit.AttackDamage / unit.AttackInterval : 0f;
            return $"{unit.DisplayName}\n$ {unit.HireCost}\n사거리 {unit.AttackRange:F1} · DPS {dps:F1}";
        }

        private void ClearButtons()
        {
            foreach (var go in _buttons)
                if (go != null) Destroy(go);
            _buttons.Clear();
        }
    }
}
