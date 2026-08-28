using UnityEngine;

namespace PMF.Data
{
    /// <summary>효과음 정의 하나 (G-11, ADR-0011). 클립은 절차 생성(AudioClip.Create) — 외부 에셋 0개.
    /// 볼륨·최소 재생 간격·동시 상한은 SO 로 조절한다.
    /// ※ 런타임에 CreateInstance 로 만든 인스턴스만 Setup 을 쓴다. .asset 파일은 런타임에 수정 금지 (CLAUDE.md §4).</summary>
    [CreateAssetMenu(menuName = "PMF/Sfx Definition")]
    public sealed class SfxDefinition : ScriptableObject
    {
        [SerializeField] private AudioClip _clip;
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;
        [Tooltip("같은 소리의 최소 재생 간격 (실시간 초). 근접 타격 피로 방지 (4x 배속 = 초당 9회).")]
        [SerializeField] private float _minInterval = 0.05f;
        [Tooltip("동시 재생 상한. 40마리 교전에서 소리가 뭉개지지 않게.")]
        [SerializeField, Min(1)] private int _maxConcurrent = 4;

        public AudioClip Clip => _clip;
        public float Volume => _volume;
        public float MinInterval => _minInterval;
        public int MaxConcurrent => _maxConcurrent;

        /// <summary>런타임 생성 인스턴스 전용. 에셋에는 절대 쓰지 마라.</summary>
        public void SetupRuntime(AudioClip clip, float volume, float minInterval, int maxConcurrent)
        {
            _clip = clip;
            _volume = volume;
            _minInterval = minInterval;
            _maxConcurrent = maxConcurrent;
        }
    }
}