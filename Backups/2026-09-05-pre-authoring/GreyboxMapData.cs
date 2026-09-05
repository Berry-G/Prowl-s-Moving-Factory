using System.Collections.Generic;
using UnityEngine;

namespace PMF.EditorTools
{
    /// <summary>
    /// 테스트 맵 레이아웃 (P-04 지정, P-21까지 유지).
    /// 32x18, 경로가 좌우로 지그재그 (꺾임 4회), Buildable 비대칭, 마을 2곳,
    /// 벽(Blocked) 덩어리 3개 + 물(Water) 1덩어리 (G-22).
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

        // --- 마을 슬롯 2곳 ---
        // 두 마을은 도로를 건너지 않고 서로 오갈 수 있는 <b>같은 구역</b>에 있다 (실측: 걸어다닐 수 있는
        // 땅은 2구역 178칸 / 215칸이고 마을 둘 다 215칸 구역).
        // 그래서 어느 마을에서 뽑아도 같은 곳으로 보낼 수 있고, 마을은 "출발 지점"이지 "담당 구역"이 아니다.
        public static readonly Vector2Int[] Villages =
        {
            new Vector2Int(10, 13),  // 서쪽 — 경로 전반부에서 가깝다
            new Vector2Int(27, 9),   // 동쪽 — 탈출 직전 구간에서 가깝다
        };

        // --- 장애물: 벽 3덩어리 + 물 1덩어리 ---
        // 통행 규칙은 넷 다 같다(아군·적 모두 못 들어간다). 다른 건 <b>사거리 판정</b>뿐이다 (G-22, ADR-0017):
        //   벽(Blocked) — 근접·원거리 모두 너머를 못 때린다
        //   물(Water)   — 원거리는 그냥 쏘고, 근접만 못 붙는다
        public static readonly RectInt Blob1 = new RectInt(18, 2, 5, 3);
        public static readonly RectInt Blob2 = new RectInt(4, 1, 3, 3);
        public static readonly RectInt Blob3 = new RectInt(27, 13, 4, 3);

        /// <summary>물 장애물. <b>도로 S7(y=6, 탈출 직전 구간) 바로 위에 붙여 놓는다.</b>
        ///
        /// 도로에서 멀리 떨어진 물은 아무 의미가 없다 — 근접이든 원거리든 어차피 사거리가 안 닿으니
        /// 두 병종의 차이가 화면에 드러나지 않는다. 도로에 붙여야 비로소 판단이 생긴다:
        /// 동쪽 마을(27,9)에서 탈출 구간을 지키려 할 때 <b>쥐는 물 너머로 그냥 쏘고, 고양이는
        /// x=30 쪽으로 돌아 들어가야 한다.</b> "여기는 원거리 자리다" 가 지형으로 읽힌다.
        ///
        /// 이 사각형은 어떤 칸도 고립시키지 않는다 — x=30 열이 열려 있어 y=7~8 도 계속 이어져 있다.
        /// 배치 가능 칸은 391 → 381 로 10칸 줄어든다.</summary>
        public static readonly RectInt WaterBlob = new RectInt(25, 7, 5, 2);

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
            // ⚠️ 코너 노드는 반드시 도로가 꺾이는 셀에 정확히 놓아야 한다.
            // 한 칸이라도 어긋나면 이웃 노드와의 직선 구간이 대각선이 되어 코너를 가로지른다
            // (= 액터가 길 밖으로 나간다). 2026-08-29 실측으로 N03·N06·N10·N11 이 어긋나 있었다.
            // 도로 코너: (8,9) (8,4) (16,4) (16,13) (24,13) (24,6)
            new NodeDef("N03",           8, 4),   // S2↔S3 코너 (was 8,5)
            new NodeDef("N04",          11, 4),
            new NodeDef("N05",          15, 4),
            new NodeDef("N06",          16, 4),   // S3↔S4 코너 (was 16,5)
            new NodeDef("N07",          16, 13),
            new NodeDef("N08",          18, 13),
            new NodeDef("N09",          21, 13),
            new NodeDef("N10",          24, 13),  // S5↔S6 코너 (was 24,12)
            new NodeDef("N11",          24, 6),   // S6↔S7 코너 (was 24,7)
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

        /// <summary>맵 전체 칠하기 (Ground/Road/Buildable/VillageSlot/Blocked/Water 판정).</summary>
        public enum Category { Empty, Ground, Road, Buildable, Village, Blocked, Water }

        public static Category GetCategory(int x, int y)
        {
            foreach (var v in Villages)
                if (v.x == x && v.y == y) return Category.Village;
            if (InRect(WaterBlob, x, y))
                return Category.Water;
            if (InRect(Blob1, x, y) || InRect(Blob2, x, y) || InRect(Blob3, x, y))
                return Category.Blocked;
            if (OnRoad(x, y)) return Category.Road;

            // 2026-08-30 확정: <b>도로와 장애물이 아니면 어디든 배치할 수 있다.</b>
            // 사각형으로 배치 구역을 오려내던 방식을 버렸다 — 어디는 되고 어디는 안 되는 이유를
            // 화면에서 설명할 수 없었고, 실제로 "길로 막히지도 않았는데 왜 못 가지?" 라는 지적을 받았다.
            // 격자는 이제 **겹치기를 막는 용도**다 (한 칸에 한 유닛). 이동 경로는 칸에 묶이지 않는다.
            // 제약을 걸고 싶으면 장애물을 놓아서 건다 — 거대 바위(Blocked) 또는 호수(Water), G-22.
            if (x >= 1 && x <= 30 && y >= 1 && y <= 16)
                return Category.Buildable;

            // 테두리 1칸은 비워 둔다.
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
