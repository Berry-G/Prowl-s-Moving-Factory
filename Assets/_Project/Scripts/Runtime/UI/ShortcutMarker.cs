using System.Collections.Generic;
using UnityEngine;
using PMF.Combat;
using PMF.Pathing;

namespace PMF.UI
{
    /// <summary>
    /// 지름길 엣지 위의 클릭 가능한 마커. 잠김 = 노란 점선 + 비용 텍스트, 열림 = 노랑 실선.
    /// ShortcutController 가 런타임 생성한다.
    /// </summary>
    public sealed class ShortcutMarker : MonoBehaviour
    {
        private const float LineWidth = 0.08f;
        private const float DashLength = 0.3f;
        private const float DashGap = 0.2f;
        private const int LineOrder = 20;
        private LineRenderer _line;
        private readonly List<LineRenderer> _dashes = new List<LineRenderer>();
        private Material _material;
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
            Vector3 from = edge.From.WorldPosition;
            Vector3 to = edge.To.WorldPosition;
            Midpoint = (from + to) * 0.5f;
            transform.position = Midpoint;
            _material = new Material(Shader.Find("Sprites/Default"));
            _line = CreateLine("Line", from, to);

            // 왜: 실제로 끊긴 선분으로 그려 카메라·텍스처 설정에 관계없이 점선이 유지된다.
            float length = Vector3.Distance(from, to);
            Vector3 direction = length > 0f ? (to - from) / length : Vector3.zero;
            for (float d = 0f; d < length; d += DashLength + DashGap)
                _dashes.Add(CreateLine("Dash", from + direction * d,
                    from + direction * Mathf.Min(d + DashLength, length)));

            var labelGo = new GameObject("Cost");
            labelGo.transform.SetParent(transform, false);
            _label = labelGo.AddComponent<TextMesh>();
            _label.fontSize = 32;
            _label.characterSize = 0.12f;
            _label.anchor = TextAnchor.MiddleCenter;
            _label.color = Color.yellow;
            var labelRenderer = _label.GetComponent<MeshRenderer>();
            labelRenderer.sortingLayerName = "Deploy";
            labelRenderer.sortingOrder = LineOrder + 1;
            SetLabel(edge.Cost.ToString("F0"));
            SetOpen(false);
        }

        private LineRenderer CreateLine(string name, Vector3 from, Vector3 to)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.startWidth = line.endWidth = LineWidth;
            line.sharedMaterial = _material;
            // 왜: 배치·마을 타일도 Deploy 레이어다. 그보다 높은 순서로 모든 타일 위에 표시한다.
            line.sortingLayerName = "Deploy";
            line.sortingOrder = LineOrder;
            line.startColor = line.endColor = new Color(1f, 0.95f, 0.2f, 1f);
            return line;
        }

        internal void SetLabel(string text) { if (_label != null) _label.text = text; }

        internal void SetOpen(bool open)
        {
            _open = open;
            if (_line != null) _line.enabled = open;
            foreach (var dash in _dashes) dash.enabled = !open;
            if (_label != null) _label.gameObject.SetActive(!open);
        }

        internal void Pulse() => _pulse = 0.25f;

        private void Update()
        {
            if (_pulse <= 0f) return;
            _pulse = Mathf.Max(0f, _pulse - Time.deltaTime);
            if (_line != null)
                _line.startWidth = _line.endWidth = LineWidth * (1f + _pulse * 1.6f);
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }

        public bool IsNear(Vector3 world, float radius) =>
            (Midpoint - world).sqrMagnitude <= radius * radius;
    }
}
