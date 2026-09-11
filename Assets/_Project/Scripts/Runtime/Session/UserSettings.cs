using System;
using System.Collections.Generic;
using UnityEngine;

namespace PMF.Session
{
    /// <summary>목적: 저장/적용 단일 창구. 구조: 씬 서비스가 아닌 설정 저장소. 불변: 도메인 리로드 OFF 초기화. 근거: ADR-0023.</summary>
    public static class UserSettings
    {
        private const string Key = "PMF.UserSettings.v1";
        private static SettingsData _current;
        public static event Action Changed;
        public static SettingsData Current => (_current ??= Load()).Copy();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { _current = null; Changed = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            _current = Load(); ApplyGraphics(); ApplyResolution(); ApplyAudio();
        }
        public static SettingsData Decode(string json)
        {
            var data = new SettingsData();
            try { if (!string.IsNullOrWhiteSpace(json)) JsonUtility.FromJsonOverwrite(json, data); }
            catch (ArgumentException) { data = new SettingsData(); }
            data.Normalize(); return data;
        }
        private static SettingsData Load() => Decode(PlayerPrefs.GetString(Key, ""));
        public static void Change(Action<SettingsData> edit)
        {
            var before = Current;
            var next = before.Copy(); edit(next); next.Normalize(); _current = next;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(next)); PlayerPrefs.Save();
            if (before.Fps != next.Fps || before.VSync != next.VSync) ApplyGraphics();
            if (before.Width != next.Width || before.Height != next.Height) ApplyResolution();
            ApplyAudio(); Changed?.Invoke();
        }
        private static void ApplyGraphics()
        {
            QualitySettings.vSyncCount = _current.VSync ? 1 : 0;
            // 왜: 데스크톱 Unity는 VSync ON일 때 targetFrameRate를 무시한다. 선택값은 별도 보존.
            Application.targetFrameRate = _current.EffectiveFps;
        }
        private static void ApplyAudio() => AudioListener.volume = _current.Muted ? 0f : _current.Master;
        private static void ApplyResolution()
        {
            if (_current.Width == 0) return;
            var wanted = new Vector2Int(_current.Width, _current.Height);
            if (!AvailableResolutions().Contains(wanted)) return; // 모니터 변경 시 현재 지원 모드 유지
#if !UNITY_EDITOR
            Screen.SetResolution(wanted.x, wanted.y, Screen.fullScreenMode);
#endif
        }
        public static List<Vector2Int> AvailableResolutions()
        {
            var list = new List<Vector2Int>();
            foreach (var r in Screen.resolutions)
            {
                var size = new Vector2Int(r.width, r.height);
                if (!list.Contains(size)) list.Add(size);
            }
            var current = new Vector2Int(Screen.width, Screen.height);
            if (current.x > 0 && current.y > 0 && !list.Contains(current)) list.Add(current);
            list.Sort((a,b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return list;
        }
    }
}
