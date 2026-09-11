using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using PMF.Data;

namespace PMF.Audio
{
    /// <summary>효과음 재생 창구 (G-11, ADR-0011). 클립은 절차 생성, Awake 에서 한 번 만든다.
    /// 각 액터가 AudioSource 를 들지 않게 — 이 컴포넌트가 유일한 재생 창구다.
    /// 동시 재생 상한 + 같은 소리 최소 간격(실시간) + 일시정지 중 재생 금지 + 음소거(M).</summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        public enum SfxId
        {
            CatHit,       // 고양이 근접 타격 — 짧고 가볍고 빠르다
            RatShot,      // 쥐 원거리 발사 — 낮고 약간 길게
            RobotDown,    // 로봇 격파 — 금속 파편 하강음
            EscorteeHurt, // 보호대상 피격 — 낮고 둔탁
            Hire,         // 고용 확정 — 긍정적 상승음
            DeployDone,   // 배치 완료 — 짧은 확인음
            Victory,      // 승리
            Defeat,       // 패배
        }

        [SerializeField] private float _masterVolume = 1f;

        private readonly List<AudioSource> _sources = new List<AudioSource>();
        private SfxDefinition[] _defs;
        private float[] _lastPlayedReal;
        private readonly List<float>[] _activeEnds = new List<float>[8];
        private bool _muted;

        private float _sfxVolume = 1f;
        private readonly Dictionary<AudioSource,float> _voiceVolumes = new Dictionary<AudioSource,float>();
        private void OnEnable() { PMF.Session.UserSettings.Changed += ApplyUserSettings; ApplyUserSettings(); }
        private void OnDisable() => PMF.Session.UserSettings.Changed -= ApplyUserSettings;
        private void ApplyUserSettings()
        {
            var settings = PMF.Session.UserSettings.Current;
            _muted = settings.Muted; _sfxVolume = settings.Sfx;
            foreach (var pair in _voiceVolumes)
                if (pair.Key != null) pair.Key.volume = pair.Value * _sfxVolume;
        }

        private void Awake()
        {
            for (int i = 0; i < _activeEnds.Length; i++)
                if (_activeEnds[i] == null) _activeEnds[i] = new List<float>();

            // 클립 생성은 Awake 에서 한 번 (매 재생마다 만들지 마라).
            _defs = new SfxDefinition[8];
            _defs[(int)SfxId.CatHit] = MakeDef("sfx_cat_hit", ProceduralSfx.Square("cat_hit", 880f, 0.06f), 0.5f, 0.09f, 3);
            _defs[(int)SfxId.RatShot] = MakeDef("sfx_rat_shot", ProceduralSfx.SineSweep("rat_shot", 220f, 180f, 0.12f), 0.55f, 0.12f, 3);
            _defs[(int)SfxId.RobotDown] = MakeDef("sfx_robot_down", ProceduralSfx.MetallicDown("robot_down", 440f, 110f, 0.3f), 0.6f, 0.08f, 4);
            _defs[(int)SfxId.EscorteeHurt] = MakeDef("sfx_escortee_hurt", ProceduralSfx.SineSweep("escortee_hurt", 130f, 90f, 0.18f), 0.8f, 0.15f, 2);
            _defs[(int)SfxId.Hire] = MakeDef("sfx_hire", ProceduralSfx.SineSweep("hire", 523f, 784f, 0.15f), 0.7f, 0.1f, 2);
            _defs[(int)SfxId.DeployDone] = MakeDef("sfx_deploy", ProceduralSfx.SineSweep("deploy", 660f, 660f, 0.08f), 0.6f, 0.1f, 2);
            _defs[(int)SfxId.Victory] = MakeDef("sfx_victory", ProceduralSfx.SineSweep("victory", 523f, 1046f, 0.4f), 0.8f, 0f, 1);
            _defs[(int)SfxId.Defeat] = MakeDef("sfx_defeat", ProceduralSfx.SineSweep("defeat", 300f, 150f, 0.4f), 0.8f, 0f, 1);
            _lastPlayedReal = new float[8];

            for (int i = 0; i < 8; i++)
            {
                var go = new GameObject($"SfxSource{i}");
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _sources.Add(src);
            }
        }

        private static SfxDefinition MakeDef(string name, AudioClip clip, float volume, float minInterval, int maxConcurrent)
        {
            var def = ScriptableObject.CreateInstance<SfxDefinition>();
            def.name = name;
            def.SetupRuntime(clip, volume, minInterval, maxConcurrent);
            return def;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.mKey.wasPressedThisFrame && !PMF.UI.PauseMenu.IsOpen)
                ToggleMute();
        }

        /// <summary>마스터 볼륨 (StageDefinition 전역 볼륨 × 개별 volume). G-12 설정 메뉴가 쓴다.</summary>
        public void SetMasterVolume(float volume) => _masterVolume = Mathf.Clamp01(volume);

        /// <summary>현재 마스터 볼륨 — G-12 슬라이더 초기값.</summary>
        public float MasterVolume => _masterVolume;

        /// <summary>음소거 상태 — G-12 토글 표시용.</summary>
        public bool IsMuted => _muted;

        public void ToggleMute() => SetMuted(!_muted);

        public void SetMuted(bool muted)
        {
            PMF.Session.UserSettings.Change(settings => settings.Muted = muted);
        }

        public void Play(SfxId id)
        {
            if (_muted) return;

            // 일시정지 중 새 소리 금지 (Time.timeScale=0 에도 AudioSource 는 도니까 따로 막는다).
            if (PMF.Session.GameClock.Instance != null && PMF.Session.GameClock.Instance.IsPaused) return;
            if (Time.timeScale <= 0f) return;

            int index = (int)id;
            var def = _defs[index];
            if (def == null || def.Clip == null) return;

            // 최소 재생 간격 — 실시간 기준 (배속과 무관하게 피로를 막는다).
            float now = Time.realtimeSinceStartup;
            if (now - _lastPlayedReal[index] < def.MinInterval) return;

            // 동시 재생 상한 — 끝난 것부터 정리하고 센다.
            _activeEnds[index].RemoveAll(end => end <= now);
            if (_activeEnds[index].Count >= def.MaxConcurrent) return;

            var source = FindFreeSource();
            if (source == null) return;

            _voiceVolumes[source] = def.Volume * _masterVolume;
            source.volume = _voiceVolumes[source] * _sfxVolume;
            source.PlayOneShot(def.Clip);
            _lastPlayedReal[index] = now;
            _activeEnds[index].Add(now + def.Clip.length);
        }

        private AudioSource FindFreeSource()
        {
            foreach (var src in _sources)
                if (!src.isPlaying) return src;
            return null;   // 전부 재생 중이면 스킵 — 소음 방지
        }
    }
}