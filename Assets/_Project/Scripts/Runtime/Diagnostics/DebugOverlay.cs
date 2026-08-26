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

        private bool _visible = true;
        private readonly StringBuilder _sb = new StringBuilder(256);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _enemiesSpawned = 0;
            _enemiesKilled = 0;
            _alliesDeployedCount = 0;
            _marchingLosses = 0;
            _marchSecondsSum = 0f;
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

        private static bool _escorteeInvincible;

        private void OnGUI()
        {
            if (!_visible) return;

            var session = GameSession.Instance;
            var clock = GameClock.Instance;
            if (session == null || clock == null) return;

            _sb.Clear();
            _sb.AppendLine($"t={session.ElapsedTime:F1}s speed={clock.Speed}x{(clock.IsPaused ? " PAUSED" : "")}");

            var escortee = session.Escortee;
            if (escortee != null && escortee.Health != null)
            {
                float hp = escortee.Health.Current;
                _sb.AppendLine($"escortee hp={hp:F0}/{escortee.Health.Max:F0} node={escortee.CurrentNode}");
                if (escortee.Follower.HasRoute)
                    _sb.AppendLine($"progress={escortee.Follower.Progress01:P0}");
            }

            int alliesAlive = TargetRegistry.CountAlive(Team.Ally);
            _sb.AppendLine($"enemy alive={TargetRegistry.CountAlive(Team.Enemy)} spawned={_enemiesSpawned} killed={_enemiesKilled}");
            _sb.AppendLine($"allies alive={alliesAlive} deployed={_alliesDeployedCount} losses(march)={_marchingLosses}");

            var wallet = session.Wallet;
            if (wallet != null)
            {
                float avg = _alliesDeployedCount > 0 ? _marchSecondsSum / _alliesDeployedCount : 0f;
                _sb.AppendLine($"wallet={wallet.Amount} gained={wallet.TotalGained} spent={wallet.TotalSpent}");
                _sb.AppendLine($"avg march={avg:F1}s");
            }
            else
            {
                _sb.AppendLine("wallet=N/A");
            }

            GUI.Label(new Rect(8f, 8f, 420f, 200f), _sb.ToString());
        }
    }
}
#endif
