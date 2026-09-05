using System.Collections.Generic;
using UnityEngine;
using PMF.Grid;

namespace PMF.Pathing
{
    /// <summary>아군이 도로·장애물을 <b>피해서</b> 걸어가는 경로를 푼다.
    ///
    /// <b>격자 패스파인딩이 아니다.</b> ADR-0004 는 A*/NavMesh 그리드 탐색을 금지하고
    /// "노드 단위 그래프 탐색만" 허용한다. 여기서는 장애물의 <b>바깥 모서리</b>에만 노드를 두고,
    /// 서로 직선으로 보이는 노드끼리 이으며(가시성 그래프), 그 위에서 다익스트라를 돈다.
    /// 칸 단위로 꺾이지 않고 모서리만 스치는 자연스러운 경로가 나온다
    /// (요구: "이동 경로가 반드시 타일 단위로 움직일 필요는 없음").
    ///
    /// 왜 필요했나: 예전에는 "직선이 막히면 마을을 한 번 경유"로 때웠다. 그런데 걸어다닐 수 있는 땅이
    /// 실제로는 크게 이어져 있어서(실측: 두 마을이 같은 215칸 구역), 갈 수 있는데도 못 간다고 거절하거나
    /// 멀쩡한 거리를 마을까지 되돌아갔다가 가는 이상한 동선이 나왔다.
    ///
    /// 그래프는 맵당 한 번만 만든다. 노드는 수십 개 수준이라 매 질의 비용도 작다.</summary>
    public static class AllyWalkGraph
    {
        private static readonly List<Vector3> _nodes = new List<Vector3>();
        private static readonly List<List<int>> _edges = new List<List<int>>();
        private static bool _built;

        // 도메인 리로드 OFF — static 이 다음 플레이로 샌다 (CLAUDE.md §3).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _nodes.Clear();
            _edges.Clear();
            _built = false;
        }

        /// <summary>from 에서 to 까지 도로·장애물을 밟지 않는 경로를 찾는다.
        /// 성공하면 <paramref name="result"/> 에 <b>경유점들과 목적지</b>가 순서대로 담긴다 (from 은 제외).
        /// 직선이 뚫려 있으면 목적지 하나만 담긴다 — 평소에는 최단거리 그대로다.</summary>
        public static bool TryFindPath(Vector3 from, Vector3 to, List<Vector3> result)
        {
            result.Clear();

            if (IsClear(from, to))
            {
                result.Add(to);
                return true;
            }

            EnsureBuilt();
            if (_nodes.Count == 0) return false;

            // 출발·도착에서 보이는 모서리 노드만 추려 다익스트라를 돈다.
            int count = _nodes.Count;
            var distance = new float[count];
            var previous = new int[count];
            var visited = new bool[count];
            for (int i = 0; i < count; i++)
            {
                distance[i] = float.MaxValue;
                previous[i] = -1;
            }

            for (int i = 0; i < count; i++)
                if (IsClear(from, _nodes[i]))
                    distance[i] = Vector3.Distance(from, _nodes[i]);

            int best = -1;
            float bestTotal = float.MaxValue;

            for (int step = 0; step < count; step++)
            {
                int current = -1;
                float min = float.MaxValue;
                for (int i = 0; i < count; i++)
                {
                    if (visited[i] || distance[i] >= min) continue;
                    min = distance[i];
                    current = i;
                }
                if (current < 0) break;
                visited[current] = true;

                // 여기서 목적지가 보이면 후보 완성.
                if (IsClear(_nodes[current], to))
                {
                    float total = distance[current] + Vector3.Distance(_nodes[current], to);
                    if (total < bestTotal)
                    {
                        bestTotal = total;
                        best = current;
                    }
                }

                var neighbours = _edges[current];
                for (int k = 0; k < neighbours.Count; k++)
                {
                    int n = neighbours[k];
                    if (visited[n]) continue;
                    float d = distance[current] + Vector3.Distance(_nodes[current], _nodes[n]);
                    if (d >= distance[n]) continue;
                    distance[n] = d;
                    previous[n] = current;
                }
            }

            if (best < 0) return false;

            // 역추적 → 경유점 순서대로
            var reversed = new List<int>();
            for (int i = best; i >= 0; i = previous[i]) reversed.Add(i);
            for (int i = reversed.Count - 1; i >= 0; i--) result.Add(_nodes[reversed[i]]);
            result.Add(to);
            return true;
        }

        /// <summary>두 점을 잇는 직선이 도로·장애물을 지나지 않는가.
        /// <b>장애물 2종(벽·물)은 이동에 대해서는 똑같이 막힌 칸이다</b> (G-22).</summary>
        public static bool IsClear(Vector3 from, Vector3 to)
        {
            var grid = GridSystem.Instance;
            if (grid == null) return true;

            float distance = Vector3.Distance(from, to);
            int steps = Mathf.CeilToInt(distance / (grid.CellSize * 0.5f));
            for (int i = 0; i <= steps; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, i / (float)Mathf.Max(steps, 1));
                if (IsObstacle(grid, grid.WorldToCell(p))) return false;
            }
            return true;
        }

        private static void EnsureBuilt()
        {
            if (_built) return;
            var grid = GridSystem.Instance;
            if (grid == null) return;

            _built = true;
            _nodes.Clear();
            _edges.Clear();

            // 장애물의 <b>바깥 모서리</b>만 노드로 삼는다.
            // 어떤 칸 c 가, 대각 방향의 칸은 막혀 있는데 그 사이 두 직교 칸은 뚫려 있다면
            // c 는 그 장애물을 돌아 나가는 지점이다. 벽을 따라 늘어선 칸은 노드가 되지 않는다.
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    var c = new GridCoord(x, y);
                    if (IsObstacle(grid, c)) continue;

                    bool corner = false;
                    for (int k = 0; k < 4 && !corner; k++)
                    {
                        int dx = (k == 0 || k == 3) ? 1 : -1;
                        int dy = (k <= 1) ? 1 : -1;
                        if (!IsObstacle(grid, new GridCoord(x + dx, y + dy))) continue;
                        if (IsObstacle(grid, new GridCoord(x + dx, y))) continue;
                        if (IsObstacle(grid, new GridCoord(x, y + dy))) continue;
                        corner = true;
                    }
                    if (corner) _nodes.Add(grid.CellToWorld(c));
                }
            }

            for (int i = 0; i < _nodes.Count; i++) _edges.Add(new List<int>());
            for (int i = 0; i < _nodes.Count; i++)
            {
                for (int j = i + 1; j < _nodes.Count; j++)
                {
                    if (!IsClear(_nodes[i], _nodes[j])) continue;
                    _edges[i].Add(j);
                    _edges[j].Add(i);
                }
            }

            Debug.Log($"[AllyWalkGraph] 모서리 노드 {_nodes.Count}개로 아군 이동 그래프 구성");
        }

        /// <summary>아군이 발을 디딜 수 없는 칸인가. 도로 + 장애물 2종(벽·물) 전부다 (G-22).</summary>
        /// <summary>아군이 발을 디딜 수 없는 칸인가. 도로 + 장애물 2종(벽·물) 전부다 (G-22).
        /// 범위 밖은 <see cref="GridSystem.GetCell"/> 이 Blocked 로 돌려주므로 자동으로 막힌다.</summary>
        private static bool IsObstacle(GridSystem grid, GridCoord c)
            => !grid.IsWalkable(c) || grid.GetCell(c) == CellType.Road;
    }
}
