#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using PMF.Combat;
using PMF.Session;

namespace PMF.Diagnostics
{
    /// <summary>
    /// 디버그 오버레이 (F1 토글) + 치트 (F2~F5). 빌드에는 포함되지 않는다.
    /// OnGUI 문자열은 StringBuilder 필드 재사용으로 조립.
    /// </summary>
    public sealed class DebugOverlay : MonoBehaviour
    {
        // --- 통계 (P-21 수치 기록용, static + 리셋 필수) ---
        private static int _enemiesSpawned;
        private static int _enemiesKilled;
        private static int _alliesDeployedCount;
        private static int _marchingLosses;
        private static float _marchSecondsSum;
        private static bool _escorteeInvincible;

        /// <summary>오버레이 글자 크기. 작아서 못 읽겠다는 지적(G-13)이 있어 인스펙터에서 조절 가능하게 뺐다.</summary>
        [SerializeField] private int _fontSize = 16;

        private const string CollapsedPrefKey = "PMF.DebugOverlay.Collapsed";
        private const float ReferenceHeight = 720f;   // _fontSize 가 기준으로 삼는 화면 높이
        private const float Pad = 8f;
        private const float Margin = 8f;

        private float _uiScale = 1f;

        private bool _visible = true;          // F1 — 통째로 숨김
        private bool _collapsed = true;        // F6 — 접기. 시연 화면에 디버그가 떠 있으면 안 되므로 기본 접힘.
        private readonly StringBuilder _sb = new StringBuilder(256);

        // OnGUI 는 프레임당 여러 번 호출된다. GUIStyle·Texture 를 매번 new 하지 마라 (함정 목록).
        private GUIStyle _textStyle;
        private GUIStyle _buttonStyle;
        private Texture2D _bgTex;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _enemiesSpawned = 0;
            _enemiesKilled = 0;
            _alliesDeployedCount = 0;
            _marchingLosses = 0;
            _marchSecondsSum = 0f;
            _escorteeInvincible = false;   // 빠뜨리면 F4 치트가 다음 플레이에서 반대로 동작한다.
        }

        internal static void NotifyEnemySpawned() => _enemiesSpawned++;
        internal static void NotifyEnemyKilled() => _enemiesKilled++;
        internal static void NotifyAllyDeployed(float marchSeconds)
        {
            _alliesDeployedCount++;
            _marchSecondsSum += marchSeconds;   // 평균 행군 시간 (D-03 검증용)
        }
        internal static void NotifyAllyLostWhileMarching() => _marchingLosses++;   // D-03 검증용

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.f1Key.wasPressedThisFrame) _visible = !_visible;

            // F1(전체 숨김)과 F6(접기)은 다른 기능이다. 둘 다 남긴다 (G-13).
            if (keyboard.f6Key.wasPressedThisFrame) SetCollapsed(!_collapsed);

            if (keyboard.f2Key.wasPressedThisFrame)
                GameSession.Instance?.Wallet?.Add(1000);

            if (keyboard.f3Key.wasPressedThisFrame) KillAllEnemies();

            if (keyboard.f4Key.wasPressedThisFrame) ToggleEscorteeInvincible();

            if (keyboard.f5Key.wasPressedThisFrame)
                GameSession.Instance?.DeclareVictory();
        }

        private static void KillAllEnemies()
        {
            var all = FindObjectsByType<Health>();
            foreach (var h in all)
                if (h.Team == Team.Enemy && h.IsAlive)
                    h.TakeDamage(float.MaxValue, "cheat");
        }

        private static void ToggleEscorteeInvincible()
        {
            var escortee = GameSession.Instance != null ? GameSession.Instance.Escortee : null;
            if (escortee == null || escortee.Health == null) return;

            bool wasInvincible = !_escorteeInvincible;
            _escorteeInvincible = wasInvincible;
            escortee.Health.SetInvincible(wasInvincible);
            Debug.Log($"[Cheat] 보호대상 무적 = {wasInvincible}");
        }

        private void OnGUI()
        {
            if (!_visible) return;

            var session = GameSession.Instance;
            var clock = GameClock.Instance;
            if (session == null || clock == null) return;

            EnsureStyles();

            var content = new GUIContent(_collapsed ? BuildSummary(session, clock) : BuildFull(session, clock));

            // 고정 크기(420×200)면 줄이 늘 때 잘린다. 내용에 맞춰 재는 것이 G-13 요구사항.
            Vector2 size = _textStyle.CalcSize(content);
            float pad = Pad * _uiScale;
            float margin = Margin * _uiScale;
            float w = size.x + pad * 2f;
            float h = size.y + pad * 2f;
            var box = new Rect(Screen.width - w - margin, Screen.height - h - margin, w, h);

            // 배경이 없으면 밝은 맵 위에서 글자가 묻힌다.
            GUI.DrawTexture(box, _bgTex, ScaleMode.StretchToFill);
            GUI.Label(new Rect(box.x + pad, box.y + pad, size.x, size.y), content, _textStyle);

            // 단축키를 모르는 사람도 접을 수 있게 모서리 버튼을 같이 둔다.
            var toggle = new Rect(box.xMax - 84f * _uiScale, box.y - 26f * _uiScale, 84f * _uiScale, 24f * _uiScale);
            if (GUI.Button(toggle, _collapsed ? "펴기 F6" : "접기 F6", _buttonStyle))
                SetCollapsed(!_collapsed);
        }

        /// <summary>접힘 상태 한 줄 요약. 시연 화면을 가리지 않을 만큼만 남긴다.</summary>
        private string BuildSummary(GameSession session, GameClock clock)
        {
            _sb.Clear();
            _sb.Append($"t={session.ElapsedTime:F1}s  {clock.Speed}x{(clock.IsPaused ? " ⏸" : "")}");
            _sb.Append($"  적 {TargetRegistry.CountAlive(Team.Enemy)}");
            _sb.Append($"  아군 {TargetRegistry.CountAlive(Team.Ally)}");
            if (session.Wallet != null) _sb.Append($"  $ {session.Wallet.Amount}");
            return _sb.ToString();
        }

        /// <summary>펼침 상태 전체. 섹션으로 나눠야 눈이 값을 찾는다 (G-13).</summary>
        private string BuildFull(GameSession session, GameClock clock)
        {
            _sb.Clear();
            _sb.AppendLine($"[세션]  t={session.ElapsedTime:F1}s   속도 {clock.Speed}x{(clock.IsPaused ? "   ⏸ 일시정지" : "")}");

            var escortee = session.Escortee;
            if (escortee != null && escortee.Health != null)
            {
                _sb.Append($"        보호대상 {escortee.Health.Current:F0}/{escortee.Health.Max:F0}   위치 {escortee.CurrentNode}");
                _sb.AppendLine(escortee.Follower.HasRoute ? $"   진행 {escortee.Follower.Progress01:P0}" : "");
            }

            _sb.AppendLine($"[적]    생존 {TargetRegistry.CountAlive(Team.Enemy)}   누적 스폰 {_enemiesSpawned}   격파 {_enemiesKilled}");

            float avg = _alliesDeployedCount > 0 ? _marchSecondsSum / _alliesDeployedCount : 0f;
            _sb.AppendLine($"[아군]  생존 {TargetRegistry.CountAlive(Team.Ally)}   배치 {_alliesDeployedCount}   행군 중 손실 {_marchingLosses}   평균 행군 {avg:F1}s");

            var wallet = session.Wallet;
            _sb.Append(wallet != null
                ? $"[경제]  보유 {wallet.Amount}   획득 {wallet.TotalGained}   지출 {wallet.TotalSpent}"
                : "[경제]  지갑 없음");
            return _sb.ToString();
        }

        private void SetCollapsed(bool collapsed)
        {
            _collapsed = collapsed;
            PlayerPrefs.SetInt(CollapsedPrefKey, collapsed ? 1 : 0);   // 재생을 껐다 켜도 유지 (G-13 DoD)
            PlayerPrefs.Save();
        }

        private void EnsureStyles()
        {
            // OnGUI 좌표계는 실제 백버퍼 픽셀이다. 2560×1440 에서 fontSize 16 은 720p 기준 8px 로 보인다.
            // 그래서 720p 를 기준 해상도로 두고 화면 높이에 비례해 키운다.
            _uiScale = Mathf.Max(1f, Screen.height / ReferenceHeight);
            int wanted = Mathf.RoundToInt(_fontSize * _uiScale);

            // GUI.skin 은 OnGUI 안에서만 유효하므로 여기서 만든다. 크기가 그대로면 다시 만들지 않는다.
            if (_textStyle != null && _textStyle.fontSize == wanted) return;

            // 내장 GUI 폰트에는 한글 글리프가 없다. uGUI 와 같은 폰트를 쓴다.
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _textStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = wanted,
                wordWrap = false,
                alignment = TextAnchor.UpperLeft,
            };
            _textStyle.normal.textColor = Color.white;

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                font = font,
                fontSize = Mathf.Max(10, wanted - 4),
            };

            if (_bgTex == null)
            {
                _bgTex = new Texture2D(1, 1);
                _bgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.78f));
                _bgTex.Apply();
                _bgTex.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        private void Awake()
        {
            _collapsed = PlayerPrefs.GetInt(CollapsedPrefKey, 1) != 0;   // 기본값 1 = 접힘
        }

        private void OnDestroy()
        {
            if (_bgTex != null) Destroy(_bgTex);
        }
    }
}
#endif
