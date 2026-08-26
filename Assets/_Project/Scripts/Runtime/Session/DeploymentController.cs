using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using PMF.Actors;
using PMF.Data;
using PMF.Grid;
using PMF.Session;

namespace PMF.Session
{
    /// <summary>
    /// 배치 조작. 입력 흐름 (정확히 이 순서):
    ///   [대기] → 마을 클릭 → [유닛 선택] → 유닛 버튼 → [슬롯 선택] → Buildable 칸 클릭 → 고용 확정
    ///   우클릭 / ESC → 어느 단계에서든 [대기]
    /// </summary>
    public sealed class DeploymentController : MonoBehaviour
    {
        private enum Mode { Idle, UnitSelect, SlotSelect }

        private Mode _mode = Mode.Idle;

        [SerializeField] private UI.HirePanel _hirePanel;

        private Camera _camera;
        private Village _selectedVillage;
        private UnitDefinition _selectedUnit;
        private readonly HashSet<GridCoord> _reservedSlots = new HashSet<GridCoord>();
        private readonly List<GameObject> _highlights = new List<GameObject>();

        public bool IsBusy => _mode != Mode.Idle;

        private void Start()
        {
            // Awake 캐시 원칙 — Start 에서 한 번만 찾는다.
            _camera = Camera.main;
            if (_hirePanel == null)
                _hirePanel = FindAnyObjectByType<UI.HirePanel>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (mouse == null) return;

            // 우클릭 / ESC → 취소
            if ((mouse.rightButton != null && mouse.rightButton.wasPressedThisFrame) ||
                (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
            {
                Cancel();
                return;
            }

            if (!mouse.leftButton.wasPressedThisFrame) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;   // UI 클릭이 맵으로 새는 것을 차단

            Vector3 world = ScreenToWorld(mouse.position.ReadValue());
            GridCoord coord = GridSystem.Instance.WorldToCell(world);

            switch (_mode)
            {
                case Mode.Idle:
                case Mode.UnitSelect:
                    TryPickVillage(coord);   // 유닛 선택 중에도 다른 마을로 이동 허용
                    break;
                case Mode.SlotSelect:
                    TryConfirmSlot(coord);
                    break;
            }
        }

        private Vector3 ScreenToWorld(Vector2 screen)
        {
            Vector3 world = _camera.ScreenToWorldPoint(screen);
            world.z = 0f;   // 필수 — Orthographic 에서도 z 를 0으로.
            return world;
        }

        private void TryPickVillage(GridCoord coord)
        {
            var villages = FindObjectsByType<Village>();
            foreach (var village in villages)
            {
                village.Cache();
                if (village.SlotCoord != coord) continue;

                _selectedVillage = village;
                _mode = Mode.UnitSelect;

                var wallet = GameSession.Instance.Wallet;
                _hirePanel.Show(village.HireableUnits, wallet, OnUnitPicked);
                Debug.Log($"[Deployment] 마을 선택: {village.name} {coord}");
                return;
            }
        }

        private void OnUnitPicked(UnitDefinition unit)
        {
            _selectedUnit = unit;
            _mode = Mode.SlotSelect;
            _hirePanel.Hide();
            ShowHighlights();
            Debug.Log($"[Deployment] 유닛 선택: {unit.DisplayName} — 슬롯을 고르라");
        }

        private void TryConfirmSlot(GridCoord coord)
        {
            if (!GridSystem.Instance.IsBuildable(coord) || _reservedSlots.Contains(coord))
            {
                Debug.Log("[Deployment] 배치할 수 없는 칸");
                return;
            }

            var wallet = GameSession.Instance.Wallet;
            int cost = _selectedUnit.HireCost;
            if (!wallet.CanAfford(cost))
            {
                Debug.Log($"[Deployment] 자원 부족 ({cost} 필요)");
                return;
            }

            wallet.TrySpend(cost);
            _reservedSlots.Add(coord);

            var go = Instantiate(_selectedUnit.Prefab,
                                 _selectedVillage.transform.position, Quaternion.identity);

            var actorsRoot = GameObject.Find("--- Actors ---");
            if (actorsRoot != null)
            {
                var allies = actorsRoot.transform.Find("Allies");
                if (allies != null) go.transform.SetParent(allies, true);
            }

            var ally = go.GetComponent<AllyUnit>();
            if (ally != null) ally.BeginMarch(_selectedVillage, _selectedUnit, coord);

            // DoD 로그 형식 준수
            Debug.Log($"Hire: {_selectedUnit.DisplayName} from {_selectedVillage.name} to {coord}");

            Cancel();
        }

        public void Cancel()
        {
            _mode = Mode.Idle;
            _selectedVillage = null;
            _selectedUnit = null;
            if (_hirePanel != null) _hirePanel.Hide();
            ClearHighlights();
        }

        /// <summary>Buildable && 미예약 칸 하이라이트 (반투명 하늘색 오버레이).</summary>
        private void ShowHighlights()
        {
            ClearHighlights();

            var grid = GridSystem.Instance;
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    var coord = new GridCoord(x, y);
                    if (grid.GetCell(coord) != CellType.Buildable) continue;
                    if (_reservedSlots.Contains(coord)) continue;

                    var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    Destroy(quad.GetComponent<Collider>());   // 물리 미사용 (ADR-0005)
                    quad.transform.position = grid.CellToWorld(coord);
                    quad.transform.localScale = new Vector3(grid.CellSize * 0.9f, grid.CellSize * 0.9f, 1f);

                    var renderer = quad.GetComponent<Renderer>();
                    renderer.material = new Material(Shader.Find("Sprites/Default"));
                    renderer.material.color = new Color(0.5f, 0.9f, 1f, 0.35f);
                    renderer.sortingLayerName = "Deploy";

                    _highlights.Add(quad);
                }
            }
        }

        private void ClearHighlights()
        {
            foreach (var h in _highlights)
                if (h != null) Destroy(h);
            _highlights.Clear();
        }
    }
}
