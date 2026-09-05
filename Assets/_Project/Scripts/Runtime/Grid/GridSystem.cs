using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PMF.Grid
{
    /// <summary>
    /// 씬 단일. 격자 ↔ 월드 변환의 유일한 창구.
    /// 다른 어떤 클래스도 좌표 변환 수식을 직접 갖지 마라.
    ///
    /// 셀 중심 규약:
    ///   셀 (x, y) 의 중심 월드 좌표 = _origin + ((x + 0.5f) * _cellSize, (y + 0.5f) * _cellSize)
    ///   즉 _origin 은 셀 (0,0) 의 좌하단 모서리다.
    ///   WorldToCell 은 Mathf.FloorToInt 를 쓴다 (음수 좌표에서 (int) 캐스팅은 0 방향으로 잘라서 틀린다).
    /// </summary>
    public sealed class GridSystem : MonoBehaviour
    {
        public static GridSystem Instance { get; private set; }

        [SerializeField] private int _width = 32;
        [SerializeField] private int _height = 18;
        [SerializeField] private Vector3 _origin = new Vector3(-16f, -9f, 0f);
        [SerializeField] private float _cellSize = 1f;

        [Header("Tilemap 레이어 (덮어쓰기 우선순위: Ground → Road → Buildable → VillageSlot → Water → Blocked)")]
        [SerializeField] private Tilemap _ground;
        [SerializeField] private Tilemap _road;
        [SerializeField] private Tilemap _buildable;
        [SerializeField] private Tilemap _villageSlot;
        [SerializeField] private Tilemap _water;
        [SerializeField] private Tilemap _blocked;

        private CellType[] _cells;

        public int Width => _width;
        public int Height => _height;
        public Vector3 Origin => _origin;
        public float CellSize => _cellSize;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError($"[{nameof(GridSystem)}] 씬에 두 개 이상 존재합니다.", this);
                enabled = false;
                return;
            }

            Instance = this;
            BuildFromTilemaps();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Tilemap 레이어 스캔으로 셀 데이터를 채운다.
        /// 타일맵이 하나도 연결되어 있지 않으면 전부 Ground 로 초기화한다 (테스트/P-03 기본 동작).
        /// </summary>
        public void BuildFromTilemaps()
        {
            int count = _width * _height;
            _cells = new CellType[count];

            bool hasAnyLayer = _ground != null || _road != null || _buildable != null
                               || _villageSlot != null || _water != null || _blocked != null;
            if (!hasAnyLayer)
            {
                for (int i = 0; i < count; i++) _cells[i] = CellType.Ground;
                return;
            }

            // 아무 타일도 없는 칸은 Blocked (맵 밖 = 갈 수 없음).
            for (int i = 0; i < count; i++) _cells[i] = CellType.Blocked;

            PaintLayer(_ground, CellType.Ground);
            PaintLayer(_road, CellType.Road);
            PaintLayer(_buildable, CellType.Buildable);
            PaintLayer(_villageSlot, CellType.VillageSlot);
            PaintLayer(_water, CellType.Water);
            PaintLayer(_blocked, CellType.Blocked);

            LogCellCounts();
        }

        private void PaintLayer(Tilemap layer, CellType type)
        {
            if (layer == null) return;

            // GridSystem 의 범위를 기준으로 순회하고 Tilemap 에 질의한다 (cellBounds 을 기준으로 삼지 마라).
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    if (layer.HasTile(new Vector3Int(x, y, 0)))
                        _cells[y * _width + x] = type;
                }
            }
        }

        private void LogCellCounts()
        {
            var counts = new int[System.Enum.GetValues(typeof(CellType)).Length];
            for (int i = 0; i < _cells.Length; i++) counts[(int)_cells[i]]++;

            var sb = new StringBuilder("[GridSystem] ");
            sb.Append(nameof(CellType.Ground)).Append('=').Append(counts[(int)CellType.Ground]);
            sb.Append(' ').Append(nameof(CellType.Road)).Append('=').Append(counts[(int)CellType.Road]);
            sb.Append(' ').Append(nameof(CellType.Buildable)).Append('=').Append(counts[(int)CellType.Buildable]);
            sb.Append(' ').Append(nameof(CellType.VillageSlot)).Append('=').Append(counts[(int)CellType.VillageSlot]);
            sb.Append(' ').Append(nameof(CellType.Water)).Append('=').Append(counts[(int)CellType.Water]);
            sb.Append(' ').Append(nameof(CellType.Blocked)).Append('=').Append(counts[(int)CellType.Blocked]);
            Debug.Log(sb.ToString(), this);
        }

        /// <summary>셀의 중심 월드 좌표. (모서리가 아니다)</summary>
        public Vector3 CellToWorld(GridCoord coord)
            => _origin + new Vector3((coord.X + 0.5f) * _cellSize, (coord.Y + 0.5f) * _cellSize, 0f);

        /// <summary>
        /// 월드 좌표가 속한 셀. 범위 밖이어도 좌표는 반환하므로 InBounds 로 확인하라.
        /// FloorToInt: _origin 이 셀 (0,0) 의 좌하단 모서리이므로 RoundToInt 가 아니다.
        /// </summary>
        public GridCoord WorldToCell(Vector3 world)
        {
            int x = Mathf.FloorToInt((world.x - _origin.x) / _cellSize);
            int y = Mathf.FloorToInt((world.y - _origin.y) / _cellSize);
            return new GridCoord(x, y);
        }

        public bool InBounds(GridCoord coord)
            => coord.X >= 0 && coord.X < _width && coord.Y >= 0 && coord.Y < _height;

        /// <summary>범위 밖이면 CellType.Blocked 를 반환한다 (예외를 던지지 않는다).</summary>
        public CellType GetCell(GridCoord coord)
            => InBounds(coord) ? _cells[coord.Y * _width + coord.X] : CellType.Blocked;

        /// <summary>걸어 들어갈 수 있는 칸인가. <b>장애물 2종(벽·물)은 둘 다 못 들어간다</b> (G-22).</summary>
        public bool IsWalkable(GridCoord coord)
        {
            var cell = GetCell(coord);
            return cell != CellType.Blocked && cell != CellType.Water;
        }

        public bool IsBuildable(GridCoord coord) => GetCell(coord) == CellType.Buildable;

        /// <summary>사거리 판정에서 이 칸이 <paramref name="waterBlocks"/> 기준으로 막힌 칸인가.
        /// 벽은 언제나 막고, 물은 근접 병종에게만 막힌다 (G-22).</summary>
        private bool BlocksAttack(GridCoord coord, bool waterBlocks)
        {
            var cell = GetCell(coord);
            return cell == CellType.Blocked || (waterBlocks && cell == CellType.Water);
        }

        /// <summary>두 점 사이가 <b>공격 가능하게</b> 뚫려 있는가.
        ///
        /// <b>도로는 절대 막지 않는다.</b> 적은 항상 도로 위를 걷기 때문에, 도로를 차단으로 치면
        /// 모든 적이 사거리 밖이 되어 게임이 성립하지 않는다.
        /// 그래서 아군의 <b>이동</b> 판정(도로를 밟을 수 없다)과는 다른 함수다 —
        /// <see cref="PMF.Pathing.AllyWalkGraph.IsClear"/> 를 재사용하지 마라. 둘은 목적이 다르다.
        ///
        /// <paramref name="waterBlocks"/>: 근접 병종이면 true. 물은 시야를 가리지 않지만
        /// 걸어 들어갈 수 없어서 붙을 수가 없다 (G-22).
        ///
        /// 격자↔월드 변환이 필요한 판정이므로 CLAUDE.md §4 좌표 규칙에 따라 여기(GridSystem)에 둔다.</summary>
        public bool HasLineOfSight(Vector3 from, Vector3 to, bool waterBlocks)
        {
            float distance = Vector3.Distance(from, to);
            int steps = Mathf.CeilToInt(distance / (_cellSize * 0.5f));
            for (int i = 0; i <= steps; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, i / (float)Mathf.Max(steps, 1));
                if (BlocksAttack(WorldToCell(p), waterBlocks)) return false;
            }
            return true;
        }

        /// <summary>from 에서 <paramref name="direction"/> 방향으로 막히기 전까지 공격이 닿는 거리.
        /// <paramref name="maxDistance"/> 를 넘지 않는다. 사거리 원을 장애물 모양대로 잘라 그릴 때 쓴다 (G-22).</summary>
        public float LineOfSightDistance(Vector3 from, Vector3 direction, float maxDistance, bool waterBlocks)
        {
            float step = _cellSize * 0.25f;
            float travelled = 0f;
            for (float d = step; d <= maxDistance; d += step)
            {
                if (BlocksAttack(WorldToCell(from + direction * d), waterBlocks)) return travelled;
                travelled = d;
            }
            return maxDistance;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
            Vector3 size = new Vector3(_width * _cellSize, _height * _cellSize, 0f);

            for (int x = 0; x <= _width; x++)
            {
                Vector3 from = _origin + new Vector3(x * _cellSize, 0f, 0f);
                Gizmos.DrawLine(from, from + new Vector3(0f, size.y, 0f));
            }
            for (int y = 0; y <= _height; y++)
            {
                Vector3 from = _origin + new Vector3(0f, y * _cellSize, 0f);
                Gizmos.DrawLine(from, from + new Vector3(size.x, 0f, 0f));
            }
        }
#endif
    }
}
