using System.Collections.Generic;
using UnityEngine;

namespace PMF.Combat
{
    /// <summary>
    /// 물리 엔진 대신 쓰는 대상 등록소. static 이므로 리셋 주의.
    /// 도메인 리로드가 꺼져 있으므로 ResetStatics + GameSession.Awake 의 Clear() 이중 안전장치.
    /// </summary>
    public static class TargetRegistry
    {
        private static readonly List<IDamageable>[] _lists = new List<IDamageable>[3]
        {
            new List<IDamageable>(),   // Ally
            new List<IDamageable>(),   // Enemy
            new List<IDamageable>(),   // Escortee
        };

        public static void Register(IDamageable target)
        {
            if (target == null || !target.IsAlive) return;
            var list = _lists[(int)target.Team];
            if (!list.Contains(target)) list.Add(target);
        }

        public static void Unregister(IDamageable target)
        {
            if (target == null) return;
            _lists[(int)target.Team].Remove(target);
        }

        /// <summary>origin 기준 range 안에서 team 소속 살아있는 대상 중 가장 가까운 것. 없으면 null.</summary>
        public static IDamageable FindNearest(Vector3 origin, float range, Team team)
        {
            var list = _lists[(int)team];
            float rangeSqr = range * range;
            IDamageable best = null;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < list.Count; i++)
            {
                var t = list[i];
                if (!t.IsAlive) continue;                     // 제거는 Unregister 에 맡긴다
                float sqr = (t.Position - origin).sqrMagnitude;
                if (sqr <= rangeSqr && sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = t;
                }
            }
            return best;
        }

        /// <summary>
        /// origin 기준 range 안 team 소속, anchor 에 가장 가까운 대상. 없으면 null.
        /// </summary>
        public static IDamageable FindNearestTo(Vector3 origin, float range, Team team, Vector3 anchor)
        {
            var list = _lists[(int)team];
            float rangeSqr = range * range;
            IDamageable best = null;
            float bestAnchorSqr = float.MaxValue;
            bool found = false;

            for (int i = 0; i < list.Count; i++)
            {
                var t = list[i];
                if (!t.IsAlive) continue;
                float distSqr = (t.Position - origin).sqrMagnitude;
                if (distSqr > rangeSqr) continue;

                float anchorSqr = (t.Position - anchor).sqrMagnitude;
                if (!found || anchorSqr < bestAnchorSqr)
                {
                    found = true;
                    bestAnchorSqr = anchorSqr;
                    best = t;
                }
            }
            return best;
        }

        public static int CountAlive(Team team)
        {
            var list = _lists[(int)team];
            int count = 0;
            for (int i = 0; i < list.Count; i++)
                if (list[i].IsAlive) count++;
            return count;
        }

        /// <summary>씬 재시작/리셋용 전체 초기화. GameSession.Awake 에서 호출.</summary>
        public static void Clear()
        {
            for (int i = 0; i < _lists.Length; i++) _lists[i].Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Clear();
        }
    }
}
