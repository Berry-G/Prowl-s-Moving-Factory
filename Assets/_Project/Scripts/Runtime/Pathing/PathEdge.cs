using UnityEngine;
using PMF.Grid;

namespace PMF.Pathing
{
    /// <summary>그래프 엣지. 통행 권한과 지름길 여부를 갖는다.</summary>
    public sealed class PathEdge
    {
        public int Id { get; }
        public PathNode From { get; }
        public PathNode To   { get; }

        /// <summary>기본값 = 월드 거리</summary>
        public float Cost { get; }

        public PathAgent Allowed { get; }
        public bool IsShortcut { get; }

        private bool _isOpen;
        /// <summary>지름길이 아니면 항상 true</summary>
        public bool IsOpen => !IsShortcut || _isOpen;

        internal PathEdge(int id, PathNode from, PathNode to, PathAgent allowed, bool isShortcut)
        {
            Id = id;
            From = from;
            To = to;
            Cost = Vector3.Distance(from.WorldPosition, to.WorldPosition);
            Allowed = allowed;
            IsShortcut = isShortcut;
            _isOpen = false;
        }

        /// <summary>(Allowed &amp; agent) != 0 &amp;&amp; IsOpen</summary>
        public bool CanTraverse(PathAgent agent) => (Allowed & agent) != 0 && IsOpen;

        internal void SetOpen(bool open) => _isOpen = open;

        public override string ToString() => $"Edge{Id}:{From.Id}->{To.Id}{(IsShortcut ? "(shortcut)" : "")}";
    }
}
