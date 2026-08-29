using UnityEngine;
using UnityEngine.UI;
using PMF.Session;

namespace PMF.UI
{
    /// <summary>현재 자원 표시. Wallet.OnChanged 구독으로 변경 시에만 갱신 (매 프레임 문자열 금지).</summary>
    public sealed class ResourceLabel : MonoBehaviour
    {
        private static readonly Color Neutral = Color.white;
        private static readonly Color GainColor = new Color(0.45f, 1f, 0.55f);
        private static readonly Color SpendColor = new Color(1f, 0.45f, 0.42f);
        private const float FlashSeconds = 0.35f;

        /// <summary>이 값 미만의 증가는 강조하지 않는다.
        /// Wallet 의 초당 수입은 <b>+1 씩</b> 들어오므로(누산기가 1을 넘을 때마다) 그대로 두면
        /// 라벨이 쉬지 않고 번쩍인다. 강조는 플레이어가 일으킨 변동에만 쓴다.
        /// 감소는 전부 플레이어 행동(고용·강화)이므로 크기와 무관하게 강조한다.</summary>
        private const int MinGainToFlash = 2;

        private Text _text;
        private Wallet _wallet;
        private int _lastAmount = -1;

        private float _flashRemaining;
        private Color _flashColor = Neutral;

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
            _text.color = Neutral;   // 첫 표시는 강조하지 않는다
            _flashRemaining = 0f;
        }

        private void OnDestroy()
        {
            if (_wallet != null) _wallet.OnChanged -= OnWalletChanged;
        }

        private void Update()
        {
            if (_flashRemaining <= 0f) return;

            // 일시정지(timeScale=0) 중에도 강조가 굳지 않도록 실시간을 쓴다.
            _flashRemaining -= Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(_flashRemaining / FlashSeconds);
            _text.color = Color.Lerp(Neutral, _flashColor, k);
        }

        private void OnWalletChanged(int amount)
        {
            if (amount == _lastAmount) return;

            // 늘면 초록, 줄면 붉게 짧게 강조 (G-14). 첫 갱신(_lastAmount<0)은 제외.
            int delta = amount - _lastAmount;
            if (_lastAmount >= 0 && (delta < 0 || delta >= MinGainToFlash))
            {
                _flashColor = delta > 0 ? GainColor : SpendColor;
                _flashRemaining = FlashSeconds;
            }

            _lastAmount = amount;
            _text.text = $"$ {amount}";
        }
    }
}
