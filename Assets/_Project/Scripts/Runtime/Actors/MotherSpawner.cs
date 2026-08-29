using UnityEngine;
using PMF.Data;
using PMF.Pathing;
using PMF.Session;

namespace PMF.Actors
{
    /// <summary>
    /// 이동하는 적 스폰 지점. 처치 불가 (Health 없음, 레지스트리 미등록).
    /// 상태머신: Idle → Chasing → Burst(골격만).
    /// </summary>
    public sealed class MotherSpawner : MonoBehaviour
    {
        private enum State { Idle, Chasing, Burst }

        private State _state = State.Idle;
        private float _stateTimer;

        private GameSession _session;
        private PathGraph _graph;
        private Escortee _escortee;
        private StageDefinition _def;

        private readonly PathFollower _follower = new PathFollower();
        private readonly System.Collections.Generic.List<PathNode> _route =
            new System.Collections.Generic.List<PathNode>();

        private SpriteRenderer _sprite;
        private Color _baseColor;
        private Vector3 _baseScale;
        private Transform _enemiesParent;
        private UI.SpawnTelegraph _telegraph;
        private bool _telegraphActive;

        // --- 스폰 리듬 (G-19): 등간격이 아니라 묶음(volley) + 휴지(rest) ---
        private enum SpawnPhase { Rest, Volley }

        private SpawnPhase _phase = SpawnPhase.Rest;
        private float _restTimer;        // 다음 묶음까지 남은 휴지
        private float _spacingTimer;     // 묶음 안에서 다음 한 마리까지
        private int _volleyRemaining;    // 이번 묶음에 남은 마릿수

        /// <summary>아직 활동 전(첫 스폰 유예 중)인가. HUD 보조 정보용 (G-14).</summary>
        public bool IsIdle => _state == State.Idle;

        /// <summary>묶음이 지금 나가는 중인가. HUD 게이지 표시용 (G-19).</summary>
        public bool IsVolleyFiring => _state == State.Chasing && _phase == SpawnPhase.Volley;

        /// <summary>다음 묶음까지 남은 게임시간(초). 묶음 진행 중이면 0.
        /// 유예 중이면 활동 시작까지 남은 시간을 준다.</summary>
        public float SecondsToNextVolley
        {
            get
            {
                if (_def == null) return 0f;
                if (_state != State.Chasing) return Mathf.Max(_stateTimer, 0f);
                return _phase == SpawnPhase.Rest ? Mathf.Max(_restTimer, 0f) : 0f;
            }
        }

        /// <summary>휴지 진행도 0~1 (1 = 곧 묶음이 나간다). HUD 게이지용 (G-19).</summary>
        public float VolleyGauge01
        {
            get
            {
                if (_def == null || _state != State.Chasing) return 0f;
                if (_phase == SpawnPhase.Volley) return 1f;
                float rest = _def.SpawnRestSeconds;
                return rest <= 0f ? 1f : Mathf.Clamp01(1f - _restTimer / rest);
            }
        }

        private void Start()
        {
            _session = GameSession.Instance;
            _graph = PathGraph.Instance;
            if (_session == null || _graph == null)
            {
                Debug.LogError($"[{nameof(MotherSpawner)}] GameSession/PathGraph 없음", this);
                enabled = false;
                return;
            }

            _def = _session.Definition;
            _escortee = _session.Escortee;
            if (_escortee == null)
            {
                Debug.LogError($"[{nameof(MotherSpawner)}] 보호대상 없음", this);
                enabled = false;
                return;
            }

            // 모체가 보호대상을 따라잡으면 게임이 성립하지 않는다 — 반드시 검증.
            if (_def.MotherSpeed >= _def.EscorteeSpeed)
                Debug.LogError($"[{nameof(MotherSpawner)}] MotherSpeed({_def.MotherSpeed}) >= " +
                               $"EscorteeSpeed({_def.EscorteeSpeed}). 모체는 느려야 한다.", this);

            var actorsRoot = GameObject.Find("--- Actors ---");
            if (actorsRoot != null) _enemiesParent = actorsRoot.transform.Find("Enemies");

            _sprite = GetComponent<SpriteRenderer>();
            if (_sprite != null) _baseColor = _sprite.color;
            _baseScale = transform.localScale;

            // 스폰 예고 링 (G-10) — 모체 자식. 예고 중 모체가 이동해도 따라간다.
            var telegraphGo = new GameObject("SpawnTelegraph");
            telegraphGo.transform.SetParent(transform, false);
            _telegraph = telegraphGo.AddComponent<UI.SpawnTelegraph>();

            _stateTimer = _def.MotherSpawnDelay;

            _session.OnEscorteeReachedNode += OnEscorteeMoved;
        }

        private void OnDisable()
        {
            if (_session != null) _session.OnEscorteeReachedNode -= OnEscorteeMoved;
        }

        /// <summary>목적지 재계산은 이 이벤트로만. 매 프레임 재계산 금지.</summary>
        private void OnEscorteeMoved(PathNode node) => RecalculateDestination();

        private void Update()
        {
            if (_session.Result != GameResult.InProgress) return;

            switch (_state)
            {
                case State.Idle:
                    _stateTimer -= Time.deltaTime;
                    if (_stateTimer <= 0f)
                    {
                        _state = State.Chasing;
                        _stateTimer = 0f;
                        // 추격을 시작하면 첫 묶음까지 한 번 쉬고 들어간다 (G-19).
                        _phase = SpawnPhase.Rest;
                        _restTimer = _def.SpawnRestSeconds;
                        RecalculateDestination();
                    }
                    break;

                case State.Chasing:
                    MoveAndSpawn();
                    break;

                case State.Burst:
                    // 골격만. 진입 조건은 비워 둔다 (GDD §7 — 프로토타입 범위 밖).
                    break;
            }
        }

        private void RecalculateDestination()
        {
            if (!_def.MotherFollowsPath) return;

            var from = _follower.HasRoute ? _follower.CurrentNode
                                          : _graph.FindNearestNode(transform.position, PathAgent.Enemy);
            var goal = _graph.FindNearestNode(_escortee.transform.position, PathAgent.Enemy);
            if (from == null || goal == null) return;

            // 지름길 개방 등으로 goal 이 unreachable 이 될 수 있다.
            // 폴백: 도달 가능한 노드 중 보호대상에 가장 가까운 것. 얼어붙은 모체는 게임 끝이므로 필수 처리.
            if (!_graph.TryFindRoute(from, goal, PathAgent.Enemy, _route))
            {
                var fallback = _graph.FindReachableNodeNearestTo(_escortee.transform.position,
                                                                 PathAgent.Enemy, from);
                if (fallback == null || fallback == from) return;
                if (!_graph.TryFindRoute(from, fallback, PathAgent.Enemy, _route)) return;
            }

            _follower.SetRoute(_route, transform.position);
            transform.position = _follower.Position;
        }

        private void MoveAndSpawn()
        {
            float dt = Time.deltaTime;

            if (_def.MotherFollowsPath && _follower.HasRoute && !_follower.IsFinished)
            {
                _follower.Advance(_def.MotherSpeed * dt);
                transform.position = _follower.Position;
            }
            else if (!_def.MotherFollowsPath)
            {
                // D-06 실험용 직진 이동
                Vector3 toEscortee = _escortee.transform.position - transform.position;
                if (toEscortee.sqrMagnitude > 0.0001f)
                    transform.position += toEscortee.normalized * (_def.MotherSpeed * dt);
            }

            UpdateSpawnRhythm(dt);
        }

        /// <summary>묶음(volley) + 휴지(rest) 리듬 (G-19).
        /// 등간격이면 긴장의 오르내림이 없다. 몰려오고 숨 돌리는 파형이 있어야
        /// 플레이어가 휴지에 재배치(G-03)를 하게 된다.
        /// 웨이브제가 아니다 — 번호도 클리어 보너스도 없다. 호흡만 만든다 (GDD §4).</summary>
        private void UpdateSpawnRhythm(float dt)
        {
            if (_phase == SpawnPhase.Rest)
            {
                _restTimer -= dt;

                // 예고(G-10)는 묶음 시작 전 <b>한 번</b>이다. 마리마다 울리지 않는다.
                // 예고 시간은 휴지에 포함된다 — 첫 묶음이 늦어지지 않는다.
                if (!_telegraphActive && _restTimer <= _def.SpawnTelegraphSeconds && _restTimer > 0f)
                {
                    _telegraphActive = true;
                    _telegraph.Show(_def.SpawnTelegraphSeconds, 1.2f);
                }

                if (_telegraphActive)
                {
                    float k = _def.SpawnTelegraphSeconds > 0f
                        ? Mathf.Clamp01(1f - _restTimer / _def.SpawnTelegraphSeconds)
                        : 0f;
                    transform.localScale = _baseScale * (1f + 0.15f * k);   // 살짝 커졌다 돌아온다
                }

                if (_restTimer > 0f) return;

                // 묶음 시작 — 링이 사라지는 그 프레임에 첫 적이 나온다.
                _telegraphActive = false;
                _telegraph.End();
                transform.localScale = _baseScale;
                _phase = SpawnPhase.Volley;
                _volleyRemaining = _def.SpawnVolleyCount;
                _spacingTimer = 0f;
            }

            // InvokeRepeating 금지 — Update 타이머 누산.
            _spacingTimer -= dt;
            while (_volleyRemaining > 0 && _spacingTimer <= 0f)
            {
                SpawnEnemy();
                _volleyRemaining--;
                if (_volleyRemaining > 0) _spacingTimer += _def.SpawnVolleySpacing;
            }

            if (_volleyRemaining <= 0)
            {
                _phase = SpawnPhase.Rest;
                _restTimer = _def.SpawnRestSeconds;
            }
        }

        private void SpawnEnemy()
        {
            var definition = PickFromSpawnTable();
            if (definition == null || definition.Prefab == null)
            {
                Debug.LogError($"[{nameof(MotherSpawner)}] 스폰 테이블에서 낼 적이 없다 (Prefab 포함 확인)", this);
                return;
            }

            var go = Instantiate(definition.Prefab, transform.position, Quaternion.identity);
            if (_enemiesParent != null) go.transform.SetParent(_enemiesParent, true);

            var startNode = _graph.FindNearestNode(transform.position, PathAgent.Enemy);
            var enemy = go.GetComponent<Enemy>();
            if (enemy != null) enemy.InitializeFromMother(startNode, definition, HealthMultiplier());
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Diagnostics.DebugOverlay.NotifyEnemySpawned();
#endif
        }

        /// <summary>거리로 인한 난이도 차이는 스폰되는 적의 체력으로 조절한다 (회의 결정 13).
        /// 기준은 <b>보호대상의 진행도</b>다 — 실시간 거리로 하면 고무줄 난이도로 읽힌다.</summary>
        private float HealthMultiplier()
        {
            if (_def == null) return 1f;
            float progress = _escortee != null && _escortee.Follower.HasRoute
                ? _escortee.Follower.Progress01
                : 0f;
            return _def.EnemyHealthMultiplierAt(progress);
        }

        /// <summary>가중치 추첨. 시드를 고정하지 않는다 (G-17 작업 2).
        /// 버스트 전용 테이블은 만들지 않는다 — 테이블은 하나다 (G-17 작업 3).</summary>
        private EnemyDefinition PickFromSpawnTable()
        {
            var table = _def != null ? _def.SpawnTable : null;
            if (table == null || table.Count == 0)
            {
                Debug.LogError($"[{nameof(MotherSpawner)}] 스폰 테이블이 비어 있다 — StageDefinition 을 확인하라", this);
                return null;
            }

            int total = 0;
            for (int i = 0; i < table.Count; i++)
                if (table[i].Enemy != null && table[i].Weight > 0) total += table[i].Weight;

            if (total <= 0)
            {
                Debug.LogError($"[{nameof(MotherSpawner)}] 스폰 테이블의 가중치 합이 0이다 — 아무것도 뽑을 수 없다", this);
                return null;
            }

            // Random.Range(int,int) 는 상한 배타 — [0, total) 이 정확히 필요한 범위다.
            int roll = Random.Range(0, total);
            for (int i = 0; i < table.Count; i++)
            {
                if (table[i].Enemy == null || table[i].Weight <= 0) continue;
                roll -= table[i].Weight;
                if (roll < 0) return table[i].Enemy;
            }

            return null;   // 위 누적이 total 과 일치하므로 도달하지 않는다
        }
    }
}
