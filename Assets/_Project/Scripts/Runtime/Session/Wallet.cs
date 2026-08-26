using System;
using UnityEngine;

namespace PMF.Session
{
    /// <summary>
    /// 자원. float 로 누산하고 정수 부분만 Amount 에 반영한다.
    /// 표시 갱신은 OnChanged 구독으로 변경 시에만.
    /// </summary>
    public sealed class Wallet : MonoBehaviour
    {
        private int _amount;
        private float _accumulator;
        private int _totalGained;
        private int _totalSpent;
        private bool _initialized;

        public int Amount => _amount;
        public int TotalGained => _totalGained;
        public int TotalSpent => _totalSpent;

        [SerializeField] private int _startingResource = 150;
        [SerializeField] private float _resourcePerSecond = 8f;

        public event Action<int> OnChanged;   // 변경 후 잔액

        public void Initialize(int startingResource, float resourcePerSecond)
        {
            _startingResource = startingResource;
            _resourcePerSecond = resourcePerSecond;
            _amount = startingResource;
            _accumulator = 0f;
            _totalGained = startingResource;
            _totalSpent = 0;
            _initialized = true;
            OnChanged?.Invoke(_amount);
        }

        private void Update()
        {
            if (!_initialized) return;

            // Time.deltaTime (unscaledDeltaTime 이 아니다) — 일시정지 중에는 늘지 않는다.
            _accumulator += _resourcePerSecond * Time.deltaTime;
            if (_accumulator >= 1f)
            {
                int whole = Mathf.FloorToInt(_accumulator);
                _accumulator -= whole;
                Add(whole);
            }
        }

        public bool CanAfford(int cost) => _amount >= cost;

        /// <summary>부족하면 아무것도 하지 않고 false. 부분 차감 금지.</summary>
        public bool TrySpend(int cost)
        {
            if (!CanAfford(cost)) return false;
            _amount -= cost;
            _totalSpent += cost;
            OnChanged?.Invoke(_amount);
            return true;
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            _amount += amount;
            _totalGained += amount;
            OnChanged?.Invoke(_amount);
        }
    }
}
