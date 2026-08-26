using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PMF.Grid;
using PMF.Pathing;

namespace PMF.Tests
{
    /// <summary>경로 탐색 기본 테스트 (P-06 DoD 1,2,6,7,8).</summary>
    public class PathGraphTests
    {
        protected GameObject _graphGo;
        protected PathGraph _graph;
        protected readonly List<PathNode> _result = new List<PathNode>();

        [SetUp]
        public virtual void SetUp()
        {
            var gridGo = new GameObject("grid");
            var grid = gridGo.AddComponent<GridSystem>();
            SetGridField(grid, "_width", 32);
            SetGridField(grid, "_height", 32);
            SetGridField(grid, "_origin", Vector3.zero);
            SetGridField(grid, "_cellSize", 1f);
            grid.BuildFromTilemaps();

            _graphGo = new GameObject("graph");
            _graph = _graphGo.AddComponent<PathGraph>();
        }

        [TearDown]
        public virtual void TearDown()
        {
            Object.DestroyImmediate(_graphGo);
            var grid = Object.FindAnyObjectByType<GridSystem>();
            if (grid != null) Object.DestroyImmediate(grid.gameObject);
        }

        internal static void SetGridField(object target, string name, object value)
        {
            typeof(GridSystem)
                .GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(target, value);
        }

        /// <summary>셀 중심에 노드 저작 컴포넌트를 만든다. 시작/탈출 플래그 지정 가능.</summary>
        internal PathNodeAuthoring Node(int x, int y, bool isStart = false, bool isExit = false)
        {
            var go = new GameObject($"node_{x}_{y}");
            go.transform.SetParent(_graphGo.transform);
            go.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0f);
            var authoring = go.AddComponent<PathNodeAuthoring>();

            // PathGraph.Validate 는 시작/탈출 노드 부재 시 LogError — 테스트 그래프에도 플래그를 세운다.
            var so = new UnityEditor.SerializedObject(authoring);
            so.FindProperty("_isEscorteeStart").boolValue = isStart;
            so.FindProperty("_isExit").boolValue = isExit;
            so.ApplyModifiedPropertiesWithoutUndo();
            return authoring;
        }

        internal static void Connect(PathNodeAuthoring from, PathNodeAuthoring to,
                                     bool shortcut = false, PathAgent allowed = PathAgent.All,
                                     bool bidirectional = true)
        {
            from.Connections.Add(new PathNodeAuthoring.Connection
            {
                Target = to,
                Allowed = shortcut ? allowed : PathAgent.All,
                IsShortcut = shortcut,
                Bidirectional = bidirectional,
            });
        }

        protected void Build() => _graph.Build();

        [Test]
        public void StraightLine_FindsShortestPath()
        {
            var a = Node(0, 0, isStart: true);
            var b = Node(1, 0);
            var c = Node(2, 0, isExit: true);
            Connect(a, b);
            Connect(b, c);
            Build();

            bool ok = _graph.TryFindRoute(_graph.GetNode(0), _graph.GetNode(2),
                                          PathAgent.All, _result);
            Assert.IsTrue(ok);
            Assert.AreEqual(3, _result.Count);   // result[0]=from
            Assert.AreSame(_graph.GetNode(2), _result[2]);
        }

        [Test]
        public void Branch_CheaperSideChosen()
        {
            var a = Node(0, 0, isStart: true);
            var detour = Node(3, 3);
            var b = Node(8, 8, isExit: true);

            Connect(a, b);          // 직통이 최단
            Connect(a, detour);
            Connect(detour, b);
            Build();

            bool ok = _graph.TryFindRoute(_graph.GetNode(0), _graph.GetNode(1),
                                          PathAgent.All, _result);
            Assert.IsTrue(ok);
            Assert.AreEqual(2, _result.Count);   // 우회하지 않는다.
        }

        [Test]
        public void Unreachable_ReturnsFalse_EmptyResult()
        {
            Node(0, 0, isStart: true);
            Node(5, 5, isExit: true);
            // 연결 없음 — 고립 노드 검증 로그는 의도된 것 (Id 부여 순서와 무관하게 정규식 매치).
            var isolated = new System.Text.RegularExpressions.Regex(@"^\[PathGraph\] 고립 노드: Node\d+\(\d+, \d+\)$");
            LogAssert.Expect(LogType.Error, isolated);
            LogAssert.Expect(LogType.Error, isolated);
            Build();

            bool ok = _graph.TryFindRoute(_graph.GetNode(0), _graph.GetNode(1),
                                          PathAgent.All, _result);
            Assert.IsFalse(ok);
            Assert.IsEmpty(_result);
        }

        [Test]
        public void FromEqualsTo_ReturnsTrue_CountOne()
        {
            var a = Node(0, 0, isStart: true);
            var b = Node(1, 0, isExit: true);
            Connect(a, b);
            Build();

            bool ok = _graph.TryFindRoute(_graph.GetNode(1), _graph.GetNode(1),
                                          PathAgent.All, _result);
            Assert.IsTrue(ok);
            Assert.AreEqual(1, _result.Count);
            Assert.AreSame(_graph.GetNode(1), _result[0]);
        }

        [Test]
        public void SameResultList_100Calls_NoPollution()
        {
            var a = Node(0, 0, isStart: true);
            var b = Node(1, 0);
            var c = Node(2, 0, isExit: true);
            Connect(a, b);
            Connect(b, c);
            Build();

            for (int i = 0; i < 100; i++)
            {
                bool ok = _graph.TryFindRoute(_graph.GetNode(0), _graph.GetNode(2),
                                              PathAgent.All, _result);
                Assert.IsTrue(ok);
                Assert.AreEqual(3, _result.Count);
                Assert.AreSame(_graph.GetNode(0), _result[0]);
                Assert.AreSame(_graph.GetNode(2), _result[2]);
            }
        }
    }
}
