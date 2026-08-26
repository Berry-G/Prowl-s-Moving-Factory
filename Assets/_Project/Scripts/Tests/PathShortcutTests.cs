using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PMF.Grid;
using PMF.Pathing;

namespace PMF.Tests
{
    /// <summary>지름길 통행 권한 테스트 (P-06 DoD 3,4,5 — 이 태스크의 핵심).</summary>
    public sealed class PathShortcutTests : PathGraphTests
    {
        private int FindShortcutEdgeId()
        {
            foreach (var node in _graph.Nodes)
                foreach (var edge in node.Edges)
                    if (edge.IsShortcut) return edge.Id;
            return -1;
        }

        [Test]
        public void Enemy_NeverUsesShortcut_EvenWhenOpen()
        {
            var a = Node(0, 0, isStart: true);
            var mid = Node(4, 4);
            var b = Node(8, 8, isExit: true);
            Connect(a, mid);
            Connect(mid, b);
            Connect(a, b, shortcut: true, allowed: PathAgent.Escortee);
            Build();

            int edgeId = FindShortcutEdgeId();
            Assert.GreaterOrEqual(edgeId, 0);
            Assert.IsTrue(_graph.OpenShortcut(edgeId));   // 열어도

            bool ok = _graph.TryFindRoute(_graph.GetNode(0), _graph.GetNode(2),
                                          PathAgent.Enemy, _result);
            Assert.IsTrue(ok);
            Assert.AreEqual(3, _result.Count);   // 적은 무조건 우회 경로
        }

        [Test]
        public void ClosedShortcut_BlocksEscortee()
        {
            var a = Node(0, 0, isStart: true);
            var b = Node(3, 3, isExit: true);
            Connect(a, b, shortcut: true, allowed: PathAgent.Escortee);
            Build();

            bool ok = _graph.TryFindRoute(_graph.GetNode(0), _graph.GetNode(1),
                                          PathAgent.Escortee, _result);
            Assert.IsFalse(ok);
            Assert.IsEmpty(_result);   // 실패 시 result 는 비어 있다.
        }

        [Test]
        public void OpenShortcut_ShortensEscorteeRoute_AndSecondOpenReturnsFalse()
        {
            var a = Node(0, 0, isStart: true);
            var far = Node(6, 6);
            var b = Node(12, 12, isExit: true);

            Connect(a, far);
            Connect(far, b);
            Connect(a, b, shortcut: true, allowed: PathAgent.Escortee);
            Build();

            Assert.IsTrue(_graph.TryFindRoute(_graph.GetNode(0), _graph.GetNode(2),
                                              PathAgent.Escortee, _result));
            int longLength = _result.Count;

            Assert.IsTrue(_graph.OpenShortcut(FindShortcutEdgeId()));
            // 이미 열려 있으면 false.
            Assert.IsFalse(_graph.OpenShortcut(FindShortcutEdgeId()));

            Assert.IsTrue(_graph.TryFindRoute(_graph.GetNode(0), _graph.GetNode(2),
                                              PathAgent.Escortee, _result));
            Assert.Less(_result.Count, longLength);   // 더 짧아진다.
        }
    }
}
