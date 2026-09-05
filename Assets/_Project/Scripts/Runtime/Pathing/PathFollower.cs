using System.Collections.Generic;
using UnityEngine;

namespace PMF.Pathing
{
    /// <summary>
    /// 경로를 따라 걷는 동작만 담당하는 순수 클래스 (MonoBehaviour 가 아니다).
    /// Escortee, Enemy, AllyUnit, 모체가 각자 하나씩 필드로 들고 쓴다.
    /// transform 은 진실이 아니고 Position 의 결과를 반영만 한다.
    /// </summary>
    public sealed class PathFollower
    {
        private readonly List<PathNode> _route = new List<PathNode>();
        private int _nextIndex;          // _route 중 다음 목표 노드의 인덱스
        private Vector3 _position;
        private float _totalLength;
        private float _travelled;
        private bool _finished;

        public bool HasRoute => _route.Count > 0;
        public bool IsFinished => !HasRoute || _finished;
        public Vector3 Position => _position;
        public PathNode CurrentNode => _route.Count > 0 ? _route[Mathf.Max(_nextIndex - 1, 0)] : null;
        public PathNode NextNode => (!_finished && _nextIndex < _route.Count) ? _route[_nextIndex] : null;

        /// <summary>전체 경로 길이. 0이면 나누기 방어용으로 1을 반환한다.</summary>
        public float TotalLength => _totalLength > 0f ? _totalLength : 1f;
        public float RemainingDistance => Mathf.Max(_totalLength - _travelled, 0f);
        public float Progress01 => Mathf.Clamp01(_travelled / TotalLength);

        /// <summary>아직 통과하지 않은 노드를 buffer 에 채운다 (행군선 그리기용. 할당 없음).</summary>
        public void FillRemainingNodes(List<PathNode> buffer)
        {
            buffer.Clear();
            if (!HasRoute || _finished) return;
            for (int i = _nextIndex; i < _route.Count; i++)
                buffer.Add(_route[i]);
        }

        /// <summary>
        /// 경로 중간에 호출될 수 있다. 현재 위치에서 새 경로의 첫 노드로 이어지도록 처리한다.
        /// route[0] 은 항상 "지금 향하고 있거나 방금 지난 노드"여야 한다.
        /// 어긋나면 LogWarning 을 찍고 startPosition 에 서 있는 것을 유지한다 (순간이동은 호출자 몫).
        /// </summary>
        public void SetRoute(IReadOnlyList<PathNode> route, Vector3 startPosition)
        {
            _route.Clear();
            if (route == null || route.Count == 0)
            {
                Clear();
                return;
            }

            for (int i = 0; i < route.Count; i++) _route.Add(route[i]);

            // 이 메서드는 <b>엣지 중간에 호출되는 것이 정상</b>이다(위 요약 참조).
            // route[0] 에 정확히 서 있으라고 요구하면 그 정상 사용까지 전부 경고가 된다.
            //
            // 허용 범위를 "나가는 엣지"(route[0]→route[1]) 길이로 잡았더니 여전히 오탐이 났다
            // (2026-08-30 실측: 한 판에 47건). 재계산 시점의 액터는 route[0] 을 <b>다음 목표</b>로
            // 삼고 <b>들어오는 엣지</b> 위에 서 있기 때문이다 — 나가는 엣지 길이와는 상관이 없다.
            // 그래서 route[0] 에 붙은 엣지 중 가장 긴 것을 상한으로 쓴다. 액터가 정상적으로 있을 수
            // 있는 최대 거리가 그것이다. 그보다 멀면 경로가 엉뚱한 곳에서 시작한 것이니 진짜 버그다.
            const float snapEpsilon = 0.05f;
            float tolerance = snapEpsilon;
            var incident = _route[0].Edges;
            for (int i = 0; i < incident.Count; i++)
                if (incident[i].Cost > tolerance) tolerance = incident[i].Cost;
            if (_route.Count > 1)
                tolerance = Mathf.Max(tolerance,
                                      Vector3.Distance(_route[0].WorldPosition, _route[1].WorldPosition));
            if (Vector3.SqrMagnitude(_route[0].WorldPosition - startPosition) > tolerance * tolerance)
                Debug.LogWarning($"[PathFollower] route[0]({_route[0]}) 이 startPosition({startPosition}) 에서 " +
                                 $"첫 엣지 길이({tolerance:F1})보다 멀다. 호출자 경로를 점검하라.");

            _position = startPosition;
            _nextIndex = _route.Count > 1 ? 1 : 0;
            _finished = false;
            RecalculateLength();
        }

        public void Clear()
        {
            _route.Clear();
            _nextIndex = 0;
            _position = Vector3.zero;
            _totalLength = 0f;
            _travelled = 0f;
            _finished = false;
        }

        /// <summary>
        /// distance 만큼 전진시킨다. 경로 끝에 도달하면 IsFinished 가 true 가 된다.
        /// 노드를 하나 이상 통과했으면 true 를 반환한다 (경로 재계산 트리거용).
        /// </summary>
        public bool Advance(float distance)
        {
            if (!HasRoute || _finished || distance <= 0f) return false;

            bool passedAny = false;
            // 무한루프 방어: 반복 상한 (경로 길이 + 버퍼). 길이 0 엣지(같은 셀에 노드 2개) 대비.
            int guard = _route.Count + 2;

            while (distance > 0f && !_finished && guard-- > 0)
            {
                PathNode target = NextNode;
                if (target == null) { _finished = true; break; }

                Vector3 toTarget = target.WorldPosition - _position;
                float segLen = toTarget.magnitude;

                if (segLen <= 1e-4f)
                {
                    // 길이 0 엣지 — 노드만 소비하고 진행.
                    _nextIndex++;
                    passedAny = true;
                    CheckArrival();
                    continue;
                }

                if (distance >= segLen)
                {
                    _position = target.WorldPosition;
                    distance -= segLen;
                    _travelled += segLen;
                    _nextIndex++;
                    passedAny = true;
                    CheckArrival();
                }
                else
                {
                    _position += toTarget.normalized * distance;
                    _travelled += distance;
                    distance = 0f;
                }
            }

            if (guard <= 0)
                Debug.LogError($"[PathFollower] 노드 소비 반복 상한 초과. 경로 데이터 확인 (길이 0 엣지 다수?).");

            return passedAny;
        }

        private void CheckArrival()
        {
            if (_nextIndex >= _route.Count)
                _finished = true;
        }

        private void RecalculateLength()
        {
            _totalLength = 0f;
            _travelled = 0f;
            Vector3 prev = _position;
            for (int i = _nextIndex; i < _route.Count; i++)
            {
                _totalLength += Vector3.Distance(prev, _route[i].WorldPosition);
                prev = _route[i].WorldPosition;
            }
        }
    }
}
