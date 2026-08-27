using UnityEngine;

namespace PMF.UI
{
    /// <summary>사거리 원 (G-06). LineRenderer 원 — 세그먼트 48.
    /// 반지름 출처는 반드시 Attacker.Range (티어 반영값) — 정의값을 쓰면 미리보기가 거짓말한다.</summary>
    public sealed class RangeCircle : MonoBehaviour
    {
        private const int Segments = 48;

        private LineRenderer _line;

        private void Awake()
        {
            EnsureLine();
        }

        private void EnsureLine()
        {
            if (_line != null) return;
            _line = gameObject.AddComponent<LineRenderer>();
            _line.positionCount = Segments + 1;
            _line.loop = true;
            _line.startWidth = 0.05f;
            _line.endWidth = 0.05f;
            _line.material = new Material(Shader.Find("Sprites/Default"));
            _line.sortingLayerName = "Deploy";
            _line.sortingOrder = 6;   // 안 주면 타일 밑에 깔린다
            _line.useWorldSpace = false;
            _line.enabled = false;
        }

        /// <summary>원을 그린다. center 는 셀 중심(GridSystem.CellToWorld) 또는 유닛 위치.</summary>
        public void Show(Vector3 center, float radius, Color color)
        {
            EnsureLine();
            transform.position = center;
            float r = Mathf.Max(0.05f, radius);
            for (int i = 0; i <= Segments; i++)
            {
                float angle = i / (float)Segments * Mathf.PI * 2f;
                _line.SetPosition(i, new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f));
            }
            _line.startColor = _line.endColor = color;
            _line.enabled = true;
        }

        public void Hide()
        {
            if (_line != null) _line.enabled = false;
        }
    }
}