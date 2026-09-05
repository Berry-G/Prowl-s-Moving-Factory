using UnityEngine;
using PMF.Combat;

namespace PMF.UI
{
    /// <summary>사격 연출 (G-07). 발사 순간 대상까지 선을 그렸다 지운다.
    /// 액터당 하나를 재사용 — 매 발사마다 생성하지 않는다. 투사체 없음 (히트스캔 유지, 함정 목록).
    /// 근접(고양이) = 짧은 호, 원거리(쥐·로봇) = 직선 — 정체성이 전투에서도 읽히게.</summary>
    public sealed class ShotLine : MonoBehaviour
    {
        private const int ArcSegments = 12;

        private LineRenderer _line;
        private float _timer;
        private IDamageable _target;

        private void Awake() => EnsureLine();

        private void EnsureLine()
        {
            if (_line != null) return;
            _line = gameObject.AddComponent<LineRenderer>();
            _line.loop = false;
            _line.material = new Material(Shader.Find("Sprites/Default"));
            _line.sortingLayerName = "Deploy";
            _line.sortingOrder = 7;
            _line.useWorldSpace = true;
            _line.enabled = false;
        }

        /// <summary>seconds 는 게임시간 기준 (StageDefinition.ShotLineSeconds) — 배속에서 짧게, 일시정지에서 유지.</summary>
        public void Show(Vector3 from, IDamageable target, Color color, bool meleeStyle, float seconds)
        {
            EnsureLine();
            _target = target;
            _timer = seconds;

            Vector3 to = target.Position;
            if (meleeStyle)
            {
                // 짧은 호 — from→to 를 수직 방향으로 살짝 불룩하게.
                _line.positionCount = ArcSegments + 1;
                Vector3 dir = to - from;
                float dist = dir.magnitude;
                if (dist < 0.001f) dist = 0.001f;
                dir /= dist;
                Vector3 normal = new Vector3(-dir.y, dir.x, 0f);
                float bulge = 0.35f * dist;
                for (int i = 0; i <= ArcSegments; i++)
                {
                    float t = i / (float)ArcSegments;
                    Vector3 p = Vector3.Lerp(from, to, t) + normal * (bulge * Mathf.Sin(t * Mathf.PI));
                    _line.SetPosition(i, p);
                }
                _line.startWidth = _line.endWidth = 0.09f;
            }
            else
            {
                _line.positionCount = 2;
                _line.SetPosition(0, from);
                _line.SetPosition(1, to);
                _line.startWidth = _line.endWidth = 0.05f;
            }

            _line.startColor = _line.endColor = color;
            _line.enabled = true;
        }

        private void Update()
        {
            if (_timer <= 0f) return;

            // 대상이 도중에 죽거나 파괴되면 선을 즉시 지운다 (DamageableExtensions 참조).
            if (!_target.IsUsable() || !_target.IsAlive)
            {
                _timer = 0f;
                _line.enabled = false;
                return;
            }

            // 게임시간 기준 (함정 목록) — 배속에서 정상적으로 짧게, 일시정지(timeScale=0)에서는 유지.
            _timer -= Time.deltaTime;
            if (_timer <= 0f) _line.enabled = false;
        }
    }
}