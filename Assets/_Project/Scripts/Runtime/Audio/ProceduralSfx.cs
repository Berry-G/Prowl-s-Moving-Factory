using UnityEngine;

namespace PMF.Audio
{
    /// <summary>효과음 절차 생성 (G-11, ADR-0011 A안). AudioClip.Create + SetData — 전부 코드다.
    /// 재료 3개: 사인파(부드러운 음) · 구형파(건조한 타격) · 화이트 노이즈(파편).
    /// 전부 exponential decay 엔벨로프 — 안 씌우면 "삐-" 하고 끊긴다. 0.05~0.4초.</summary>
    public static class ProceduralSfx
    {
        private const int SampleRate = 44100;

        public static AudioClip Synth(string name, float duration, System.Func<float, float> sample)
        {
            int samples = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SampleRate;
                float progress = t / duration;
                float envelope = Mathf.Exp(-5f * progress);   // exponential decay
                data[i] = Mathf.Clamp(sample(t) * envelope, -1f, 1f);
            }
            var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>사인파 스윕 (freqStart → freqEnd). 부드러운 음.</summary>
        public static AudioClip SineSweep(string name, float freqStart, float freqEnd, float duration)
            => Synth(name, duration, t =>
            {
                float freq = Mathf.Lerp(freqStart, freqEnd, t / duration);
                return Mathf.Sin(2f * Mathf.PI * freq * t);
            });

        /// <summary>구형파. 건조한 타격.</summary>
        public static AudioClip Square(string name, float freq, float duration)
            => Synth(name, duration, t => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t)) * 0.6f);

        /// <summary>화이트 노이즈. 파편/금속 느낌.</summary>
        public static AudioClip Noise(string name, float duration)
            => Synth(name, duration, _ => Random.Range(-1f, 1f) * 0.8f);

        /// <summary>하강음 + 노이즈 혼합 — 금속 파편 (로봇 격파).</summary>
        public static AudioClip MetallicDown(string name, float freqStart, float freqEnd, float duration)
            => Synth(name, duration, t =>
            {
                float freq = Mathf.Lerp(freqStart, freqEnd, t / duration);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f + Random.Range(-1f, 1f) * 0.3f;
            });
    }
}