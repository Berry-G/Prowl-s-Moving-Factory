using UnityEngine;
using UnityEngine.UI;
using PMF.Data;
using PMF.Session;

namespace PMF.UI
{
    /// <summary>고용 버튼의 잔액 표시. CanAfford 가 false 면 비활성(회색).</summary>
    public sealed class AffordabilityTint : MonoBehaviour
    {
        private Button _button;
        private Text _label;
        private UnitDefinition _unit;
        private int _lastAmount = -1;

        public void Bind(UnitDefinition unit)
        {
            _unit = unit;
            _button = GetComponent<Button>();
            // 라벨 문구는 HirePanel 이 정한다 (G-16 에서 이름/비용/사거리·DPS 3줄이 되었다).
            // 여기서는 색과 interactable 만 만진다.
            _label = GetComponentInChildren<Text>();
        }

        private void OnEnable()
        {
            var wallet = GameSession.Instance != null ? GameSession.Instance.Wallet : null;
            if (wallet != null)
            {
                wallet.OnChanged += OnWalletChanged;
                OnWalletChanged(wallet.Amount);
            }
        }

        private void OnDisable()
        {
            var session = GameSession.Instance;
            if (session != null && session.Wallet != null)
                session.Wallet.OnChanged -= OnWalletChanged;
        }

        private void OnWalletChanged(int amount)
        {
            Refresh(amount);
        }

        public void Refresh(Wallet wallet) => Refresh(wallet.Amount);

        private void Refresh(int amount)
        {
            if (_unit == null || amount == _lastAmount) return;
            _lastAmount = amount;
            bool canAfford = amount >= _unit.HireCost;
            if (_button != null) _button.interactable = canAfford;
            if (_label != null) _label.color = canAfford ? Color.white : new Color(1f, 1f, 1f, 0.4f);
        }
    }
}
