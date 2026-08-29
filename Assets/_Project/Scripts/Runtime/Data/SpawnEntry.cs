using UnityEngine;

namespace PMF.Data
{
    /// <summary>스폰 테이블의 한 줄 (G-17). 어떤 적을 얼마의 가중치로 낼 것인가.
    /// 항목이 하나뿐이면 그 하나만 계속 나온다 — 단일 참조였던 기존 동작과 같다.</summary>
    [System.Serializable]
    public struct SpawnEntry
    {
        [SerializeField] private EnemyDefinition _enemy;
        [Tooltip("추첨 가중치. 클수록 자주 나온다. 0 이면 뽑히지 않는다.")]
        [SerializeField] private int _weight;

        public EnemyDefinition Enemy => _enemy;
        public int Weight => _weight;
    }
}
