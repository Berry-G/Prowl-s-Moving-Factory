using UnityEngine;
using PMF.Combat;

namespace PMF.UI
{
    /// <summary>매직 미사일 — 쥐 수인(마법사)의 투사체 연출.
    ///
    /// <b>피해는 여전히 즉시 들어간다.</b> 투사체가 도착해야 맞는 방식으로 바꾸면 사거리·공격 간격이
    /// 전부 다시 계산돼야 하고, G-21 에서 막 맞춰 둔 밸런스가 어긋난다.
    /// 매직 미사일은 원래 <b>빗나가지 않는 주문</b>이므로, 유도해서 반드시 도달하는 이 연출과 설정이 맞는다.
    ///
    /// 대상을 따라가며 날아가고 도착하면 사라진다. 대상이 먼저 죽으면 그 자리로 마저 날아가 사라진다
    /// (허공에서 멈추면 어색하다).
    ///
    /// 발사마다 하나씩 만든다. 쥐 수인은 공격 간격이 길어 동시 개수가 적다 —
    /// 수가 많아지면 P-19(풀링) 때 같이 다룬다.</summary>
    public sealed class MagicMissile : MonoBehaviour
    {
        private const float ArriveDistance = 0.12f;
        private const float MaxLifetimeSeconds = 3f;   // 안전장치 — 어떤 이유로든 도달 못 하면 사라진다
        private const float BurstSeconds = 0.22f;      // 착탄 폭발이 퍼지며 사라지는 시간

        // 외부 에셋 0 규칙에 맞춰 스프라이트를 코드로 만든다 (GreyboxSprites·ProceduralSfx 와 같은 방식).
        private static Sprite _sprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _sprite = null;   // 도메인 리로드 OFF — static 이 샌다

        private IDamageable _target;
        private Vector3 _lastKnownTargetPosition;
        private float _speed;
        private float _life;
        private SpriteRenderer _sprite2D;
        private Transform _trail;

        // 착탄 폭발 (메테오, ADR-0021). 0 이면 단일 대상이라 그냥 사라진다.
        private float _splashRadius;
        private float _burstTimer;
        private Color _color;

        /// <summary>발사. <paramref name="speed"/> 는 게임시간 기준 (배속·일시정지를 따른다).</summary>
        /// <param name="splashRadius">범위 공격 반경 (셀). 0 보다 크면 착탄 지점에서 그 크기로 터진다.
        /// 피해 판정은 <see cref="Combat.Attacker"/> 가 이미 끝냈다 — 이건 <b>왜 저 적도 맞았는지</b>를
        /// 보여 주는 연출이다.</param>
        public static void Spawn(Vector3 from, IDamageable target, Color color, float speed, Transform parent,
                                 float splashRadius = 0f)
        {
            if (target == null) return;

            var go = new GameObject("MagicMissile");
            go.transform.position = from;
            if (parent != null) go.transform.SetParent(parent, true);

            var missile = go.AddComponent<MagicMissile>();
            missile._splashRadius = Mathf.Max(0f, splashRadius);
            missile.Init(target, color, speed);
        }

        private void Init(IDamageable target, Color color, float speed)
        {
            _color = color;
            _target = target;
            _lastKnownTargetPosition = target.Position;
            _speed = Mathf.Max(0.1f, speed);
            _life = MaxLifetimeSeconds;

            _sprite2D = gameObject.AddComponent<SpriteRenderer>();
            _sprite2D.sprite = GetOrCreateSprite();
            _sprite2D.color = color;
            _sprite2D.sortingLayerName = "Actors";
            _sprite2D.sortingOrder = 8;
            transform.localScale = Vector3.one * 0.22f;

            // 꼬리 — 날아가는 방향이 읽히게 짧은 선을 뒤에 붙인다.
            var trailGo = new GameObject("Trail");
            trailGo.transform.SetParent(transform, false);
            _trail = trailGo.transform;
            var line = trailGo.AddComponent<LineRenderer>();
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.sortingLayerName = "Actors";
            line.sortingOrder = 7;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = 0.10f;
            line.endWidth = 0.02f;
            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, 0f);
        }

        private void Update()
        {
            // 게임시간 기준 — 배속에서 빨라지고 일시정지에서 멈춘다 (다른 연출과 같은 규칙).
            float dt = Time.deltaTime;

            if (_burstTimer > 0f) { UpdateBurst(dt); return; }

            _life -= dt;
            if (_life <= 0f) { Destroy(gameObject); return; }

            // 대상이 살아 있으면 계속 따라간다 (유도). 죽었으면 마지막 위치로 마저 날아간다.
            // IsUsable 이 먼저다 — 적이 파괴된 뒤에는 IDamageable 이 null 로 보이지 않아
            // Position 에 닿는 순간 터진다 (DamageableExtensions 참조).
            if (_target.IsUsable() && _target.IsAlive) _lastKnownTargetPosition = _target.Position;

            Vector3 to = _lastKnownTargetPosition - transform.position;
            float distance = to.magnitude;
            if (distance <= ArriveDistance) { Arrive(); return; }

            Vector3 step = to / distance * (_speed * dt);
            Vector3 previous = transform.position;
            transform.position += step;

            var line = _trail != null ? _trail.GetComponent<LineRenderer>() : null;
            if (line != null)
            {
                line.SetPosition(0, transform.position);
                line.SetPosition(1, previous - step * 2f);
            }
        }

        /// <summary>도착. 범위 공격이면 그 자리에서 터지고, 아니면 그냥 사라진다.</summary>
        private void Arrive()
        {
            if (_splashRadius <= 0f) { Destroy(gameObject); return; }

            _burstTimer = BurstSeconds;
            if (_trail != null) Destroy(_trail.gameObject);   // 꼬리를 끌고 퍼지면 지저분하다
        }

        /// <summary>착탄 폭발 — 스플래시 반경까지 퍼지면서 흐려진다.
        /// 반경을 그대로 쓰므로 <b>실제로 피해가 들어간 범위와 그림이 일치한다.</b></summary>
        private void UpdateBurst(float dt)
        {
            _burstTimer -= dt;
            if (_burstTimer <= 0f) { Destroy(gameObject); return; }

            float t = 1f - _burstTimer / BurstSeconds;   // 0 → 1
            transform.localScale = Vector3.one * Mathf.Lerp(0.22f, _splashRadius * 2f, t);

            if (_sprite2D != null)
                _sprite2D.color = new Color(_color.r, _color.g, _color.b, _color.a * (1f - t));
        }

        /// <summary>8×8 원 스프라이트를 한 번만 만들어 재사용한다.</summary>
        private static Sprite GetOrCreateSprite()
        {
            if (_sprite != null) return _sprite;

            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                    // 가장자리로 갈수록 흐려지는 원 — 발광체처럼 보인다.
                    float a = Mathf.Clamp01(1f - d);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }
            texture.Apply();

            _sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _sprite.hideFlags = HideFlags.HideAndDontSave;
            return _sprite;
        }
    }
}
