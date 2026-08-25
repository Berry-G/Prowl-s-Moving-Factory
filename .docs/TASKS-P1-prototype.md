# TASKS — 1차 그레이박스 프로토타입

> 버전 0.2 · 2026-08-25 · Unity **6000.5.7f1**
> **목표: 맵 + 디펜스 게임의 틀.** 아트 없음, 사운드 없음, 종족/병종 없음, 메타 진행 없음.
> 완료 기준은 "`.docs/GDD.md` §14 통과 조건 4개를 눈으로 판정할 수 있는 상태".

---

## 이 문서를 쓰는 법 (작업자용)

1. **태스크는 번호 순서대로 한 번에 하나씩** 한다. 건너뛰지 마라. 앞 태스크의 산출물이 뒤 태스크의 전제다.
2. 태스크를 시작하기 전에 반드시 읽어라: `CLAUDE.md` → `.docs/ARCHITECTURE.md` 의 해당 절.
3. **DoD(수용 조건)를 전부 만족해야 완료**다. 하나라도 미달이면 "부분 완료"라고 정직하게 보고하라.
4. **⚠️ 표시는 실제로 자주 나는 실수다.** 읽고 넘어가지 말고 확인하라.
5. **🚫 표시는 임의 판단 금지 항목이다.** "이게 더 나을 것 같아서" 바꾸지 마라. 바꾸고 싶으면 물어라.
6. 각 태스크 끝에 `[ ]` 를 `[x]` 로 바꿔 진행 상황을 남겨라.

### 진행 현황

| 단계 | 태스크 | 상태 |
|---|---|---|
| 0. 환경 | P-00 ~ P-02, **P-02B(MCP)** | ☐ |
| 1. 격자와 맵 | P-03 ~ P-04 | ☐ |
| 2. 경로 | P-05 ~ P-07 | ☐ |
| 3. 판의 뼈대 | P-08 ~ P-09 | ☐ |
| 4. 액터 | P-10 ~ P-12 | ☐ |
| 5. 플레이어 조작 | P-13 ~ P-16 | ☐ |
| 6. 마감 | P-17 ~ P-21 | ☐ |

---
---

# 단계 0 — 환경 세팅

---

## `[ ]` P-00. 버전 관리 초기화

**목표:** 지금부터의 모든 변경을 되돌릴 수 있게 만든다.

**선행:** 없음. **이것이 첫 번째 태스크다. 코드를 한 줄도 쓰기 전에 한다.**

**작업:**
1. 프로젝트 루트에서 `git init`
2. Unity 공식 `.gitignore` 를 루트에 생성 (아래 내용 사용)
3. `.gitattributes` 생성 — Git LFS 대상 지정
4. `git lfs install` 후 `git add . && git commit -m "chore: initial Unity 6000.5.7f1 project"`

**`.gitignore` (최소 필수):**
```
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/
[Mm]emoryCaptures/
[Rr]ecordings/
.vs/
.vscode/
.idea/
*.csproj
*.unityproj
*.sln
*.suo
*.user
*.booproj
*.pidb
*.svd
*.pdb
*.mdb
*.apk
*.aab
*.unitypackage
*.app
sysinfo.txt
crashlytics-build.properties
/[Aa]ssets/[Ss]treamingAssets/aa.meta
/[Aa]ssets/[Ss]treamingAssets/aa/*
```

**`.gitattributes`:**
```
* text=auto

*.cs diff=csharp text
*.unity  -text merge=unityyamlmerge diff
*.prefab -text merge=unityyamlmerge diff
*.asset  -text merge=unityyamlmerge diff

*.png  filter=lfs diff=lfs merge=lfs -text
*.jpg  filter=lfs diff=lfs merge=lfs -text
*.psd  filter=lfs diff=lfs merge=lfs -text
*.aseprite filter=lfs diff=lfs merge=lfs -text
*.wav  filter=lfs diff=lfs merge=lfs -text
*.ogg  filter=lfs diff=lfs merge=lfs -text
*.mp3  filter=lfs diff=lfs merge=lfs -text
*.fbx  filter=lfs diff=lfs merge=lfs -text
```

**DoD:**
- `git status` 가 깨끗하다
- `Library/`, `Temp/`, `*.csproj`, `*.sln` 이 추적되지 않는다
- 첫 커밋에 `Assets/`, `Packages/`, `ProjectSettings/` 가 포함되어 있다

**⚠️ 함정**
- `Library/` 를 실수로 커밋하면 레포가 수 GB가 된다. 커밋 전에 `git status` 로 파일 수를 확인하라. 정상이면 수십~수백 개다.
- **`ProjectSettings/` 는 반드시 커밋한다.** 이걸 무시하면 협업자가 프로젝트 설정을 전부 잃는다.
- Git LFS 는 **파일을 추가하기 전에** 설정해야 한다. 나중에 켜면 이미 들어간 파일은 LFS로 안 간다.

**🚫 임의 판단 금지**
- GitHub 원격 저장소를 임의로 만들거나 push 하지 마라. 사용자가 지시할 때만 한다.

---

## `[ ]` P-01. 프로젝트 설정 정리

**목표:** 이후 모든 태스크가 딛고 설 설정을 못 박는다. **여기서 빠뜨린 설정은 나중에 원인 불명 버그로 돌아온다.**

**선행:** P-00

**작업 (Project Settings 순서대로):**

### 1) Editor
- **Enter Play Mode Options** → ✅ 체크
- **Reload Domain** → ☐ **해제** (빠른 재생)
- **Reload Scene** → ✅ 유지 (해제하면 씬 상태가 남아 디버깅이 어려워진다)

### 2) Player → Other Settings
- **Active Input Handling** → **`Input System Package (New)`**
  - `Both` 로 두지 마라. 구 `Input` API 가 컴파일돼서 실수로 섞여 들어간다.
- **Scripting Backend** → `IL2CPP` (Windows)
- **Api Compatibility Level** → `.NET Standard 2.1`
- Company Name / Product Name 설정

### 3) Tags and Layers → Sorting Layers
아래 순서로 **정확히** 추가한다 (`Default` 는 남겨 둔다):
```
Background, Ground, Path, Deploy, Actors, Projectile, FX
```

### 4) Graphics / URP Renderer — Transparency Sort
- ⚠️ **URP를 쓰면 Project Settings → Graphics 의 Transparency Sort 설정이 무시된다.**
  `Assets/Settings/Renderer2D.asset` 을 선택하고 인스펙터에서:
  - **Transparency Sort Mode** → `Custom Axis`
  - **Transparency Sort Axis** → `(0, 1, 0)`
- 이유: Y가 작을수록 앞에 그려져야 한다. 지금 안 해 두면 쿼터뷰 전환 시 전부 깨진다.

### 5) Time
- **Fixed Timestep** → 기본값(0.02) 유지. 이 프로젝트는 물리를 안 쓰므로 건드릴 이유가 없다.

### 6) Package Manager — 미사용 패키지 제거
**Package Manager UI 에서 제거하라. `Packages/manifest.json` 을 손으로 편집하지 마라.**

> ### ⚠️ 정정 (2026-08-26)
> 이 목록에는 원래 `com.unity.ai.assistant` 가 있었으나 **뺐습니다.**
> 그 패키지가 **Unity 공식 MCP 서버**를 담고 있어서, 지우면 Claude가 에디터에 붙을 수 없습니다.
> 이미 지웠다면 **P-02B** 에서 다시 넣습니다.

제거 대상:
```
com.unity.ai.inference
com.unity.multiplayer.center
com.unity.visualscripting
com.unity.collab-proxy
com.unity.modules.ai            ← NavMesh. 이 프로젝트는 안 쓴다
com.unity.modules.xr
com.unity.modules.vehicles
com.unity.modules.cloth
com.unity.modules.terrain
com.unity.modules.terrainphysics
com.unity.modules.wind
com.unity.modules.umbra
com.unity.modules.androidjni
com.unity.modules.adaptiveperformance
com.unity.modules.unityanalytics
```

**유지 대상 (지우지 마라):** `com.unity.ai.assistant`(**공식 MCP 서버**), `com.unity.2d.*` 전부, `com.unity.inputsystem`, `com.unity.ugui`, `com.unity.test-framework`, `com.unity.render-pipelines.universal`, `com.unity.ide.*`, `com.unity.modules.physics2d`, `com.unity.modules.physicscore2d`

**DoD:**
- 위 6개 항목이 전부 적용되어 있다
- **패키지 제거 후 콘솔에 에러가 0개다** (에러가 나면 그 패키지를 되돌린다)
- `ProjectSettings/` 변경분을 커밋했다

**⚠️ 함정**
- 패키지를 여러 개 한꺼번에 지우고 나서 에러가 나면 원인을 못 찾는다. **2~3개씩 지우고 매번 콘솔을 확인하라.**
- `com.unity.modules.physics2d` 를 "물리 안 쓰니까"라며 지우지 마라. 2D 패키지들이 의존한다.
- Sorting Layer 는 **이름 철자와 순서가 코드/프리팹과 계약**이다. 오타 나면 조용히 `Default` 로 떨어진다.

---

## `[ ]` P-02. 폴더 구조와 어셈블리 정의

**목표:** 파일이 어디로 가야 하는지 고민할 필요가 없게 만든다.

**선행:** P-01

**작업:**
1. 아래 폴더를 생성한다 (Unity 에디터의 Project 창에서 만들어라 — `.meta` 가 같이 생겨야 한다):
```
Assets/_Project/
  Scripts/Runtime/
    Grid/  Pathing/  Actors/  Combat/  Session/  Data/  UI/  Diagnostics/
  Scripts/Editor/
  Scripts/Tests/
  Data/
    Units/  Enemies/  Stages/
  Prefabs/
  Scenes/
  Art/            ← 비워 둔다
```
2. Assembly Definition 3개 생성:

| 파일 | 이름 | 설정 |
|---|---|---|
| `Scripts/Runtime/PMF.Runtime.asmdef` | `PMF.Runtime` | Assembly References: `Unity.InputSystem` |
| `Scripts/Editor/PMF.Editor.asmdef` | `PMF.Editor` | Platforms: **Editor 만**, References: `PMF.Runtime` |
| `Scripts/Tests/PMF.Tests.asmdef` | `PMF.Tests` | Platforms: **Editor 만**, References: `PMF.Runtime`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, Define Constraints: `UNITY_INCLUDE_TESTS` |

3. `Assets/_Project/Scenes/Stage_Greybox.unity` 씬 생성 (빈 2D 씬). `Assets/Scenes/SampleScene.unity` 는 **지우지 말고 그냥 둔다.**
4. Build Profiles → Scene List 에 `Stage_Greybox` 를 인덱스 0으로 등록.

**DoD:**
- 폴더 구조가 위와 일치한다
- asmdef 3개가 있고 콘솔 에러가 0개다
- `Stage_Greybox.unity` 가 빌드 씬 목록 0번이다

**⚠️ 함정**
- asmdef 를 만들면 **그 폴더 아래 스크립트가 다른 어셈블리(Assembly-CSharp)에서 안 보인다.** 참조를 빠뜨리면 "타입을 찾을 수 없음" 에러가 난다. 원인은 항상 asmdef 참조 누락이다.
- `PMF.Tests.asmdef` 에 Define Constraint `UNITY_INCLUDE_TESTS` 를 안 넣으면 **빌드에 테스트 코드가 포함된다.**
- 탐색기에서 폴더를 만들면 Unity 가 인식할 때까지 `.meta` 가 없어 git 에 이상하게 들어간다. **에디터에서 만들어라.**

**🚫 임의 판단 금지**
- 어셈블리를 더 잘게 쪼개지 마라. 프로토타입에는 3개면 충분하다.
- `Assets/` 최상위에 새 폴더를 만들지 마라.

---

## `[ ]` P-02B. Unity MCP 연결 — Claude가 에디터를 직접 보게 하기

**목표:** Claude가 **컴파일 에러와 콘솔 로그를 스스로 읽게** 만든다.
이것이 없으면 Claude는 코드를 쓰고도 그게 컴파일되는지 알 수 없다. `CLAUDE.md` §5 의
"컴파일 검증이 불가능하면 그 사실을 명시하라"는 규칙이 바로 이 상태를 전제한 것이다.

**선행:** P-02
**성격:** 선택이지만 **강력 권장.** P-03 부터 코드가 시작되므로 그 전에 붙이는 게 이득이 가장 크다.

### 왜 이걸 먼저 하는가

| MCP 없이 | MCP 연결 후 |
|---|---|
| 코드를 쓰고 "에디터에서 확인해 주세요" 로 끝남 | Claude가 직접 컴파일 결과를 확인 |
| 에러가 나면 사용자가 복사해서 붙여넣어야 함 | Claude가 콘솔에서 바로 읽음 |
| 씬/프리팹 연결을 사용자가 손으로 | Claude가 일부 자동화 가능 |
| 왕복 1회 = 사용자 개입 1회 | 왕복이 Claude 안에서 닫힘 |

### 선택지 두 가지

**A. Unity 공식 MCP (권장, 먼저 시도)**

- 패키지: `com.unity.ai.assistant` (Unity 6000.0+ 지원 → 6.5 OK)
- 추가 런타임 **불필요** (Python·Node 안 씀)
- 1st party 라 6.6/6.7 업그레이드에 같이 따라옴
- 단점: 프리릴리스(`-pre`) 라 UI/동작이 바뀔 수 있음

**B. MCP for Unity (CoplayDev) — A가 안 되면**

- 커뮤니티 표준. 도구 수가 더 많음
- Python 3.10+ 와 `uv` 필요 (이 PC에는 이미 있음)
- Unity 2021.3 ~ 6.x 지원

### 작업 — A안 (공식)

1. **패키지 설치**
   Window → Package Manager → `+` → *Add package by name...* → `com.unity.ai.assistant`
   - 목록에 안 보이면 Package Manager 우상단 톱니 → **Enable Pre-release Packages** 체크
2. **브리지 확인**
   Edit → Project Settings → **AI → Unity MCP Server**
   - **Unity Bridge** 가 **Running**(초록)인지 확인. 멈춰 있으면 **Start**
3. **클라이언트 자동 구성**
   같은 화면 **Integrations** 섹션 → **Claude Code** 선택 → 구성 적용
4. **연결 승인**
   Claude Code 를 이 프로젝트 폴더에서 실행 → Unity 의 **Pending Connections** 에 뜨면 **Accept**
   - 한 번 승인하면 이후 자동 재연결
5. **검증**
   ```
   claude mcp list        →  unity 계열 서버가 ✔ Connected
   ```
   Claude에게 "콘솔 에러 읽어줘" 를 시켜서 실제로 읽히는지 확인

### 작업 — B안 (CoplayDev)

1. Window → Package Manager → `+` → **Add package from git URL**
   ```
   https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main
   ```
2. 임포트되면 **설정 마법사**가 자동 실행 → Python/`uv` 초록 확인 → **Done**
3. 감지된 클라이언트 목록에서 **Claude Code** 체크 → **Configure Selected**
4. 상태 패널이 **Connected** 인지 확인

**DoD:**
- `claude mcp list` 에 Unity 서버가 **Connected** 로 뜬다
- Claude가 Unity 콘솔의 컴파일 에러를 **직접 읽어서** 보고할 수 있다
- 에디터를 껐다 켠 뒤에도 자동 재연결된다
- MCP 설정 파일이 git 에 커밋되었는지 확인 (`.mcp.json` 이 생겼다면)

**⚠️ 함정**
- **에디터가 떠 있어야만 MCP가 동작한다.** Unity를 닫으면 도구 호출이 전부 실패한다. Claude가 "MCP가 안 붙는다"고 하면 **가장 먼저 에디터가 켜져 있는지 확인**하라.
- Unity가 **컴파일 중이거나 Import 중이면** MCP 응답이 지연되거나 실패한다. 정상이다. 잠시 후 재시도.
- 두 방식(A, B)을 **동시에 붙이지 마라.** 도구 이름이 겹쳐 Claude가 어느 쪽을 부를지 혼란스러워진다. 하나를 고르고 다른 하나는 제거하라.
- 공식 패키지는 **AI Assistant 기능 전체**를 같이 들여온다. 에디터 안 채팅 UI는 쓰지 않아도 되지만, 패키지를 지우면 MCP도 같이 사라진다는 점만 기억하라.
- Claude Code 는 MCP 설정 변경 후 **재시작해야** 새 서버를 인식한다.

**🚫 임의 판단 금지**
- MCP가 붙었다고 해서 **씬을 마음대로 편집하지 마라.** 태스크에 명시된 것만 한다.
- MCP로 스크립트를 생성하지 마라. 파일 작성은 기존 도구로 하고, MCP는 **검증(컴파일·콘솔·플레이 상태)** 에 쓴다.

---
---

# 단계 1 — 격자와 맵

---

## `[ ]` P-03. GridCoord + GridSystem

**목표:** 격자 ↔ 월드 좌표 변환의 **유일한 창구**를 만든다. 이 게임에서 가장 중요한 파일이다.

**선행:** P-02
**참조:** `ARCHITECTURE.md` §3.1

**만들 파일:**
```
Scripts/Runtime/Grid/GridCoord.cs
Scripts/Runtime/Grid/CellType.cs
Scripts/Runtime/Grid/GridSystem.cs
Scripts/Tests/GridSystemTests.cs
```

**구현 지시:**
- `GridCoord` 는 `readonly struct`. `ARCHITECTURE.md` §3.1 의 시그니처를 **그대로** 구현한다.
- `GridSystem` 은 씬 단일. 인스펙터에 `_width`, `_height`, `_origin`(Vector3), `_cellSize`(기본 1.0) 를 노출.
- **셀 중심 규약을 확정한다:**
  > 셀 `(x, y)` 의 **중심** 월드 좌표 = `_origin + new Vector3((x + 0.5f) * _cellSize, (y + 0.5f) * _cellSize, 0f)`
  > 즉 `_origin` 은 셀 (0,0) 의 **좌하단 모서리**다.
- 따라서 `WorldToCell` 은 `Mathf.FloorToInt((world - _origin) / _cellSize)` 를 쓴다. **`RoundToInt` 가 아니다.**
- `_cells` 는 `CellType[]` 1차원 배열 (`index = y * Width + x`). 2차원 배열을 쓰지 마라 (직렬화가 안 된다).
- 셀 데이터 채우기는 P-04 에서 한다. 지금은 전부 `Ground` 로 초기화.
- `OnDrawGizmosSelected` 에서 격자 선을 그린다 (에디터 확인용).

**DoD:**
- 콘솔 에러 0개
- `Stage_Greybox` 씬에 `GridSystem` 오브젝트가 배치되어 있고 `_width=32, _height=18` 이다
- 씬 뷰에서 GridSystem 선택 시 격자가 보인다
- **EditMode 테스트가 통과한다:**
  - `CellToWorld` → `WorldToCell` 왕복이 모든 셀에서 원래 좌표를 돌려준다
  - `_origin` 이 음수일 때도(예: `(-16, -9, 0)`) 왕복이 성립한다
  - `InBounds` 가 경계값(-1, 0, Width-1, Width)에서 올바르다
  - `GetCell` 이 범위 밖에서 예외 없이 `Blocked` 를 반환한다

**⚠️ 함정**
- **음수 좌표에서 `(int)` 캐스팅은 0 방향으로 자른다.** `(int)(-0.5f)` 는 `0` 이지 `-1` 이 아니다. 반드시 `Mathf.FloorToInt` 를 써라. 맵 원점을 화면 중앙에 두는 순간 이 버그가 터진다.
- `GetHashCode` 를 대충 만들면 딕셔너리 성능이 무너진다. `(X * 397) ^ Y` 정도면 충분하다.
- `readonly struct` 에 `==` 연산자를 만들면 `Equals`/`GetHashCode` 도 **반드시 같이** 오버라이드해야 한다. 안 하면 컴파일 경고가 나고 동작이 미묘하게 틀린다.

**🚫 임의 판단 금지**
- `Vector2Int` 를 `GridCoord` 대신 쓰지 마라. 월드 좌표와 섞여도 컴파일이 통과해서 조용히 틀린다.
- Unity 의 `Grid` 컴포넌트의 `CellToWorld` 를 그대로 쓰지 마라. **셀 중심이 아니라 모서리를 반환한다.**

---

## `[ ]` P-04. 맵 저작 — Tilemap 레이어로 셀 타입 그리기

**목표:** 기획자/개발자가 **타일을 칠하듯** 맵을 그리고, 그것이 `CellType` 으로 읽히게 한다.

**선행:** P-03

**만들/고칠 파일:**
```
Scripts/Runtime/Grid/GridSystem.cs        (수정: Tilemap 스캔 추가)
Scripts/Runtime/Grid/CellPaletteBuilder.cs (선택: 그레이박스 타일 자동 생성)
Scripts/Editor/GridSystemEditor.cs         (인스펙터에 "타일맵에서 다시 읽기" 버튼)
```

**구현 지시:**
- 씬에 Unity `Grid` 오브젝트를 만들고 그 아래에 **Tilemap 4개**를 만든다:
  `Tilemap_Ground`, `Tilemap_Road`, `Tilemap_Buildable`, `Tilemap_Blocked`
- `GridSystem` 에 이 4개 Tilemap 참조를 `[SerializeField]` 로 받는다.
- `Awake` (그리고 에디터 버튼) 에서 4개를 스캔해 `_cells` 를 채운다.
- **덮어쓰기 우선순위 (반드시 이 순서):**
  `Ground` → `Road` → `Buildable` → `VillageSlot` → `Blocked`
  (뒤에 오는 것이 앞을 덮는다. Blocked 가 가장 강하다.)
- 아무 타일도 없는 칸은 `Blocked` 로 둔다 (맵 밖 = 갈 수 없음).
- 그레이박스 타일 에셋은 Unity 기본 흰 사각 스프라이트로 4종 만들고 색만 다르게 한다 (GDD §12 색상 규약).
- `Grid` 컴포넌트의 `Cell Size` 는 `(1, 1, 0)`, `GridSystem._cellSize` 도 `1` 로 **일치시킨다.**
- ⚠️ `GridSystem._origin` 과 `Grid` 오브젝트의 `transform.position` 도 **일치시켜야 한다.** 어긋나면 화면의 타일과 논리 격자가 반 칸씩 밀린다.

**테스트 맵 (P-21까지 이 배치를 유지하라):**
```
32 × 18. 경로가 맵을 좌우로 가른다.
- 좌측 (x=0~1) 에 보호대상 시작 지점
- 우측 (x=30~31) 에 탈출 지점
- Road 가 좌→우로 지그재그 (최소 2회 꺾임)
- Road 위/아래에 Buildable 칸을 비대칭으로 배치 (한쪽이 더 많게)
- 마을 3곳: 시작 근처 1개, 중앙 위 1개, 우측 아래 1개
- Blocked 덩어리 2~3개로 배치 선택을 강제
```

**DoD:**
- 씬에서 타일을 칠하고 "타일맵에서 다시 읽기" 버튼을 누르면 `GridSystem` 의 셀 데이터가 갱신된다
- 씬 뷰 기즈모에서 `Buildable` 칸이 하늘색으로, `Blocked` 가 어둡게 구분되어 보인다
- 위 테스트 맵이 실제로 그려져 있다
- 플레이 시작 시 콘솔에 셀 타입별 개수가 1줄 로그로 찍힌다 (예: `Ground=180 Road=64 Buildable=48 Blocked=284`)

**⚠️ 함정**
- `Tilemap.HasTile(Vector3Int)` 의 인자는 **셀 좌표**다. 월드 좌표를 넣으면 조용히 전부 false 가 된다.
- `Tilemap.cellBounds` 는 타일이 실제로 칠해진 범위다. `GridSystem` 의 `Width/Height` 와 다를 수 있다. **`GridSystem` 의 범위를 기준으로 순회하고 Tilemap 에 질의하라.** 반대로 하지 마라.
- Tilemap 4개의 `Tilemap Renderer` 의 Sorting Layer 를 각각 `Ground` / `Path` / `Deploy` / `Ground` 로 지정하라. 안 하면 겹쳐서 안 보인다.
- 도메인 리로드가 꺼져 있으므로 `_cells` 가 이전 플레이의 값을 유지할 수 있다. `Awake` 에서 **항상 새로 채워라.**

**🚫 임의 판단 금지**
- Tilemap 대신 코드로 맵을 하드코딩하지 마라. 기획자가 맵을 못 만지게 된다.
- 커스텀 `TileBase` 상속 클래스를 만들지 마라. Tilemap 레이어 분리로 충분하다.

---
---

# 단계 2 — 경로

---

## `[ ]` P-05. 경로 그래프 저작 도구

**목표:** 씬에서 노드를 배치하고 선으로 잇기만 하면 경로가 만들어지게 한다.

**선행:** P-04
**참조:** `ARCHITECTURE.md` §3.2, `GDD.md` §6

**만들 파일:**
```
Scripts/Runtime/Pathing/PathAgent.cs
Scripts/Runtime/Pathing/PathNodeAuthoring.cs
Scripts/Runtime/Pathing/PathNode.cs
Scripts/Runtime/Pathing/PathEdge.cs
Scripts/Runtime/Pathing/PathGraph.cs        (이번엔 그래프 구축까지만. 탐색은 P-06)
```

**구현 지시:**
- `PathNodeAuthoring` (MonoBehaviour, 씬에 배치):
  ```csharp
  [SerializeField] private List<Connection> _connections;
  [SerializeField] private bool _isExit;
  [SerializeField] private bool _isEscorteeStart;

  [System.Serializable]
  public struct Connection
  {
      public PathNodeAuthoring Target;
      public PathAgent Allowed;      // 기본값 PathAgent.All
      public bool IsShortcut;
      public bool Bidirectional;     // 기본 true
  }
  ```
- `PathGraph.Awake` 에서 자식(또는 씬 전체)의 `PathNodeAuthoring` 을 모아 런타임 `PathNode`/`PathEdge` 그래프를 만든다.
  - 노드 `Id` 는 **결정적으로** 부여하라 — 씬 계층 순서(`GetComponentsInChildren` 순서)를 쓴다. `GetInstanceID` 를 쓰지 마라(매 실행 달라진다).
  - `PathNode.Coord` 는 `GridSystem.Instance.WorldToCell(transform.position)` 로 스냅한다.
  - `PathNode.WorldPosition` 은 `GridSystem.Instance.CellToWorld(Coord)` — **즉 노드는 항상 셀 중심에 정렬된다.** 씬에서 대충 놓아도 스냅된다.
- `Bidirectional` 이면 반대 방향 엣지도 자동 생성한다 (같은 `Allowed`, 같은 `IsShortcut`).
- **기즈모** (`OnDrawGizmos`, 항상 표시):
  - 노드: 지름 0.3 구. 시작=흰색, 탈출=금색, 일반=회색
  - 일반 엣지: 회색 실선 + 방향 화살표
  - 지름길 엣지: 노란 점선
  - 일방통행 엣지: 화살표 하나만
- **검증 로그**를 `Awake` 에 넣어라:
  - 시작 노드가 정확히 1개인가
  - 탈출 노드가 1개 이상인가
  - 연결이 0개인 고립 노드가 있는가
  - `Blocked` 셀 위에 놓인 노드가 있는가
  → 위반이면 `Debug.LogError` 로 **어느 오브젝트인지** 함께 찍는다 (`Debug.LogError(msg, authoring)`)

**DoD:**
- 테스트 맵 위에 노드 12개 이상이 배치되고 연결되어 있다
- 지름길 엣지가 최소 1개 있다 (`Allowed = Escortee` 만, `IsShortcut = true`)
- 씬 뷰에서 경로가 선으로 보인다
- 플레이 시작 시 검증 로그가 통과하고 `PathGraph.Nodes.Count` 가 로그에 찍힌다

**⚠️ 함정**
- `OnDrawGizmos` 안에서 `PathGraph.Instance` 를 참조하지 마라. 플레이 중이 아닐 땐 null 이다. **저작 컴포넌트가 자기 데이터로 직접 그린다.**
- `List<Connection>` 안의 `Target` 이 null 인 항목을 반드시 걸러라. 씬에서 요소를 추가하면 처음엔 null 이다. null 참조 예외의 1순위 원인이다.
- 양방향 엣지를 만들 때 **A→B 와 B→A 를 각각 저작하면 중복 엣지가 생긴다.** `Bidirectional` 플래그로 한쪽만 저작하도록 하고, 중복이 감지되면 경고를 찍어라.
- `PathAgent` 는 `[System.Flags]` 열거형이다. 인스펙터에서 여러 개 선택하려면 **`[SerializeField]` 필드에 Unity 가 자동으로 다중선택 UI를 주지 않는다.** `[EnumFlags]` 계열 커스텀 PropertyDrawer 를 만들거나, `PathAgent.All` 을 기본값으로 두고 지름길만 `Escortee` 로 바꾸게 하라. **후자를 택하라 (더 단순하다).**

**🚫 임의 판단 금지**
- 경로를 ScriptableObject 나 JSON 으로 빼지 마라. 씬 저작이 훨씬 빠르고 시각적이다.
- 노드 자동 생성(Road 타일에서 그래프를 추론)을 만들지 마라. 분기와 지름길을 표현할 수 없다.

---

## `[ ]` P-06. 경로 탐색 + 통행 권한

**목표:** 지름길 기믹이 성립하는 탐색기를 만든다. **적이 지름길을 못 타는 것이 이 태스크의 핵심이다.**

**선행:** P-05

**만들/고칠 파일:**
```
Scripts/Runtime/Pathing/PathGraph.cs      (수정: TryFindRoute, FindNearestNode, OpenShortcut)
Scripts/Tests/PathGraphTests.cs
```

**구현 지시:**
- `TryFindRoute` 는 **다익스트라**로 구현한다 (노드 수십 개 수준이므로 충분하고, A* 휴리스틱 버그 여지가 없다).
- 탐색 중 **`edge.CanTraverse(agent)` 가 false 인 엣지는 아예 없는 것으로 취급**한다.
- `result` 리스트는 호출자가 준 것을 `Clear()` 후 채운다. **내부에서 `new List` 하지 마라.**
- 작업용 자료구조(`distance` 배열, `previous` 배열, 우선순위 큐)는 **`PathGraph` 의 필드로 재사용**한다. 매 호출 할당 금지.
- `OpenShortcut(edgeId)` 는 해당 엣지(그리고 반대 방향 엣지)의 `IsOpen` 을 true 로 만들고 `OnGraphChanged` 를 발행한다.
- ⚠️ **`OnGraphChanged` 는 static 이벤트가 아니라 인스턴스 이벤트다.** 그래도 구독자는 `OnDisable` 에서 반드시 해제한다.

**DoD — EditMode 테스트가 전부 통과:**
1. 단순 직선 그래프에서 최단 경로가 나온다
2. 분기가 있을 때 **비용이 낮은 쪽**이 선택된다
3. `PathAgent.Enemy` 로 탐색하면 **지름길 엣지가 결과에 절대 포함되지 않는다**
4. 지름길이 닫혀 있으면 `Escortee` 도 못 지나간다
5. `OpenShortcut` 후 `Escortee` 경로가 **더 짧아진다**
6. 도달 불가능한 목적지에 대해 `false` 를 반환하고 `result` 가 비어 있다
7. `from == to` 이면 `true` 를 반환하고 `result.Count == 1`
8. 같은 `result` 리스트로 100번 연속 호출해도 결과가 오염되지 않는다

**⚠️ 함정**
- 다익스트라의 `visited` / `distance` 배열을 **매 호출 초기화하는 것을 빠뜨리면**, 두 번째 호출부터 이전 결과가 섞여 엉뚱한 경로가 나온다. 재현이 어렵고 원인 찾기가 지옥이다. **재사용 배열은 반드시 매 호출 리셋하라.**
- 경로를 `previous` 로 역추적한 뒤 **뒤집는 것을 잊지 마라.** `result[0]` 이 `from` 이어야 한다.
- `float` 비용 비교에 `==` 를 쓰지 마라.
- 우선순위 큐가 없다고 `List` 를 매번 `Sort` 하지 마라. 노드가 적으니 **선형 탐색 min 추출로 충분**하다 (O(V²)). 단순한 쪽을 택하라.

**🚫 임의 판단 금지**
- A* Pathfinding Project 같은 외부 에셋을 도입하지 마라.
- Unity `NavMesh` 를 쓰지 마라. 2D 격자 + 그래프에 부적합하고, 지름길 통행 권한을 표현할 수 없다.

---

## `[ ]` P-07. PathFollower — 경로 위를 걷는 로직

**목표:** Escortee / Enemy / AllyUnit 이 **똑같은 코드**로 걷게 한다. 여기서 중복을 만들면 셋의 동작이 서서히 어긋난다.

**선행:** P-06

**만들 파일:**
```
Scripts/Runtime/Pathing/PathFollower.cs
Scripts/Tests/PathFollowerTests.cs
```

**구현 지시:**
- **MonoBehaviour 가 아니다.** 순수 C# 클래스. `new PathFollower()` 로 만들어 필드에 들고 쓴다.
- `Advance(float distance)`:
  - 현재 위치에서 `NextNode` 방향으로 `distance` 만큼 이동
  - `distance` 가 남은 구간보다 크면 노드를 통과하고 **남은 거리로 계속 진행** (프레임당 여러 노드 통과 가능)
  - 노드를 하나라도 통과했으면 `true` 반환
  - 마지막 노드에 도달하면 `IsFinished = true`, 이후 `Advance` 는 아무것도 하지 않고 `false`
- `SetRoute` 는 **경로 중간에 호출될 수 있다.** 현재 위치에서 새 경로의 첫 노드로 이어지도록 처리하라.
  - ⚠️ 이때 `route[0]` 이 현재 위치와 다르면 순간이동한다. **`route[0]` 은 항상 "지금 향하고 있거나 방금 지난 노드"여야 한다.** 호출자가 이를 보장하고, `PathFollower` 는 어긋나면 `Debug.LogWarning` 을 찍는다.

**DoD — EditMode 테스트 통과:**
1. 3노드 직선 경로를 총 길이만큼 `Advance` 하면 마지막 노드에 정확히 도착하고 `IsFinished == true`
2. 한 번의 `Advance` 로 노드 2개를 건너뛰어도 위치가 정확하다
3. 총 길이보다 긴 거리를 넘겨도 마지막 노드를 지나치지 않는다 (오버슛 없음)
4. `Advance(0)` 이 상태를 바꾸지 않는다
5. `Clear()` 후 `HasRoute == false`, `Advance` 가 안전하다 (예외 없음)

**⚠️ 함정**
- **오버슛 버그가 이 클래스의 단골이다.** 마지막 노드에서 `distance` 가 남았을 때 계속 진행하면 경로 밖으로 날아간다. 반드시 `IsFinished` 로 잘라라.
- `while` 루프로 노드를 소비할 때 **무한 루프 방어**를 넣어라. 길이가 0인 엣지(같은 셀에 노드 2개)가 있으면 무한 루프가 된다. 반복 횟수 상한(예: `route.Count + 1`)을 두고 초과 시 `Debug.LogError`.
- `Vector3.MoveTowards` 를 쓰면 편하지만, 남은 거리를 직접 계산해야 다중 노드 통과가 된다. 직접 계산하라.

---
---

# 단계 3 — 판의 뼈대

---

## `[ ]` P-08. GameClock — 배속과 일시정지

**목표:** 테스트 효율. **이걸 먼저 만들어야 이후 모든 태스크의 확인이 빨라진다.** 나중으로 미루지 마라.

**선행:** P-02
**참조:** `ARCHITECTURE.md` §3.4

**만들 파일:**
```
Scripts/Runtime/Session/GameClock.cs
Scripts/Runtime/Diagnostics/DebugHotkeys.cs
```

**구현 지시:**
- `SetSpeed(float)` 는 `Time.timeScale` 을 설정한다. 허용값: `1, 2, 4`.
- `Pause()` 는 `Time.timeScale = 0`, `Resume()` 은 마지막 `Speed` 로 복귀.
- 핫키 (Input System 사용):
  - `Space` → 일시정지 토글
  - `1` / `2` / `3` → 배속 1× / 2× / 4×
  - `R` → 스테이지 재시작 (P-17에서 연결)
- ⚠️ `OnDestroy` / `OnApplicationQuit` 에서 **`Time.timeScale = 1` 로 복구**하라. 안 하면 일시정지 상태로 플레이를 끝냈을 때 다음 플레이가 멈춘 채 시작한다. (도메인 리로드가 꺼져 있어서 더 잘 발생한다)

**DoD:**
- 씬에서 Space / 1 / 2 / 3 이 동작한다
- 일시정지 중에 플레이를 멈췄다가 다시 시작해도 정상 속도로 시작한다
- 화면 좌상단에 현재 배속이 텍스트로 보인다 (임시 OnGUI 도 허용)

**⚠️ 함정**
- `Time.timeScale = 0` 일 때 `Update` 는 계속 돌지만 `Time.deltaTime` 이 0이다. UI 애니메이션 등은 `Time.unscaledDeltaTime` 을 써야 한다.
- 구 입력 API(`Input.GetKeyDown(KeyCode.Space)`)를 쓰지 마라. **P-01 에서 Active Input Handling 을 New 로 바꿨으므로 컴파일 에러가 난다.** 올바른 형태:
  ```csharp
  using UnityEngine.InputSystem;
  if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) { ... }
  ```
- `Keyboard.current` 는 **null 일 수 있다.** 항상 null 체크하라.

---

## `[ ]` P-09. Combat 기반 — Health / IDamageable / TargetRegistry / Attacker

**목표:** 물리 엔진 없이 "누가 누구를 쏠 수 있는가"를 해결한다.

**선행:** P-02
**참조:** `ARCHITECTURE.md` §3.3

**만들 파일:**
```
Scripts/Runtime/Combat/Team.cs
Scripts/Runtime/Combat/IDamageable.cs
Scripts/Runtime/Combat/Health.cs
Scripts/Runtime/Combat/TargetRegistry.cs
Scripts/Runtime/Combat/Attacker.cs
```

**구현 지시:**
- `TargetRegistry` 는 `Team` 별로 `List<IDamageable>` 3개를 들고 있는다. 매번 전체를 순회하지 마라.
- `Register`/`Unregister` 는 `Health.OnEnable`/`OnDisable` 에서 호출.
- **`ResetStatics()` 를 반드시 구현하라** (`ARCHITECTURE.md` §3.3 참조). 안 하면 두 번째 플레이부터 죽은 유령 타겟이 남아 유닛들이 허공을 쏜다.
- 거리 비교는 `sqrMagnitude` vs `range * range`.
- `FindNearest` / `FindNearestTo` 순회 중 `IsAlive == false` 인 항목을 만나면 건너뛴다. (제거는 `Unregister` 에 맡긴다 — 순회 중 컬렉션 수정 금지)
- `Attacker.Update`:
  ```
  if (!Enabled) return;
  _cooldown -= Time.deltaTime;
  if (_cooldown > 0f) return;
  타겟 탐색 → 없으면 return
  타겟.TakeDamage(Damage, this)
  OnFired?.Invoke(타겟)
  _cooldown = Interval;
  ```
- 타겟 선택 규칙은 GDD §8: **`FindNearestTo(내 위치, Range, Enemy, 보호대상 위치)`** 를 기본으로 쓴다. 보호대상이 없으면 `FindNearest`.
- 투사체는 만들지 마라. **히트스캔(즉시 데미지)** 이다. 발사 연출은 `OnFired` 를 구독한 뷰가 선 하나 그리는 정도로 충분하다.

**DoD:**
- 빈 씬에 `Health(Team.Enemy)` 오브젝트와 `Attacker(TargetTeam=Enemy)` 오브젝트를 놓으면 사거리 안에서 데미지가 들어가고 0이 되면 `OnDied` 가 뜬다
- 플레이를 두 번 연속 시작해도 이전 판의 타겟이 남지 않는다 (`TargetRegistry.CountAlive` 로 확인)
- `Attacker.Enabled = false` 면 아무것도 쏘지 않는다

**⚠️ 함정**
- **`OnEnable`/`OnDisable` 쌍이 맞지 않으면 레지스트리에 유령이 쌓인다.** 특히 P-19 에서 풀링을 붙이면 여기가 바로 터진다. 지금부터 정확히 짝을 맞춰라.
- `TakeDamage` 안에서 `OnDied` 를 발행하고, 그 구독자가 다시 `TakeDamage` 를 부르면 재귀한다. `IsAlive` 를 **먼저 false 로 만들고** 이벤트를 발행하라.
- `Health` 를 `Destroy` 하면 같은 프레임 안에서는 아직 살아 있다. `Unregister` 를 `OnDisable` 에서 하고, `Destroy` 직후 `gameObject.SetActive(false)` 를 호출해 즉시 빠지게 하라.
- 정수 나눗셈으로 DPS 계산하지 마라. 전부 `float`.

**🚫 임의 판단 금지**
- `Collider2D` + `Physics2D.OverlapCircle` 로 타겟을 찾지 마라. (ADR-0005)
- 데미지 타입, 방어력, 속성 상성을 만들지 마라. 프로토타입 범위 밖이다.

---
---

# 단계 4 — 액터

---

## `[ ]` P-10. Escortee — 보호대상

**목표:** 보호대상이 경로를 따라 탈출 지점까지 자동으로 간다.

**선행:** P-07, P-09

**만들 파일:**
```
Scripts/Runtime/Actors/Escortee.cs
Scripts/Runtime/Session/GameSession.cs     (골격)
Prefabs/Escortee.prefab
```

**구현 지시:**
- `Escortee` 는 `PathFollower` 를 필드로 갖고, `Health(Team.Escortee)` 를 갖는다.
- `Start`:
  - `PathGraph` 에서 `IsEscorteeStart` 노드와 `IsExit` 노드를 찾는다
  - `TryFindRoute(start, exit, PathAgent.Escortee, _route)` → `PathFollower.SetRoute`
- `Update`:
  - `_follower.Advance(_speed * Time.deltaTime)` 가 true 를 반환하면 → `GameSession.Instance` 에 `OnEscorteeReachedNode(현재 노드)` 를 발행시킨다
  - `transform.position = _follower.Position`
  - `IsFinished` 면 `GameSession.DeclareVictory()`
- `Health.OnDied` → `GameSession.DeclareDefeat()`
- `PathGraph.OnGraphChanged` 구독 → **경로 재계산** (지름길이 열렸을 때). `OnDisable` 에서 해제.
  - ⚠️ 재계산 시 시작점은 `_follower.CurrentNode` 다. 원래 시작 노드가 아니다.
- 그레이박스: 흰색 원, 지름 0.6, Sorting Layer `Actors`.

**GameSession 골격 (이번 태스크에서 만드는 범위):**
- `Instance`, `Result`, `ElapsedTime`, `Escortee` 프로퍼티
- `DeclareVictory()` / `DeclareDefeat()` → `Result` 설정 + `OnGameEnded` 발행 + `GameClock.Pause()`
- `OnEscorteeReachedNode` 이벤트
- UI 는 P-17 에서 붙인다. 지금은 `Debug.Log` 로 충분하다.

**DoD:**
- 플레이하면 흰 원이 경로를 따라 좌→우로 이동한다
- 탈출 지점 도달 시 콘솔에 "VICTORY" 가 찍히고 게임이 멈춘다
- 배속 2×, 4× 에서 이동 속도가 비례해서 빨라진다 (`Time.deltaTime` 을 제대로 썼다는 증거)
- 일시정지 중에는 완전히 멈춘다

**⚠️ 함정**
- `Start` 에서 `GridSystem`/`PathGraph` 를 참조하는데, Script Execution Order 를 설정하지 않았다면 아직 초기화 전일 수 있다. **`ARCHITECTURE.md` §4 의 실행 순서를 지금 등록하라.**
- `transform.position` 을 직접 갱신하는 것과 `PathFollower.Position` 이 **서로 다른 진실이 되면 안 된다.** `PathFollower` 가 유일한 진실이고 transform 은 그 결과를 반영만 한다. 반대로 하지 마라.
- 승리/패배 후에도 `Update` 가 계속 돌아 `DeclareVictory` 가 여러 번 호출된다. `Result != InProgress` 면 즉시 return 하라.

---

## `[ ]` P-11. MotherSpawner — 이동하는 적 스폰 지점

**목표:** **이 게임의 유일한 차별점을 구현한다.** 여기가 밋밋하면 프로토타입이 실패한다.

**선행:** P-10

**만들 파일:**
```
Scripts/Runtime/Actors/MotherSpawner.cs
Scripts/Runtime/Data/StageDefinition.cs
Prefabs/Mother.prefab
Data/Stages/Stage_Greybox.asset
```

**구현 지시:**
- 상태머신: `Idle` → `Chasing` → `Burst` → `Chasing`
  - `Idle`: 스테이지 시작 후 `MotherSpawnDelay` 초 동안
  - `Chasing`: 경로를 따라 보호대상 쪽으로 이동 + `MotherSpawnInterval` 마다 스폰
  - `Burst`: **골격만 만들고 진입 조건은 비워 둔다.** (GDD §7 — 프로토타입 범위 밖)
- 이동:
  - `StageDefinition.MotherFollowsPath == true` (기본) → `PathFollower` 로 경로 추종. 목적지는 보호대상의 현재 노드
  - `false` → 보호대상 방향으로 직선 이동 (D-06 실험용)
  - 목적지 재계산은 `GameSession.OnEscorteeReachedNode` 구독으로만. **매 프레임 재계산 금지.**
- **처치 불가:** `Health` 컴포넌트를 붙이지 마라. `TargetRegistry` 에 등록하지 마라. 아군이 조준할 수 없어야 한다.
- 스폰:
  - 위치 = 모체의 **현재 위치**
  - `Enemy` 프리팹을 인스턴스화하고 `PathGraph.FindNearestNode(내 위치, PathAgent.Enemy)` 를 시작 노드로 준다
  - 스폰 0.5초 전 예고 연출 (그레이박스: 모체 색이 잠깐 밝아짐)
- 속도는 반드시 보호대상보다 느리다. `Awake` 에서 `MotherSpeed >= EscorteeSpeed` 면 `Debug.LogError` 로 경고하라.
- 그레이박스: 진한 자주색 사각, 크기 1.5 (유닛보다 확연히 크게), Sorting Layer `Actors`.

**DoD:**
- 모체가 지연 후 등장해 보호대상을 따라간다
- 일정 간격으로 적이 스폰되고, **스폰 위치가 모체를 따라 이동하는 것이 눈으로 명확히 보인다**
- 모체를 클릭하거나 유닛 사거리에 넣어도 아무 데미지가 들어가지 않는다
- `Stage_Greybox.asset` 에서 속도/간격을 바꾸면 플레이에 반영된다

**⚠️ 함정**
- **모체가 보호대상을 따라잡으면 게임이 성립하지 않는다.** 속도 검증 로그를 반드시 넣어라.
- 보호대상이 지름길을 타면 모체는 못 따라간다 → `TryFindRoute` 가 `false` 를 반환할 수 있다. 이때 **경로 없음으로 멈추게 하지 말고**, 보호대상에 가장 가까운 *도달 가능한* 노드로 향하게 하라. 안 그러면 모체가 얼어붙어 게임이 끝나버린다. **이건 지름길 기믹의 필연적 부작용이므로 반드시 처리하라.**
- `Instantiate` 를 부모 없이 하면 하이어라키가 지저분해진다. `--- Actors ---/Enemies/` 아래에 넣어라.
- 스폰 간격을 `InvokeRepeating` 으로 만들지 마라. `Time.timeScale` 과 일시정지에 제대로 반응하지 않는다. **`Update` 에서 타이머를 누산하라.**

**🚫 임의 판단 금지**
- 모체에 체력이나 페이즈를 붙이지 마라. GDD §7 은 "처치 불가능"이다.
- 버스트 모드를 지금 구현하지 마라. 상태만 만들어 두고 비워라.

---

## `[ ]` P-12. Enemy — 잡몹

**목표:** 적이 보호대상을 추격해 공격한다.

**선행:** P-11

**만들 파일:**
```
Scripts/Runtime/Actors/Enemy.cs
Scripts/Runtime/Data/EnemyDefinition.cs
Prefabs/Enemy_Basic.prefab
Data/Enemies/Enemy_Basic.asset
```

**구현 지시:**
- 구성: `PathFollower` + `Health(Team.Enemy)` + `Attacker(TargetTeam = Escortee)`
- 상태머신: `Moving` → `Attacking` → `Moving`
  - `Moving`: 보호대상 노드로 경로 추종
  - `Attacking`: 보호대상이 `AttackRange` 안에 들어오면 **정지하고** 공격
  - 보호대상이 사거리를 벗어나면 다시 `Moving`
- 경로 재계산 트리거 (**이 3개만**):
  1. 스폰 시 1회
  2. `GameSession.OnEscorteeReachedNode` 발생 시
  3. 자신이 노드를 통과했을 때 (`Advance` 가 true)
- `Health.OnDied` → 격파 연출 후 제거. 프로토타입은 그냥 `Destroy`. (풀링은 P-19)
- `StageDefinition.EnemiesTargetAllies == true` 이면 `Attacker.TargetTeam` 을 상황에 따라 `Ally` 로도 전환 — **기본값은 false 이므로 조건문만 넣고 동작은 비워 둔다.**
- 그레이박스: 빨간 원, 지름 0.4, Sorting Layer `Actors`.

**DoD:**
- 스폰된 적이 보호대상을 향해 이동하고, 따라잡으면 멈춰서 공격한다
- 보호대상 체력이 실제로 깎이고 0이 되면 "DEFEAT" 가 찍힌다
- 적은 보호대상보다 빠르다 (`EnemyDefinition.MoveSpeed > StageDefinition.EscorteeSpeed`) — 아니면 검증 로그로 경고
- 적이 **지름길을 통과하지 못한다** (지름길을 연 뒤 눈으로 확인)
- 적 30마리가 동시에 있어도 프레임 드랍이 없다

**⚠️ 함정**
- 적마다 매 프레임 `TryFindRoute` 를 부르면 30마리 × 60fps = 초당 1800회 다익스트라다. **재계산 트리거를 위 3개로 제한하라.** 이걸 어기면 프로토타입 단계에서 이미 버벅인다.
- 적이 보호대상을 "따라잡았는데" 보호대상이 계속 도망가면 `Moving` ↔ `Attacking` 이 매 프레임 토글된다. **히스테리시스**를 넣어라 (공격 진입 = `Range`, 이탈 = `Range * 1.2f`).
- `GameSession.OnEscorteeReachedNode` 를 적 30마리가 구독하면 이벤트 발행 한 번에 30번의 경로 재계산이 동시에 일어나 스파이크가 생긴다. **프레임 분산**은 지금 하지 말고, P-21 에서 실측 후 필요하면 하라. (지금 넣으면 조기 최적화다)
- 이벤트 구독을 `OnEnable` 에서 하고 `OnDisable` 에서 해제하라. `Destroy` 되는 적이 구독을 남기면 `MissingReferenceException` 폭탄이 된다.

---
---

# 단계 5 — 플레이어 조작

---

## `[ ]` P-13. Village + 배치 슬롯 선택

**목표:** 플레이어가 "어느 마을에서 어디로 보낼지" 고르는 입력을 만든다. **행군은 아직 안 만든다.**

**선행:** P-04, P-06

**만들 파일:**
```
Scripts/Runtime/Actors/Village.cs
Scripts/Runtime/Session/DeploymentController.cs
Scripts/Runtime/UI/HirePanel.cs
Prefabs/Village.prefab
```

**구현 지시:**
- `Village`:
  - `VillageSlot` 셀 위에 배치. `Start` 에서 `PathGraph.FindNearestNode(위치, PathAgent.Ally)` 를 자기 **출발 노드**로 캐시
  - 고용 가능한 `UnitDefinition` 목록을 `[SerializeField]` 로 가짐 (프로토타입은 1종)
- `DeploymentController` — 입력 흐름 (**정확히 이 순서**):
  ```
  [대기]
    → 마을 클릭        → [유닛 선택] (HirePanel 표시)
    → 유닛 버튼 클릭   → [슬롯 선택] (Buildable 칸 전부 하이라이트)
    → Buildable 칸 클릭 → 고용 확정 (P-14 에서 유닛 생성)
    → 우클릭 / ESC     → [대기] 로 취소
  ```
- 마우스 → 셀 변환:
  ```csharp
  Vector3 screen = Mouse.current.position.ReadValue();
  Vector3 world  = _camera.ScreenToWorldPoint(screen);
  world.z = 0f;                                   // ⚠️ 필수
  GridCoord coord = GridSystem.Instance.WorldToCell(world);
  ```
- 슬롯 하이라이트: `Buildable` 이면서 **이미 유닛이 없는** 칸만. 반투명 하늘색 오버레이.
- 마을 클릭 판정도 셀 기준으로 한다 (`WorldToCell` 결과가 마을의 `VillageSlot` 좌표와 같은가).

**DoD:**
- 마을을 클릭하면 고용 패널이 뜬다
- 유닛을 고르면 배치 가능한 칸이 전부 하이라이트된다
- 칸을 클릭하면 콘솔에 `Hire: <유닛> from <마을> to (x,y)` 가 찍힌다
- 우클릭/ESC 로 어느 단계에서든 취소된다
- **UI 위를 클릭했을 때 뒤의 맵이 반응하지 않는다**

**⚠️ 함정**
- **`ScreenToWorldPoint` 에서 z 를 0으로 만드는 것을 빠뜨리면** Orthographic 카메라에서도 좌표가 어긋난다. 위 코드 그대로 써라.
- **UI 클릭이 맵으로 새어 나가는 문제**는 반드시 처리하라:
  ```csharp
  using UnityEngine.EventSystems;
  if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
  ```
  이걸 빠뜨리면 고용 버튼을 누를 때마다 뒤의 칸도 같이 선택되어 원인 모를 버그가 된다.
- Input System 을 쓸 때 `EventSystem` 오브젝트에는 **`InputSystemUIInputModule`** 이 붙어야 한다 (구 `StandaloneInputModule` 이 아니다). Canvas 를 만들면 Unity 가 자동으로 맞춰 주지만, 안 되어 있으면 UI 클릭이 아예 안 먹는다.
- `Camera.main` 을 `Update` 에서 부르지 마라. `Awake` 에서 캐시.
- 마우스 클릭은 `Mouse.current.leftButton.wasPressedThisFrame` 이다. `isPressed` 는 누르고 있는 동안 매 프레임 true 다.

**🚫 임의 판단 금지**
- 드래그 앤 드롭으로 만들지 마라. 클릭 3단계로 확정한다.
- 배치 슬롯을 자유 위치로 만들지 마라. `Buildable` 셀만이다.

---

## `[ ]` P-14. AllyUnit — 고용, 행군, 배치

**목표:** **이 게임의 핵심 트레이드오프를 구현한다.** "지금 보내면 도착했을 땐 늦다"가 여기서 생긴다.

**선행:** P-13

**만들 파일:**
```
Scripts/Runtime/Actors/AllyUnit.cs
Scripts/Runtime/Data/UnitDefinition.cs
Prefabs/Ally_Basic.prefab
Data/Units/Ally_Basic.asset
```

**구현 지시:**
- 상태머신 (`GDD.md` §8 그대로):
  ```
  Queued → Marching → Deployed
  ```
- `Marching`:
  - 경로: `Village.출발노드` → `PathGraph.FindNearestNode(목표 슬롯 위치, PathAgent.Ally)` 까지 `PathFollower`
  - 그 노드에 도착하면 **거기서 목표 슬롯 셀 중심까지 직선 이동** (슬롯은 경로 위에 없으므로)
  - **`Attacker.Enabled = false`** ← 행군 중 공격 불가. GDD §8 규칙이다.
  - `StageDefinition.AlliesCanDieWhileMarching == false` (기본) 이면 `TargetRegistry` 에 **등록하지 않는다** (적이 못 때림)
- `Deployed`:
  - 슬롯 중심에 정확히 스냅 (`GridSystem.CellToWorld`)
  - `Attacker.Enabled = true`, `TargetRegistry` 등록
  - **스스로 움직이지 않는다.** 재배치 기능 없음 (D-05 기본값)
- 슬롯 점유 관리: `DeploymentController` 가 `HashSet<GridCoord>` 로 예약된 슬롯을 관리한다. **고용 확정 즉시 예약**하고, 도착 전에 다른 유닛이 같은 칸을 못 고르게 한다.
- 그레이박스 색: 행군 중 연한 파랑 → 배치 후 진한 파랑. 상태 전환이 **눈으로 명확히** 보여야 한다.
- 행군 경로를 **선으로 그려라** (`LineRenderer` 또는 기즈모). 플레이어가 "얼마나 걸리는지"를 봐야 트레이드오프가 성립한다.

**DoD:**
- 고용하면 유닛이 마을에서 나와 슬롯까지 **실제로 걸어간다**
- 행군 중에는 사거리 안에 적이 있어도 쏘지 않는다
- 도착하면 색이 바뀌고 사격을 시작한다
- 같은 칸을 두 유닛이 예약할 수 없다
- 먼 슬롯을 고르면 눈에 띄게 오래 걸린다 (**핵심 체감**)
- 배속 4× 에서도 행군이 정상 동작한다

**⚠️ 함정**
- **행군 목적지 노드가 슬롯에서 너무 멀면** 유닛이 이상한 경로로 돌아간다. `FindNearestNode` 결과와 슬롯 사이 거리가 3셀을 넘으면 `Debug.LogWarning` 을 찍어 맵 저작 문제를 드러내라.
- 도착 후 마지막 직선 이동 구간에서 `Blocked` 셀을 통과할 수 있다. 프로토타입에서는 **허용**한다 (경로 저작을 잘 하면 안 생긴다). 다만 이 사실을 주석에 남겨라.
- `Attacker.Enabled` 를 상태 전환 시점에 **정확히 한 번** 바꿔라. `Update` 에서 매 프레임 대입하면 다른 코드가 끈 것을 덮어쓴다.
- 슬롯 예약을 `Deployed` 시점에 하면 **행군 중 겹치기 버그**가 난다. 고용 확정 시점에 예약하라.
- 유닛이 행군 중에 게임이 끝나면 그대로 멈춰야 한다. `GameSession.Result != InProgress` 체크.

**🚫 임의 판단 금지**
- 유닛에 블로킹(적 저지)을 넣지 마라. (ADR-0003)
- 배치된 유닛을 옮기거나 회수하는 기능을 만들지 마라. (D-05 기본값 "아니오")

---

## `[ ]` P-15. Wallet — 자원과 고용 비용

**목표:** 고용에 비용을 붙여 선택을 강제한다.

**선행:** P-14

**만들 파일:**
```
Scripts/Runtime/Session/Wallet.cs
Scripts/Runtime/UI/ResourceLabel.cs
```

**구현 지시:**
- `StageDefinition.StartingResource` 로 시작, `ResourcePerSecond` 로 자동 증가.
  - ⚠️ `float` 로 누산하고 정수 부분만 `Amount` 에 반영하라. `int` 로 매 프레임 더하면 증가가 안 되거나 폭주한다.
- `TrySpend` 는 **부족하면 아무것도 하지 않고 false**. 부분 차감 금지.
- `DeploymentController` 는 고용 확정 전에 `CanAfford` 확인. 부족하면 슬롯 클릭이 거절되고 UI에 표시.
- 고용 패널의 버튼은 `CanAfford` 가 false 면 비활성(회색).
- 적 처치 보상은 **넣지 마라** (GDD §10).

**DoD:**
- 화면에 현재 자원이 보이고 초당 증가한다
- 자원이 부족하면 고용이 거절되고 그 이유가 UI에 보인다
- 고용 시 정확히 `UnitDefinition.HireCost` 만큼 차감된다
- 일시정지 중에는 자원이 늘지 않는다

**⚠️ 함정**
- 자원 표시를 `Update` 에서 `text = amount.ToString()` 로 매 프레임 갱신하면 GC 쓰레기가 생긴다. **`Wallet.OnChanged` 구독으로 변경 시에만 갱신하라.**
- 일시정지 중 증가를 막으려면 `Time.deltaTime` 을 써라 (`unscaledDeltaTime` 이 아니다).

---

## `[ ]` P-16. 지름길 기믹

**목표:** "돈으로 시간을 산다"는 두 번째 선택지를 만든다.

**선행:** P-15, P-06

**만들 파일:**
```
Scripts/Runtime/UI/ShortcutMarker.cs
Scripts/Runtime/Session/ShortcutController.cs
```

**구현 지시:**
- 각 지름길 엣지의 중점에 클릭 가능한 마커를 배치 (`PathGraph` 를 순회해 런타임 생성).
- 마커 표시: 잠김 = 노란 점선 + 비용 텍스트, 열림 = 노란 실선.
- 클릭 → `Wallet.TrySpend(StageDefinition.ShortcutCost)` → 성공 시 `PathGraph.OpenShortcut(edgeId)`.
- `OpenShortcut` 이 `OnGraphChanged` 를 발행 → `Escortee` 가 경로를 재계산해 지름길로 방향을 튼다.
- 열린 순간 짧은 피드백 (그레이박스: 마커가 잠깐 커짐 + 콘솔 로그).

**DoD:**
- 지름길을 열면 **보호대상이 즉시 그쪽으로 방향을 바꾼다**
- 적과 모체는 지름길로 따라오지 못하고 원래 경로로 돌아간다 (눈으로 확인)
- 결과적으로 보호대상과 추격자 사이 거리가 벌어진다
- 자원이 부족하면 열리지 않는다
- 한 번 연 지름길은 다시 닫히지 않고, 중복 결제되지 않는다

**⚠️ 함정**
- **P-11 에서 경고한 "모체가 얼어붙는 문제"가 여기서 실제로 발생한다.** 지름길을 연 직후 모체/적이 멈추거나 사라지지 않는지 반드시 확인하라. 멈추면 P-11 의 폴백(도달 가능한 최근접 노드)이 동작하지 않는 것이다.
- `Escortee` 의 경로 재계산 시작점은 **현재 노드**다. 원래 시작 노드로 재계산하면 뒤로 순간이동한다.
- 양방향 지름길이면 반대 방향 엣지도 같이 열어야 한다. `OpenShortcut` 이 이를 처리하는지 확인하라.

---
---

# 단계 6 — 마감과 검증

---

## `[ ]` P-17. 승패 UI와 재시작

**선행:** P-10

**작업:**
- 승리/패배 시 화면 중앙에 결과 패널 (텍스트 + "재시작" 버튼)
- `R` 키 / 버튼 → `GameSession.RestartStage()` → 현재 씬 리로드
- 리로드 시 `Time.timeScale` 복구, `TargetRegistry` 초기화 확인

**DoD:**
- 승리/패배가 화면에 뜬다
- 재시작이 깨끗하게 동작한다 — **5번 연속 재시작해도 콘솔 에러 0개, 유령 오브젝트 0개**

**⚠️ 함정**
- `SceneManager.LoadScene` 후에도 `static` 상태(`TargetRegistry`)는 살아남는다. **씬 로드 시점에도 리셋되는지 확인하라.** `RuntimeInitializeOnLoadMethod` 는 씬 리로드로는 다시 불리지 않는다. `GameSession.Awake` 에서 명시적으로 `TargetRegistry.Clear()` 를 부르는 것이 확실하다.
- 일시정지 상태에서 재시작하면 새 씬이 멈춘 채로 시작한다. 로드 전에 `Time.timeScale = 1`.

---

## `[ ]` P-18. 디버그 오버레이

**목표:** P-21 플레이 테스트에서 **수치를 눈으로 읽을 수 있게** 한다. 이게 없으면 검증이 감으로 끝난다.

**선행:** P-16

**만들 파일:** `Scripts/Runtime/Diagnostics/DebugOverlay.cs`

**표시 항목 (F1 로 토글):**
```
경과 시간 / 배속
보호대상: 체력, 진행률(%), 현재 노드
모체: 상태, 보호대상과의 거리, 다음 스폰까지
적: 생존 수 / 누적 스폰 수 / 누적 처치 수
아군: 행군 중 n / 배치됨 n
자원: 현재 / 누적 획득 / 누적 지출
평균 행군 시간 (초)          ← D-03 검증용
행군 중 손실 수              ← D-03 검증용
```

**치트 키:**
- `F2` — 자원 +1000
- `F3` — 화면의 적 전멸
- `F4` — 보호대상 무적 토글
- `F5` — 즉시 승리

**DoD:**
- F1 로 오버레이가 켜지고 위 수치가 실시간으로 보인다
- 치트 키가 동작한다
- **빌드에는 포함되지 않는다** — 클래스 전체를 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 로 감싼다

**⚠️ 함정**
- `OnGUI` 에서 문자열을 매 프레임 조립하면 GC 스파이크가 난다. 프로토타입에서는 허용하되, `StringBuilder` 를 필드로 두고 재사용하라.

---

## `[ ]` P-19. 오브젝트 풀링 (적)

**선행:** P-18. **이 태스크는 P-18 까지 끝난 뒤에 한다. 미리 하지 마라.**

**전제:** P-18 오버레이에서 동시 생존 적이 40마리를 넘고, 프로파일러에 `Instantiate`/GC 스파이크가 실제로 보일 때만 한다. **안 보이면 이 태스크를 건너뛰고 그렇게 보고하라.**

**작업:**
- `Scripts/Runtime/Combat/EnemyPool.cs` — 단순 스택 기반 풀
- `Enemy` 에 `OnSpawnedFromPool()` / `OnReturnedToPool()` 추가 — **모든 상태를 초기화**

**DoD:**
- 적 100마리를 연속 스폰/처치해도 GC 할당이 거의 없다
- 재사용된 적의 체력/상태/경로가 이전 생애의 값을 갖지 않는다
- `TargetRegistry` 에 유령이 쌓이지 않는다 (`CountAlive` 로 확인)

**⚠️ 함정**
- **풀링은 `OnEnable`/`OnDisable` 쌍이 안 맞는 버그를 전부 폭발시킨다.** P-09 에서 짝을 맞춰 뒀는지 여기서 검증된다.
- 이벤트 구독 해제를 빠뜨린 적이 풀에서 부활하면 **구독이 2배로 쌓인다.** 데미지가 2배로 들어가는 원인 모를 버그가 된다.

---

## `[ ]` P-20. 스테이지 데이터 정리와 통합

**선행:** P-17 (P-19 는 선택)

**작업:**
- 코드에 남은 매직 넘버를 전부 `StageDefinition` / `UnitDefinition` / `EnemyDefinition` 으로 이관
- `Data/Stages/Stage_Greybox.asset` 에 초기 밸런스 수치를 채운다 (아래는 **시작점일 뿐, 튜닝 대상**)

| 항목 | 시작값 |
|---|---|
| 보호대상 속도 | 1.2 셀/초 |
| 보호대상 체력 | 100 |
| 모체 속도 | 0.8 셀/초 |
| 모체 등장 지연 | 5초 |
| 모체 스폰 간격 | 3초 |
| 적 속도 | 2.0 셀/초 |
| 적 체력 | 20 |
| 적 공격력 / 간격 | 5 / 1.0초 |
| 아군 사거리 | 3.5 셀 |
| 아군 공격력 / 간격 | 10 / 0.8초 |
| 아군 행군 속도 | 2.5 셀/초 |
| 아군 고용 비용 | 50 |
| 시작 자원 | 150 |
| 초당 자원 | 8 |
| 지름길 비용 | 120 |

- 한 판이 **90~150초**에 끝나도록 맵 길이/속도를 조정한다. 이보다 길면 테스트 회전이 안 돈다.

**DoD:**
- 코드에 밸런스 매직 넘버가 없다 (`grep` 으로 확인)
- SO 값만 바꿔서 난이도를 바꿀 수 있다
- 한 판이 90~150초에 끝난다
- 처음부터 끝까지 콘솔 에러/경고 0개로 플레이가 완주된다

---

## `[ ]` P-21. 플레이 테스트 세션

**목표:** **재미를 판정한다.** 이 프로젝트에서 가장 중요한 태스크다.

**선행:** P-20

**진행 방법:**
1. 개발자와 기획자가 **각자 5판 이상** 플레이한다
2. 매 판 후 아래를 기록한다 (`.docs/playtest/YYYY-MM-DD.md`)
3. 코드를 고치기 전에 **먼저 다 기록한다**

**기록 항목:**

### A. 통과 조건 판정 (GDD §14)
| 조건 | 판정 | 근거 |
|---|---|---|
| 배치 타이밍을 놓쳐서 "아깝다"는 감정이 발생했는가 | ☐ | |
| 모체 이동으로 기존 배치가 무의미해지는 순간이 명확했는가 | ☐ | |
| 보호대상이 도착하기 *전에* 미리 고용하게 되었는가 | ☐ | |
| 지름길을 열지 말지 실제로 고민했는가 | ☐ | |

### B. 수치 기록 (P-18 오버레이에서)
- 평균 행군 시간 / 총 고용 수 / 사용 자원 비율(병력:지름길)
- 보호대상 최저 체력 / 클리어 시간

### C. 미결정 사항 관찰 (GDD §13)
- **D-03 (행군 중 사망)**: 토글을 켜고 5판 더 한다. "돈만 날렸다"는 허무함이 발생하는가?
- **D-06 (모체 경로 추종 vs 직진)**: 토글을 바꿔 각 3판. 어느 쪽이 재배치 압박이 큰가?
- **D-01 (뒤에 남은 유닛)**: 남겨진 유닛이 몇 개나 되는가? 신경 쓰이는가?

**DoD:**
- 플레이 테스트 기록 문서가 `.docs/playtest/` 에 있다
- 통과 조건 4개 각각에 ✅/❌ 판정이 있다
- D-01, D-03, D-06 에 대한 결정이 내려지고 **ADR로 기록**되었다

> ### ⛔ 게이트
> **통과 조건 4개 중 2개 이상이 ❌ 면, 다음 단계로 가지 마라.**
> 종족을 열 개 넣어도 재미없다. 기획서로 돌아가 핵심 루프를 다시 짜라.
> 여기서 몇 주 쓰는 것이 몇 달을 아낀다.

---
---

## 부록. 프로토타입 범위 밖 (지금 만들지 마라)

이것들을 "있으면 좋을 것 같아서" 만들지 마라. **전부 P-21 통과 후다.**

- 버스트 모드 (상태 골격만)
- 마을 복수화, 종족별 병종, 유닛 등급/승급
- 스탠딩 일러스트, 대화 시스템, 서사, 튜토리얼
- 사운드, 파티클, 애니메이션
- 세이브/로드, 메타 진행, 스테이지 선택
- 데이터 파이프라인 (구글 시트 → SO 임포터) ← **P-21 통과 직후 1순위**
- 스팀 연동, 빌드 파이프라인
- 쿼터뷰 카메라 기울임
- 오브젝트 풀링 (적 외 다른 것)
- 옵션 메뉴, 해상도 대응, 로컬라이제이션
