using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using PMF.Pathing;

namespace PMF.Tests
{
    /// <summary>PathFollower 순수 로직 테스트 (P-07 DoD 5항목).</summary>
    public sealed class PathFollowerTests
    {
        private static PathNode MakeNode(int id, float x, float y)
            => new PathNode(id,
                new PMF.Grid.GridCoord(Mathf.FloorToInt(x), Mathf.FloorToInt(y)),
                new Vector3(x, y, 0f));

        [Test]
        public void ThreeNodeRoute_ExactArrival_IsFinished()
        {
            var follower = new PathFollower();
            var route = new List<PathNode>
            {
                MakeNode(0, 0f, 0f),
                MakeNode(1, 3f, 0f),
                MakeNode(2, 6f, 0f),
            };

            follower.SetRoute(route, route[0].WorldPosition);
            bool passed = follower.Advance(6f);   // 총 길이 = 6

            Assert.IsTrue(passed || follower.IsFinished);
            Assert.AreEqual(new Vector3(6f, 0f, 0f), follower.Position);
            Assert.IsTrue(follower.IsFinished);
        }

        [Test]
        public void SingleAdvance_PassesTwoNodes_AccuratePosition()
        {
            var follower = new PathFollower();
            var route = new List<PathNode>
            {
                MakeNode(0, 0, 0),
                MakeNode(1, 2, 0),
                MakeNode(2, 4, 0),
                MakeNode(3, 7, 0),
            };
            follower.SetRoute(route, route[0].WorldPosition);

            bool passed = follower.Advance(5f);   // 노드 2개 통과 + 마지막 구간 1 진행
            Assert.IsTrue(passed);
            Assert.AreEqual(new Vector3(5f, 0f, 0f), follower.Position);
            Assert.IsFalse(follower.IsFinished);

            // 남은 2 를 전진하면 정확히 마지막에 도착한다.
            follower.Advance(2f);
            Assert.AreEqual(new Vector3(7f, 0f, 0f), follower.Position);
            Assert.IsTrue(follower.IsFinished);
        }

        [Test]
        public void Overshoot_DoesNotPassLastNode()
        {
            var follower = new PathFollower();
            var route = new List<PathNode>
            {
                MakeNode(0, 0f, 0f),
                MakeNode(1, 4f, 0f),
            };
            follower.SetRoute(route, route[0].WorldPosition);

            follower.Advance(100f);   // 총 길이보다 훨씬 긴 거리

            Assert.AreEqual(new Vector3(4f, 0f, 0f), follower.Position);   // 오버슛 없음
            Assert.IsTrue(follower.IsFinished);
            // 이후 Advance 는 아무것도 하지 않는다.
            Assert.IsFalse(follower.Advance(10f));
        }

        [Test]
        public void AdvanceZero_ChangesNothing()
        {
            var follower = new PathFollower();
            var route = new List<PathNode>
            {
                MakeNode(0, 0f, 0f),
                MakeNode(1, 4f, 0f),
            };
            follower.SetRoute(route, route[0].WorldPosition);

            bool passed = follower.Advance(0f);

            Assert.IsFalse(passed);
            Assert.IsFalse(follower.IsFinished);
            Assert.AreEqual(new Vector3(0f, 0f, 0f), follower.Position);
        }

        [Test]
        public void Clear_SafeAfterClear()
        {
            var follower = new PathFollower();
            var route = new List<PathNode>
            {
                MakeNode(0, 0f, 0f),
                MakeNode(1, 4f, 0f),
            };
            follower.SetRoute(route, route[0].WorldPosition);

            follower.Clear();

            Assert.IsFalse(follower.HasRoute);
            Assert.IsTrue(follower.IsFinished);
            Assert.DoesNotThrow(() => follower.Advance(1f));
        }

        [Test]
        public void SetRoute_Midway_RestartsFromCurrentPosition()
        {
            var follower = new PathFollower();
            var first = new List<PathNode>
            {
                MakeNode(0, 0f, 0f),
                MakeNode(1, 4f, 0f),
            };
            follower.SetRoute(first, first[0].WorldPosition);
            follower.Advance(3f);
            Assert.AreEqual(new Vector3(3f, 0f, 0f), follower.Position);

            // 중간에 경로 재설정: 현재 위치가 시작점이고 route[0] 은 방금 지난 노드.
            var second = new List<PathNode>
            {
                MakeNode(1, 4f, 0f),
                MakeNode(2, 8f, 0f),
            };
            follower.SetRoute(second, follower.Position);
            Assert.AreEqual(new Vector3(3f, 0f, 0f), follower.Position);

            follower.Advance(1f);
            Assert.AreEqual(new Vector3(4f, 0f, 0f), follower.Position);
            Assert.AreSame(second[0], follower.CurrentNode);
        }

        [Test]
        public void Progress_TracksTravel()
        {
            var follower = new PathFollower();
            var route = new List<PathNode>
            {
                MakeNode(0, 0f, 0f),
                MakeNode(1, 10f, 0f),
            };
            follower.SetRoute(route, route[0].WorldPosition);

            follower.Advance(5f);
            Assert.AreEqual(0.5f, follower.Progress01, 0.001f);
            Assert.AreEqual(5f, follower.RemainingDistance, 0.001f);
        }
    }
}
