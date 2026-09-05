using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using PMF.Grid;

namespace PMF.Tests
{
    /// <summary>장애물 2종의 <b>사거리 판정</b> 규칙 (G-22, ADR-0017).
    ///
    /// 이 규칙은 조용히 되돌아가기 쉽다 — "장애물이면 다 막으면 되지" 로 한 줄만 고쳐도
    /// 물이 원거리를 막아버리거나, 도로가 시야를 끊어 <b>모든 적이 사거리 밖</b>이 된다.
    /// 그래서 세 경우(벽·물·도로)를 각각 못박아 둔다.</summary>
    public sealed class ObstacleSightTests
    {
        private GameObject _root;

        /// <summary>가로 8칸짜리 한 줄 맵. x=3 칸만 지정한 타입으로 칠하고 나머지는 Ground.</summary>
        private GridSystem BuildRowWithObstacleAt3(CellType obstacle)
        {
            _root = new GameObject("grid");
            var grid = _root.AddComponent<GridSystem>();

            var type = typeof(GridSystem);
            type.GetField("_width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(grid, 8);
            type.GetField("_height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(grid, 3);
            type.GetField("_origin", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(grid, Vector3.zero);
            type.GetField("_cellSize", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(grid, 1f);

            var groundMap = new GameObject("ground").AddComponent<Tilemap>();
            var obstacleMap = new GameObject("obstacle").AddComponent<Tilemap>();
            groundMap.transform.SetParent(_root.transform);
            obstacleMap.transform.SetParent(_root.transform);

            string field = obstacle == CellType.Water ? "_water"
                         : obstacle == CellType.Road ? "_road"
                         : "_blocked";
            type.GetField("_ground", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(grid, groundMap);
            type.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(grid, obstacleMap);

            var tile = ScriptableObject.CreateInstance<Tile>();
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 8; x++)
                    groundMap.SetTile(new Vector3Int(x, y, 0), tile);
            obstacleMap.SetTile(new Vector3Int(3, 1, 0), tile);

            grid.BuildFromTilemaps();
            Assert.AreEqual(obstacle, grid.GetCell(new GridCoord(3, 1)), "테스트 맵 구성 실패");
            return grid;
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
        }

        private static (Vector3 left, Vector3 right) Sides(GridSystem grid)
            => (grid.CellToWorld(new GridCoord(1, 1)), grid.CellToWorld(new GridCoord(5, 1)));

        [Test]
        public void 벽은_근접도_원거리도_막는다()
        {
            var grid = BuildRowWithObstacleAt3(CellType.Blocked);
            var (left, right) = Sides(grid);
            Assert.IsFalse(grid.HasLineOfSight(left, right, waterBlocks: true), "근접이 벽을 넘었다");
            Assert.IsFalse(grid.HasLineOfSight(left, right, waterBlocks: false), "원거리가 벽을 넘었다");
        }

        [Test]
        public void 물은_근접만_막고_원거리는_통과시킨다()
        {
            var grid = BuildRowWithObstacleAt3(CellType.Water);
            var (left, right) = Sides(grid);
            Assert.IsFalse(grid.HasLineOfSight(left, right, waterBlocks: true), "근접이 물을 건넜다");
            Assert.IsTrue(grid.HasLineOfSight(left, right, waterBlocks: false), "원거리가 물 너머를 못 쐈다");
        }

        [Test]
        public void 도로는_시야를_막지_않는다()
        {
            // 적은 항상 도로 위를 걷는다. 도로가 시야를 끊으면 모든 적이 사거리 밖이 된다.
            var grid = BuildRowWithObstacleAt3(CellType.Road);
            var (left, right) = Sides(grid);
            Assert.IsTrue(grid.HasLineOfSight(left, right, waterBlocks: true), "근접이 도로 너머를 못 봤다");
            Assert.IsTrue(grid.HasLineOfSight(left, right, waterBlocks: false), "원거리가 도로 너머를 못 봤다");
        }

        [Test]
        public void 통행은_벽과_물을_똑같이_막는다()
        {
            var wall = BuildRowWithObstacleAt3(CellType.Blocked);
            Assert.IsFalse(wall.IsWalkable(new GridCoord(3, 1)), "벽에 들어갈 수 있다고 나왔다");
            Object.DestroyImmediate(_root);

            var water = BuildRowWithObstacleAt3(CellType.Water);
            Assert.IsFalse(water.IsWalkable(new GridCoord(3, 1)), "물에 들어갈 수 있다고 나왔다");
            Assert.IsTrue(water.IsWalkable(new GridCoord(2, 1)), "멀쩡한 땅을 못 걷는다고 나왔다");
        }

        [Test]
        public void 잘린_사거리는_장애물_직전에서_멈춘다()
        {
            var grid = BuildRowWithObstacleAt3(CellType.Blocked);
            var from = grid.CellToWorld(new GridCoord(1, 1));   // 셀 중심 = (1.5, 1.5)
            // x=3 칸의 왼쪽 경계는 월드 x=3.0 → 1.5 에서 1.5 만큼 떨어져 있다.
            float reach = grid.LineOfSightDistance(from, Vector3.right, 5f, waterBlocks: false);
            Assert.Less(reach, 1.75f, "벽을 지나쳐서 쟀다");
            Assert.Greater(reach, 1.0f, "벽에 닿기도 전에 멈췄다");

            // 막힌 것이 없는 방향은 상한을 그대로 돌려준다.
            float clear = grid.LineOfSightDistance(from, Vector3.left, 1f, waterBlocks: false);
            Assert.AreEqual(1f, clear, 0.001f);
        }
    }
}
