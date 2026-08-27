using UnityEngine;

namespace PMF.Data
{
    /// <summary>업그레이드 한 단계의 정의 (G-05, ADR-0009). Lv1 은 기본 필드(_attackDamage/_attackRange)가 담당.</summary>
    [System.Serializable]
    public struct UpgradeTier
    {
        [SerializeField] private int _cost;
        [SerializeField] private float _attackDamage;
        [SerializeField] private float _attackRange;

        public int Cost => _cost;
        public float AttackDamage => _attackDamage;
        public float AttackRange => _attackRange;
    }

    /// <summary>아군 유닛 정의. 읽기 전용. 런타임에 필드를 수정하지 마라.</summary>
    [CreateAssetMenu(menuName = "PMF/Unit Definition")]
    public sealed class UnitDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Ally";
        [SerializeField] private float _moveSpeed = 2.5f;          // 행군 속도 (셀/초)
        [SerializeField] private float _maxHealth = 50f;
        [SerializeField] private float _attackRange = 3.5f;
        [SerializeField] private float _attackDamage = 10f;
        [SerializeField] private float _attackInterval = 0.8f;
        [SerializeField] private int _hireCost = 50;
        [Tooltip("재배치 쿨다운 (초). ADR-0008 B안 — 두 종족 모두 3.0 으로 시작, 종족 차등 금지 (G-03).")]
        [SerializeField] private float _redeployCooldown = 3f;
        [Tooltip("회수 환불률. 투입 총액(고용비+업그레이드비) × 이 값. 1.0 이면 배치가 무위험 도박이 된다 (G-04, ADR-0008).")]
        [SerializeField] private float _refundRatio = 0.5f;
        [Tooltip("업그레이드 티어들 (인덱스 0 = Lv2, 1 = Lv3). 강화 축은 공격력·사거리뿐 (ADR-0009). 최대 2단계.")]
        [SerializeField] private UpgradeTier[] _tiers = System.Array.Empty<UpgradeTier>();
        [SerializeField] private GameObject _prefab;

        public string DisplayName => _displayName;
        public float MoveSpeed => _moveSpeed;
        public float MaxHealth => _maxHealth;
        public float AttackRange => _attackRange;
        public float AttackDamage => _attackDamage;
        public float AttackInterval => _attackInterval;
        public int HireCost => _hireCost;
        public float RedeployCooldown => _redeployCooldown;
        public float RefundRatio => _refundRatio;
        public System.Collections.Generic.IReadOnlyList<UpgradeTier> Tiers => _tiers;
        public GameObject Prefab => _prefab;
    }
}
