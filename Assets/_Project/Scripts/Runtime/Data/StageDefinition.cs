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
        [Tooltip("모체가 내보낼 적 목록과 가중치 (G-17). 항목이 하나면 그 하나만 나온다.")]
        [SerializeField] private SpawnEntry[] _spawnTable = new SpawnEntry[0];
        [Tooltip("보호대상 진행도(0~1) → 스폰되는 적의 최대 체력 배율 (회의 결정 13).\n" +
                 "모체와 보호대상 속도가 둘 다 고정이므로 거리는 진행도의 함수다.\n" +
                 "⚠️ 실시간 거리에 반응시키지 마라 — 고무줄 난이도로 읽힌다.")]
        [SerializeField] private AnimationCurve _enemyHealthByProgress = AnimationCurve.Constant(0f, 1f, 1f);
        [SerializeField] private bool _motherFollowsPath = true;      // D-06 토글

        [Header("자원")]
        [SerializeField] private int _startingResource = 150;
        [SerializeField] private float _resourcePerSecond = 8f;
        [SerializeField] private int _shortcutCost = 120;

        [Header("배치 UI")]
        [Tooltip("배치 UI(고용 패널 등)가 떠 있는 동안의 슬로우모션 배속. 감각 수치 — P-21 에서 튜닝 대상.")]
        [SerializeField] private float _uiSlowMotionScale = 0.1f;
        [Tooltip("사격 선 연출 지속 시간 (초, 게임시간 기준). G-07.")]
        [SerializeField] private float _shotLineSeconds = 0.07f;

        [Header("격파 연출 (G-08)")]
        [Tooltip("피격 플래시 지속 시간 (초, 게임시간 기준).")]
        [SerializeField] private float _hitFlashSeconds = 0.08f;
        [Tooltip("격파 시 흩어질 부품 조각 수 (GDD §12: 폭발이 아니라 부품 흩어짐).")]
        [SerializeField] private int _debrisCount = 5;
        [Tooltip("부품 조각 수명 (초, 게임시간 기준).")]
        [SerializeField] private float _debrisSeconds = 0.35f;
        [Tooltip("체력바를 만피에서 숨길 것인가 (G-09). 기본 true.")]
        [SerializeField] private bool _healthBarHideWhenFull = true;
        [Tooltip("스폰 예고 시간 (초, 게임시간 기준). 스폰 간격에 포함된다 (G-10, GDD §7).")]
        [SerializeField] private float _spawnTelegraphSeconds = 0.6f;
        [Tooltip("전역 효과음 볼륨 (G-11). 음소거 토글은 SfxPlayer(M 키).")]
        [SerializeField, Range(0f, 1f)] private float _masterVolume = 1f;

        [Header("미결정 토글 (GDD §13)")]
        [SerializeField] private bool _alliesCanDieWhileMarching = false;   // D-03 토글
        [SerializeField] private bool _enemiesTargetAllies = false;         // D-04 토글

        public float EscorteeSpeed => _escorteeSpeed;
        public float EscorteeMaxHealth => _escorteeMaxHealth;

        public float MotherSpeed => _motherSpeed;
        public float MotherSpawnDelay => _motherSpawnDelay;
        public float MotherSpawnInterval => _motherSpawnInterval;
        public System.Collections.Generic.IReadOnlyList<SpawnEntry> SpawnTable => _spawnTable;
        public bool MotherFollowsPath => _motherFollowsPath;

        /// <summary>진행도(0~1)에 따른 적 체력 배율. 곡선이 비어 있으면 1배.</summary>
        public float EnemyHealthMultiplierAt(float progress01)
        {
            if (_enemyHealthByProgress == null || _enemyHealthByProgress.length == 0) return 1f;
            return Mathf.Max(0.01f, _enemyHealthByProgress.Evaluate(Mathf.Clamp01(progress01)));
        }

        public int StartingResource => _startingResource;
        public float ResourcePerSecond => _resourcePerSecond;
        public int ShortcutCost => _shortcutCost;

        public bool AlliesCanDieWhileMarching => _alliesCanDieWhileMarching;
        public bool EnemiesTargetAllies => _enemiesTargetAllies;
        public float UiSlowMotionScale => _uiSlowMotionScale;
        public float ShotLineSeconds => _shotLineSeconds;
        public float HitFlashSeconds => _hitFlashSeconds;
        public int DebrisCount => _debrisCount;
        public float DebrisSeconds => _debrisSeconds;
        public bool HealthBarHideWhenFull => _healthBarHideWhenFull;
        public float SpawnTelegraphSeconds => _spawnTelegraphSeconds;
        public float MasterVolume => _masterVolume;
    }
}
