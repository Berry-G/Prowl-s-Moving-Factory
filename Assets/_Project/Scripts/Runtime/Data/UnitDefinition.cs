using UnityEngine;

namespace PMF.Data
{
    /// <summary>업그레이드 한 단계의 정의 (G-05, ADR-0009). Lv1 은 기본 필드(_attackDamage/_attackRange)가 담당.</summary>
    /// <summary>업그레이드 한 단계의 정의 (G-05, ADR-0009 / ADR-0020). Lv1 은 기본 필드(_attackDamage/_attackRange)가 담당.
    ///
    /// <b>선형 배열이 아니라 분기 트리다 (ADR-0020).</b> 배열은 평면으로 두고
    /// <see cref="Level"/>(도달 레벨) 과 <see cref="Branch"/>(갈래) 로 트리를 표현한다 —
    /// 중첩 배열은 Unity 인스펙터에서 다루기 어렵기 때문이다.
    ///
    /// 수치·효과 필드는 <b>0 / false 면 "유지"</b>다. 덮어쓰기가 아니라 누적이므로,
    /// Lv3 티어에 Lv2 에서 얻은 손기술 값을 다시 적을 필요가 없다.</summary>
    [System.Serializable]
    public struct UpgradeTier
    {
        [Tooltip("정보 패널의 버튼에 뜨는 이름. 비우면 'Lv{n} 강화'.")]
        [SerializeField] private string _displayName;
        [SerializeField] private int _cost;

        [Tooltip("이 티어를 사면 도달하는 레벨 (2·3·4). 0 이면 '배열 인덱스 + 2' 로 간주한다 — 옛 선형 데이터 호환.")]
        [SerializeField] private int _level;
        [Tooltip("갈래. 0 = 공통(분기 없음) · 1 = A 분기 · 2 = B 분기. 한 번 고른 갈래는 되돌릴 수 없다.")]
        [SerializeField] private int _branch;

        [Header("수치 (0 이면 유지)")]
        [SerializeField] private float _attackDamage;
        [SerializeField] private float _attackRange;

        [Header("효과 (0 / false 면 유지)")]
        [Tooltip("손기술 — 행동반경(사거리) 안에서 적이 죽으면 처치 보상에 이만큼 가산된다. 0.5 = +50%.")]
        [SerializeField] private float _scavengeBonus;
        [Tooltip("독 — 초당 피해량. 0 보다 크면 이 유닛의 공격이 독을 부여한다.")]
        [SerializeField] private float _poisonDamagePerSecond;
        [Tooltip("독 지속 시간 (초). 같은 대상을 다시 때리면 갱신된다.")]
        [SerializeField] private float _poisonDuration;
        [Tooltip("독 피해가 들어가는 간격 (초). 매 프레임 넣으면 피격 플래시가 계속 터진다 — 0 이면 0.5 로 본다.")]
        [SerializeField] private float _poisonTickInterval;
        [Tooltip("맹독 — 이 독에 걸린 적은 체력을 회복하지 못한다.")]
        [SerializeField] private bool _poisonBlocksHealing;
        [Tooltip("처형 — 잔여 체력 비율이 이 값 미만인 적을 즉사시킨다. 0.05 = 5%.")]
        [SerializeField] private float _executeThreshold;
        [Tooltip("보스는 처형되지 않고 대신 이 배율의 치명타를 받는다. 처형을 쓰면 1 보다 크게 둘 것.")]
        [SerializeField] private float _bossCritMultiplier;
        [Tooltip("범위 공격(메테오) — 착탄 지점 중심 반경 (셀). 0 이면 단일 대상.")]
        [SerializeField] private float _splashRadius;
        [Tooltip("주 대상 외가 받는 피해 비율. 0.6 = 60%. 주 대상은 언제나 100% 다.")]
        [SerializeField] private float _splashDamageRatio;
        [Tooltip("저격(헬파이어) — 사거리 안에서 가장 먼 적을 우선한다.")]
        [SerializeField] private bool _prefersFarthestTarget;

        public string DisplayName => _displayName;
        public int Cost => _cost;

        /// <summary>이 티어가 도달시키는 레벨. 0(미설정)이면 호출자가 인덱스로 보정한다 —
        /// <see cref="UnitDefinition.LevelOf"/> 를 쓸 것.</summary>
        public int Level => _level;

        /// <summary>갈래 (0 = 공통). 0 이 아닌 티어를 사는 순간 유닛의 분기가 확정된다.</summary>
        public int Branch => _branch;

        public float AttackDamage => _attackDamage;
        public float AttackRange => _attackRange;

        public float ScavengeBonus => _scavengeBonus;
        public float PoisonDamagePerSecond => _poisonDamagePerSecond;
        public float PoisonDuration => _poisonDuration;
        public float PoisonTickInterval => _poisonTickInterval;
        public bool PoisonBlocksHealing => _poisonBlocksHealing;
        public float ExecuteThreshold => _executeThreshold;
        public float BossCritMultiplier => _bossCritMultiplier;
        public float SplashRadius => _splashRadius;
        public float SplashDamageRatio => _splashDamageRatio;
        public bool PrefersFarthestTarget => _prefersFarthestTarget;
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
        [Tooltip("근접 병종인가 (G-22). 켜면 사거리 안의 적에게 걸어가 붙어서 때리고, 물 너머는 못 때린다.")]
        [SerializeField] private bool _isMelee;
        [Tooltip("근접 병종이 적에게서 이만큼 떨어진 곳까지만 다가간다 (셀). 겹쳐서 서지 않게 하는 값 (G-22).")]
        [SerializeField] private float _meleeContactDistance = 0.45f;
        [SerializeField] private int _hireCost = 50;
        [Tooltip("재배치 쿨다운 (초). ADR-0008 B안 — 두 종족 모두 3.0 으로 시작, 종족 차등 금지 (G-03).")]
        [SerializeField] private float _redeployCooldown = 3f;
        [Tooltip("회수 환불률. 투입 총액(고용비+업그레이드비) × 이 값. 1.0 이면 배치가 무위험 도박이 된다 (G-04, ADR-0008).")]
        [SerializeField] private float _refundRatio = 0.5f;
        [Tooltip("업그레이드 트리 (ADR-0020). 평면 배열이고 트리 모양은 각 티어의 Level·Branch 가 정한다. " +
                 "Level 을 비워 두면 '인덱스 + 2' 로 읽히므로 옛 선형 데이터도 그대로 돈다.")]
        [SerializeField] private UpgradeTier[] _tiers = System.Array.Empty<UpgradeTier>();
        [SerializeField] private GameObject _prefab;

        public string DisplayName => _displayName;
        public float MoveSpeed => _moveSpeed;
        public float MaxHealth => _maxHealth;
        public float AttackRange => _attackRange;
        public float AttackDamage => _attackDamage;
        public float AttackInterval => _attackInterval;

        /// <summary>근접 병종인가 (G-22, ADR-0017). 사거리 안 적에게 걸어가 붙는다 + 물 너머는 못 때린다.</summary>
        public bool IsMelee => _isMelee;

        /// <summary>근접 접근 시 적과 유지하는 최소 거리 (셀).</summary>
        public float MeleeContactDistance => _meleeContactDistance;

        public int HireCost => _hireCost;
        public float RedeployCooldown => _redeployCooldown;
        public float RefundRatio => _refundRatio;
        public System.Collections.Generic.IReadOnlyList<UpgradeTier> Tiers => _tiers;
        public GameObject Prefab => _prefab;

        /// <summary><paramref name="index"/> 번 티어가 도달시키는 레벨.
        /// <see cref="UpgradeTier.Level"/> 이 0(미설정)이면 <c>index + 2</c> 로 본다 —
        /// 분기가 없던 옛 선형 데이터(쥐 수인)를 그대로 굴리기 위한 폴백이다.</summary>
        public int LevelOf(int index)
        {
            if (index < 0 || index >= _tiers.Length) return 0;
            int level = _tiers[index].Level;
            return level > 0 ? level : index + 2;
        }

        /// <summary>현재 상태에서 살 수 있는 다음 단계 티어들의 인덱스를 <paramref name="result"/> 에 채운다.
        ///
        /// 후보 조건은 둘이다: ① 도달 레벨이 정확히 <c>currentLevel + 1</c> ②
        /// 갈래가 공통(0)이거나, 아직 분기를 안 골랐거나(<paramref name="branch"/> 가 0), 고른 갈래와 같을 것.
        /// 그래서 Lv2→Lv3 에서는 갈래 둘이 모두 나오고, 갈래를 고른 뒤 Lv3→Lv4 에서는 그 갈래만 나온다.</summary>
        /// <param name="currentLevel">현재 레벨 (Lv1 = 1).</param>
        /// <param name="branch">이미 확정된 갈래. 0 = 아직 안 고름.</param>
        public void CollectNextTiers(int currentLevel, int branch,
                                     System.Collections.Generic.List<int> result)
        {
            if (result == null) return;
            result.Clear();

            int next = currentLevel + 1;
            for (int i = 0; i < _tiers.Length; i++)
            {
                if (LevelOf(i) != next) continue;
                int b = _tiers[i].Branch;
                if (b != 0 && branch != 0 && b != branch) continue;
                result.Add(i);
            }
        }
    }
}
