using System;
using UnityEngine;

namespace PMF.Session
{
    /// <summary>목적: 사용자 설정 저장 모델. 구조: 씬/SO와 분리. 불변: 밸런스 SO에 쓰지 않음. 근거: ADR-0023.</summary>
    [Serializable]
    public sealed class SettingsData
    {
        [SerializeField] private bool _edgeScrolling = true;
        [SerializeField] private int _fps = 60;
        [SerializeField] private bool _vSync;
        [SerializeField] private int _width;
        [SerializeField] private int _height;
        [SerializeField] private float _master = 1f;
        [SerializeField] private float _bgm = 1f;
        [SerializeField] private float _sfx = 1f;
        [SerializeField] private bool _muted;
        public bool EdgeScrolling { get => _edgeScrolling; set => _edgeScrolling = value; }
        public int Fps { get => _fps; set => _fps = value; }
        public bool VSync { get => _vSync; set => _vSync = value; }
        public int Width { get => _width; set => _width = value; }
        public int Height { get => _height; set => _height = value; }
        public float Master { get => _master; set => _master = value; }
        public float Bgm { get => _bgm; set => _bgm = value; }
        public float Sfx { get => _sfx; set => _sfx = value; }
        public bool Muted { get => _muted; set => _muted = value; }
        public int EffectiveFps => VSync ? -1 : Fps;
        public SettingsData Copy() => JsonUtility.FromJson<SettingsData>(JsonUtility.ToJson(this));
        public void Normalize()
        {
            if (_fps != 60 && _fps != 120 && _fps != 144 && _fps != 165 && _fps != -1) _fps = 60;
            if (_width < 640 || _height < 360 || _width > 16384 || _height > 16384) _width = _height = 0;
            _master = Volume(_master); _bgm = Volume(_bgm); _sfx = Volume(_sfx);
        }
        private static float Volume(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Clamp01(value);
    }
}
