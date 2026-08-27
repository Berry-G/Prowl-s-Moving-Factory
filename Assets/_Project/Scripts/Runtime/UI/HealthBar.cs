using UnityEngine;
using PMF.Combat;

namespace PMF.UI
{
    /// <summary>월드 체력바 (G-09). 액터마다 uGUI Canvas 를 만들지 않는다 — LineRenderer 2줄 (배경+게이지).
    /// 만피에는 숨기고(StageDefinition.HealthBarHideWhenFull), 피해를 입은 순간부터 보인다.
    /// 액터가 파괴되면 자식이므로 같이 사라진다.</summary>
    public sealed class HealthBar : MonoBehaviour
    {
        private const float Width = 0.6f;
        private const float YOffset = 0.45f;

        private Transform _follow;
        private Health _health;
        private bool _hideWhenFull;
        private LineRenderer _bg;
        private LineRenderer _fg;
        private bool _shown;

        public void Init(Transform follow, Health health, bool hideWhenFull)
        {
            _follow = follow;
            _health = health;
            _hideWhenFull = hideWhenFull;
            _bg = CreateLine(new Color(0f, 0f, 0f, 0.5f), 0.09f, 8);
            _fg = CreateLine(new Color(0.2f, 0.9f, 0.3f, 0.95f), 0.06f, 9);
            SetShown(false);
        }

        private LineRenderer CreateLine(Color color, float width, int order)
        {
            var go = new GameObject("Line");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.loop = false;
            line.startWidth = line.endWidth = width;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.sortingLayerName = "Deploy";
            line.sortingOrder = order;
            line.useWorldSpace = true;
            line.startColor = line.endColor = color;
            line.enabled = false;
            return line;
        }

        private void Update()
        {
            if (_health == null || !_health.IsAlive)
            {
                SetShown(false);   // 적이 죽으면 체력바도 같이 사라진다
                return;
            }

            float ratio = Mathf.Clamp01(_health.Current / _health.Max);
            if (_hideWhenFull && ratio >= 1f)
            {
                SetShown(false);
                return;
            }

            SetShown(true);
            Vector3 basePos = _follow != null ? _follow.position : _health.Position;
            Vector3 left = basePos + Vector3.left * (Width * 0.5f) + Vector3.up * YOffset;
            Vector3 right = basePos + Vector3.right * (Width * 0.5f) + Vector3.up * YOffset;
            _bg.SetPosition(0, left);
            _bg.SetPosition(1, right);
            _fg.SetPosition(0, left);
            _fg.SetPosition(1, Vector3.Lerp(left, right, ratio));
            var c = Color.Lerp(new Color(1f, 0.25f, 0.2f, 0.95f), new Color(0.2f, 0.9f, 0.3f, 0.95f), ratio);
            _fg.startColor = _fg.endColor = c;
        }

        private void SetShown(bool shown)
        {
            if (_shown == shown) return;
            _shown = shown;
            if (_bg != null) _bg.enabled = shown;
            if (_fg != null) _fg.enabled = shown;
        }
    }
}