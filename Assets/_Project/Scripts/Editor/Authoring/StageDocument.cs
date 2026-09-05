namespace PMF.EditorTools.Authoring
{
    /// <summary>
    /// TOON 형식으로 직렬화할 수 있는 스테이지 데이터의 C# 구조체.
    /// 설계 정본: 에디터 레포 docs/SDD-05-저작파이프라인.md
    /// </summary>
    public struct StageDocument
    {
        public string Schema;
        public string Name;

        public MapDef Map;
        public PathDef Path;
        public SpawnDef Spawn;
        public BurstDef Burst;
        public EconomyDef Economy;
        public EscorteeDef Escortee;
        public MotherDef Mother;
        public PresentationDef Presentation;
        public ToggleDef Toggles;

        public struct MapDef { public int Width; public int Height; public float OriginX, OriginY; public CellType[] Cells; }
        public struct PathDef { public PathNodeDef[] Nodes; public PathEdgeDef[] Edges; }
        public struct PathNodeDef { public string Id; public string Role; public int X, Y; }
        public struct PathEdgeDef { public string From; public string To; public bool Bidirectional; public bool Shortcut; public string Allowed; }
        public struct SpawnDef { public int VolleyCount; public float VolleySpacing, RestSeconds, TelegraphSeconds; public SpawnTableRow[] Table; public HealthPoint[] HealthByProgress; }
        public struct SpawnTableRow { public string Enemy; public int Weight; }
        public struct HealthPoint { public float T; public float Mul; }
        public struct BurstDef { public string[] TriggerNodeIds; public float Duration; public int VolleyCount; public float RestSeconds, RecoverySpeedMultiplier, RecoverySeconds; }
        public struct EconomyDef { public int StartingResource, ShortcutCost; public DifficultyRow[] Difficulties; }
        public struct DifficultyRow { public string Difficulty; public string DisplayName; public float KillReward, ResourcePerSecond; }
        public struct EscorteeDef { public float Speed, MaxHealth; }
        public struct MotherDef { public float Speed, SpawnDelay; public bool FollowsPath; }
        public struct PresentationDef { public float UiSlowMotionScale, ShotLineSeconds, MagicMissileSpeed, HitFlashSeconds, DebrisSeconds; public int DebrisCount; public bool HealthBarHideWhenFull; public float MasterVolume; }
        public struct ToggleDef { public bool AlliesCanDieWhileMarching, EnemiesTargetAllies; }
    }

    public enum CellType { Empty = 0, Ground = 1, Road = 2, Buildable = 3, Village = 4, Blocked = 5, Water = 6 }
}