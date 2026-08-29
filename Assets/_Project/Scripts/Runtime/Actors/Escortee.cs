using System.Collections.Generic;
using UnityEngine;
using PMF.Combat;
using PMF.Grid;
using PMF.Pathing;
using PMF.Session;

namespace PMF.Actors
{
    /// <summary>
    /// 보호대상. 경로를 따라 탈출 지점까지 자동으로 간다.
    /// transform 은 진실이 아니고 PathFollower.Position 의 결과를 반영만 한다.
    /// </summary>
    public sealed class Escortee : MonoBehaviour
    {
        private readonly PathFollower _follower = new PathFollower();
        private readonly List<PathNode> _route = new List<PathNode>();

        private GameSession _session;
        private PathGraph _graph;
        private Health _health;
        private float _speed = 1.2f;
        private PathNode _exitNode;

        public PathFollower Follower => _follower;
        public PathNode CurrentNode => _follower.HasRoute ? _follower.CurrentNode : null;
        public Health Health => _health;

        private void Start()
        {
            _session = GameSession.Instance;
            _graph = PathGraph.Instance;
            if (_session == null || _graph == null)
            {
                Debug.LogError($"[{nameof(Escortee)}] GameSession/PathGraph 없음 — Script Execution Order 확인", this);
                enabled = false;
                return;
            }

            if (_session.Definition != null)
                _speed = _session.Definition.EscorteeSpeed;

            // 체력바 (G-09) — 보호대상은 맞는 순간부터 크게 읽혀야 한다.
            var barGo = new GameObject("HealthBar");
            barGo.transform.SetParent(transform, false);
            barGo.AddComponent<PMF.UI.HealthBar>()
                .Init(transform, GetComponent<PMF.Combat.Health>(),
                      _session.Definition != null && _session.Definition.HealthBarHideWhenFull);

            _session.RegisterEscortee(this);

            _health = GetComponent<Health>();
            if (_health != null)
            {
                _health.Initialize(_session.Definition != null ? _session.Definition.EscorteeMaxHealth : 100f,
                                   Team.Escortee);
                _health.OnDied += OnDied;
                _health.OnDamaged += OnDamaged;
            }

            // 시작 노드 → 탈출 노드 최초 경로
            var start = _graph.EscorteeStartNode ?? _graph.FindNearestNode(transform.position, PathAgent.Escortee);
            _exitNode = _graph.ExitNodes.Count > 0 ? _graph.ExitNodes[0] : null;
            if (start != null && _exitNode != null &&
                _graph.TryFindRoute(start, _exitNode, PathAgent.Escortee, _route))
            {
                _follower.SetRoute(_route, start.WorldPosition);
                transform.position = _follower.Position;
            }
            else
            {
                Debug.LogError($"[{nameof(Escortee)}] 초기 경로 탐색 실패", this);
            }

            // 지름길 개방 시 재계산 (시작점은 반드시 현재 노드)
            _graph.OnGraphChanged += RecalculateRoute;
        }

        private void OnDisable()
        {
            if (_graph != null) _graph.OnGraphChanged -= RecalculateRoute;
            if (_health != null) _health.OnDied -= OnDied;
            if (_health != null) _health.OnDamaged -= OnDamaged;
        }

        /// <summary>보호대상 피격음 (G-11) — 낮고 둔탁. 다른 소리와 확실히 구분.</summary>
        private void OnDamaged(Health health, float amount)
        {
            if (_session != null && _session.Sfx != null)
                _session.Sfx.Play(PMF.Audio.SfxPlayer.SfxId.EscorteeHurt);
        }

        private void Update()
        {
            if (_session == null || _session.Result != GameResult.InProgress) return;

            if (!_follower.HasRoute || _follower.IsFinished)
            {
                if (_follower.IsFinished) _session.DeclareVictory();
                return;
            }

            bool passedNode = _follower.Advance(_speed * Time.deltaTime);
            transform.position = _follower.Position;

            if (passedNode)
                _session.NotifyEscorteeReachedNode(_follower.CurrentNode);

            if (_follower.IsFinished)
                _session.DeclareVictory();
        }

        /// <summary>지름길이 열렸을 때. 현재 노드에서 탈출 노드까지 재계산.</summary>
        /// <summary>경로를 다시 푼다 (지름길 개방 등 그래프 변경 시).
        ///
        /// ⚠️ <b>보호대상은 앞으로만 간다.</b>
        /// 방금 지난 노드(CurrentNode)에서 다시 풀면 뒤로 돌아가는 경로가 나올 수 있다.
        /// 실제로 지름길을 이미 지나친 뒤에 구매하면 되돌아가 지름길을 타려 했다 (2026-08-29 사용자 보고).
        /// 그래서 <b>지금 향하고 있는 노드에서</b> 다시 풀고, 그 앞에 방금 지난 노드를 붙여
        /// PathFollower 의 "route[0] 은 방금 지났거나 향하는 노드" 규약을 지킨다.
        /// 결과적으로 이미 지나친 지름길은 경로 후보에서 자연히 빠진다 — 구매는 되지만 그쪽으로 가지 않는다.</summary>
        private void RecalculateRoute()
        {
            if (_exitNode == null) return;

            var next = _follower.NextNode;
            if (next == null) return;   // 이미 도착했거나 경로가 없다 — 다시 풀 것이 없다

            if (!_graph.TryFindRoute(next, _exitNode, PathAgent.Escortee, _route)) return;

            var current = _follower.CurrentNode;
            if (current != null && current != next && (_route.Count == 0 || _route[0] != current))
                _route.Insert(0, current);

            _follower.SetRoute(_route, transform.position);
            // 실패 시 기존 경로 유지 (지름길은 Escortee 전용이라 실패는 이론상 없다)
        }

        private void OnDied(Health health)
        {
            _session.DeclareDefeat();
        }
    }
}
