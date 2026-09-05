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
            AddFractional(_resourcePerSecond * Time.deltaTime);
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

        /// <summary>소수 단위 수입. 1 이 쌓일 때마다 정수로 지급하고 나머지는 이월한다 (G-23).
        ///
        /// 시간 수입과 처치 보상이 <b>같은 누적기</b>를 쓴다. 따로 두면 각자 1 미만의 잔돈을 들고 있다가
        /// 판이 끝날 때 버려져, 표시 수입이 설정값보다 조금씩 모자란다.
        /// 처치 보상이 4.5 처럼 정수가 아니어도 되는 이유가 이것이다.</summary>
        public void AddFractional(float amount)
        {
            if (amount <= 0f) return;
            _accumulator += amount;
            if (_accumulator < 1f) return;

            int whole = Mathf.FloorToInt(_accumulator);
            _accumulator -= whole;
            Add(whole);
        }
    }
}
