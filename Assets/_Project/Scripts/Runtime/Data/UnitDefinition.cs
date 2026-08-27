using UnityEngine;

namespace PMF.Data
{
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
        public GameObject Prefab => _prefab;
    }
}
