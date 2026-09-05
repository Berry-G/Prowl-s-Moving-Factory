using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using PMF.Data;
using PMF.Grid;
using PMF.Pathing;

namespace PMF.Session
{
    /// <summary>
    /// 지름길 기믹. "돈으로 시간을 산다".
    /// 클릭 → Wallet.TrySpend(ShortcutCost) → PathGraph.OpenShortcut → OnGraphChanged 로 보호대상 재계산.
    /// </summary>
    public sealed class ShortcutController : MonoBehaviour
    {
        [SerializeField] private float _clickRadius = 0.8f;

        private readonly List<UI.ShortcutMarker> _markers = new List<UI.ShortcutMarker>();
        private Camera _camera;
        private Session.DeploymentController _deployment;
        private UI.ShortcutPanel _panel;

        private void Start()
        {
            _camera = Camera.main;
            _panel = FindAnyObjectByType<UI.ShortcutPanel>();
            if (_panel == null)
                Debug.LogError($"[{nameof(ShortcutController)}] ShortcutPanel 이 씬에 없다 — 구매 UI 를 띄울 수 없다", this);
            BuildMarkers();
        }

        private void BuildMarkers()
        {
            var graph = PathGraph.Instance;
            if (graph == null) return;

            int cost = GameSession.Instance.Definition != null
                ? GameSession.Instance.Definition.ShortcutCost : 0;

            foreach (var node in graph.Nodes)
            {
                foreach (var edge in node.Edges)
                {
                    if (!edge.IsShortcut) continue;
                    if (edge.From.Id > edge.To.Id) continue;   // 양방향 중 하나만 마커

                    var go = new GameObject($"Shortcut_{edge.From.Id}_{edge.To.Id}");
                    var marker = go.AddComponent<UI.ShortcutMarker>();
                    marker.Setup(edge);
                    marker.SetLabel($"{cost}");
                    _markers.Add(marker);
                }
            }
        }

        /// <summary>확인 패널에서 [개방] 을 눌렀을 때. 여기서 처음으로 자원이 빠진다.</summary>
        private void Purchase(UI.ShortcutMarker marker, int cost)
        {
            var wallet = GameSession.Instance.Wallet;
            if (marker == null || wallet == null || marker.IsOpen) return;
            if (!wallet.TrySpend(cost)) return;

            if (PathGraph.Instance.OpenShortcut(marker.EdgeId))
            {
                marker.SetOpen(true);
                marker.Pulse();
                Debug.Log($"[Shortcut] 지름길 개방! (edge {marker.EdgeId}, -{cost})");
            }
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (UI.PauseMenu.IsOpen || UI.ShortcutPanel.IsOpen) return;   // 모달이 떠 있으면 게임판 클릭 없음

            if (_deployment == null) _deployment = FindAnyObjectByType<DeploymentController>();
            if (_deployment != null && _deployment.IsBusy)
                return;   // 배치 조작 중에는 지름길 클릭을 먹지 않는다.

            // 이 클릭이 이미 재배치 명령으로 소비됐으면 지름길 패널까지 열지 않는다.
            // DeploymentController 가 [DefaultExecutionOrder(-100)] 로 항상 먼저 돌게 된 뒤로는
            // 마커 근처의 배치 가능 칸을 클릭하면 "이동 명령 + 지름길 패널"이 한 클릭에 같이 터진다.
            if (_deployment != null && _deployment.RedeployedThisFrame) return;

            Vector3 screen = mouse.position.ReadValue();
            Vector3 world = _camera.ScreenToWorldPoint(screen);
            world.z = 0f;

            foreach (var marker in _markers)
            {
                if (marker.IsOpen || !marker.IsNear(world, _clickRadius)) continue;

                var definition = GameSession.Instance.Definition;
                var wallet = GameSession.Instance.Wallet;
                if (definition == null || wallet == null) return;

                // 즉시 지출하지 않는다 — 되돌릴 수 없는 소비라 확인 단계를 둔다.
                // 자원이 모자라도 패널은 띄운다: 얼마가 필요한지 보여야 한다.
                int cost = definition.ShortcutCost;
                var captured = marker;
                _panel.Show(cost, wallet.CanAfford(cost), () => Purchase(captured, cost));
                return;
            }
        }

        /// <summary>
        /// SelectionController 용 읽기 전용 판정 — 이 위치 근처에 닫힌 지름길 마커가 있으면
        /// 클릭은 지름길 몫이다 (맵 클릭 입력 우선순위 3, TASKS G-02).
        /// </summary>
        public bool IsNearMarker(Vector3 world, float radius)
        {
            foreach (var marker in _markers)
                if (marker != null && !marker.IsOpen && marker.IsNear(world, radius))
                    return true;
            return false;
        }
    }
}
