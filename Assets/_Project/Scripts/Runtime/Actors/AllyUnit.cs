using UnityEngine;
using PMF.Combat;
using PMF.Data;
using PMF.Grid;
using PMF.Pathing;
using PMF.Session;

namespace PMF.Actors
{
    /// <summary>
    /// 아군 유닛. 상태머신: Queued → Marching → Deployed.
    /// 행군 중에는 공격 불가 (Attacker.Enabled = false).
    /// D-03 토글이 꺼져 있으면(기본) 행군 중 TargetRegistry 에 등록되지 않는다.
    /// </summary>
    public sealed class AllyUnit : MonoBehaviour
    {
        private enum State { Queued, Marching, Deployed }

        private State _state = State.Queued;
        private readonly PathFollower _follower = new PathFollower();
        private readonly System.Collections.Generic.List<PathNode> _route =
            new System.Collections.Generic.List<PathNode>();
        private readonly System.Collections.Generic.List<PathNode> _lineBuffer =
            new System.Collections.Generic.List<PathNode>();

        private GameSession _session;
        private GridSystem _grid;
        private PathGraph _graph;
        private UnitDefinition _def;
        private StageDefinition _stage;
        private Health _health;
        private Attacker _attacker;
        private SpriteRenderer _sprite;
        private LineRenderer _marchLine;

        private GridCoord _targetSlot;
        private bool _onStraightLeg;          // 마지막 노드 → 슬롯 직선 구간
        private float _marchStartTime;

        [SerializeField] private Color _marchingColor = new Color(0.55f, 0.75f, 1f);   // 연한 파랑
        [SerializeField] private Color _deployedColor = new Color(0.15f, 0.3f, 0.8f);  // 진한 파랑

        public bool IsDeployed => _state == State.Deployed;

        private void Awake()
        {
            _session = GameSession.Instance;
            _grid = GridSystem.Instance;
            _graph = PathGraph.Instance;

            _health = GetComponent<Health>();
            _attacker = GetComponent<Attacker>();
            _sprite = GetComponent<SpriteRenderer>();
            _marchLine = GetComponent<LineRenderer>();
            if (_marchLine != null) _marchLine.enabled = false;

            // 기본은 미등록. BeginMarch(D-03 on) 또는 Deploy 시 등록.
            if (_health != null) _health.SetRegistryEnabled(false);

            if (_attacker != null) _attacker.Enabled = false;
        }

        private void Start()
        {
            if (_def == null || _session == null || _health == null) return;
            ConfigureCombat();
            _health.Initialize(_def.MaxHealth, Team.Ally);
            _health.OnDied += OnDied;
        }

        private void OnDisable()
        {
            if (_health != null) _health.OnDied -= OnDied;
        }

        /// <summary>고용 확정 즉시 호출. village.DepartureNode 에서 slot 까지 걷는다.</summary>
        public void BeginMarch(Village village, UnitDefinition definition, GridCoord slot)
        {
            _def = definition;
            _targetSlot = slot;
            _state = State.Marching;
            _marchStartTime = Time.time;
            _stage = _session.Definition;

            var goalNode = _graph.FindNearestNode(_grid.CellToWorld(slot), PathAgent.Ally);
            if (goalNode == null)
            {
                Debug.LogError($"[{nameof(AllyUnit)}] 목적지 근처 노드 없음", this);
                Deploy();
                return;
            }

            // 행군 목적지 노드가 슬롯에서 너무 멀면 맵 저작 문제 → 경고로 드러낸다.
            float nodeToSlot = Vector3.Distance(goalNode.WorldPosition, _grid.CellToWorld(slot));
            if (nodeToSlot > 3f * _grid.CellSize)
                Debug.LogWarning($"[{nameof(AllyUnit)}] 목적지 노드가 슬롯에서 {nodeToSlot:F1} 셀 — 맵 저작 점검", this);

            var fromNode = village.DepartureNode ?? _graph.FindNearestNode(transform.position, PathAgent.Ally);
            if (fromNode == null || !_graph.TryFindRoute(fromNode, goalNode, PathAgent.Ally, _route))
            {
                Debug.LogError($"[{nameof(AllyUnit)}] 행군 경로 탐색 실패: {fromNode} -> {goalNode}", this);
                Deploy();
                return;
            }

            _onStraightLeg = false;
            _follower.SetRoute(_route, transform.position);

            // 행군 중 등록 정책 (D-03)
            if (_stage != null && _stage.AlliesCanDieWhileMarching && _health != null)
                _health.SetRegistryEnabled(true);

            if (_sprite != null) _sprite.color = _marchingColor;
            UpdateMarchLine();
        }

        private void ConfigureCombat()
        {
            if (_attacker == null) return;
            _attacker.Range = _def.AttackRange;
            _attacker.Damage = _def.AttackDamage;
            _attacker.Interval = _def.AttackInterval;
            _attacker.TargetTeam = Team.Enemy;
        }

        private void Update()
        {
            if (_session.Result != GameResult.InProgress)
                return;   // 행군 중에 게임이 끝나면 그대로 멈춘다.

            switch (_state)
            {
                case State.Marching:
                    MarchStep();
                    break;
                case State.Deployed:
                    if (_attacker != null) _attacker.Anchor = EscortAnchor();
                    break;
            }
        }

        private Vector3 EscortAnchor()
        {
            var escortee = _session.Escortee;
            return escortee != null ? escortee.transform.position : transform.position;
        }

        private void MarchStep()
        {
            float step = _def.MoveSpeed * Time.deltaTime;

            if (!_onStraightLeg)
            {
                bool passed = _follower.Advance(step);
                transform.position = _follower.Position;
                if (passed) UpdateMarchLine();

                if (_follower.IsFinished)
                {
                    _onStraightLeg = true;
                    // 주석: 마지막 직선 구간은 Blocked 셀을 통과할 수 있다. 프로토타입에서는 허용
                    // (경로/마을 저작을 잘 하면 드물다). 거리 경고는 BeginMarch 에서 이미 검사.
                }
            }
            else
            {
                Vector3 target = _grid.CellToWorld(_targetSlot);
                transform.position = Vector3.MoveTowards(transform.position, target, step);
                if ((transform.position - target).sqrMagnitude < 0.0001f)
                    Deploy();
            }
        }

        private void Deploy()
        {
            _state = State.Deployed;

            // 슬롯 중심에 정확히 스냅.
            transform.position = _grid.CellToWorld(_targetSlot);

            if (_marchLine != null) _marchLine.enabled = false;
            if (_sprite != null) _sprite.color = _deployedColor;

            // 상태 전환 시점에 정확히 한 번만 바꾼다 (Update 에서 매 프레임 대입 금지).
            if (_attacker != null) _attacker.Enabled = true;

            // 배치 시점에 등록 (D-03 off 기본).
            if (_health != null) _health.SetRegistryEnabled(true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Diagnostics.DebugOverlay.NotifyAllyDeployed(Time.time - _marchStartTime);
#endif
        }

        private void OnDied(Health health)
        {
            if (_state == State.Marching &&
                (_stage == null || _stage.AlliesCanDieWhileMarching))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Diagnostics.DebugOverlay.NotifyAllyLostWhileMarching();
#endif
            }
            Destroy(gameObject);
        }

        /// <summary>행군 경로를 선으로 그린다 — "얼마나 걸리는지"가 보여야 트레이드오프가 성립.</summary>
        private void UpdateMarchLine()
        {
            if (_marchLine == null) return;
            if (_follower.IsFinished) { _marchLine.enabled = false; return; }

            _follower.FillRemainingNodes(_lineBuffer);
            int count = 1 + _lineBuffer.Count;
            _marchLine.positionCount = count;
            _marchLine.SetPosition(0, transform.position);
            for (int i = 0; i < _lineBuffer.Count; i++)
                _marchLine.SetPosition(i + 1, _lineBuffer[i].WorldPosition);
            _marchLine.enabled = true;
        }
    }
}
