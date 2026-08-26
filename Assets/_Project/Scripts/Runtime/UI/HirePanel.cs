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
        [SerializeField] private RectTransform _buttonRoot;

        private Action<UnitDefinition> _onPicked;

        public void Show(IReadOnlyList<UnitDefinition> units, Wallet wallet,
                         Action<UnitDefinition> onPicked)
        {
            _onPicked = onPicked;
            BuildButtons(units);
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

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

                var rect = (RectTransform)buttonGo.transform;
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(220f, 44f);
                rect.anchoredPosition = new Vector2(0f, -40f - i * 52f);

                buttonGo.GetComponent<Image>().color = new Color(0.25f, 0.35f, 0.55f);

                var button = buttonGo.GetComponent<Button>();
                var captured = unit;
                button.onClick.AddListener(() => _onPicked?.Invoke(captured));

                // CanAfford false → 비활성(회색)
                buttonGo.AddComponent<AffordabilityTint>().Bind(unit);
            }
        }

        private void ClearButtons()
        {
            if (_buttonRoot == null) return;
            for (int i = _buttonRoot.childCount - 1; i >= 0; i--)
                Destroy(_buttonRoot.GetChild(i).gameObject);
        }
    }
}
