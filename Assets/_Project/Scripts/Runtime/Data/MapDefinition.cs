using UnityEngine;
using PMF.Grid;

namespace PMF.Data
{
    /// <summary>
    /// 맵 데이터: 2차원 셀 배열. 런타임에 GridSystem 이 이 데이터로 Tilemap 을 채운다.
    /// 설계 정본: 에디터 레포 docs/SDD-05-저작파이프라인.md (SDD-05 §4)
    /// </summary>
    [CreateAssetMenu(menuName = "PMF/Map Definition")]
    public sealed class MapDefinition : ScriptableObject
    {
        [SerializeField] private int _width;
        [SerializeField] private int _height;
        [SerializeField] private Vector2 _origin;
        /// <summary>셀 타입을 byte 로 저장. 인덱스 = y * width + x. 첫 행이 y=0 (맨 아래).</summary>
        [SerializeField] private byte[] _cells;

        public int Width => _width;
        public int Height => _height;
        public Vector2 Origin => _origin;
        public byte[] Cells => _cells;

        public void Set(int width, int height, Vector2 origin, byte[] cells)
        {
            _width = width;
            _height = height;
            _origin = origin;
            _cells = cells;
        }

        /// <summary>(x,y) 좌표의 셀 타입을 돌려준다. 타일이 없으면(255) false.</summary>
        public bool TryGetCell(int x, int y, out CellType cell)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height)
            {
                cell = CellType.Blocked;
                return false;
            }
            int idx = y * _width + x;
            if (idx < 0 || idx >= _cells.Length || _cells[idx] == 255)
            {
                cell = CellType.Blocked;
                return false;
            }
            cell = (CellType)_cells[idx];
            return true;
        }
    }
}