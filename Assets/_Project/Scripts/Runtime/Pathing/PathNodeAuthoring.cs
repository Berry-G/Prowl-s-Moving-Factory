using System;
using System.Collections.Generic;
using UnityEngine;

namespace PMF.Pathing
{
    /// <summary>
    /// 씬에 배치하는 경로 노드 저작 컴포넌트. PathGraph.Awake 에서 수집된다.
    /// 기즈모는 저작 컴포넌트가 자기 데이터로 직접 그린다 (플레이 중이 아닐 땐 Instance 가 null 이므로).
    /// </summary>
    public sealed class PathNodeAuthoring : MonoBehaviour
    {
        [Serializable]
        public struct Connection
        {
            public PathNodeAuthoring Target;
            public PathAgent Allowed;      // 기본값 PathAgent.All. 지름길만 Escortee 로 바꾼다.
            public bool IsShortcut;
            public bool Bidirectional;     // 기본 true
        }

        [SerializeField] private List<Connection> _connections = new List<Connection>();
        [SerializeField] private bool _isExit;
        [SerializeField] private bool _isEscorteeStart;

        /// <summary>PathGraph.Build() 가 채우는 런타임 노드.</summary>
        public PathNode RuntimeNode { get; private set; }

        public List<Connection> Connections => _connections;
        public bool IsExit => _isExit;
        public bool IsEscorteeStart => _isEscorteeStart;

        internal void Attach(PathNode node) => RuntimeNode = node;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // 노드: 시작=흰색, 탈출=금색, 일반=회색
            if (_isEscorteeStart) Gizmos.color = Color.white;
            else if (_isExit) Gizmos.color = new Color(1f, 0.84f, 0f);
            else Gizmos.color = Color.gray;

            Gizmos.DrawWireSphere(transform.position, 0.15f);
            Gizmos.DrawSphere(transform.position, 0.1f);

            if (_connections == null) return;

            foreach (var conn in _connections)
            {
                if (conn.Target == null) continue;

                if (conn.IsShortcut)
                {
                    // 지름길: 노란 점선 근사 (짧은 세그먼트 반복)
                    Gizmos.color = Color.yellow;
                    DrawDashedLine(transform.position, conn.Target.transform.position);
                }
                else
                {
                    Gizmos.color = new Color(0.7f, 0.7f, 0.7f, 0.9f);
                    Gizmos.DrawLine(transform.position, conn.Target.transform.position);
                    DrawArrow(transform.position, conn.Target.transform.position);
                    if (conn.Bidirectional)
                        DrawArrow(conn.Target.transform.position, transform.position);
                }
            }
        }

        private static void DrawDashedLine(Vector3 a, Vector3 b)
        {
            Vector3 dir = b - a;
            float len = dir.magnitude;
            if (len < 0.01f) return;
            dir /= len;

            const float dash = 0.25f;
            for (float t = 0f; t < len; t += dash * 2f)
            {
                Vector3 from = a + dir * t;
                Vector3 to = a + dir * Mathf.Min(t + dash, len);
                Gizmos.DrawLine(from, to);
            }
        }

        private static void DrawArrow(Vector3 from, Vector3 to)
        {
            Vector3 dir = to - from;
            float len = dir.magnitude;
            if (len < 0.3f) return;
            dir /= len;

            Vector3 mid = from + dir * (len * 0.55f);
            Vector3 left = new Vector3(-dir.y, dir.x, 0f);
            Gizmos.DrawLine(mid, mid - dir * 0.2f + left * 0.12f);
            Gizmos.DrawLine(mid, mid - dir * 0.2f - left * 0.12f);
        }
#endif
    }
}
