using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using PMF.Grid;

namespace PMF.Tests
{
    /// <summary>GridCoord / GridSystem 좌표 변환 테스트 (P-03 DoD).</summary>
    public sealed class GridSystemTests
    {
        private static void Configure(GridSystem grid, int width, int height,
                                      Vector3 origin, float cellSize)
        {
            var type = typeof(GridSystem);
            type.GetField("_width", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(grid, width);
            type.GetField("_height", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(grid, height);
            type.GetField("_origin", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(grid, origin);
            type.GetField("_cellSize", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(grid, cellSize);
        }

        private static GridSystem CreateGrid(int width, int height, Vector3 origin)
        {
            var go = new GameObject("grid");
            var grid = go.AddComponent<GridSystem>();
            // 타일맵 미연결 → BuildFromTilemaps 가 전부 Ground 로 채운다 (P-03 기본 동작).
            Configure(grid, width, height, origin, 1f);
            grid.BuildFromTilemaps();
            return grid;
        }

        [TestCase(-16f, -9f)]
        [TestCase(0f, 0f)]
        [TestCase(-5.5f, 3f)]
        public void CellToWorld_WorldToCell_RoundTrip_AllCells(float ox, float oy)
        {
            var grid = CreateGrid(8, 6, new Vector3(ox, oy, 0f));
            for (int y = 0; y < 6; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    var coord = new GridCoord(x, y);
                    var center = grid.CellToWorld(coord);
                    var back = grid.WorldToCell(center);
                    Assert.AreEqual(coord, back, $"({ox},{oy}) 셀 ({x},{y}) 왕복 실패");
                }
            }
            Object.DestroyImmediate(grid.gameObject);
        }

        [Test]
        public void WorldToCell_UsesFloor_NotTruncation()
        {
            var grid = CreateGrid(4, 4, new Vector3(-2f, -2f, 0f));
            // origin(-2) 에서 월드 -2.5 는 한 칸 아래인 셀 (-1,-1) 이다.
            // ((int) 캐스팅이면 -0.5 가 0 으로 잘려서 (0,0) 이 나온다 — 그 버그를 잡는다.
            var coord = grid.WorldToCell(new Vector3(-2.5f, -2.5f, 0f));
            Assert.AreEqual(new GridCoord(-1, -1), coord);
            Object.DestroyImmediate(grid.gameObject);
        }

        [Test]
        public void InBounds_BoundaryValues()
        {
            var grid = CreateGrid(4, 4, Vector3.zero);

            Assert.IsTrue(grid.InBounds(new GridCoord(0, 0)));
            Assert.IsTrue(grid.InBounds(new GridCoord(3, 3)));
            Assert.IsFalse(grid.InBounds(new GridCoord(-1, 0)));
            Assert.IsFalse(grid.InBounds(new GridCoord(0, -1)));
            Assert.IsFalse(grid.InBounds(new GridCoord(4, 0)));
            Assert.IsFalse(grid.InBounds(new GridCoord(0, 4)));

            Object.DestroyImmediate(grid.gameObject);
        }

        [Test]
        public void GetCell_OutOfBounds_ReturnsBlocked_NoException()
        {
            var grid = CreateGrid(4, 4, Vector3.zero);
            Assert.AreEqual(CellType.Blocked, grid.GetCell(new GridCoord(-1, 0)));
            Assert.AreEqual(CellType.Blocked, grid.GetCell(new GridCoord(99, 99)));
            Object.DestroyImmediate(grid.gameObject);
        }

        [Test]
        public void BuildFromTilemaps_NoLayers_FillsGround()
        {
            var grid = CreateGrid(4, 4, Vector3.zero);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    Assert.AreEqual(CellType.Ground, grid.GetCell(new GridCoord(x, y)));
            Object.DestroyImmediate(grid.gameObject);
        }

        [Test]
        public void TilemapLayers_PriorityOrder()
        {
            // Ground 위에 Road, 그 위에 Blocked — 뒤 레이어가 이긴다.
            var go = new GameObject("grid");
            var grid = go.AddComponent<GridSystem>();

            var groundGo = new GameObject("g");
            var roadGo = new GameObject("r");
            var blockedGo = new GameObject("b");
            var groundMap = groundGo.AddComponent<Tilemap>();
            var roadMap = roadGo.AddComponent<Tilemap>();
            var blockedMap = blockedGo.AddComponent<Tilemap>();

            var tile = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();

            Configure(grid, 4, 4, Vector3.zero, 1f);
            typeof(GridSystem)
                .GetField("_ground", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(grid, groundMap);
            typeof(GridSystem)
                .GetField("_road", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(grid, roadMap);
            typeof(GridSystem)
                .GetField("_blocked", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(grid, blockedMap);

            groundMap.SetTile(new Vector3Int(0, 0, 0), tile);   // Ground
            roadMap.SetTile(new Vector3Int(1, 0, 0), tile);     // Road (Ground 없음 → Road)
            groundMap.SetTile(new Vector3Int(2, 0, 0), tile);
            roadMap.SetTile(new Vector3Int(2, 0, 0), tile);     // Ground+Road → Road 우선
            groundMap.SetTile(new Vector3Int(3, 0, 0), tile);
            roadMap.SetTile(new Vector3Int(3, 0, 0), tile);
            blockedMap.SetTile(new Vector3Int(3, 0, 0), tile);  // 전부 덮음 → Blocked

            grid.BuildFromTilemaps();

            Assert.AreEqual(CellType.Ground, grid.GetCell(new GridCoord(0, 0)));
            Assert.AreEqual(CellType.Road, grid.GetCell(new GridCoord(1, 0)));
            Assert.AreEqual(CellType.Road, grid.GetCell(new GridCoord(2, 0)));
            Assert.AreEqual(CellType.Blocked, grid.GetCell(new GridCoord(3, 0)));
            // 칠해지지 않은 곳은 Blocked.
            Assert.AreEqual(CellType.Blocked, grid.GetCell(new GridCoord(0, 1)));

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(groundGo);
            Object.DestroyImmediate(roadGo);
            Object.DestroyImmediate(blockedGo);
        }
    }
}
