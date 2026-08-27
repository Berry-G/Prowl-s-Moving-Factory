using System.Collections.Generic;
using UnityEngine;

namespace PMF.EditorTools
{
    /// <summary>
    /// 테스트 맵 레이아웃 (P-04 지정, P-21까지 유지).
    /// 32x18, 경로가 좌우로 지그재그 (꺾임 4회), Buildable 비대칭, 마을 3곳, Blocked 덩어리 3개.
    /// </summary>
    internal static class GreyboxMapData
    {
        public const int Width = 32;
        public const int Height = 18;
        public static readonly Vector3 Origin = new Vector3(-16f, -9f, 0f);

        // --- 도로 세그먼트 ---
        public static readonly List<Vector2Int> RoadSegments = new List<Vector2Int>
        {
            new Vector2Int(1, 9),  new Vector2Int(8, 9),    // S1 가로
            new Vector2Int(8, 4),  new Vector2Int(8, 9),    // S2 세로
            new Vector2Int(8, 4),  new Vector2Int(16, 4),   // S3 가로
            new Vector2Int(16, 4), new Vector2Int(16, 13),  // S4 세로
            new Vector2Int(16, 13), new Vector2Int(24, 13), // S5 가로
            new Vector2Int(24, 6), new Vector2Int(24, 13),  // S6 세로
            new Vector2Int(24, 6), new Vector2Int(30, 6),   // S7 가로
        };

        // --- Buildable 사각형들 (비대칭: A 28 > B 12) ---
        // G-01 (2026-08-27): 근접 병종(고양이 수인) 사거리 1.6 이 경로에 닿으려면 Buildable 이 도로에
        // **인접(셀 중심 거리 1.0)** 해야 한다. 기존 rects 는 모두 도로에서 2칸 떨어져 있어서
        // 근접이 경로 전체를 커버하지 못했다 (147 샘플 중 126 미커버 실측). → 도로 쪽으로 확장.
        // 사거리는 절대 올리지 않는다 (TASKS G-01 확정 처방 ①).
        public static readonly RectInt BuildableA = new RectInt(2, 10, 6, 5);   // 아래로 확장 — S1 도로(y=9)에 인접
        public static readonly RectInt BuildableB = new RectInt(9, 5, 7, 4);    // 왼쪽·아래로 확장 — S2(x=8)·S3(y=4)에 인접
        public static readonly RectInt BuildableC = new RectInt(25, 7, 5, 5);   // 왼쪽·아래로 확장 — S6(x=24)·S7(y=6)에 인접
        public static readonly RectInt BuildableD = new RectInt(17, 9, 3, 4);   // 신규 — S4(x=16)·S5(y=13)에 인접
        public static readonly RectInt BuildableE = new RectInt(20, 12, 4, 1);  // 신규 — S5(y=13)·S6(x=24)에 인접

        // --- 마을 슬롯 3곳 ---
        public static readonly Vector2Int[] Villages =
        {
            new Vector2Int(4, 10),   // 시작 근처
            new Vector2Int(12, 10),  // 중앙 위
            new Vector2Int(26, 4),   // 우측 아래
        };

        // --- Blocked 덩어리 3개 ---
        public static readonly RectInt Blob1 = new RectInt(18, 2, 5, 3);
        public static readonly RectInt Blob2 = new RectInt(4, 1, 3, 3);
        public static readonly RectInt Blob3 = new RectInt(27, 13, 4, 3);

        // --- 경로 노드 좌표 ---
        public static readonly Vector2Int N0_Start = new Vector2Int(1, 9);
        public static readonly Vector2Int N13_Exit = new Vector2Int(30, 6);

        public static readonly Vector2Int EscorteeSpawnCell = N0_Start;

        /// <summary>모체는 "추격자"다 — 호위대상과 같은 시작점에서 출발해야 뒤쫓아오는 그림이 된다.
        /// 예전에 탈출 지점 근처(반대쪽 끝)에 찍혀 있어서 방향이 거꾸로였다.</summary>
        public static readonly Vector2Int MotherSpawnCell = N0_Start;

        /// <summary>노드 정의: 이름 / 셀 / 플래그. 순서 = Id (계층 순서).</summary>
        public struct NodeDef
        {
            public string Name;
            public Vector2Int Cell;
            public bool IsStart;
            public bool IsExit;
            public NodeDef(string name, int x, int y, bool isStart = false, bool isExit = false)
            {
                Name = name; Cell = new Vector2Int(x, y); IsStart = isStart; IsExit = isExit;
            }
        }

        public static readonly NodeDef[] Nodes =
        {
            new NodeDef("N00_start",     1, 9,  isStart: true),
            new NodeDef("N01",           5, 9),
            new NodeDef("N02",           8, 9),
            new NodeDef("N03",           8, 5),
            new NodeDef("N04",          11, 4),
            new NodeDef("N05",          15, 4),
            new NodeDef("N06",          16, 5),
            new NodeDef("N07",          16, 13),
            new NodeDef("N08",          18, 13),
            new NodeDef("N09",          21, 13),
            new NodeDef("N10",          24, 12),
            new NodeDef("N11",          24, 7),
            new NodeDef("N12",          27, 6),
            new NodeDef("N13_exit",     30, 6, isExit: true),
            new NodeDef("B01_branch",   12, 8),
            new NodeDef("B02_branch",   20, 10),
        };

        /// <summary>엣지: (fromIndex, toIndex). Bidirectional=true, Allowed=All.</summary>
        public static readonly (int from, int to)[] Edges =
        {
            (0, 1), (1, 2), (2, 3), (3, 4), (4, 5), (5, 6), (6, 7),
            (7, 8), (8, 9), (9, 10), (10, 11), (11, 12), (12, 13),
            (14, 4), (14, 5),      // B01 분기
            (15, 8), (15, 9),      // B02 분기
        };

        /// <summary>지름길 엣지: 보호대상 전용. n5 → n8 우회 절약.</summary>
        public static readonly (int from, int to) ShortcutEdge = (5, 8);

        /// <summary>맵 전체 칠하기 (Ground/Road/Buildable/VillageSlot/Blocked 판정).</summary>
        public enum Category { Empty, Ground, Road, Buildable, Village, Blocked }

        public static Category GetCategory(int x, int y)
        {
            foreach (var v in Villages)
                if (v.x == x && v.y == y) return Category.Village;
            if (InRect(Blob1, x, y) || InRect(Blob2, x, y) || InRect(Blob3, x, y))
                return Category.Blocked;
            if (InRect(BuildableA, x, y) || InRect(BuildableB, x, y) || InRect(BuildableC, x, y)
                || InRect(BuildableD, x, y) || InRect(BuildableE, x, y))
                return Category.Buildable;
            if (OnRoad(x, y)) return Category.Road;

            // 테두리 1칸은 비워 둔다 (Blocked).
            if (x >= 1 && x <= 30 && y >= 1 && y <= 16)
                return Category.Ground;
            return Category.Empty;
        }

        private static bool OnRoad(int x, int y)
        {
            for (int i = 0; i + 1 < RoadSegments.Count; i += 2)
            {
                var a = RoadSegments[i];
                var b = RoadSegments[i + 1];
                if (a.y == b.y && y == a.y && x >= Mathf.Min(a.x, b.x) && x <= Mathf.Max(a.x, b.x))
                    return true;
                if (a.x == b.x && x == a.x && y >= Mathf.Min(a.y, b.y) && y <= Mathf.Max(a.y, b.y))
                    return true;
            }
            return false;
        }

        private static bool InRect(RectInt r, int x, int y)
            => x >= r.xMin && x < r.xMax && y >= r.yMin && y < r.yMax;
    }
}
