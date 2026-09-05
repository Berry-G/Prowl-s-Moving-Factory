using UnityEngine;

namespace PMF.Data
{
    /// <summary>잡몹 정의. 읽기 전용. 런타임에 필드를 수정하지 마라.</summary>
    [CreateAssetMenu(menuName = "PMF/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Enemy";
        [SerializeField] private float _moveSpeed = 2.0f;          // 셀/초 — 보호대상보다 빨라야 한다
        [SerializeField] private float _maxHealth = 20f;
        [SerializeField] private float _attackRange = 1.0f;
        [SerializeField] private float _attackDamage = 5f;
        [SerializeField] private float _attackInterval = 1.0f;
        [Tooltip("보스인가 (ADR-0020). 보스는 처형되지 않고 대신 치명타를 받는다. " +
                 "프로토타입에는 아직 보스가 없다 — 처형자 규칙이 반쪽으로 남지 않게 미리 뚫어 둔 문이다.")]
        [SerializeField] private bool _isBoss;
        [SerializeField] private GameObject _prefab;

        public string DisplayName => _displayName;
        public float MoveSpeed => _moveSpeed;
        public float MaxHealth => _maxHealth;
        public float AttackRange => _attackRange;
        public float AttackDamage => _attackDamage;
        public float AttackInterval => _attackInterval;

        /// <summary>보스인가. 처형(ADR-0020)의 예외 대상이다.</summary>
        public bool IsBoss => _isBoss;

        public GameObject Prefab => _prefab;
    }
}
