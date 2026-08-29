using UnityEngine;
using PMF.Combat;
using PMF.Data;
using PMF.Pathing;
using PMF.Session;

namespace PMF.Actors
{
    /// <summary>
    /// 잡몹. 보호대상을 추격해 공격한다.
    /// 상태머신: Moving → Attacking → Moving (히스테리시스: 진입 Range, 이탈 Range * 1.2).
    /// 경로 재계산 트리거는 딱 3개: 스폰 시 / 보호대상 노드 통과 시 / 자신이 노드 통과 시.
    /// </summary>
    public sealed class Enemy : MonoBehaviour
    {
        private enum State { Moving, Attacking }

        private readonly PathFollower _follower = new PathFollower();
        private readonly System.Collections.Generic.List<PathNode> _route =
            new System.Collections.Generic.List<PathNode>();

        private State _state = State.Moving;
        private GameSession _session;
        private PathGraph _graph;
        private Escortee _escortee;
        private EnemyDefinition _def;
        private Attacker _attacker;
        private PMF.UI.ShotLine _shotLine;
        private UnityEngine.LineRenderer _threatLine;
        private Health _health;
        private PathNode _spawnNode;
        private float _healthMultiplier = 1f;

        /// <param name="definition">이 개체의 정의. 스폰 테이블(G-17)이 종류를 고르므로
        /// 스테이지 SO 에서 되읽으면 안 된다 — 2종째부터 전부 같은 적이 되어 버린다.</param>
        /// <param name="healthMultiplier">진행도별 체력 배율 (회의 결정 13).</param>
        public void InitializeFromMother(PathNode startNode, EnemyDefinition definition, float healthMultiplier = 1f)
        {
            // Start 이전에 호출될 수 있으므로 여기선 데이터만 받아 두고 Start 에서 사용.
            _spawnNode = startNode;
            _def = definition;
            _healthMultiplier = Mathf.Max(0.01f, healthMultiplier);
        }

        /// <summary>모체를 거치지 않고 놓인 적을 위한 폴백. 없으면 null.</summary>
        private EnemyDefinition FirstFromSpawnTable()
        {
            var table = _session.Definition != null ? _session.Definition.SpawnTable : null;
            if (table == null) return null;
            for (int i = 0; i < table.Count; i++)
                if (table[i].Enemy != null) return table[i].Enemy;
            return null;
        }

        private void Start()
        {
            _session = GameSession.Instance;
            _graph = PathGraph.Instance;
            if (_session == null || _graph == null)
            {
                Debug.LogError($"[{nameof(Enemy)}] GameSession/PathGraph 없음", this);
                enabled = false;
                return;
            }

            _escortee = _session.Escortee;

            // 정의는 모체가 스폰 시 넘겨준다 (G-17). 씬에 직접 놓인 적처럼 그 경로를 안 탄 개체만
            // 스폰 테이블 첫 항목으로 대신한다.
            if (_def == null) _def = FirstFromSpawnTable();

            if (_escortee == null || _def == null)
            {
                Debug.LogError($"[{nameof(Enemy)}] 보호대상 또는 EnemyDefinition 없음", this);
                enabled = false;
                return;
            }

            // 적은 보호대상보다 빨라야 한다 — 아니면 검증 로그로 경고.
            if (_def.MoveSpeed <= _session.Definition.EscorteeSpeed)
                Debug.LogWarning($"[{nameof(Enemy)}] MoveSpeed({_def.MoveSpeed}) <= " +
                                 $"EscorteeSpeed({_session.Definition.EscorteeSpeed}). 적은 빨라야 한다.", this);

            _health = GetComponent<Health>();
            if (_health != null)
            {
                _health.Initialize(_def.MaxHealth * _healthMultiplier, Team.Enemy);
                _health.OnDied += OnDied;
            }

            _attacker = GetComponent<Attacker>();
            if (_attacker != null)
            {
                _attacker.Range = _def.AttackRange;
                _attacker.Damage = _def.AttackDamage;
                _attacker.Interval = _def.AttackInterval;
                _attacker.TargetTeam = Team.Escortee;

                // 사격 연출 (G-07) — 적 공격은 붉은 직선 (로봇 = 원거리 사격 톤).
                _attacker.OnFired += HandleFired;
                var shotGo = new GameObject("ShotLine");
                shotGo.transform.SetParent(transform, false);
                _shotLine = shotGo.AddComponent<PMF.UI.ShotLine>();

                // 피격 플래시 (G-08) — 기존 Health.OnDamaged 구독.
                var flash = gameObject.AddComponent<PMF.UI.HitFlash>();
                flash.Init(GetComponent<PMF.Combat.Health>(), GetComponent<SpriteRenderer>(),
                           _session.Definition != null ? _session.Definition.HitFlashSeconds : 0.08f);

                // 체력바 (G-09) — 월드 LineRenderer. 만피에는 숨김.
                var barGo = new GameObject("HealthBar");
                barGo.transform.SetParent(transform, false);
                barGo.AddComponent<PMF.UI.HealthBar>()
                    .Init(transform, GetComponent<PMF.Combat.Health>(),
                          _session.Definition != null ? _session.Definition.HealthBarHideWhenFull : true);

                // 위협선 (G-09) — 공격 중일 때 대상까지 붉은 계열 선.
                var threatGo = new GameObject("ThreatLine");
                threatGo.transform.SetParent(transform, false);
                _threatLine = threatGo.AddComponent<UnityEngine.LineRenderer>();
                _threatLine.positionCount = 2;
                _threatLine.startWidth = _threatLine.endWidth = 0.03f;
                _threatLine.material = new Material(Shader.Find("Sprites/Default"));
                _threatLine.sortingLayerName = "Deploy";
                _threatLine.sortingOrder = 7;
                _threatLine.useWorldSpace = true;
                _threatLine.startColor = _threatLine.endColor = new Color(1f, 0.1f, 0.1f, 0.5f);
                _threatLine.enabled = false;
            }

            // D-04 토글: 기본값 false. true 면 상황에 따라 TargetTeam 을 Ally 로 전환 (동작은 비워 둔다).
            if (_session.Definition.EnemiesTargetAllies)
            {
                // 프로토타입: 전환 규칙 미구현 (GDD §13 D-04).
            }

            RecalculateRoute();
        }

        private void OnEnable()
        {
            if (_session != null) _session.OnEscorteeReachedNode += OnEscorteeMoved;
        }

        private void OnDisable()
        {
            // 구독 해제를 빠뜨리면 MissingReferenceException 폭탄.
            if (_session != null) _session.OnEscorteeReachedNode -= OnEscorteeMoved;
            if (_health != null) _health.OnDied -= OnDied;
            if (_attacker != null) _attacker.OnFired -= HandleFired;
        }

        /// <summary>적 공격 연출 (G-07) — 붉은 직선. 원거리 스타일.</summary>
        private void HandleFired(IDamageable target)
        {
            if (_shotLine == null || target == null) return;
            float seconds = _session.Definition != null ? _session.Definition.ShotLineSeconds : 0.07f;
            _shotLine.Show(transform.position, target, new Color(1f, 0.25f, 0.2f, 0.95f), false, seconds);
        }

        private void OnEscorteeMoved(PathNode node) => RecalculateRoute();

        private void Update()
        {
            if (_session.Result != GameResult.InProgress) return;
            if (_escortee == null) return;

            float distSqr = (_escortee.transform.position - transform.position).sqrMagnitude;
            float range = _def.AttackRange;

            switch (_state)
            {
                case State.Moving:
                    if (distSqr <= range * range)
                    {
                        _state = State.Attacking;      // 사거리 진입
                        break;
                    }
                    if (!_follower.HasRoute || _follower.IsFinished)
                    {
                        RecalculateRoute();            // 도달했는데 사거리 밖 → 재계산
                        break;
                    }
                    if (_follower.Advance(_def.MoveSpeed * Time.deltaTime))
                    {
                        // 자신이 노드를 통과 → 재계산 트리거 3번
                        transform.position = _follower.Position;
                        RecalculateRoute();
                    }
                    else
                    {
                        transform.position = _follower.Position;
                    }
                    break;

                case State.Attacking:
                    // 이탈 = Range * 1.2 — 매 프레임 Moving/Attacking 토글 방지.
                    float exitRange = range * 1.2f;
                    if (distSqr > exitRange * exitRange)
                        _state = State.Moving;
                    break;
            }

            // 위협선 (G-09) — 공격 중일 때만 대상까지 붉은 선.
            bool attacking = _state == State.Attacking;
            if (_threatLine != null && _threatLine.enabled != attacking)
                _threatLine.enabled = attacking;
            if (attacking && _threatLine != null)
            {
                _threatLine.SetPosition(0, transform.position);
                _threatLine.SetPosition(1, _escortee.transform.position);
            }
        }

        /// <summary>목적지 = 보호대상의 현재 노드.</summary>
        private void RecalculateRoute()
        {
            var from = _follower.HasRoute ? _follower.CurrentNode : _spawnNode;
            if (from == null)
                from = _graph.FindNearestNode(transform.position, PathAgent.Enemy);

            var goal = _escortee.CurrentNode
                       ?? _graph.FindNearestNode(_escortee.transform.position, PathAgent.Enemy);
            if (from == null || goal == null) return;

            // 지름길은 Escortee 전용이라 unreachable 가능 → 도달 가능한 최근접 노드로 폴백.
            if (!_graph.TryFindRoute(from, goal, PathAgent.Enemy, _route))
            {
                var fallback = _graph.FindReachableNodeNearestTo(
                    _escortee.transform.position, PathAgent.Enemy, from);
                if (fallback == null || fallback == from) return;
                if (!_graph.TryFindRoute(from, fallback, PathAgent.Enemy, _route)) return;
            }

            _follower.SetRoute(_route, transform.position);
            transform.position = _follower.Position;
        }

        private void OnDied(Health health)
        {
            // 격파 연출 (G-08, GDD §12) — 부품이 흩어진다. 레지스트리 해제는 Health.TakeDamage 가 이미 했다.
            var stage = _session != null ? _session.Definition : null;
            int count = stage != null ? stage.DebrisCount : 5;
            float seconds = stage != null ? stage.DebrisSeconds : 0.35f;
            var sr = GetComponent<SpriteRenderer>();
            var color = sr != null ? sr.color : new Color(0.9f, 0.3f, 0.25f);
            UI.DebrisScatter.Spawn(transform.position, color, count, seconds);

            // 로봇 격파음 (G-11).
            if (_session != null && _session.Sfx != null)
                _session.Sfx.Play(Audio.SfxPlayer.SfxId.RobotDown);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Diagnostics.DebugOverlay.NotifyEnemyKilled();
#endif
            Destroy(gameObject);
        }
    }
}
