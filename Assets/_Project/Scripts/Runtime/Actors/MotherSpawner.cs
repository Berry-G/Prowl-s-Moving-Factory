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
        private Transform _enemiesParent;

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

            // 스폰 0.5초 전 예고 연출 — 모체 색이 잠깐 밝아진다.
            float timeToNext = Mathf.Max(_def.MotherSpawnInterval - _stateTimer, 0f);
            if (_sprite != null)
            {
                float flash = timeToNext < 0.5f ? 0.5f : 0f;
                _sprite.color = Color.Lerp(_baseColor, Color.white, flash);
            }

            // InvokeRepeating 금지 — Update 타이머 누산.
            _stateTimer += dt;
            if (_stateTimer >= _def.MotherSpawnInterval)
            {
                _stateTimer -= _def.MotherSpawnInterval;
                SpawnEnemy();
            }
        }

        private void SpawnEnemy()
        {
            var definition = _def.MotherSpawnEnemy;
            if (definition == null || definition.Prefab == null)
            {
                Debug.LogError($"[{nameof(MotherSpawner)}] MotherSpawnEnemy/Prefab 없음", this);
                return;
            }

            var go = Instantiate(definition.Prefab, transform.position, Quaternion.identity);
            if (_enemiesParent != null) go.transform.SetParent(_enemiesParent, true);

            var startNode = _graph.FindNearestNode(transform.position, PathAgent.Enemy);
            var enemy = go.GetComponent<Enemy>();
            if (enemy != null) enemy.InitializeFromMother(startNode);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Diagnostics.DebugOverlay.NotifyEnemySpawned();
#endif
        }
    }
}
