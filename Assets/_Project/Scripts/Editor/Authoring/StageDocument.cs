namespace PMF.EditorTools.Authoring
{
    /**
 * 목적: .toon ↔ C# 사이의 순수 DTO. SDD-02 §1 의 C# 판. Unity 타입 의존 0.
 * 왜 이 구조인가: EditMode 테스트가 에셋 없이 돌 수 있고, TS StageDocument 와 1:1 대응한다.
 * 바꾸면 안 되는 것: 필드명·타입. TS 쪽과 맞지 않으면 ToonWriter/ToonReader 가 깨진다.
 * 근거: SDD-05 §4 [D-05-04], SDD-02 §1 [D-02-01]
 */
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