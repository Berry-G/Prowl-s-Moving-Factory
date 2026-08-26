using UnityEngine;
using PMF.Data;
using PMF.Grid;
using PMF.Pathing;

namespace PMF.Actors
{
    /// <summary>
    /// 마을. VillageSlot 셀 위에 배치된다. 고용 가능한 유닛 목록을 갖는다.
    /// 공격받지 않는다 (GDD §13 D-02 기본값).
    /// </summary>
    public sealed class Village : MonoBehaviour
    {
        [SerializeField] private UnitDefinition[] _hireableUnits;

        private PathGraph _graph;
        private GridSystem _grid;
        private GridCoord _slotCoord;
        private PathNode _departureNode;
        private bool _cached;

        public UnitDefinition[] HireableUnits => _hireableUnits;
        public GridCoord SlotCoord => _slotCoord;
        /// <summary>아군 행군의 출발 노드.</summary>
        public PathNode DepartureNode => _departureNode;

        private void Start()
        {
            Cache();
        }

        /// <summary>DeploymentController 가 Start 전에 참조할 수 있게 public.</summary>
        public void Cache()
        {
            if (_cached) return;

            _graph = PathGraph.Instance;
            _grid = GridSystem.Instance;
            if (_graph == null || _grid == null)
            {
                Debug.LogError($"[{nameof(Village)}] GridSystem/PathGraph 없음", this);
                return;
            }

            _slotCoord = _grid.WorldToCell(transform.position);
            _departureNode = _graph.FindNearestNode(transform.position, PathAgent.Ally);
            if (_departureNode == null)
                Debug.LogError($"[{nameof(Village)}] 출발 노드 없음: {name}", this);

            _cached = true;
        }
    }
}
