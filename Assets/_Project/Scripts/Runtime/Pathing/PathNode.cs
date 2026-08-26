using System.Collections.Generic;
using UnityEngine;
using PMF.Grid;

namespace PMF.Pathing
{
    /// <summary>경로 그래프의 노드. 항상 셀 중심에 정렬된다.</summary>
    public sealed class PathNode
    {
        public int Id { get; }
        public GridCoord Coord { get; }
        public Vector3 WorldPosition { get; }   // GridSystem.CellToWorld 결과를 캐시

        private readonly List<PathEdge> _edges = new List<PathEdge>();
        public IReadOnlyList<PathEdge> Edges => _edges;

        /// <summary>테스트에서도 노드를 만들 수 있도록 public.</summary>
        public PathNode(int id, GridCoord coord, Vector3 worldPosition)
        {
            Id = id;
            Coord = coord;
            WorldPosition = worldPosition;
        }

        internal void AddEdge(PathEdge edge) => _edges.Add(edge);

        public override string ToString() => $"Node{Id}{Coord}";
    }
}
