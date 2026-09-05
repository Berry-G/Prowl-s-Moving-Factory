namespace PMF.Grid
{
    /// <summary>셀 타입. Tilemap 레이어 스캔 결과로 채워진다.
    ///
    /// <b>장애물은 2종이다</b> (G-22). <b>이동은 둘 다 똑같이 막지만 사거리 판정이 다르다</b>:
    /// <list type="bullet">
    /// <item><see cref="Blocked"/> — 벽/바위. 시야를 끊는다. 근접·원거리 <b>모두</b> 넘어서 때릴 수 없다.</item>
    /// <item><see cref="Water"/> — 물/호수. 시야는 뚫려 있다. 걸어 들어갈 수 없으니
    ///       <b>근접만</b> 못 때리고, 원거리는 그대로 쏜다.</item>
    /// </list>
    /// 값을 바꾸지 마라 — 직렬화된 Tilemap 스캔 결과와 통계 배열 인덱스가 이 숫자에 묶여 있다.</summary>
    public enum CellType
    {
        Blocked     = 0,
        Ground      = 1,
        Road        = 2,
        Buildable   = 3,
        VillageSlot = 4,
        Water       = 5,
    }
}
