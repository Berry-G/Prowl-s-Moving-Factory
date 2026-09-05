using UnityEngine;

namespace PMF.Data
{
    /// <summary>난이도. 설정 패널에서 고른다 (G-23).
    ///
    /// <b>이 게임의 난이도는 경제로 조절한다 — 적 체력이 아니다.</b>
    /// 회의 결정 12 는 *진행도에 따른* 난이도 배율을 적 체력에만 걸라고 정했고
    /// (`StageDefinition.EnemyHealthMultiplierAt`), 그건 그대로 유효하다.
    /// 여기 난이도는 그 위에 얹히는 <b>플레이어 선택</b>이고 <b>수입만</b> 바꾼다.
    /// 둘을 같이 곱하지 않는 이유는 결정 12 와 같다 — 곱하면 난이도가 예측 불가능해진다.
    ///
    /// 값을 바꾸지 마라. 직렬화된 설정 선택이 이 숫자에 묶인다.</summary>
    public enum Difficulty
    {
        Easy   = 0,
        Normal = 1,
        Hard   = 2,
    }

    /// <summary>난이도 한 종의 경제 수치 (G-23).
    /// 밸런스 수치이므로 코드 상수가 아니라 StageDefinition 에서 저작한다 (CLAUDE.md §4 매직 넘버 금지).</summary>
    [System.Serializable]
    public struct DifficultyTier
    {
        [SerializeField] private Difficulty _difficulty;
        [Tooltip("화면에 보일 이름.")]
        [SerializeField] private string _displayName;
        [Tooltip("적 1기 격파 보상. 소수를 허용한다 — 누적해서 정수 단위로 지급한다 (Wallet).")]
        [SerializeField] private float _killReward;
        [Tooltip("초당 자원. 0 이면 시간 수입이 아예 없다 = 잡아야만 번다.")]
        [SerializeField] private float _resourcePerSecond;

        public Difficulty Difficulty => _difficulty;
        public string DisplayName => _displayName;
        public float KillReward => _killReward;
        public float ResourcePerSecond => _resourcePerSecond;
    }
}
