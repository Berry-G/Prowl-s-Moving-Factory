using UnityEngine;

namespace PMF.Pathing
{
    /// <summary>누가 이 엣지를 통행할 수 있는가. 비트 플래그.</summary>
    [System.Flags]
    public enum PathAgent
    {
        None     = 0,
        Escortee = 1 << 0,
        Enemy    = 1 << 1,
        Ally     = 1 << 2,
        All      = Escortee | Enemy | Ally,
    }
}
