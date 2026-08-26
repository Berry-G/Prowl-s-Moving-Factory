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

            _session.RegisterEscortee(this);

            _health = GetComponent<Health>();
            if (_health != null)
            {
                _health.Initialize(_session.Definition != null ? _session.Definition.EscorteeMaxHealth : 100f,
                                   Team.Escortee);
                _health.OnDied += OnDied;
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
        private void RecalculateRoute()
        {
            if (_exitNode == null) return;

            var from = _follower.CurrentNode;
            if (from == null) return;

            if (_graph.TryFindRoute(from, _exitNode, PathAgent.Escortee, _route))
                _follower.SetRoute(_route, transform.position);
            // 실패 시 기존 경로 유지 (지름길은 Escortee 전용이라 실패는 이론상 없다)
        }

        private void OnDied(Health health)
        {
            _session.DeclareDefeat();
        }
    }
}
