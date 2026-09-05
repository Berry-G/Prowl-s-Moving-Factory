using System.Collections.Generic;
using UnityEngine;

namespace PMF.Combat
{
    /// <summary>
    /// "손기술"(고양이 Lv2, ADR-0020) 등록소 — <b>행동반경 안에서 죽은 적의 보상을 올린다.</b>
    ///
    /// 판정 기준은 적이 <b>죽은 자리</b>다. 누가 죽였는지는 보지 않는다:
    /// 도적이 훑고 다니는 구역에서 나온 고철을 주워 담는다는 뜻이라 그 편이 설정과도 맞고,
    /// 막타 소유권을 추적하지 않아도 되어 구현도 단순하다.
    ///
    /// 반경의 중심은 유닛의 몸이 아니라 <b>배치 슬롯</b>(<see cref="Attacker.RangeOrigin"/>)이다.
    /// 근접 병종은 적에게 붙으러 원 안을 돌아다니므로, 몸을 중심으로 잡으면 보상 반경이 같이 흔들린다 (G-22).
    ///
    /// ⚠️ 겹쳐도 <b>합산하지 않고 가장 큰 것 하나만</b> 쓴다. 합산을 허용하면 고양이를 한 칸에 몰아 놓는 것이
    /// 언제나 정답이 되어 배치 판단이 사라진다.
    ///
    /// ⚠️ 도메인 리로드가 꺼져 있으므로 static 목록은 반드시 리셋한다 (CLAUDE.md §3).
    /// </summary>
    public static class ScavengeRegistry
    {
        private struct Entry
        {
            public Object Owner;
            public Vector3 Center;
            public float RadiusSqr;
            public float Bonus;
        }

        private static readonly List<Entry> _entries = new List<Entry>();

        /// <summary>보상 반경을 등록하거나 갱신한다. 같은 <paramref name="owner"/> 는 하나만 남는다 —
        /// 재배치·추가 강화 때마다 이걸 다시 부르면 된다.</summary>
        public static void Register(Object owner, Vector3 center, float radius, float bonus)
        {
            if (owner == null) return;
            if (bonus <= 0f || radius <= 0f)
            {
                Unregister(owner);
                return;
            }

            var entry = new Entry
            {
                Owner = owner,
                Center = center,
                RadiusSqr = radius * radius,
                Bonus = bonus,
            };

            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Owner != owner) continue;
                _entries[i] = entry;
                return;
            }
            _entries.Add(entry);
        }

        public static void Unregister(Object owner)
        {
            if (owner == null) return;
            for (int i = _entries.Count - 1; i >= 0; i--)
                if (_entries[i].Owner == owner) _entries.RemoveAt(i);
        }

        /// <summary><paramref name="position"/> 에서 적이 죽었을 때 처치 보상에 곱할 배율.
        /// 아무 반경에도 안 들어가면 1.</summary>
        public static float MultiplierAt(Vector3 position)
        {
            float best = 0f;
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var e = _entries[i];
                if (e.Owner == null)          // 파괴된 유닛 — 지연 정리
                {
                    _entries.RemoveAt(i);
                    continue;
                }
                if ((position - e.Center).sqrMagnitude > e.RadiusSqr) continue;
                if (e.Bonus > best) best = e.Bonus;
            }
            return 1f + best;
        }

        /// <summary>씬 재시작/리셋용 전체 초기화.</summary>
        public static void Clear() => _entries.Clear();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Clear();
    }
}
