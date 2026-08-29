using System.Collections.Generic;
using UnityEngine;
using PMF.Grid;

namespace PMF.Pathing
{
    /// <summary>경로 그래프의 노드. 항상 셀 중심에 정렬된다.</summary>
    public sealed class PathNode
    {
        public int Id { get; }

        /// <summary>저작 오브젝트 이름 (예: "N06"). 스테이지 SO 가 노드를 이름으로 가리킬 때 쓴다 (G-20 버스트 트리거).
        /// 이름으로 참조하면 오타가 런타임까지 갈 수 있으므로 <b>쓰는 쪽에서 시작 시 조회해 검증</b>해야 한다.</summary>
        public string Name { get; }

        public GridCoord Coord { get; }
        public Vector3 WorldPosition { get; }   // GridSystem.CellToWorld 결과를 캐시

        private readonly List<PathEdge> _edges = new List<PathEdge>();
        public IReadOnlyList<PathEdge> Edges => _edges;

        /// <summary>테스트에서도 노드를 만들 수 있도록 public.</summary>
        public PathNode(int id, GridCoord coord, Vector3 worldPosition, string name = null)
        {
            Id = id;
            Coord = coord;
            WorldPosition = worldPosition;
            Name = string.IsNullOrEmpty(name) ? $"N{id:00}" : name;
        }

        internal void AddEdge(PathEdge edge) => _edges.Add(edge);

        public override string ToString() => $"Node{Id}{Coord}";
    }
}
