using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using PMF.Combat;

namespace PMF.Tests
{
    /// <summary>쥐 마법사(ADR-0021)가 쓰는 타겟 조회 — 저격(가장 먼 적)과 범위(반경 안 전부).
    ///
    /// 기존 <c>FindNearest</c> 계열이 "보호대상에 가장 가까운 적" 을 고르는 것과 정반대 방향이라,
    /// 두 규칙이 서로를 덮어쓰지 않는지가 여기서 지켜진다.</summary>
    public class TargetingTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly List<IDamageable> _found = new List<IDamageable>();

        [SetUp]
        public void SetUp() => TargetRegistry.Clear();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _spawned.Count; i++)
                if (_spawned[i] != null) Object.DestroyImmediate(_spawned[i]);
            _spawned.Clear();
            TargetRegistry.Clear();
        }

        /// <summary>x 좌표에 적 하나를 놓는다.</summary>
        private Health Enemy(float x)
        {
            var go = new GameObject($"enemy@{x}");
            go.transform.position = new Vector3(x, 0f, 0f);
            _spawned.Add(go);

            var health = go.AddComponent<Health>();
            health.Initialize(100f, Team.Enemy);
            return health;
        }

        // ---------- 저격 (헬파이어) ----------

        [Test]
        public void 저격은_사거리_안에서_가장_먼_적을_고른다()
        {
            Enemy(1f);
            var far = Enemy(4f);
            Enemy(2f);

            var picked = TargetRegistry.FindFarthest(Vector3.zero, 5f, Team.Enemy);
            Assert.AreSame(far, picked);
        }

        [Test]
        public void 저격도_사거리_밖은_고르지_않는다()
        {
            var inRange = Enemy(3f);
            Enemy(9f);   // 사거리 밖 — 더 멀지만 후보가 아니다

            var picked = TargetRegistry.FindFarthest(Vector3.zero, 5f, Team.Enemy);
            Assert.AreSame(inRange, picked);
        }

        [Test]
        public void 저격은_기본_타겟팅과_반대쪽을_고른다()
        {
            var near = Enemy(1f);
            var far = Enemy(4f);

            Assert.AreSame(near, TargetRegistry.FindNearest(Vector3.zero, 5f, Team.Enemy));
            Assert.AreSame(far, TargetRegistry.FindFarthest(Vector3.zero, 5f, Team.Enemy));
        }

        [Test]
        public void 적이_없으면_저격은_null_이다()
        {
            Assert.IsNull(TargetRegistry.FindFarthest(Vector3.zero, 5f, Team.Enemy));
        }

        [Test]
        public void 저격은_필터에_걸린_적을_건너뛴다()
        {
            var reachable = Enemy(2f);
            var blocked = Enemy(4f);

            var picked = TargetRegistry.FindFarthest(Vector3.zero, 5f, Team.Enemy,
                                                     t => !ReferenceEquals(t, blocked));
            Assert.AreSame(reachable, picked);
        }

        // ---------- 범위 (메테오) ----------

        [Test]
        public void 범위는_반경_안의_적을_모두_담는다()
        {
            Enemy(0f);
            Enemy(1f);
            Enemy(5f);   // 반경 밖

            TargetRegistry.CollectInRadius(Vector3.zero, 2f, Team.Enemy, _found);
            Assert.AreEqual(2, _found.Count);
        }

        [Test]
        public void 반경이_0이면_아무도_담기지_않는다()
        {
            Enemy(0f);

            TargetRegistry.CollectInRadius(Vector3.zero, 0f, Team.Enemy, _found);
            Assert.IsEmpty(_found);
        }

        [Test]
        public void 범위는_호출마다_목록을_비운다()
        {
            Enemy(0f);
            TargetRegistry.CollectInRadius(Vector3.zero, 2f, Team.Enemy, _found);
            Assert.AreEqual(1, _found.Count);

            // 아무도 없는 곳 — 이전 결과가 남아 있으면 안 된다.
            TargetRegistry.CollectInRadius(new Vector3(50f, 0f, 0f), 2f, Team.Enemy, _found);
            Assert.IsEmpty(_found);
        }

        [Test]
        public void 범위는_다른_팀을_담지_않는다()
        {
            Enemy(0f);

            var allyGo = new GameObject("ally");
            _spawned.Add(allyGo);
            allyGo.AddComponent<Health>().Initialize(100f, Team.Ally);

            TargetRegistry.CollectInRadius(Vector3.zero, 3f, Team.Enemy, _found);
            Assert.AreEqual(1, _found.Count);
        }

        [Test]
        public void 범위_목록은_복사본이라_순회_중_피해를_줘도_안전하다()
        {
            // 실제 스플래시가 하는 일: 담아 놓고 하나씩 때린다. 죽으면 레지스트리에서 빠지는데,
            // 내부 리스트를 그대로 순회했다면 여기서 터진다.
            Enemy(0f);
            Enemy(0.5f);
            Enemy(1f);

            TargetRegistry.CollectInRadius(Vector3.zero, 2f, Team.Enemy, _found);
            Assert.AreEqual(3, _found.Count);

            for (int i = 0; i < _found.Count; i++)
                _found[i].TakeDamage(999f, this);

            Assert.AreEqual(0, TargetRegistry.CountAlive(Team.Enemy));
        }
    }
}
