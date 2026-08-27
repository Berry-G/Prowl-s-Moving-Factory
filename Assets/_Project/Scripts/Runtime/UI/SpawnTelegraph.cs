using UnityEngine;

namespace PMF.UI
{
    /// <summary>스폰 예고 링 (G-10, GDD §7 "스폰 직전 예고 연출"). 모체 자식 — 예고 중 모체가 이동해도 따라간다.
    /// 링이 사라지는 그 프레임에 적이 생성되어야 한다 (호출자 = MotherSpawner 책임).
    /// 게임시간 기준 — 배속 4x 에서도 인지되고 일시정지 중에는 멈춘다.</summary>
    public sealed class SpawnTelegraph : MonoBehaviour
    {
        private const int Segments = 32;

        private LineRenderer _line;
        private float _duration;
        private float _timer;
        private float _maxRadius;
        private bool _running;

        private void Awake() => EnsureLine();

        private void EnsureLine()
        {
            if (_line != null) return;
            _line = gameObject.AddComponent<LineRenderer>();
            _line.positionCount = Segments + 1;
            _line.loop = true;
            _line.startWidth = _line.endWidth = 0.07f;
            _line.material = new Material(Shader.Find("Sprites/Default"));
            _line.sortingLayerName = "Deploy";
            _line.sortingOrder = 8;
            _line.useWorldSpace = false;
            _line.startColor = _line.endColor = new Color(1f, 0.35f, 0.2f, 0.9f);
            _line.enabled = false;
        }

        /// <summary>예고 시작. duration 은 게임시간(StageDefinition.SpawnTelegraphSeconds).</summary>
        public void Show(float duration, float maxRadius)
        {
            EnsureLine();
            _duration = duration;
            _timer = duration;
            _maxRadius = maxRadius;
            _running = true;
            _line.enabled = true;
        }

        /// <summary>예고 종료 — 이 프레임에 적이 생성된다.</summary>
        public void End()
        {
            _running = false;
            if (_line != null) _line.enabled = false;
        }

        private void Update()
        {
            if (!_running) return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _running = false;
                _line.enabled = false;
                return;
            }

            float k = 1f - (_timer / _duration);   // 0 → 1 확산
            float r = Mathf.Lerp(0.4f, _maxRadius, k);
            for (int i = 0; i <= Segments; i++)
            {
                float angle = i / (float)Segments * Mathf.PI * 2f;
                _line.SetPosition(i, new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f));
            }
            var c = _line.startColor;
            c.a = 0.9f * (1f - k * 0.5f);
            _line.startColor = _line.endColor = c;
        }
    }
}