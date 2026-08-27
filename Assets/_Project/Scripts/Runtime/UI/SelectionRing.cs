using UnityEngine;
using PMF.Actors;

namespace PMF.UI
{
    /// <summary>선택된 유닛 발밑 링 (G-02). LineRenderer 원 — 파티클/애니메이션 금지 규약(코드 보간만) 준수.</summary>
    public sealed class SelectionRing : MonoBehaviour
    {
        private const int Segments = 48;

        [SerializeField] private float _radius = 0.45f;
        [SerializeField] private Color _color = new Color(1f, 1f, 1f, 0.9f);

        private LineRenderer _line;
        private Transform _target;

        private void Awake()
        {
            _line = gameObject.AddComponent<LineRenderer>();
            _line.positionCount = Segments + 1;
            _line.loop = true;
            _line.startWidth = 0.06f;
            _line.endWidth = 0.06f;
            _line.material = new Material(Shader.Find("Sprites/Default"));
            _line.sortingLayerName = "Deploy";
            _line.sortingOrder = 6;
            _line.startColor = _line.endColor = _color;
            _line.useWorldSpace = false;   // 로컬 원형 좌표를 찍어두고 transform 만 옮긴다.
            for (int i = 0; i <= Segments; i++)
            {
                float angle = i / (float)Segments * Mathf.PI * 2f;
                _line.SetPosition(i, new Vector3(Mathf.Cos(angle) * _radius, Mathf.Sin(angle) * _radius, 0f));
            }
            _line.enabled = false;
        }

        /// <summary>선택 대상을 묶는다. null 이면 링을 숨긴다.</summary>
        public void Bind(AllyUnit unit)
        {
            _target = unit != null ? unit.transform : null;
            if (_line != null) _line.enabled = _target != null;
            if (_target == null) return;
            transform.position = _target.position;
        }

        public void Clear() => Bind(null);

        private void LateUpdate()
        {
            if (_target == null)
            {
                if (_line != null && _line.enabled) _line.enabled = false;
                return;
            }
            // 유닛이 행군해도 링이 따라 붙는다.
            transform.position = _target.position;
        }
    }
}