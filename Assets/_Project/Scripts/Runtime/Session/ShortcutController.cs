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

        private void Start()
        {
            _camera = Camera.main;
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

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (_deployment == null) _deployment = FindAnyObjectByType<DeploymentController>();
            if (_deployment != null && _deployment.IsBusy)
                return;   // 배치 조작 중에는 지름길 클릭을 먹지 않는다.

            Vector3 screen = mouse.position.ReadValue();
            Vector3 world = _camera.ScreenToWorldPoint(screen);
            world.z = 0f;

            foreach (var marker in _markers)
            {
                if (marker.IsOpen || !marker.IsNear(world, _clickRadius)) continue;

                var definition = GameSession.Instance.Definition;
                var wallet = GameSession.Instance.Wallet;
                if (definition == null || wallet == null) return;

                if (!wallet.CanAfford(definition.ShortcutCost))
                {
                    Debug.Log($"[Shortcut] 자원 부족 ({definition.ShortcutCost} 필요)");
                    return;
                }

                if (!wallet.TrySpend(definition.ShortcutCost)) return;

                if (PathGraph.Instance.OpenShortcut(marker.EdgeId))
                {
                    marker.SetOpen(true);
                    marker.Pulse();
                    Debug.Log($"[Shortcut] 지름길 개방! (edge {marker.EdgeId}, -{definition.ShortcutCost})");
                }
                return;
            }
        }
    }
}
