using System.Collections.Generic;
using UnityEngine;
using PMF.Combat;
using PMF.Pathing;

namespace PMF.UI
{
    /// <summary>
    /// 지름길 엣지 위의 클릭 가능한 마커. 잠김 = 노란 반투명 선 + 비용 텍스트, 열림 = 노랑 실선.
    /// ShortcutController 가 런타임 생성한다.
    /// </summary>
    public sealed class ShortcutMarker : MonoBehaviour
    {
        private LineRenderer _line;
        private TextMesh _label;
        private int _edgeId = -1;
        private bool _open;
        private float _pulse;

        public int EdgeId => _edgeId;
        public bool IsOpen => _open;
        public Vector3 Midpoint { get; private set; }

        internal void Setup(PathEdge edge)
        {
            _edgeId = edge.Id;
            Midpoint = (edge.From.WorldPosition + edge.To.WorldPosition) * 0.5f;

            transform.position = Midpoint;

            _line = new GameObject("Line").AddComponent<LineRenderer>();
            _line.transform.SetParent(transform, false);
            _line.positionCount = 2;
            _line.SetPosition(0, edge.From.WorldPosition);
            _line.SetPosition(1, edge.To.WorldPosition);
            _line.startWidth = 0.08f;
            _line.endWidth = 0.08f;
            _line.material = new Material(Shader.Find("Sprites/Default"));
            _line.sortingLayerName = "Path";
            SetOpen(false);

            var labelGo = new GameObject("Cost");
            labelGo.transform.SetParent(transform, false);
            _label = labelGo.AddComponent<TextMesh>();
            _label.text = edge.Cost.ToString("F0");
            _label.fontSize = 32;
            _label.characterSize = 0.12f;
            _label.anchor = TextAnchor.MiddleCenter;
            _label.color = Color.yellow;

            // 지름길 비용은 StageDefinition.ShortcutCost — 라벨은 Controller 가 갱신할 수 있다.
        }

        internal void SetLabel(string text) { if (_label != null) _label.text = text; }

        internal void SetOpen(bool open)
        {
            _open = open;
            if (_line != null)
                _line.startColor = _line.endColor =
                    new Color(1f, 0.95f, 0.2f, open ? 1f : 0.45f);
            if (_label != null) _label.gameObject.SetActive(!open);
        }

        /// <summary>열린 순간 짧은 피드백 — 마커가 잠깐 커진다.</summary>
        internal void Pulse() => _pulse = 0.25f;

        private void Update()
        {
            if (_pulse > 0f)
            {
                _pulse -= Time.deltaTime;
                float scale = 1f + Mathf.Max(_pulse, 0f) * 1.6f;
                if (_line != null)
                {
                    _line.startWidth = 0.08f * scale;
                    _line.endWidth = 0.08f * scale;
                }
            }
        }

        /// <summary>클릭 판정용: world 와 엣지 중점 사이 거리.</summary>
        public bool IsNear(Vector3 world, float radius) =>
            (Midpoint - world).sqrMagnitude <= radius * radius;
    }
}
