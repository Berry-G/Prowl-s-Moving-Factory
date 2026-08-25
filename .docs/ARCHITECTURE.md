# ARCHITECTURE — 시스템 구조와 확정 API

> 버전 0.2 · 2026-08-25
> **이 문서의 시그니처는 계약이다.** 태스크를 구현할 때 여기 적힌 이름·인자·반환형을 그대로 써라.
> 더 나은 이름이 떠올라도 바꾸지 마라. 바꾸려면 이 문서를 먼저 고치고 사용자에게 알려라.
> 시그니처에 없는 public 멤버를 추가하는 것은 허용된다. 있는 것을 바꾸는 것은 허용되지 않는다.

---

## 1. 계층 구조

```
┌───────────────────────────────────────────────┐
│ UI          HirePanel, ShortcutButton, HUD    │  uGUI. 로직을 갖지 않는다.
├───────────────────────────────────────────────┤
│ Session     GameSession, GameClock, Wallet    │  판의 상태, 승패, 자원, 시간
├───────────────────────────────────────────────┤
│ Actors      Escortee, MotherSpawner, Enemy,   │  MonoBehaviour. 상태머신을 갖는다.
│             AllyUnit, Village                 │
├───────────────────────────────────────────────┤
│ Services    GridSystem, PathGraph,            │  씬 단일. 질문에 답만 한다.
│             TargetRegistry, PathFollower      │
├───────────────────────────────────────────────┤
│ Data        *Definition (ScriptableObject)    │  읽기 전용. 런타임에 수정 금지.
└───────────────────────────────────────────────┘
```

**의존 방향은 위에서 아래로만.** Services 가 Actors 를 알면 안 된다. Data 는 아무것도 모른다.

### 씬 단일 서비스 (이 4개만 싱글턴 허용)

`GridSystem`, `PathGraph`, `GameSession`, `GameClock`

싱글턴 패턴은 아래 형태로 **통일**한다. 다른 형태를 쓰지 마라.

```csharp
public sealed class GridSystem : MonoBehaviour
{
    public static GridSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError($"[{nameof(GridSystem)}] 씬에 두 개 이상 존재합니다.", this);
            enabled = false;
            return;
        }
        Instance = this;
        // ... 초기화
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
```

> **왜 `OnDestroy` 에서 null 로 되돌리는가:** Reload Domain 을 껐기 때문에 `static Instance` 가 플레이 종료 후에도 파괴된 오브젝트를 가리킨 채 남는다. 그 상태로 다음 플레이를 시작하면 `Instance` 가 null 이 아닌데 죽어 있는(`MissingReferenceException`) 최악의 버그가 난다.

---

## 2. 네임스페이스

| 네임스페이스 | 내용 |
|---|---|
| `PMF.Grid` | 격자, 좌표 변환, 셀 타입 |
| `PMF.Pathing` | 경로 그래프, 탐색, 경로 추종 |
| `PMF.Actors` | Escortee, MotherSpawner, Enemy, AllyUnit, Village |
| `PMF.Combat` | 데미지, 타겟 탐색, 사격 |
| `PMF.Session` | GameSession, GameClock, Wallet, 승패 |
| `PMF.Data` | ScriptableObject Definition |
| `PMF.UI` | uGUI 뷰 |
| `PMF.Diagnostics` | 디버그 오버레이, 기즈모, 치트 |
| `PMF.EditorTools` | 에디터 전용 (PMF.Editor.asmdef) |

---

## 3. 확정 API

### 3.1 `PMF.Grid`

```csharp
namespace PMF.Grid
{
    public enum CellType
    {
        Blocked     = 0,
        Ground      = 1,
        Road        = 2,
        Buildable   = 3,
        VillageSlot = 4,
    }

    /// 격자 좌표. Vector2Int 를 직접 쓰지 말고 이 타입을 써라 (월드 좌표와 혼동 방지).
    [System.Serializable]
    public readonly struct GridCoord : System.IEquatable<GridCoord>
    {
        public readonly int X;
        public readonly int Y;

        public GridCoord(int x, int y);

        public static GridCoord operator +(GridCoord a, GridCoord b);
        public static GridCoord operator -(GridCoord a, GridCoord b);
        public static bool operator ==(GridCoord a, GridCoord b);
        public static bool operator !=(GridCoord a, GridCoord b);

        public bool Equals(GridCoord other);
        public override bool Equals(object obj);
        public override int GetHashCode();
        public override string ToString();   // "(3, 7)"

        /// 체비셰프 거리 아님 — 맨해튼 거리
        public static int ManhattanDistance(GridCoord a, GridCoord b);
    }

    /// 씬 단일. 격자 ↔ 월드 변환의 유일한 창구.
    /// 다른 어떤 클래스도 좌표 변환 수식을 직접 갖지 마라.
    public sealed class GridSystem : MonoBehaviour
    {
        public static GridSystem Instance { get; private set; }

        public int Width  { get; }
        public int Height { get; }

        /// 셀의 중심 월드 좌표. (모서리가 아니다)
        public Vector3 CellToWorld(GridCoord coord);

        /// 월드 좌표가 속한 셀. 범위 밖이어도 좌표는 반환하므로 InBounds 로 확인하라.
        public GridCoord WorldToCell(Vector3 world);

        public bool InBounds(GridCoord coord);

        /// 범위 밖이면 CellType.Blocked 를 반환한다 (예외를 던지지 않는다).
        public CellType GetCell(GridCoord coord);

        public bool IsWalkable(GridCoord coord);   // Blocked 가 아니면 true
        public bool IsBuildable(GridCoord coord);  // CellType.Buildable 일 때만 true
    }
}
```

> **함정:** `WorldToCell` 을 `Mathf.RoundToInt` 로 구현하면 음수 영역에서 틀린다. 셀 중심이 정수 좌표면 `RoundToInt`, 셀 좌하단이 정수 좌표면 `FloorToInt` 다. **어느 쪽을 택했는지 주석에 반드시 명시하고, `CellToWorld(WorldToCell(p)) == 그 셀의 중심` 이 성립하는지 EditMode 테스트로 확인하라.**

---

### 3.2 `PMF.Pathing`

```csharp
namespace PMF.Pathing
{
    /// 누가 이 엣지를 통행할 수 있는가. 비트 플래그.
    [System.Flags]
    public enum PathAgent
    {
        None     = 0,
        Escortee = 1 << 0,
        Enemy    = 1 << 1,
        Ally     = 1 << 2,
        All      = Escortee | Enemy | Ally,
    }

    public sealed class PathNode
    {
        public int Id { get; }
        public GridCoord Coord { get; }
        public Vector3 WorldPosition { get; }        // GridSystem.CellToWorld 결과를 캐시
        public IReadOnlyList<PathEdge> Edges { get; }
    }

    public sealed class PathEdge
    {
        public PathNode From { get; }
        public PathNode To   { get; }
        public float Cost    { get; }                // 기본값 = 월드 거리
        public PathAgent Allowed { get; }
        public bool IsShortcut { get; }
        public bool IsOpen   { get; }                // 지름길이 아니면 항상 true

        public bool CanTraverse(PathAgent agent);    // (Allowed & agent) != 0 && IsOpen
    }

    /// 씬 단일. 그래프 보유 + 탐색.
    public sealed class PathGraph : MonoBehaviour
    {
        public static PathGraph Instance { get; private set; }

        public IReadOnlyList<PathNode> Nodes { get; }

        /// 지름길을 연다. 이미 열려 있으면 false.
        public bool OpenShortcut(int edgeId);

        /// agent 가 통행 가능한 엣지만 써서 최단 경로를 찾는다.
        /// 찾으면 result 를 채우고 true. 못 찾으면 result 를 Clear 하고 false.
        /// result 는 호출자가 소유한 리스트다 (할당 방지).
        /// result[0] 은 from 자신, 마지막은 to.
        public bool TryFindRoute(PathNode from, PathNode to, PathAgent agent, List<PathNode> result);

        /// world 에서 가장 가까운 노드. agent 가 통행 가능한 엣지를 하나라도 가진 노드만 후보.
        /// 노드가 하나도 없으면 null.
        public PathNode FindNearestNode(Vector3 world, PathAgent agent);

        public PathNode GetNode(int id);

        /// 지름길이 열려서 경로가 바뀌어야 할 때 발행된다.
        /// 구독자는 OnDisable 에서 반드시 해제하라.
        public event System.Action OnGraphChanged;
    }

    /// MonoBehaviour 가 아니다. 경로를 따라 걷는 동작만 담당하는 순수 클래스.
    /// Escortee, Enemy, AllyUnit 이 각자 하나씩 필드로 들고 쓴다. 코드 중복을 만들지 마라.
    public sealed class PathFollower
    {
        public bool HasRoute { get; }
        public bool IsFinished { get; }
        public Vector3 Position { get; }
        public PathNode CurrentNode { get; }         // 마지막으로 통과한 노드
        public PathNode NextNode { get; }

        public void SetRoute(IReadOnlyList<PathNode> route, Vector3 startPosition);
        public void Clear();

        /// distance 만큼 전진시킨다. 경로 끝에 도달하면 IsFinished 가 true 가 된다.
        /// 노드를 하나 이상 통과했으면 true 를 반환한다 (경로 재계산 트리거용).
        public bool Advance(float distance);
    }
}
```

> **함정 1:** `TryFindRoute` 를 매 프레임 부르지 마라. 노드 통과 시점(`Advance` 가 true 반환) 과 `OnGraphChanged` 때만 부른다.
> **함정 2:** `List<PathNode> result` 를 매 호출마다 `new` 하지 마라. 호출하는 컴포넌트가 필드로 하나 들고 재사용한다.
> **함정 3:** 잡몹의 목적지는 **보호대상이 있는 노드**다. 보호대상이 계속 움직이므로 목적지가 바뀐다. 그래서 재계산 트리거에 "보호대상이 노드를 통과했을 때"도 포함된다 — 이건 `GameSession` 이 이벤트로 알린다. 각 잡몹이 매 프레임 보호대상 위치를 폴링하게 만들지 마라.

---

### 3.3 `PMF.Combat`

```csharp
namespace PMF.Combat
{
    public enum Team { Ally, Enemy, Escortee }

    public interface IDamageable
    {
        Team Team { get; }
        Vector3 Position { get; }
        bool IsAlive { get; }
        void TakeDamage(float amount, object source);
    }

    /// 물리 엔진 대신 쓰는 대상 등록소. static 이므로 리셋 주의(아래 참고).
    public static class TargetRegistry
    {
        public static void Register(IDamageable target);
        public static void Unregister(IDamageable target);

        /// origin 기준 range 안에서 team 에 속한 살아있는 대상 중 가장 가까운 것. 없으면 null.
        public static IDamageable FindNearest(Vector3 origin, float range, Team team);

        /// origin 기준 range 안, team 소속, anchor 에 가장 가까운 대상. 없으면 null.
        /// 아군 유닛의 기본 타겟팅에 쓴다 (anchor = 보호대상 위치).
        public static IDamageable FindNearestTo(Vector3 origin, float range, Team team, Vector3 anchor);

        public static int CountAlive(Team team);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics();   // ← 반드시 구현할 것. 도메인 리로드 OFF 대응.
    }

    /// 공격 능력. AllyUnit 과 Enemy 가 공유한다.
    public sealed class Attacker : MonoBehaviour
    {
        public float Range { get; set; }
        public float Damage { get; set; }
        public float Interval { get; set; }
        public Team TargetTeam { get; set; }
        public bool Enabled { get; set; }             // 행군 중에는 false

        public event System.Action<IDamageable> OnFired;
    }

    /// 체력. IDamageable 구현체.
    public sealed class Health : MonoBehaviour, IDamageable
    {
        public float Max { get; }
        public float Current { get; }
        public Team Team { get; }
        public Vector3 Position { get; }
        public bool IsAlive { get; }

        public void Initialize(float max, Team team);
        public void TakeDamage(float amount, object source);

        public event System.Action<Health> OnDied;
        public event System.Action<Health, float> OnDamaged;   // (self, amount)
    }
}
```

> **함정:** `Register`/`Unregister` 는 `OnEnable`/`OnDisable` 에 넣어라. `Start`/`OnDestroy` 에 넣으면 오브젝트 풀링을 붙이는 순간 깨진다.
> **함정:** `FindNearest` 는 거리 비교에 `Vector3.Distance` 대신 `sqrMagnitude` 를 쓰고 `range * range` 와 비교하라. 매 프레임 수십 번 호출된다.

---

### 3.4 `PMF.Session`

```csharp
namespace PMF.Session
{
    public enum GameResult { InProgress, Victory, Defeat }

    /// 시간 배속의 유일한 창구. Time.timeScale 을 다른 곳에서 건드리지 마라.
    public sealed class GameClock : MonoBehaviour
    {
        public static GameClock Instance { get; private set; }

        public bool IsPaused { get; }
        public float Speed { get; }                  // 1, 2, 4
        public void SetSpeed(float speed);
        public void TogglePause();
        public void Pause();
        public void Resume();
    }

    public sealed class Wallet : MonoBehaviour
    {
        public int Amount { get; }
        public bool CanAfford(int cost);
        public bool TrySpend(int cost);              // 부족하면 false, 차감하지 않음
        public void Add(int amount);
        public event System.Action<int> OnChanged;   // 변경 후 잔액
    }

    /// 판 전체의 상태. 승패 판정과 전역 이벤트 허브.
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        public GameResult Result { get; }
        public float ElapsedTime { get; }
        public Wallet Wallet { get; }
        public Actors.Escortee Escortee { get; }

        public void DeclareVictory();
        public void DeclareDefeat();
        public void RestartStage();

        /// 보호대상이 경로 노드를 통과했을 때. 잡몹 경로 재계산 트리거.
        public event System.Action<Pathing.PathNode> OnEscorteeReachedNode;
        public event System.Action<GameResult> OnGameEnded;
    }
}
```

---

### 3.5 `PMF.Data` (ScriptableObject)

**모든 Definition 은 읽기 전용이다. 런타임에 필드를 수정하지 마라.**
가변 상태가 필요하면 액터 컴포넌트가 자기 필드로 복사해서 갖는다.

```csharp
namespace PMF.Data
{
    [CreateAssetMenu(menuName = "PMF/Unit Definition")]
    public sealed class UnitDefinition : ScriptableObject
    {
        public string DisplayName { get; }
        public int HireCost { get; }
        public float MarchSpeed { get; }
        public float MaxHealth { get; }
        public float AttackRange { get; }
        public float AttackDamage { get; }
        public float AttackInterval { get; }
        public GameObject Prefab { get; }
        public Color GreyboxColor { get; }
    }

    [CreateAssetMenu(menuName = "PMF/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        public string DisplayName { get; }
        public float MoveSpeed { get; }
        public float MaxHealth { get; }
        public float AttackRange { get; }
        public float AttackDamage { get; }
        public float AttackInterval { get; }
        public GameObject Prefab { get; }
    }

    [CreateAssetMenu(menuName = "PMF/Stage Definition")]
    public sealed class StageDefinition : ScriptableObject
    {
        public float EscorteeSpeed { get; }
        public float EscorteeMaxHealth { get; }

        public float MotherSpeed { get; }
        public float MotherSpawnDelay { get; }       // 스테이지 시작 후 모체 등장까지
        public float MotherSpawnInterval { get; }
        public EnemyDefinition MotherSpawnEnemy { get; }
        public bool MotherFollowsPath { get; }       // D-06 토글

        public int StartingResource { get; }
        public float ResourcePerSecond { get; }
        public int ShortcutCost { get; }

        public bool AlliesCanDieWhileMarching { get; }   // D-03 토글
        public bool EnemiesTargetAllies { get; }         // D-04 토글
    }
}
```

> **함정:** `[CreateAssetMenu]` 를 붙였으면 실제로 `Assets/_Project/Data/` 아래에 `.asset` 파일을 생성하는 것까지가 태스크다. 클래스만 만들고 끝내지 마라.
> **함정:** SO 필드는 `[SerializeField] private float _moveSpeed;` + `public float MoveSpeed => _moveSpeed;` 형태로 만든다. public 필드로 노출하지 마라 (실수로 런타임 수정하는 경로가 생긴다).

---

## 4. 실행 순서 (Script Execution Order)

기본 순서로 두면 `Awake` 순서가 비결정적이어서 간헐적 null 참조가 난다.
**Project Settings → Script Execution Order** 에 아래를 명시적으로 등록하라.

| 순서 | 클래스 |
|---:|---|
| -200 | `GridSystem` |
| -190 | `PathGraph` |
| -180 | `GameClock` |
| -170 | `GameSession` |
| 기본 | 나머지 전부 |

액터들은 `Awake` 가 아니라 **`Start`** 에서 서비스를 참조하라. 이 두 겹의 안전장치를 다 걸어라.

---

## 5. 프리팹 / 씬 구조

```
Stage_Greybox.unity
├── --- Services ---            (빈 GameObject, 구분용)
│   ├── GridSystem              (+ Grid + Tilemap 자식)
│   ├── PathGraph
│   ├── GameClock
│   └── GameSession             (+ Wallet)
├── --- Map ---
│   ├── Grid                    (Unity Grid 컴포넌트)
│   │   └── Tilemap_Ground      (+ TilemapRenderer)
│   ├── Villages/
│   └── ExitPoint
├── --- Actors ---              (런타임 생성물의 부모)
│   ├── Escortee
│   ├── Mother
│   ├── Enemies/                (스폰된 잡몹의 부모)
│   └── Allies/
├── --- UI ---
│   └── Canvas (Screen Space - Overlay)
└── Main Camera                 (Orthographic)
```

프리팹은 `Assets/_Project/Prefabs/` 아래:
`Escortee.prefab`, `Mother.prefab`, `Enemy_Basic.prefab`, `Ally_Basic.prefab`, `Village.prefab`

---

## 6. 렌더링 / 정렬 규약

- **PPU(Pixels Per Unit) = 100**, 1셀 = 1 월드유닛 = 100px. 그레이박스 스프라이트는 Unity 기본 `Square`/`Circle` 을 쓰고 스케일로 맞춘다.
- **Sorting Layer** 를 아래 순서로 만든다 (Project Settings → Tags and Layers):
  `Background` → `Ground` → `Path` → `Deploy` → `Actors` → `Projectile` → `FX` → `UI`
- `Actors` 레이어 안에서는 **Y가 작을수록 앞**에 그린다.
  → Project Settings → Graphics → **Transparency Sort Mode = `Custom Axis`, Sort Axis = (0, 1, 0)**
  → 이걸 안 해 두면 나중에 쿼터뷰로 갈 때 캐릭터 겹침 순서가 전부 틀어진다. **지금 설정해 둬라.**

---

## 7. 성능 지침 (프로토타입 수준)

과하게 최적화하지 마라. 아래 4개만 지키면 충분하다.

1. `Update` 안에서 `new` 하지 않기 (특히 `List`, `Vector3[]`, 문자열 연결)
2. `GetComponent` 는 `Awake`/`Start` 에서만
3. 잡몹은 **오브젝트 풀링**한다 (수백 마리가 생겼다 사라진다). 단 **태스크 P-14 에서 붙인다. 그 전에 미리 만들지 마라.**
4. 거리 비교는 `sqrMagnitude`

---

## 8. 테스트

- `PMF.Tests` (EditMode) 에 **순수 로직만** 테스트한다: `GridCoord`, 좌표 변환 왕복, `PathGraph.TryFindRoute` 의 통행 권한 필터링.
- MonoBehaviour 통합 테스트(PlayMode)는 프로토타입 단계에서 만들지 마라. 유지비가 이득보다 크다.
