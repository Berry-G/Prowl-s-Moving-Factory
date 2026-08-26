using UnityEngine;

namespace PMF.Data
{
    /// <summary>스테이지 정의. 읽기 전용. 런타임에 필드를 수정하지 마라.</summary>
    [CreateAssetMenu(menuName = "PMF/Stage Definition")]
    public sealed class StageDefinition : ScriptableObject
    {
        [Header("보호대상 (Escortee)")]
        [SerializeField] private float _escorteeSpeed = 1.2f;
        [SerializeField] private float _escorteeMaxHealth = 100f;

        [Header("모체 (MotherSpawner)")]
        [SerializeField] private float _motherSpeed = 0.8f;
        [SerializeField] private float _motherSpawnDelay = 5f;
        [SerializeField] private float _motherSpawnInterval = 3f;
        [SerializeField] private EnemyDefinition _motherSpawnEnemy;
        [SerializeField] private bool _motherFollowsPath = true;      // D-06 토글

        [Header("자원")]
        [SerializeField] private int _startingResource = 150;
        [SerializeField] private float _resourcePerSecond = 8f;
        [SerializeField] private int _shortcutCost = 120;

        [Header("미결정 토글 (GDD §13)")]
        [SerializeField] private bool _alliesCanDieWhileMarching = false;   // D-03 토글
        [SerializeField] private bool _enemiesTargetAllies = false;         // D-04 토글

        public float EscorteeSpeed => _escorteeSpeed;
        public float EscorteeMaxHealth => _escorteeMaxHealth;

        public float MotherSpeed => _motherSpeed;
        public float MotherSpawnDelay => _motherSpawnDelay;
        public float MotherSpawnInterval => _motherSpawnInterval;
        public EnemyDefinition MotherSpawnEnemy => _motherSpawnEnemy;
        public bool MotherFollowsPath => _motherFollowsPath;

        public int StartingResource => _startingResource;
        public float ResourcePerSecond => _resourcePerSecond;
        public int ShortcutCost => _shortcutCost;

        public bool AlliesCanDieWhileMarching => _alliesCanDieWhileMarching;
        public bool EnemiesTargetAllies => _enemiesTargetAllies;
    }
}
