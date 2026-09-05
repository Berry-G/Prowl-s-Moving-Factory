using UnityEngine;
using PMF.Grid;

namespace PMF.UI
{
    /// <summary>사거리 원 (G-06). LineRenderer 원 — 세그먼트 48.
    /// 반지름 출처는 반드시 Attacker.Range (티어 반영값) — 정의값을 쓰면 미리보기가 거짓말한다.
    ///
    /// <b>원을 두 개 그린다</b> (G-22):
    /// <list type="number">
    /// <item>흐린 <b>온전한 원</b> — 이 유닛의 명목 사거리. 강화하면 얼마나 커지는지 여기서 읽는다.</item>
    /// <item>진한 <b>잘린 원</b> — 장애물에 막혀 실제로는 못 때리는 방향을 깎아낸 실효 사거리.</item>
    /// </list>
    /// 둘 다 보여야 "왜 원 안인데 안 때리지" 라는 오해가 안 생기고,
    /// 동시에 "저 바위를 끼고 놓으면 사거리를 버린다" 는 판단이 화면에서 보인다.
    ///
    /// 잘린 모양은 <b>중심·반지름이 바뀔 때만</b> 다시 잰다. 중심은 배치 슬롯이라 매 프레임 변할 일이 없고
    /// (<see cref="PMF.Actors.AllyUnit.RangeCenter"/>), 장애물은 스테이지 중에 변하지 않는다.</summary>
    public sealed class RangeCircle : MonoBehaviour
    {
        private const int Segments = 48;

        /// <summary>온전한 원은 흐리게 — 잘린 원이 주인공이고 이쪽은 참고선이다.</summary>
        private const float FullCircleAlpha = 0.3f;

        private LineRenderer _line;        // 온전한 원
        private LineRenderer _clipLine;    // 장애물에 잘린 원

        // 마지막으로 잰 조건. 하나라도 다르면 다시 잰다.
        private Vector3 _measuredCenter;
        private float _measuredRadius = -1f;
        private bool _measuredWaterBlocks;
        private bool _measured;

        private void Awake()
        {
            EnsureLines();
        }

        private void EnsureLines()
        {
            if (_line == null) _line = MakeLine(gameObject, Segments + 1, 0.05f, 6);
            if (_clipLine == null)
            {
                var go = new GameObject("ClippedRange");
                go.transform.SetParent(transform, false);
                _clipLine = MakeLine(go, Segments, 0.07f, 7);   // 온전한 원 위에 그린다
            }
        }

        private static LineRenderer MakeLine(GameObject host, int pointCount, float width, int sortingOrder)
        {
            var line = host.AddComponent<LineRenderer>();
            line.positionCount = pointCount;
            line.loop = true;
            line.startWidth = width;
            line.endWidth = width;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.sortingLayerName = "Deploy";
            line.sortingOrder = sortingOrder;   // 안 주면 타일 밑에 깔린다
            line.useWorldSpace = false;
            line.enabled = false;
            return line;
        }

        /// <summary>원을 그린다. center 는 셀 중심(GridSystem.CellToWorld) 또는 유닛의 배치 슬롯.
        /// <paramref name="waterBlocks"/> 는 근접 병종이면 true — 물도 사거리를 깎는다 (G-22).</summary>
        public void Show(Vector3 center, float radius, Color color, bool waterBlocks)
        {
            EnsureLines();
            transform.position = center;
            float r = Mathf.Max(0.05f, radius);

            for (int i = 0; i <= Segments; i++)
            {
                float angle = i / (float)Segments * Mathf.PI * 2f;
                _line.SetPosition(i, new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f));
            }
            var faded = color;
            faded.a *= FullCircleAlpha;
            _line.startColor = _line.endColor = faded;
            _line.enabled = true;

            UpdateClipped(center, r, color, waterBlocks);
        }

        private void UpdateClipped(Vector3 center, float radius, Color color, bool waterBlocks)
        {
            _clipLine.startColor = _clipLine.endColor = color;
            _clipLine.enabled = true;

            bool sameAsBefore = _measured
                                && _measuredRadius == radius
                                && _measuredWaterBlocks == waterBlocks
                                && (_measuredCenter - center).sqrMagnitude < 0.0001f;
            if (sameAsBefore) return;

            var grid = GridSystem.Instance;
            for (int i = 0; i < Segments; i++)
            {
                float angle = i / (float)Segments * Mathf.PI * 2f;
                var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                float reach = grid != null
                    ? grid.LineOfSightDistance(center, direction, radius, waterBlocks)
                    : radius;
                _clipLine.SetPosition(i, direction * reach);
            }

            _measured = true;
            _measuredCenter = center;
            _measuredRadius = radius;
            _measuredWaterBlocks = waterBlocks;
        }

        public void Hide()
        {
            if (_line != null) _line.enabled = false;
            if (_clipLine != null) _clipLine.enabled = false;
        }
    }
}
