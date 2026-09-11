using UnityEngine;
using PMF.Session;

namespace PMF.Audio
{
    /// <summary>목적: BGM/SFX 채널 실제 출력 연결. 구조: 소스별 채널, Master는 Listener. 불변: 에셋 볼륨 불변. 근거: ADR-0023.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioChannelSource : MonoBehaviour
    {
        public enum Channel { Bgm, Sfx }
        [SerializeField] private Channel _channel = Channel.Bgm;
        private AudioSource _source;
        private float _baseVolume;
        private void Awake() { _source=GetComponent<AudioSource>(); _baseVolume=_source.volume; }
        private void OnEnable() { UserSettings.Changed+=Apply; Apply(); }
        private void OnDisable() => UserSettings.Changed-=Apply;
        private void Apply()
        {
            if (_source == null) return;
            var settings=UserSettings.Current;
            _source.volume=_baseVolume*(_channel==Channel.Bgm ? settings.Bgm : settings.Sfx);
        }
    }
}
