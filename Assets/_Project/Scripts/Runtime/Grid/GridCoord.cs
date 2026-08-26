using System;

namespace PMF.Grid
{
    /// <summary>
    /// 격자 좌표. Vector2Int 를 직접 쓰지 말고 이 타입을 써라 (월드 좌표와 혼동 방지).
    /// </summary>
    [Serializable]
    public readonly struct GridCoord : IEquatable<GridCoord>
    {
        public readonly int X;
        public readonly int Y;

        public GridCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static GridCoord operator +(GridCoord a, GridCoord b) => new GridCoord(a.X + b.X, a.Y + b.Y);
        public static GridCoord operator -(GridCoord a, GridCoord b) => new GridCoord(a.X - b.X, a.Y - b.Y);
        public static bool operator ==(GridCoord a, GridCoord b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(GridCoord a, GridCoord b) => !(a == b);

        public bool Equals(GridCoord other) => this == other;

        public override bool Equals(object obj) => obj is GridCoord other && this == other;

        // (X * 397) ^ Y — 충분히 분산되는 해시. 딕셔너리 성능용.
        public override int GetHashCode() => unchecked((X * 397) ^ Y);

        public override string ToString() => $"({X}, {Y})";

        /// <summary>체비셰프 거리 아님 — 맨해튼 거리</summary>
        public static int ManhattanDistance(GridCoord a, GridCoord b)
            => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }
}
