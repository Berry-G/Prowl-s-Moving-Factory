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

        /// <summary>모든 팀 리스트에서 제거한다.
        /// 팀이 바뀔 때 쓴다 — <b>이전 팀을 몰라도</b> 안전하게 지운다.
        /// <see cref="Unregister"/> 는 현재 Team 리스트만 보므로, 팀을 바꾼 뒤에 부르면
        /// 옛 리스트에 유령 항목이 남는다 (2026-08-29 실제 버그).</summary>
        public static void UnregisterFromAll(IDamageable target)
        {
            if (target == null) return;
            for (int i = 0; i < _lists.Length; i++) _lists[i].Remove(target);
        }

        /// <summary>origin 기준 range 안에서 team 소속 살아있는 대상 중 가장 가까운 것. 없으면 null.</summary>
        /// <summary>origin 기준 range 안에서 team 소속 살아있는 대상 중 가장 가까운 것. 없으면 null.
        /// <paramref name="filter"/> 를 주면 통과한 대상만 후보가 된다 (사격선 판정 등, G-22).
        /// 매 프레임 호출되므로 <b>호출자가 델리게이트를 필드에 캐시</b>해서 넘겨라 — 람다를 그 자리에서 만들지 마라.</summary>
        public static IDamageable FindNearest(Vector3 origin, float range, Team team,
                                              System.Func<IDamageable, bool> filter = null)
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
                if (sqr > rangeSqr || sqr >= bestSqr) continue;
                if (filter != null && !filter(t)) continue;
                bestSqr = sqr;
                best = t;
            }
            return best;
        }

        /// <summary>
        /// origin 기준 range 안 team 소속, anchor 에 가장 가까운 대상. 없으면 null.
        /// </summary>
        /// <summary>
        /// origin 기준 range 안 team 소속, anchor 에 가장 가까운 대상. 없으면 null.
        /// <paramref name="filter"/> 규약은 <see cref="FindNearest"/> 와 같다.
        /// </summary>
        public static IDamageable FindNearestTo(Vector3 origin, float range, Team team, Vector3 anchor,
                                                System.Func<IDamageable, bool> filter = null)
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
                if (found && anchorSqr >= bestAnchorSqr) continue;
                if (filter != null && !filter(t)) continue;

                found = true;
                bestAnchorSqr = anchorSqr;
                best = t;
            }
            return best;
        }

        /// <summary>origin 기준 range 안에서 team 소속 살아있는 대상 중 <b>가장 먼</b> 것. 없으면 null.
        ///
        /// 저격(헬파이어, ADR-0021)이 쓴다 — 기본 타겟팅이 "보호대상에 가장 가까운 적"이라 발밑부터
        /// 처리하는 반면, 저격은 사거리 가장자리의 적, 즉 <b>아직 들어오지 않은 적</b>을 먼저 자른다.
        /// <paramref name="filter"/> 규약은 <see cref="FindNearest"/> 와 같다.</summary>
        public static IDamageable FindFarthest(Vector3 origin, float range, Team team,
                                               System.Func<IDamageable, bool> filter = null)
        {
            var list = _lists[(int)team];
            float rangeSqr = range * range;
            IDamageable best = null;
            float bestSqr = -1f;

            for (int i = 0; i < list.Count; i++)
            {
                var t = list[i];
                if (!t.IsAlive) continue;
                float sqr = (t.Position - origin).sqrMagnitude;
                if (sqr > rangeSqr || sqr <= bestSqr) continue;
                if (filter != null && !filter(t)) continue;
                bestSqr = sqr;
                best = t;
            }
            return best;
        }

        /// <summary>center 기준 radius 안의 team 소속 살아있는 대상을 <paramref name="result"/> 에 모두 담는다.
        ///
        /// 범위 공격(메테오, ADR-0021)이 쓴다. <b>반드시 복사본을 채운다</b> —
        /// 내부 리스트를 순회하면서 <see cref="IDamageable.TakeDamage"/> 를 부르면
        /// 죽은 대상이 <see cref="Unregister"/> 되면서 순회 중인 리스트가 바뀐다.
        /// 매 타격 호출되므로 <b>호출자가 List 를 필드에 캐시</b>해서 넘겨라.</summary>
        public static void CollectInRadius(Vector3 center, float radius, Team team,
                                           List<IDamageable> result)
        {
            if (result == null) return;
            result.Clear();
            if (radius <= 0f) return;

            var list = _lists[(int)team];
            float radiusSqr = radius * radius;
            for (int i = 0; i < list.Count; i++)
            {
                var t = list[i];
                if (!t.IsAlive) continue;
                if ((t.Position - center).sqrMagnitude > radiusSqr) continue;
                result.Add(t);
            }
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
