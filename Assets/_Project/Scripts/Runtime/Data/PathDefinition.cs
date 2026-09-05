using UnityEngine;

namespace PMF.Data
{
    /// <summary>
    /// 경로 데이터: 노드 + 엣지 배열. PathGraph 가 이 데이터로 PathNode/PathEdge 를 만든다.
    /// 설계 정본: 에디터 레포 docs/SDD-05-저작파이프라인.md (SDD-05 §4)
    /// </summary>
    [CreateAssetMenu(menuName = "PMF/Path Definition")]
    public sealed class PathDefinition : ScriptableObject
    {
        [SerializeField] private NodeDef[] _nodes;
        [SerializeField] private EdgeDef[] _edges;

        public NodeDef[] Nodes => _nodes;
        public EdgeDef[] Edges => _edges;

        [System.Serializable]
        public struct NodeDef
        {
            public string Id;
            public string Role; // start | exit | branch | waypoint
            public int X;
            public int Y;
        }

        [System.Serializable]
        public struct EdgeDef
        {
            public string From;
            public string To;
            public string Allowed;  // "All" | "Escortee" | "Escortee+Enemy+Ally" etc.
            public bool Bidirectional;
            public bool Shortcut;
        }
    }
}