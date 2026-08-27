using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using PMF.Actors;
using PMF.Combat;
using PMF.Grid;

namespace PMF.Session
{
    /// <summary>
    /// 배치된 아군 선택 (G-02). G-03(재배치)·G-04(회수)·G-05(업그레이드)·G-15(정보 패널)의 공통 전제다.
    ///
    /// ⚠️ 맵 클릭 입력 우선순위 (TASKS G-02 확정 — 이 순서를 바꾸면 마을을 못 고르거나 유닛을 못 고른다):
    ///   1) 일시정지 메뉴가 떠 있으면 → 게임 입력 전부 차단   (G-12 구현 시 여기에 검사를 추가한다)
    ///   2) 배치 모드 진행 중 (DeploymentController.IsBusy)   → DeploymentController 가 클릭을 먹는다
    ///   3) 지름길 마커 근처 (ShortcutController.IsNearMarker) → ShortcutController 가 먹는다
    ///   4) 그 외                                             → SelectionController
    /// 각 컨트롤러는 자기 차례가 아니면 클릭을 소비하지 않는다. UI 위 클릭은 EventSystem 이 먼저 걸러낸다.
    /// </summary>
    public sealed class SelectionController : MonoBehaviour
    {
        [SerializeField] private DeploymentController _deployment;
        [SerializeField] private ShortcutController _shortcuts;
        [SerializeField] private UI.SelectionRing _ring;   // 비워 두면 Start 에서 생성
        [SerializeField] private UI.RangeCircle _range;    // 선택된 유닛의 사거리 원 (G-06). 비워 두면 Start 에서 생성.

        private Camera _camera;
        private AllyUnit _selected;
        private bool _hasSelection;

        /// <summary>현재 선택된 아군. 없으면 null.</summary>
        public AllyUnit Selected => _selected;

        /// <summary>선택이 바뀔 때. null = 해제. G-03·G-05·G-15 가 여기 붙는다.</summary>
        public event Action<AllyUnit> OnSelectionChanged;

        /// <summary>선택된 유닛의 사거리 원 색 — 아군 색 (GDD §12 파랑 계열).</summary>
        private static readonly Color SelectionRangeColor = new Color(0.55f, 0.75f, 1f, 0.9f);

        private void Start()
        {
            // Awake 캐시 원칙 — Start 에서 한 번만 찾는다.
            _camera = Camera.main;
            if (_deployment == null) _deployment = FindAnyObjectByType<DeploymentController>();
            if (_shortcuts == null) _shortcuts = FindAnyObjectByType<ShortcutController>();
            if (_ring == null)
            {
                var go = new GameObject("SelectionRing");
                _ring = go.AddComponent<UI.SelectionRing>();
            }
            if (_range == null)
            {
                var go = new GameObject("SelectionRangeCircle");
                _range = go.AddComponent<UI.RangeCircle>();
            }
        }

        private void OnDisable()
        {
            Clear();
            if (_range != null) _range.Hide();
        }

        private void Update()
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if (mouse == null) return;

            // 선택된 유닛이 죽어 파괴되었으면 해제한다 (파괴된 참조는 Unity null 로 평가된다).
            if (_hasSelection && _selected == null)
            {
                Clear();
                return;
            }

            // 사거리 원 (G-06) — 반지름은 Attacker.Range (티어 반영). 강화하면 원도 커진다.
            if (_hasSelection && _range != null)
            {
                var atk = _selected.GetComponent<Combat.Attacker>();
                if (atk != null) _range.Show(_selected.transform.position, atk.Range, SelectionRangeColor);
            }
            else if (_range != null)
            {
                _range.Hide();
            }

            // 1) 일시정지 메뉴 (G-12): 구현되면 이 위에서 게임 입력을 전부 차단한다.

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Clear();
                return;
            }

            if (!mouse.leftButton.wasPressedThisFrame) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;   // UI 클릭이 맵으로 새는 것을 차단

            // 2) 배치 조작 중 → 클릭을 소비하지 않는다 (DeploymentController 몫).
            if (_deployment != null && _deployment.IsBusy) return;

            Vector3 world = _camera.ScreenToWorldPoint(mouse.position.ReadValue());
            world.z = 0f;   // 필수 — Orthographic 에서도 z 를 0으로.

            // 3) 지름길 마커 근처 → 클릭을 소비하지 않는다 (ShortcutController 몫).
            if (_shortcuts != null && _shortcuts.IsNearMarker(world, 0.8f)) return;

            // 4) 선택 — 클릭 셀에 서 있는 아군을 찾는다. 순수 수학 (Collider 금지, ADR-0005).
            //    클릭 프레임에만 Find 를 한다 — "Update 안에서 Find 금지" 규칙과 충돌하지 않는다.
            GridCoord coord = GridSystem.Instance.WorldToCell(world);
            Select(FindAllyAt(coord));
        }

        /// <summary>셀 좌표에 서 있는(또는 지나가는) 아군을 찾는다. 행군 중인 유닛도 선택된다.</summary>
        private AllyUnit FindAllyAt(GridCoord coord)
        {
            var allies = FindObjectsByType<AllyUnit>(FindObjectsSortMode.None);
            foreach (var ally in allies)
            {
                if (GridSystem.Instance.WorldToCell(ally.transform.position) == coord)
                    return ally;
            }
            return null;
        }

        public void Select(AllyUnit unit)
        {
            if (unit == _selected) return;   // 같은 유닛 재클릭은 이벤트를 다시 쏘지 않는다.

            _selected = unit;
            _hasSelection = unit != null;
            if (_ring != null) _ring.Bind(unit);
            OnSelectionChanged?.Invoke(unit);
        }

        /// <summary>선택 해제 (빈 곳 클릭 / ESC / 유닛 사망·회수).</summary>
        public void Clear()
        {
            if (!_hasSelection && _selected == null) return;
            _selected = null;
            _hasSelection = false;
            if (_ring != null) _ring.Bind(null);
            if (_range != null) _range.Hide();
            OnSelectionChanged?.Invoke(null);
        }
    }
}