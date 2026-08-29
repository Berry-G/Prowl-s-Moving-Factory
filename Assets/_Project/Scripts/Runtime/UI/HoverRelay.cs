using UnityEngine;
using UnityEngine.EventSystems;

namespace PMF.UI
{
    /// <summary>uGUI 요소의 포인터 진입/이탈을 콜백 하나로 넘긴다 (G-16 고용 버튼 hover).
    /// 이 컴포넌트는 아무것도 판단하지 않는다 — 무엇을 보여줄지는 받는 쪽이 정한다.</summary>
    public sealed class HoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private System.Action<bool> _onHover;

        public void Bind(System.Action<bool> onHover) => _onHover = onHover;

        public void OnPointerEnter(PointerEventData eventData) => _onHover?.Invoke(true);

        public void OnPointerExit(PointerEventData eventData) => _onHover?.Invoke(false);

        // 버튼이 파괴될 때 hover 가 켜진 채로 남지 않게 한다 (고용 확정 시 패널이 통째로 사라진다).
        private void OnDisable() => _onHover?.Invoke(false);
    }
}
