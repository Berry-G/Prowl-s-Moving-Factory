using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using PMF.Grid;

namespace PMF.Pathing
{
    /// <summary>
    /// 씬 단일. 그래프 보유 + 탐색.
    /// Awake 에서 자식의 PathNodeAuthoring 을 모아 런타임 PathNode/PathEdge 그래프를 만든다.
    /// 노드 Id 는 씬 계층 순서(GetComponentsInChildren 순서)로 결정적으로 부여된다.
    /// </summary>
    public sealed class PathGraph : MonoBehaviour
    {
        public static PathGraph Instance { get; private set; }

        private readonly List<PathNode> _nodes = new List<PathNode>();
        private readonly List<PathEdge> _edges = new List<PathEdge>();
        private readonly List<PathNode> _exitNodes = new List<PathNode>();
        private PathNode _escorteeStartNode;

        public IReadOnlyList<PathNode> Nodes => _nodes;
        public PathNode EscorteeStartNode => _escorteeStartNode;
        public IReadOnlyList<PathNode> ExitNodes => _exitNodes;

        /// <summary>지름길이 열려서 경로가 바뀐 때 발행. 구독자는 OnDisable 에서 반드시 해제.</summary>
        public event Action OnGraphChanged;

        // 다익스트라 작업용 버퍼 (매 호출 재사용, 매 호출 리셋 필수)
        private float[] _distance;
        private int[] _previous;
        private bool[] _visited;
        private const float Infinity = float.MaxValue;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError($"[{nameof(PathGraph)}] 씬에 두 개 이상 존재합니다.", this);
                enabled = false;
                return;
            }

            Instance = this;
            Build();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>저작 컴포넌트로 런타임 그래프를 만든다. 테스트에서도 직접 호출 가능.</summary>
        public void Build()
        {
            _nodes.Clear();
            _edges.Clear();
            _exitNodes.Clear();
            _escorteeStartNode = null;

            GridSystem grid = GridSystem.Instance;
            var authors = GetComponentsInChildren<PathNodeAuthoring>(true);

            for (int i = 0; i < authors.Length; i++)
            {
                var author = authors[i];
                var coord = grid != null ? grid.WorldToCell(author.transform.position) : new GridCoord(0, 0);
                Vector3 world = grid != null ? grid.CellToWorld(coord) : author.transform.position;

                var node = new PathNode(i, coord, world);
                author.Attach(node);
                _nodes.Add(node);

                if (author.IsEscorteeStart)
                {
                    if (_escorteeStartNode != null)
                        Debug.LogError($"[PathGraph] 시작 노드가 2개 이상: {author.name}", author);
                    _escorteeStartNode = node;
                }
                if (author.IsExit)
                    _exitNodes.Add(node);
            }

            int edgeId = 0;
            for (int i = 0; i < authors.Length; i++)
            {
                var author = authors[i];
                var from = author.RuntimeNode;
                if (from == null) continue;

                foreach (var conn in author.Connections)
                {
                    if (conn.Target == null) continue;   // 씬에서 요소 추가 직후의 null 걸러내기

                    var to = conn.Target.RuntimeNode;
                    if (to == null || to == from) continue;

                    AddEdge(edgeId++, from, to, conn.Allowed, conn.IsShortcut);

                    if (conn.Bidirectional && HasAuthoredReverse(conn.Target, author))
                        Debug.LogWarning($"[PathGraph] {author.name}->{conn.Target.name} 양방향이 반대편도 저작: 중복 엣지 위험.", author);

                    if (conn.Bidirectional)
                        AddEdge(edgeId++, to, from, conn.Allowed, conn.IsShortcut);
                }
            }

            Validate();
            LogSummary();

            int maxNodes = Mathf.Max(_nodes.Count, 1);
            _distance = new float[maxNodes];
            _previous = new int[maxNodes];
            _visited = new bool[maxNodes];
        }

        /// <summary>지름길을 연다. 이미 열려 있으면 false. 반대 방향 엣지도 함께 열린다.</summary>
        public bool OpenShortcut(int edgeId)
        {
            PathEdge edge = FindEdge(edgeId);
            if (edge == null || !edge.IsShortcut || edge.IsOpen) return false;

            edge.SetOpen(true);

            foreach (var e in _edges)
            {
                if (e.IsShortcut && !e.IsOpen && e.From == edge.To && e.To == edge.From)
                    e.SetOpen(true);
            }

            OnGraphChanged?.Invoke();
            return true;
        }

        private PathEdge FindEdge(int edgeId)
        {
            foreach (var e in _edges)
                if (e.Id == edgeId) return e;
            return null;
        }

        private void AddEdge(int id, PathNode from, PathNode to, PathAgent allowed, bool isShortcut)
        {
            foreach (var e in from.Edges)
            {
                if (e.To == to)
                {
                    Debug.LogWarning($"[PathGraph] 중복 엣지 무시: {from} -> {to}", this);
                    return;
                }
            }

            var edge = new PathEdge(id, from, to, allowed, isShortcut);
            from.AddEdge(edge);
            _edges.Add(edge);
        }

        private bool HasAuthoredReverse(PathNodeAuthoring target, PathNodeAuthoring source)
        {
            foreach (var c in target.Connections)
                if (c.Target == source) return true;
            return false;
        }

        private void Validate()
        {
            if (_nodes.Count == 0) return;

            if (_escorteeStartNode == null)
                Debug.LogError("[PathGraph] 보호대상 시작 노드(_isEscorteeStart)가 없습니다.", this);
            if (_exitNodes.Count == 0)
                Debug.LogError("[PathGraph] 탈출 노드(_isExit)가 없습니다.", this);

            GridSystem grid = GridSystem.Instance;
            foreach (var node in _nodes)
            {
                if (node.Edges.Count == 0)
                    Debug.LogError($"[PathGraph] 고립 노드: {node}", this);

                if (grid != null && !grid.IsWalkable(node.Coord))
                    Debug.LogError($"[PathGraph] Blocked 셀 위의 노드: {node}", this);
            }
        }

        private void LogSummary()
        {
            int shortcutCount = 0;
            foreach (var e in _edges)
                if (e.IsShortcut) shortcutCount++;

            var sb = new StringBuilder("[PathGraph] Nodes=").Append(_nodes.Count)
                .Append(" Edges=").Append(_edges.Count)
                .Append(" Shortcuts=").Append(shortcutCount);
            Debug.Log(sb.ToString(), this);
        }

        /// <summary>agent 통행 가능 엣지만 쓰는 최단 경로(다익스트라). 실패 시 result 는 비어 있다.</summary>
        public bool TryFindRoute(PathNode from, PathNode to, PathAgent agent, List<PathNode> result)
        {
            result.Clear();
            if (from == null || to == null || _nodes.Count == 0) return false;
            if (from == to)
            {
                result.Add(from);
                return true;
            }

            int n = _nodes.Count;
            for (int i = 0; i < n; i++)
            {
                _distance[i] = Infinity;
                _previous[i] = -1;
                _visited[i] = false;
            }
            _distance[from.Id] = 0f;

            for (int round = 0; round < n; round++)
            {
                int u = -1;
                float best = Infinity;
                for (int i = 0; i < n; i++)
                {
                    if (!_visited[i] && _distance[i] < best)
                    {
                        best = _distance[i];
                        u = i;
                    }
                }
                if (u == -1) break;
                if (u == to.Id) break;
                _visited[u] = true;

                var node = _nodes[u];
                for (int ei = 0; ei < node.Edges.Count; ei++)
                {
                    var edge = node.Edges[ei];
                    if (!edge.CanTraverse(agent)) continue;

                    float alt = _distance[u] + edge.Cost;
                    if (alt < _distance[edge.To.Id])
                    {
                        _distance[edge.To.Id] = alt;
                        _previous[edge.To.Id] = u;
                    }
                }
            }

            if (_distance[to.Id] >= Infinity) return false;

            int cursor = to.Id;
            var reverse = new List<int>(n);
            while (cursor != -1)
            {
                reverse.Add(cursor);
                cursor = _previous[cursor];
            }
            for (int i = reverse.Count - 1; i >= 0; i--)
                result.Add(_nodes[reverse[i]]);

            return result[0] == from;
        }

        /// <summary>world 에서 가장 가까운 노드. 통행 가능한 엣지를 하나라도 가진 노드만 후보.</summary>
        public PathNode FindNearestNode(Vector3 world, PathAgent agent)
        {
            PathNode best = null;
            float bestSqr = float.MaxValue;

            foreach (var node in _nodes)
            {
                bool hasTraversable = false;
                for (int i = 0; i < node.Edges.Count; i++)
                {
                    if (node.Edges[i].CanTraverse(agent)) { hasTraversable = true; break; }
                }
                if (!hasTraversable) continue;

                float sqr = (node.WorldPosition - world).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = node;
                }
            }
            return best;
        }

        /// <summary>from 에서 도달 가능한 노드 중 world 에 가장 가까운 것. 추격자 폴백용.</summary>
        public PathNode FindReachableNodeNearestTo(Vector3 world, PathAgent agent, PathNode from)
        {
            if (from == null || _nodes.Count == 0) return null;

            int n = _nodes.Count;
            var dist = new float[n];
            var visited = new bool[n];
            for (int i = 0; i < n; i++) dist[i] = Infinity;
            dist[from.Id] = 0f;

            for (int round = 0; round < n; round++)
            {
                int u = -1;
                float best = Infinity;
                for (int i = 0; i < n; i++)
                {
                    if (!visited[i] && dist[i] < best)
                    {
                        best = dist[i];
                        u = i;
                    }
                }
                if (u == -1) break;
                visited[u] = true;

                var node = _nodes[u];
                for (int ei = 0; ei < node.Edges.Count; ei++)
                {
                    var edge = node.Edges[ei];
                    if (!edge.CanTraverse(agent)) continue;
                    float alt = dist[u] + edge.Cost;
                    if (alt < dist[edge.To.Id]) dist[edge.To.Id] = alt;
                }
            }

            PathNode resultNode = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                if (dist[i] >= Infinity) continue;
                float sqr = (_nodes[i].WorldPosition - world).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    resultNode = _nodes[i];
                }
            }
            return resultNode;
        }

        public PathNode GetNode(int id) => (id >= 0 && id < _nodes.Count) ? _nodes[id] : null;
    }
}
