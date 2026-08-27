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

        [Header("Tilemap 레이어 (덮어쓰기 우선순위: Ground → Road → Buildable → VillageSlot → Blocked)")]
        [SerializeField] private Tilemap _ground;
        [SerializeField] private Tilemap _road;
        [SerializeField] private Tilemap _buildable;
        [SerializeField] private Tilemap _villageSlot;
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
                               || _villageSlot != null || _blocked != null;
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
            var counts = new int[5];
            for (int i = 0; i < _cells.Length; i++) counts[(int)_cells[i]]++;

            var sb = new StringBuilder("[GridSystem] ");
            sb.Append(nameof(CellType.Ground)).Append('=').Append(counts[1]);
            sb.Append(' ').Append(nameof(CellType.Road)).Append('=').Append(counts[2]);
            sb.Append(' ').Append(nameof(CellType.Buildable)).Append('=').Append(counts[3]);
            sb.Append(' ').Append(nameof(CellType.VillageSlot)).Append('=').Append(counts[4]);
            sb.Append(' ').Append(nameof(CellType.Blocked)).Append('=').Append(counts[0]);
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

        public bool IsWalkable(GridCoord coord) => GetCell(coord) != CellType.Blocked;

        public bool IsBuildable(GridCoord coord) => GetCell(coord) == CellType.Buildable;

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
