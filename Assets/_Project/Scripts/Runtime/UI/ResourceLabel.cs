using UnityEngine;
using UnityEngine.UI;
using PMF.Session;

namespace PMF.UI
{
    /// <summary>현재 자원 표시. Wallet.OnChanged 구독으로 변경 시에만 갱신 (매 프레임 문자열 금지).</summary>
    public sealed class ResourceLabel : MonoBehaviour
    {
        private Text _text;
        private Wallet _wallet;
        private int _lastAmount = -1;

        private void Start()
        {
            _text = GetComponent<Text>();

            var session = GameSession.Instance;
            if (session == null || session.Wallet == null)
            {
                Debug.LogError($"[{nameof(ResourceLabel)}] Wallet 없음", this);
                enabled = false;
                return;
            }

            _wallet = session.Wallet;
            _wallet.OnChanged += OnWalletChanged;
            OnWalletChanged(_wallet.Amount);
        }

        private void OnDestroy()
        {
            if (_wallet != null) _wallet.OnChanged -= OnWalletChanged;
        }

        private void OnWalletChanged(int amount)
        {
            if (amount == _lastAmount) return;
            _lastAmount = amount;
            _text.text = $"$ {amount}";
        }
    }
}
