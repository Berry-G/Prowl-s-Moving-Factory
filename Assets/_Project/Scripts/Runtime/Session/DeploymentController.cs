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
        [SerializeField] private SelectionController _selection;   // 재배치 명령 대상 (G-03). 비워 두면 Start 에서 찾는다.
        [SerializeField] private UI.RangeCircle _hoverRange;       // 배치 전·재배치 목적지 사거리 미리보기 (G-06, 노랑).

        private Camera _camera;
        private Village _selectedVillage;
        private UnitDefinition _hireHoverUnit;   // 고용 버튼 hover 중인 정의 (G-16). null = 없음
        private UnitDefinition _selectedUnit;
        private readonly HashSet<GridCoord> _reservedSlots = new HashSet<GridCoord>();
        private readonly List<GameObject> _highlights = new List<GameObject>();

        /// <summary>배치 전·재배치 목적지 미리보기 색 — 노랑 (G-06 색 구분).</summary>
        private static readonly Color PreviewRangeColor = new Color(1f, 0.95f, 0.2f, 0.9f);

        public bool IsBusy => _mode != Mode.Idle;

        private int _cancelFrame = -1;

        /// <summary>이번 프레임에 Esc·우클릭으로 배치를 취소했는가.
        /// 일시정지 메뉴(G-12)가 같은 Esc 를 이어받아 열리는 것을 막는다.</summary>
        public bool CancelledThisFrame => _cancelFrame == Time.frameCount;

        private void Start()
        {
            // Awake 캐시 원칙 — Start 에서 한 번만 찾는다.
            _camera = Camera.main;
            if (_hirePanel == null)
                _hirePanel = FindAnyObjectByType<UI.HirePanel>();
            if (_selection == null)
                _selection = FindAnyObjectByType<SelectionController>();
            if (_hoverRange == null)
            {
                var go = new GameObject("HoverRangeCircle");
                _hoverRange = go.AddComponent<UI.RangeCircle>();
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (mouse == null) return;

            // 입력 우선순위 1단계 — 일시정지 메뉴가 떠 있으면 게임 입력 전부 차단 (G-02/G-12).
            if (PMF.UI.PauseMenu.IsOpen || PMF.UI.ShortcutPanel.IsOpen) return;

            UpdateRangeHover();   // 사거리 미리보기 (G-06) — 클릭 처리와 무관하게 매 프레임.

            // 우클릭 / ESC → 취소
            if ((mouse.rightButton != null && mouse.rightButton.wasPressedThisFrame) ||
                (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
            {
                // 배치 중이었다면 이 Esc 는 여기서 소비된 것이다. 스크립트 실행 순서가 정해져 있지 않아
                // PauseMenu 가 나중에 IsBusy 를 읽으면 이미 false 라서 같은 Esc 로 메뉴까지 열린다.
                // 그래서 "이번 프레임에 취소했다"를 남긴다 (G-02 입력 우선순위 / G-12).
                if (IsBusy) _cancelFrame = Time.frameCount;
                Cancel();
                return;
            }

            // 회수 단축키 (G-04) — 선택된 유닛을 후퇴시키고 투입 총액 × 환불률을 돌려받는다.
            // 버튼은 G-15 정보 패널에 붙는다 (G-04 작업 4).
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame
                && _selection != null && _selection.Selected != null)
            {
                _selection.Selected.Retire();
                return;
            }

            // 강화 단축키 (G-05) — 선택된 유닛을 다음 티어로. 즉시 적용, 행군 없음 (ADR-0009).
            // 버튼 비활성 표시는 G-15 정보 패널이 담당한다.
            if (keyboard != null && keyboard.uKey.wasPressedThisFrame
                && _selection != null && _selection.Selected != null)
            {
                TryUpgrade(_selection.Selected);
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
                    // 입력 우선순위 2단계 세부 규칙 — 클릭 칸이 마을 슬롯이면 고용 흐름이 이긴다.
                    // 그 외에는 "선택된 유닛이 있으면 재배치 명령"이 고용보다 먼저다 (TASKS G-03).
                    if (_selection != null && _selection.Selected != null && VillageAt(coord) == null)
                    {
                        TryRedeploy(coord, _selection.Selected);
                        break;
                    }
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

        private Village VillageAt(GridCoord coord)
        {
            var villages = FindObjectsByType<Village>();
            foreach (var village in villages)
            {
                village.Cache();
                if (village.SlotCoord == coord)
                    return village;
            }
            return null;
        }

        private void TryPickVillage(GridCoord coord)
        {
            var village = VillageAt(coord);
            if (village == null) return;

            _selectedVillage = village;
            _mode = Mode.UnitSelect;
            GameClock.Instance?.EnterUiSlowMotion();   // 배치 조작 전체(고용~슬롯 선택)는 정밀 조작이 필요한 UI 취급.
            // 유닛을 고르기 전이라도 배치 UI가 뜬 순간부터 어디에 지을 수 있는지 보여준다.
            // 기준점은 마을 — 이 마을에서 걸어갈 수 있는 칸만 나온다.
            ShowHighlights(village.transform.position, village.transform.position);

            var wallet = GameSession.Instance.Wallet;
            _hirePanel.Show(village.HireableUnits, wallet, OnUnitPicked, OnUnitHovered);
            Debug.Log($"[Deployment] 마을 선택: {village.name} {coord}");
        }

        /// <summary>고용 버튼 hover (G-16). 마을 주변에 그 유닛의 사거리 원을 띄운다.
        /// null = 이탈. 실제 그리기는 <see cref="UpdateRangeHover"/> 가 매 프레임 한다.</summary>
        private void OnUnitHovered(UnitDefinition unit) => _hireHoverUnit = unit;

        private void OnUnitPicked(UnitDefinition unit)
        {
            _selectedUnit = unit;
            _hireHoverUnit = null;
            _mode = Mode.SlotSelect;
            _hirePanel.Hide();
            Debug.Log($"[Deployment] 유닛 선택: {unit.DisplayName} — 슬롯을 고르라");
        }

        private void TryConfirmSlot(GridCoord coord)
        {
            if (!GridSystem.Instance.IsBuildable(coord) || _reservedSlots.Contains(coord))
            {
                Debug.Log("[Deployment] 배치할 수 없는 칸");
                return;
            }

            // 아군은 도로를 건널 수 없다 — 돌아서도 갈 수 없는 칸이면 고용 자체를 받지 않는다.
            if (!AllyUnit.CanReach(_selectedVillage.transform.position, GridSystem.Instance.CellToWorld(coord)))
            {
                ShowNotice(GridSystem.Instance.CellToWorld(coord), "갈 수 없음");
                Debug.Log($"[Deployment] {coord} 로 가는 길이 없다");
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

            // 고용 확정음 (G-11).
            if (GameSession.Instance != null && GameSession.Instance.Sfx != null)
                GameSession.Instance.Sfx.Play(Audio.SfxPlayer.SfxId.Hire);

            var go = Instantiate(_selectedUnit.Prefab,
                                 _selectedVillage.transform.position, Quaternion.identity);

            var actorsRoot = GameObject.Find("--- Actors ---");
            if (actorsRoot != null)
            {
                var allies = actorsRoot.transform.Find("Allies");
                if (allies != null) go.transform.SetParent(allies, true);
            }

            var ally = go.GetComponent<AllyUnit>();
            if (ally != null)
            {
                ally.BeginMarch(_selectedVillage, _selectedUnit, coord);
                ally.OnLeftSlot += HandleLeftSlot;   // 이동·사망 어느 쪽으로 슬롯을 떠나도 예약을 푼다.
                ally.OnRetired += HandleRetired;     // 회수 → 환불 지급 (G-04).
            }

            // DoD 로그 형식 준수
            Debug.Log($"Hire: {_selectedUnit.DisplayName} from {_selectedVillage.name} to {coord}");

            Cancel();
        }

        public void Cancel()
        {
            _mode = Mode.Idle;
            _selectedVillage = null;
            _selectedUnit = null;
            _hireHoverUnit = null;
            if (_hirePanel != null) _hirePanel.Hide();
            ClearHighlights();
            GameClock.Instance?.ExitUiSlowMotion();   // 취소/고용 확정 어느 쪽으로 끝나도 여기서 복귀.
        }

        /// <summary>재배치 명령 (G-03, ADR-0008 B안). 행군 시간 + 쿨다운 3.0초 — 자원은 안 든다.</summary>
        /// <summary>배치 전·재배치 목적지 사거리 미리보기 (G-06). 반지름은 Attacker.Range 또는 배치할 정의의 AttackRange.
        /// 표시 시점 4곳 중 이 컨트롤러가 담당하는 2곳: 고용 슬롯 hover / 재배치 목적지 hover.
        /// (선택된 유닛의 원은 SelectionController, 고용 패널 버튼 hover 는 G-16 이 담당.)</summary>
        private void UpdateRangeHover()
        {
            if (_hoverRange == null || _camera == null) return;

            // 고용 버튼 hover (G-16) — 포인터가 UI 위에 있는 상태이므로,
            // 아래의 "UI 위면 숨긴다" 판정보다 반드시 먼저 처리해야 한다.
            if (_hireHoverUnit != null && _selectedVillage != null)
            {
                _hoverRange.Show(_selectedVillage.transform.position,
                                 _hireHoverUnit.AttackRange, PreviewRangeColor);
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                _hoverRange.Hide();
                return;
            }

            var mouse = Mouse.current;
            Vector3 world = _camera.ScreenToWorldPoint(mouse.position.ReadValue());
            world.z = 0f;
            GridCoord coord = GridSystem.Instance.WorldToCell(world);

            // 고용 슬롯 선택 중 — 곧 배치될 유닛의 사거리 미리보기 (노랑).
            if (_mode == Mode.SlotSelect && _selectedUnit != null)
            {
                if (GridSystem.Instance.IsBuildable(coord) && !_reservedSlots.Contains(coord))
                    _hoverRange.Show(GridSystem.Instance.CellToWorld(coord), _selectedUnit.AttackRange, PreviewRangeColor);
                else
                    _hoverRange.Hide();
                return;
            }

            // 재배치 목적지 hover — 선택된 유닛의 사거리(티어 반영)를 옮길 자리에 미리보기 (노랑).
            var selected = _selection != null ? _selection.Selected : null;
            if (_mode == Mode.Idle && selected != null)
            {
                var atk = selected.GetComponent<Combat.Attacker>();
                if (atk != null && GridSystem.Instance.IsBuildable(coord)
                    && !_reservedSlots.Contains(coord) && VillageAt(coord) == null)
                    _hoverRange.Show(GridSystem.Instance.CellToWorld(coord), atk.Range, PreviewRangeColor);
                else
                    _hoverRange.Hide();
                return;
            }

            _hoverRange.Hide();
        }

        /// <summary>정보 패널(G-15)의 [이동] 버튼. 갈 수 있는 칸을 하이라이트해서 보여준다.
        /// 실제 이동은 기존 경로 그대로다 — 하이라이트된 칸을 클릭하면 <see cref="TryRedeploy"/> 가 돈다.
        /// 새 모드를 만들지 않는 이유: 유닛이 선택된 상태의 맵 클릭은 이미 재배치 명령이다 (G-03).</summary>
        public void BeginRedeployTargeting(AllyUnit unit)
        {
            if (unit == null) return;
            if (!unit.CanRedeployNow)
            {
                ShowCooldownNotice(unit);
                return;
            }
            // 재배치는 유닛의 현재 위치가 기준이고, 막히면 자기 마을을 거쳐 우회한다.
            Vector3 via = unit.HomeVillage != null ? unit.HomeVillage.transform.position : unit.transform.position;
            ShowHighlights(unit.transform.position, via);
        }

        /// <summary>정보 패널(G-15)의 [업그레이드] 버튼.</summary>
        public void RequestUpgrade(AllyUnit unit)
        {
            if (unit != null) TryUpgrade(unit);
        }

        /// <summary>이동 목적지 하이라이트를 끈다. 선택이 풀리면 정보 패널이 부른다.
        /// 고용 흐름(SlotSelect) 중이면 그쪽 하이라이트이므로 건드리지 않는다.</summary>
        public void EndRedeployTargeting()
        {
            if (_mode == Mode.Idle) ClearHighlights();
        }

        /// <summary>재배치 명령 (G-03, ADR-0008 B안). 행군 시간 + 쿨다운 3.0초 — 자원은 안 든다.</summary>
        private void TryRedeploy(GridCoord coord, AllyUnit unit)
        {
            if (!unit.CanRedeployNow)
            {
                ShowCooldownNotice(unit);
                return;
            }

            if (!GridSystem.Instance.IsBuildable(coord) || _reservedSlots.Contains(coord))
            {
                Debug.Log("[Redeploy] 배치할 수 없는 칸");
                return;
            }

            // 도로 건너편으로는 옮길 수 없다.
            if (!unit.CanWalkTo(coord))
            {
                ShowNotice(GridSystem.Instance.CellToWorld(coord), "갈 수 없음");
                Debug.Log($"[Redeploy] {coord} 로 가는 길이 없다");
                return;
            }

            _reservedSlots.Add(coord);
            if (!unit.BeginRedeploy(coord))
            {
                _reservedSlots.Remove(coord);   // 실패 시 방금 예약을 즉시 되돌린다.
                return;
            }

            EndRedeployTargeting();   // [이동] 버튼으로 켠 하이라이트를 끈다 (G-15)
            Debug.Log($"[Redeploy] {unit.name} -> {coord}");
        }

        /// <summary>쿨다운 중 이동 거부 — 그 사실을 화면에 표시한다 (G-03 DoD).</summary>
        private void ShowCooldownNotice(AllyUnit unit)
        {
            float remain = unit.RedeployCooldownRemaining;
            Debug.Log($"[Redeploy] 이동 쿨다운 {remain:F1}초 남음");
            ShowNotice(unit.transform.position, $"이동 {remain:F1}초 후");
        }

        /// <summary>거절 이유를 그 자리에 잠깐 띄운다. 왜 안 되는지 화면에서 읽혀야 한다.</summary>
        private void ShowNotice(Vector3 worldPosition, string text)
        {
            var go = new GameObject("DeployNotice");
            go.transform.position = worldPosition + Vector3.up * 0.8f;
            var label = go.AddComponent<TextMesh>();
            label.text = text;
            label.fontSize = 28;
            label.characterSize = 0.1f;
            label.anchor = TextAnchor.MiddleCenter;
            label.color = Color.yellow;
            Destroy(go, 1f);
        }

        private void HandleLeftSlot(AllyUnit unit, GridCoord coord)
        {
            // 구독은 풀지 않는다 — 유닛이 살아 있는 한 재배치가 몇 번이고 일어나며,
            // 유닛이 파괴되면 이 핸들러 참조도 함께 정리된다. (G-03 실측: 풀면 2회째부터 예약 해제 누락)
            _reservedSlots.Remove(coord);
        }

        private void HandleRetired(AllyUnit unit, int refund)
        {
            GameSession.Instance.Wallet.Add(refund);
            Debug.Log($"[Retire] {unit.name} 회수 — 환불 {refund} (투입 {unit.InvestedAmount})");
        }

        /// <summary>강화 명령 (G-05, ADR-0009). 자원 차감 → 티어 상승 → Attacker 갱신. SO 는 건드리지 않는다.</summary>
        private void TryUpgrade(AllyUnit unit)
        {
            if (!unit.CanUpgrade)
            {
                Debug.Log("[Upgrade] 이미 최대 티어");
                return;
            }

            int cost = unit.NextUpgradeCost;
            var wallet = GameSession.Instance.Wallet;
            if (!wallet.CanAfford(cost))
            {
                Debug.Log($"[Upgrade] 자원 부족 ({cost} 필요)");
                return;
            }

            wallet.TrySpend(cost);
            unit.ApplyUpgrade();
            Debug.Log($"[Upgrade] {unit.name} → Lv{unit.TierLevel + 1} (비용 {cost}, 공격 {unit.GetComponent<Combat.Attacker>().Damage} · 사거리 {unit.GetComponent<Combat.Attacker>().Range})");
        }

        /// <summary>이미 <b>차지된</b> 칸을 표시한다.
        ///
        /// 2026-08-30 이후 도로와 장애물이 아니면 어디든 배치할 수 있다. 그래서 "놓을 수 있는 칸"을
        /// 전부 칠하면 맵의 대부분(391칸)이 덮여 도로도 지형도 안 보인다 — 정보가 아니라 소음이다.
        /// 격자가 남아 있는 이유는 <b>겹치기를 막기 위해서</b>이므로, 알려줘야 하는 것은 그 반대다:
        /// 여기는 이미 누가 서 있다.</summary>
        private void ShowHighlights(Vector3 origin, Vector3 via)
        {
            ClearHighlights();

            var grid = GridSystem.Instance;
            foreach (var coord in _reservedSlots)
            {
                if (!grid.InBounds(coord)) continue;

                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(quad.GetComponent<Collider>());   // 물리 미사용 (ADR-0005)
                quad.transform.position = grid.CellToWorld(coord);
                quad.transform.localScale = new Vector3(grid.CellSize * 0.9f, grid.CellSize * 0.9f, 1f);

                var renderer = quad.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Sprites/Default"));
                renderer.material.color = new Color(1f, 0.35f, 0.3f, 0.55f);   // 차지된 칸 = 붉게
                renderer.sortingLayerName = "Deploy";
                // Tilemap_Buildable 도 같은 레이어의 order=0 이라 동률이면 타일이 위로 뜬다 — 확실히 더 높게.
                renderer.sortingOrder = 5;

                _highlights.Add(quad);
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
