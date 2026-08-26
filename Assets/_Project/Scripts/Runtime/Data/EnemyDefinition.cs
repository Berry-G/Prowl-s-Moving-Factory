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
        [SerializeField] private GameObject _prefab;

        public string DisplayName => _displayName;
        public float MoveSpeed => _moveSpeed;
        public float MaxHealth => _maxHealth;
        public float AttackRange => _attackRange;
        public float AttackDamage => _attackDamage;
        public float AttackInterval => _attackInterval;
        public GameObject Prefab => _prefab;
    }
}
