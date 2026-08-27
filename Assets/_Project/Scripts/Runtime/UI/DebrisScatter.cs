using UnityEngine;

namespace PMF.UI
{
    /// <summary>로봇 격파 — 부품이 흩어진다 (G-08, GDD §12: "폭발이 아니라 부품이 흩어지는 표현").
    /// SpriteRenderer + 코드 보간만 (ParticleSystem 금지). 조각은 자가 파괴 — 씬에 잔해가 남지 않는다.
    /// 동시 잔해 상한: 조각 수 × 동시 격파 폭주(버스트 G-20) 방지.</summary>
    public sealed class DebrisScatter : MonoBehaviour
    {
        private const int MaxActive = 80;
        private static int _active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _active = 0;   // 도메인 리로드 OFF — static 상태 수동 리셋

        private static Sprite _squareSprite;

        private Vector3 _velocity;
        private float _life;
        private float _timer;
        private SpriteRenderer _sprite;
        private Color _color;

        private static Sprite SquareSprite
        {
            get
            {
                if (_squareSprite == null)
                {
                    var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
                    var pixels = new Color32[64];
                    for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
                    tex.SetPixels32(pixels);
                    tex.Apply();
                    _squareSprite = Sprite.Create(tex, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 32f);
                }
                return _squareSprite;
            }
        }

        /// <summary>격파 위치에서 부품 count 개를 흩뿌린다. 상한 초과분은 스킵 (버스트 안전).</summary>
        public static void Spawn(Vector3 position, Color baseColor, int count, float seconds)
        {
            if (count <= 0 || seconds <= 0f) return;
            for (int i = 0; i < count; i++)
            {
                if (_active >= MaxActive) return;
                var go = new GameObject("Debris");
                go.transform.position = position;
                var piece = go.AddComponent<DebrisScatter>();
                piece.Init(baseColor, seconds);
                _active++;
            }
        }

        private void Init(Color baseColor, float seconds)
        {
            _life = seconds;
            _timer = seconds;
            _color = baseColor;

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(1.5f, 3.5f);
            _velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * speed;

            _sprite = gameObject.AddComponent<SpriteRenderer>();
            _sprite.sprite = SquareSprite;
            _sprite.sortingLayerName = "Deploy";
            _sprite.sortingOrder = 8;
            float scale = Random.Range(0.08f, 0.16f);
            transform.localScale = new Vector3(scale, scale, 1f);
            var c = _color;
            c.a = 1f;
            _sprite.color = c;
        }

        private void Update()
        {
            float dt = Time.deltaTime;   // 게임시간 기준 — 배속에서 비율 유지
            _timer -= dt;
            if (_timer <= 0f)
            {
                _active--;
                Destroy(gameObject);
                return;
            }

            transform.position += _velocity * dt;
            _velocity *= 1f - Mathf.Min(1f, 4f * dt);   // 마찰 — 부품이 미끄러지며 멈춘다

            float k = Mathf.Clamp01(_timer / _life);
            var c = _color;
            c.a = k;
            _sprite.color = c;
            transform.localScale = transform.localScale * (1f - 0.5f * dt);   // 서서히 줄어들며 사라진다
        }
    }
}