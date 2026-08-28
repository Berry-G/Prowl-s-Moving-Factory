# TASKS — 1차 그레이박스 프로토타입

> 버전 0.4 · 2026-08-27 · Unity **6000.5.7f1**
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

*(2026-08-27 갱신. 상태는 **CLI 로 실측한 것만** ✅ 로 적는다 — `unity command run_tests` / `console`)*

| 단계 | 태스크 | 상태 |
|---|---|---|
| 0. 환경 | P-00 ~ P-02, **P-02B(에디터 연결)** | ✅ 완료. 검증 주력은 **Unity CLI** (2026-08-27 전환), MCP 는 보조 |
| 1. 격자와 맵 | P-03 ~ P-04 | ✅ 코드·씬·테스트 완료 |
| 2. 경로 | P-05 ~ P-07 | ✅ 테스트 통과 |
| 3. 판의 뼈대 | P-08 ~ P-09 | ✅ |
| 4. 액터 | P-10 ~ P-12 | ✅ |
| 5. 플레이어 조작 | P-13 ~ P-16 | ✅ |
| 6. 마감 | P-17 ~ P-18 | ✅ 완료 |
| 6. 마감 | **P-18B (신규)** | ✅ 완료 (2026-08-27) — 미커밋 7개 커밋 + 부채 3건 청산. 부채 3 은 현장 결정으로 단방향 엣지 방식 |
| 6. 마감 | P-19 (풀링) | ⬜ 조건부. **실측 전까지 손대지 마라** |
| 6. 마감 | P-20 | ◐ SO 이관·시작값 **완료**. 남은 DoD는 전부 **실측**(한 판 90~150초 / 콘솔 0) |
| **7. 게임화 (신규)** | **G-00 ~ G-21** | ⬜ 미착수 — **시연 가능한 퀄리티 만들기**. P-21 보다 먼저 |
| 8. 게이트 | P-21 | ⬜ 단계 7 완료 후 |

EditMode 테스트: **32/32 통과** (2026-08-27 `unity command run_tests --mode EditMode`, 1.21s)

---

### 현재 스냅샷 — 2026-08-27

**커밋된 것:** 커밋 3개. `9bee400` 에 P-03~P-18 이 전부 들어 있다.

**아직 커밋 안 된 것 (35개 변경).** 코드는 이미 동작하지만 git 에 없다:

| 무엇 | 파일 |
|---|---|
| **좌하단 HUD 신설** — 골드 / 호위대상 HP / 배속 + 배속 버튼 4개 | `UI/SpeedControls.cs`, `UI/SpeedLabel.cs`, `UI/EscorteeHealthLabel.cs` (신규) |
| **배치 UI 슬로우모션 0.1x** — 마을을 고르는 순간 시간이 느려진다 | `Session/GameClock.cs` (`EnterUiSlowMotion`/`ExitUiSlowMotion`), `Session/DeploymentController.cs` |
| **배치 하이라이트를 마을 선택 즉시 표시** (기존: 유닛 고른 뒤) + 노랑 불투명으로 변경, `sortingOrder=5` | `Session/DeploymentController.cs` |
| **재시작 시 배속 복구를 `GameClock` 경유로** (기존: `Time.timeScale` 직접 대입 — 규칙 위반이었음) | `Session/GameSession.cs` |
| **고용 버튼에 Text 자식 추가** — 없어서 글자가 안 보이던 버그 | `UI/HirePanel.cs` |
| **디버그 오버레이를 우하단으로** (좌하단 HUD 와 자리 충돌) | `Diagnostics/DebugOverlay.cs` |
| **씬 빌더 보강** — Tilemap `SetParent(parent, false)`, StageDefinition 경로 폴백, HirePanel 명시 배선 | `Editor/SceneParts.cs`, `Editor/GreyboxFactory.cs` |
| **하트 스프라이트 절차 생성** (호위대상 표식) | `Editor/GreyboxSprites.cs`, `Art/Greybox/Heart.png` |
| 주석 오타 다수 정정 (`프레ーム`, `노ード`, `리ロ드` 등 일본어 문자 혼입) | 여러 파일 |

> **⚠️ 위 변경 중 `GameClock` 슬로우모션과 하이라이트 조기 표시는 태스크에 없던 기능이다.**
> 사용자 요청으로 들어갔다. P-21 에서 "이게 재미에 기여했는가" 를 같이 판정할 것.

**발견된 부채 3건** → P-18B 에서 처리:

1. `Assets/test.unity` — Assets 최상위에 스테이지가 아닌 씬이 있다. `CLAUDE.md` §4 "씬 임의 생성 금지" 위반. 빌드 목록에도 없다.
2. `GameClock.UiSlowScale = 0.1f` 가 코드 상수다. 밸런스/감각 수치이므로 `StageDefinition` 으로 가야 한다 (P-20 DoD).
3. `PathGraphTests.Unreachable_ReturnsFalse_EmptyResult` 가 의도적으로 `LogError` 를 찍는다. 테스트는 통과하지만 콘솔이 빨개져서 **P-20 의 "콘솔 에러 0개" 판정을 오염시킨다.** `LogAssert.Expect` 로 감싸야 한다.

---
---

# 단계 0 — 환경 세팅

---

## `[x]` P-00. 버전 관리 초기화

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

## `[x]` P-01. 프로젝트 설정 정리

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

## `[x]` P-02. 폴더 구조와 어셈블리 정의

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

## `[x]` P-02B. 에디터 연결 — Claude가 컴파일·콘솔·테스트를 직접 보게 하기

**목표:** Claude가 **컴파일 에러·콘솔 로그·테스트 결과를 스스로 읽게** 만든다.
이것이 없으면 Claude는 코드를 쓰고도 그게 컴파일되는지 알 수 없다. `CLAUDE.md` §5 의
"컴파일 검증이 불가능하면 그 사실을 명시하라"는 규칙이 바로 그 상태를 전제한 것이다.

**선행:** P-02
**완료:** 2026-08-26 (MCP) → **2026-08-27 Unity CLI 로 전환**

> ### ⚠️ 2026-08-27 갱신 — 주력은 **Unity CLI**, MCP 는 보조
>
> 이 태스크는 원래 Unity 공식 **MCP** 를 붙이는 것이었고, 그건 그대로 붙어 있다.
> 그런데 Unity 가 배포하는 **`unity` CLI** 가 훨씬 낫다는 것이 확인됐다.
> 공식 MCP 패키지 문서(`Documentation~/snippets/mcp-deprecation-notice.md`)도
> **CLI 로 갈아타라**고 안내하고 있다.
>
> | | MCP (`com.unity.ai.assistant`) | **CLI (`com.unity.pipeline`)** |
> |---|---|---|
> | 도구 수 | **7개** | 200개 가까이 |
> | 씬·GameObject 조작 | **없음** | 전부 있음 |
> | 테스트 실행 | 없음 | `run_tests` |
> | 임의 C# | `internal class CommandScript` 규약 필요 | `eval` 로 바로 |
> | 비포커스 틱 | 불가 (`EditorApplication.Step()` 으로 우회) | `set_autotick` |
>
> MCP 는 6.6/6.7 에서 사라질 수 있다. **새 작업은 CLI 로 한다.**

### 왜 이걸 먼저 하는가

| 연결 없이 | 연결 후 |
|---|---|
| 코드를 쓰고 "에디터에서 확인해 주세요" 로 끝남 | Claude가 직접 컴파일 결과를 확인 |
| 에러가 나면 사용자가 복사해서 붙여넣어야 함 | Claude가 콘솔에서 바로 읽음 |
| 씬/프리팹 연결을 사용자가 손으로 | Claude가 CLI 로 직접 |
| 테스트를 사용자가 Test Runner 에서 돌림 | `run_tests` 로 Claude가 직접 |
| 왕복 1회 = 사용자 개입 1회 | 왕복이 Claude 안에서 닫힘 |

---

### 작업 A — Unity CLI (주력)

1. **CLI 설치 확인**
   ```bash
   which unity && unity --version
   ```
   이 PC 는 `%LOCALAPPDATA%/Unity/bin/unity` 에 **1.0.0-beta.6** 설치됨 (PATH 에 있음).
   없으면: `curl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh | UNITY_CLI_CHANNEL=beta bash`

2. **브릿지 패키지** — `com.unity.pipeline` (Unity 6.0+)
   `Packages/manifest.json` 에 **`0.5.0-exp.1`** 로 이미 있다. 없으면 `unity pipeline install`.
   → **이 패키지를 "미사용" 으로 판단해 지우지 마라.** CLI 가 통째로 죽는다.

3. **연결 확인**
   ```bash
   unity status --no-banner        # state 가 "ready" 여야 한다
   unity command --no-banner       # 에디터가 노출하는 명령 전체 목록
   ```

4. **에이전트 스킬 설치** (권장)
   ```bash
   unity skill install claude-code
   ```
   → `~/.claude/skills/unity-cli`. 인자를 `--help` 로 더듬지 않게 된다.

### 작업 B — Unity 공식 MCP (보조, 이미 되어 있음)

- 패키지: `com.unity.ai.assistant` **2.18.0-pre.2**
- 프로젝트 루트 `.mcp.json` 을 직접 작성했다. **서버 이름은 반드시 `unity-mcp`**:
  ```json
  { "mcpServers": { "unity-mcp": {
      "command": "${USERPROFILE}/.unity/relay/relay_win.exe", "args": ["--mcp"] } } }
  ```
- 릴레이 바이너리는 패키지 안이 아니라 **홈 디렉터리**(`%USERPROFILE%/.unity/relay/relay_win.exe`)에 있다.
  Unity 에디터가 켜질 때 자동 설치된다.
- `${USERPROFILE}` 를 쓴 이유: 절대 경로를 커밋하면 다른 PC 에서 깨진다.

---

**DoD:** *(2026-08-27 전부 충족 확인)*

- [x] `unity status` 가 **ready** 로 뜬다 → `7800 / ready / 6000.5.7f1 / PID 3988`
- [x] Claude가 Unity 콘솔의 컴파일 에러를 **직접 읽어서** 보고할 수 있다
- [x] `unity command run_tests --mode EditMode` 로 테스트가 돌아간다 → **32/32 Passed, 1.21s**
- [x] 에디터를 껐다 켠 뒤에도 자동 재연결된다
- [x] 설정 파일이 git 에 커밋되었는지 확인 (`.mcp.json`, `Packages/manifest.json`)

### 표준 검증 루프

```bash
unity status --no-banner                                   # 붙었나
unity command recompile --no-banner                        # → recompile_status 로 폴링
unity command console --tail 50 --level error --no-banner  # 에러만
unity command run_tests --mode EditMode --no-banner
```

**⚠️ 함정**

- **에디터가 떠 있어야만 동작한다.** Unity를 닫으면 도구 호출이 전부 실패한다. "안 붙는다"고 하면 **가장 먼저 에디터가 켜져 있는지 확인**하라.
- **컴파일 에러가 있으면 에디터가 Safe Mode 로 부팅**하고, 그러면 Pipeline 패키지가 로드되지 않아 **연결 자체가 실패**한다. "CLI 가 고장났다" 로 오해하지 마라. `unity pipeline list` 로 확인하고, 에러를 고친 뒤 Unity 를 재시작하라. 이때 파일 직접 편집으로 도망가지 마라.
- Unity가 **컴파일 중이거나 Import 중이면** 응답이 지연되거나 실패한다. 정상이다. 잠시 후 재시도.
- **`--no-banner` 를 항상 붙여라.** 안 붙이면 매 호출마다 배너 줄이 섞인다.
- **개별 명령의 `--help` 는 없다.** `unity command <이름> --help` 를 치면 `command` 자체의 help 가 나온다. 설명은 `unity command --query <이름> --detail full` 로 본다.
- `unity` CLI 는 **베타**(`1.0.0-beta.6`)다. 인자 이름이 바뀔 수 있다. 실패하면 `--query` 로 현재 시그니처를 다시 확인하라.
- 공식 MCP 패키지는 **AI Assistant 기능 전체**를 같이 들여온다. 에디터 안 채팅 UI는 안 써도 되지만, 패키지를 지우면 MCP도 같이 사라진다.
- Claude Code 는 `.mcp.json` 변경 후 **재시작해야** 새 서버를 인식한다.

**🚫 임의 판단 금지**

- **씬을 마음대로 편집하지 마라.** 태스크에 명시된 것만 한다.
- **C# 스크립트 파일 작성은 기존 파일 도구로 한다.** CLI 의 `create_script` / `write_text_file` 로 소스를 찍어내지 마라.
- 다만 **씬·프리팹 연결은 CLI 로 하는 것이 정석이다.** `.unity` / `.prefab` YAML 을 손으로 만지지 마라.
  (`create_gameobject`, `attach_script`, `set_serialized_field`, `instantiate_prefab`, `save_prefab_contents` …)

---
---

# 단계 1 — 격자와 맵

---

## `[x]` P-03. GridCoord + GridSystem

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

## `[x]` P-04. 맵 저작 — Tilemap 레이어로 셀 타입 그리기

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

## `[x]` P-05. 경로 그래프 저작 도구

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

## `[x]` P-06. 경로 탐색 + 통행 권한

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

## `[x]` P-07. PathFollower — 경로 위를 걷는 로직

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

## `[x]` P-08. GameClock — 배속과 일시정지

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

## `[x]` P-09. Combat 기반 — Health / IDamageable / TargetRegistry / Attacker

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

## `[x]` P-10. Escortee — 보호대상

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

## `[x]` P-11. MotherSpawner — 이동하는 적 스폰 지점

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

## `[x]` P-12. Enemy — 잡몹

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

## `[x]` P-13. Village + 배치 슬롯 선택

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

## `[x]` P-14. AllyUnit — 고용, 행군, 배치

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

## `[x]` P-15. Wallet — 자원과 고용 비용

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

## `[x]` P-16. 지름길 기믹

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

## `[x]` P-17. 승패 UI와 재시작

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

## `[x]` P-18. 디버그 오버레이

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

## `[x]` P-18B. 작업 정리 — 미커밋 변경 커밋 + 부채 청산 (2026-08-27 완료)

**선행:** P-18
**성격:** 필수. **P-19 / P-20 보다 먼저 한다.** 현재 워킹트리에 35개 변경이 커밋 없이 떠 있어서,
뭔가 깨졌을 때 되돌릴 지점이 없다.

**작업:**

1. **부채 1 — 떠도는 씬 삭제**
   `Assets/test.unity` (+ `.meta`) 를 지운다. Assets 최상위이고 빌드 목록에도 없다.
   프로토타입 씬은 `Assets/_Project/Scenes/Stage_Greybox.unity` **하나**다.
   → 지우기 전에 안에 살릴 것이 있는지 한 번 열어 보라 (GameObject 5개짜리다).

2. **부채 2 — `UiSlowScale` 을 SO 로**
   `Session/GameClock.cs` 의 `private const float UiSlowScale = 0.1f;` 를
   `StageDefinition` 의 `_uiSlowMotionScale` 로 옮긴다.
   `GameClock` 은 씬 단일 서비스라 SO 를 직접 들고 있지 않으므로,
   `GameSession` 이 `Definition` 을 읽어 `GameClock` 에 넣어 주는 형태가 맞다.
   → **0.1x 는 감각 수치다.** P-21 에서 튜닝 대상이 되므로 코드에 박아 두면 안 된다.

3. **부채 3 — 테스트가 콘솔을 더럽히지 않게**
   `Tests/PathGraphTests.cs` 의 `Unreachable_ReturnsFalse_EmptyResult` 처리.

   > 📌 현장 결정 (2026-08-27): 문서 처방인 `LogAssert.Expect` 는 **콘솔을 깨끗하게 하지 못한다** (CLI 실측 —
   > 예상 로그여도 에디터 로그 파일에 에러가 그대로 남는다). 그래서 테스트를 고쳐 LogError 자체를 안 내게 했다:
   > 두 노드를 **단방향 엣지**(`a→b`, `bidirectional: false`)로 연결하면 고립 노드가 아니어서 Validate 가 조용하고,
   > `TryFindRoute(b → a)` 는 여전히 도달 불가 → `false + 빈 결과`. 테스트 의미는 동일하게 유지됐다.

4. **커밋**
   기능 단위로 나눠라. 한 덩어리로 묶지 마라.
   - `feat(ui): 좌하단 HUD — 골드/HP/배속 라벨 + 배속 버튼`
   - `feat(deploy): 배치 UI 슬로우모션 0.1x + 하이라이트 조기 표시`
   - `fix(session): 재시작 시 timeScale 을 GameClock 경유로 복구`
   - `fix(ui): 고용 버튼 라벨 누락`
   - `fix(editor): 씬 빌더 — Tilemap 로컬 원점, StageDefinition 폴백, HirePanel 배선`
   - `chore: 주석 오타 정정 / 부채 청산`

**DoD:**
- `git status` 가 깨끗하다
- `Assets/` 최상위에 `_Project` 외 프로젝트 산출물이 없다
- `unity command run_tests --mode EditMode` → 32/32 통과, **콘솔 에러 0개**
- `GameClock` 에 밸런스 상수가 없다

**⚠️ 함정**
- 커밋 전에 **`unity command recompile` → `recompile_status` → `run_tests`** 를 돌려라. 깨진 상태를 커밋하지 마라.
- `.meta` 파일을 손으로 지우지 마라. 씬을 지울 때는 Unity 에디터(또는 `unity command delete_asset`)로 지워야 `.meta` 가 같이 정리된다.

**🚫 임의 판단 금지**
- 이 태스크에서 **새 기능을 넣지 마라.** 정리와 커밋만 한다.
- 슬로우모션 0.1x 라는 **값 자체를 바꾸지 마라.** 옮기기만 한다. 튜닝은 P-21 이다.

---

## `[ ]` P-19. 오브젝트 풀링 (적) — **조건부**

**선행:** P-18B

**전제:** P-18 오버레이에서 동시 생존 적이 40마리를 넘고, 프로파일러에 `Instantiate`/GC 스파이크가 실제로 보일 때만 한다. **안 보이면 이 태스크를 건너뛰고 그렇게 보고하라.**

### 먼저 이걸로 판단하라 (실측 없이 착수 금지)

```bash
unity command set_autotick --enable true --interval_ms 16 --no-banner   # 비포커스에도 틱 유지
unity command editor_play --no-banner
# 한 판 굴린 뒤
unity command get_performance_stats --no-banner    # 프레임 타이밍 / 메모리
unity command capture_game_view --source screen --save_path <경로> --no-banner   # 오버레이 수치 확인
```

- 오버레이의 **동시 생존 적 최댓값**이 40 미만 → **건너뛴다.** 그렇게 기록하고 P-20 으로 간다.
- 현재 밸런스(`모체 스폰 간격 3초`, `적 체력 20`)에서는 40을 넘기 어렵다. **건너뛸 가능성이 높다.**

**작업 (전제를 만족했을 때만):**
- `Scripts/Runtime/Combat/EnemyPool.cs` — 단순 스택 기반 풀
- `Enemy` 에 `OnSpawnedFromPool()` / `OnReturnedToPool()` 추가 — **모든 상태를 초기화**

**DoD:**
- 적 100마리를 연속 스폰/처치해도 GC 할당이 거의 없다
- 재사용된 적의 체력/상태/경로가 이전 생애의 값을 갖지 않는다
- `TargetRegistry` 에 유령이 쌓이지 않는다 (`CountAlive` 로 확인)

**⚠️ 함정**
- **풀링은 `OnEnable`/`OnDisable` 쌍이 안 맞는 버그를 전부 폭발시킨다.** P-09 에서 짝을 맞춰 뒀는지 여기서 검증된다.
- 이벤트 구독 해제를 빠뜨린 적이 풀에서 부활하면 **구독이 2배로 쌓인다.** 데미지가 2배로 들어가는 원인 모를 버그가 된다.
- 이 프로젝트는 **도메인 리로드가 꺼져 있다.** 풀을 `static` 으로 만들면 플레이 종료 후에도 이전 판의 인스턴스가 남는다. `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` 로 반드시 비워라.

---

## `[ ]` P-20. 스테이지 데이터 정리와 통합

**선행:** P-18B (P-19 는 선택)

### 이미 끝난 부분 (2026-08-27 확인)

SO 3종이 만들어져 있고 시작값이 전부 들어가 있다:

| 에셋 | 상태 |
|---|---|
| `Data/Stages/Stage_Greybox.asset` | ✅ 속도 1.2 / HP 100 / 모체 0.8·5초·3초 / 자원 150·8 / 지름길 120 / D-03·D-06 토글 |
| `Data/Units/Ally_Basic.asset` | ✅ 이동 2.5 / HP 50 / 사거리 3.5 / 공격 10·0.8 / 비용 50 |
| `Data/Enemies/Enemy_Basic.asset` | ✅ 이동 2.0 / HP 20 / 사거리 1.0 / 공격 5·1.0 |

값은 P-20 원안의 시작값 표와 일치한다. **이 표는 시작점일 뿐 튜닝 대상이다.**

### 남은 작업

1. **매직 넘버 잔여분 처리.** `grep` 으로 훑은 결과 밸런스성 상수는 두 개뿐이다:
   - `GameClock.UiSlowScale = 0.1f` → **P-18B 에서 이미 옮긴다.** 여기서 재확인만.
   - `Enemy.cs` 의 이탈 사거리 배수 `range * 1.2f` → 전투 감각 수치다. `EnemyDefinition._disengageRangeMultiplier` 로 뺄지 판단하라.
   - 나머지(`snapEpsilon 0.05f`, `dash 0.25f`, 라인 두께 `0.08f`, `Pulse 0.25f`)는 **표현/수치 안정용이므로 SO 로 빼지 마라.** 밸런스가 아니다.

2. **기본값 이중화 점검.** `Escortee.cs` 의 `private float _speed = 1.2f;` 처럼
   SO 값과 같은 폴백이 코드에 박힌 곳들이 있다. SO 를 고쳐도 폴백은 안 따라오므로
   **드리프트가 생긴다.** 폴백을 남기려면 명백히 틀린 값(예: `-1f`)으로 두고
   SO 미할당 시 `LogError` 로 드러내는 편이 낫다.

3. **한 판이 90~150초에 끝나도록 조정.** 맵 길이 또는 보호대상 속도를 만진다.
   이보다 길면 P-21 테스트 회전이 안 돈다.

**DoD:**
- [ ] 코드에 밸런스 매직 넘버가 없다 (`grep` 으로 확인)
- [ ] SO 값만 바꿔서 난이도를 바꿀 수 있다
- [ ] 한 판이 90~150초에 끝난다 ← **실측 필요**
- [ ] 처음부터 끝까지 콘솔 에러/경고 0개로 플레이가 완주된다 ← **실측 필요**

### 실측 방법 (CLI)

```bash
unity command set_autotick --enable true --interval_ms 16 --no-banner
unity command clear_console --no-banner
unity command editor_play --no-banner
# ... 한 판 진행 ...
unity command console --level warning --no-banner   # 경고 이상 0건이어야 한다
unity command editor_stop --no-banner
```

포커스가 없어 플레이가 얼어붙으면 `set_autotick` 이 안 먹은 것이다.
그때는 `EditorApplication.Step()` 을 `eval` 로 돌려라 — Serena 메모리 `unity_playmode_frame_freeze` 참조.

---

# 단계 7 — 시연 가능한 형태 만들기 (G-00 ~ G-21)

> ## 이 단계의 목표는 "재미"가 아니라 **"시연 가능한 퀄리티"** 다
>
> P-20 까지 끝난 뒤 나온 판정을 그대로 옮긴다:
>
> > **"프로토타입을 원한 건 맞지만, 지나치게 프로토타입스러워서
> > 시연할 수 있는 퀄리티가 안 나오는 게 제일 문제다."**
> >
> > **"뭔가 완성되어서 게임의 형태를 갖추었다는 느낌부터가 안 든다.
> > 퀄리티가 너무 낮아서 어디서부터 개선해야 할지 감도 안 올 정도로 미완성이다."**
>
> ### 그래서 합격선이 바뀐다
>
> | | P-21 게이트 (기존) | **단계 7 (여기)** |
> |---|---|---|
> | 판정 대상 | 핵심 루프가 **재미있는가** | 게임이 **완성된 물건으로 보이는가** |
> | 판정자 | 개발자 + 기획자 | **처음 보는 사람** |
> | 기준 | GDD §14 통과 조건 4개 | **남에게 보여줄 수 있는가** |
>
> "그레이박스니까 이 정도면 됐다" 는 이 단계에서 통하지 않는다.
> 도형과 색만 쓰되, **그 안에서 완성도를 끝까지 끌어올린다.**
>
> ### 원인 — 코드 실측으로 특정한 것
>
> | # | 결손 | 근거 | 심각도 |
> |---|---|---|---|
> | 1 | **배치된 유닛을 선택할 수 없다** | `DeploymentController` 에 배치 후 선택 경로 없음 | ★★★ |
> | 2 | **배치된 유닛을 이동시킬 수 없다** | `AllyUnit` 은 `Deployed` 진입 후 상태 전이 없음 | ★★★ |
> | 3 | **병종이 1종뿐이고 정체성이 없다** | `Ally_Basic` 하나. 세계관과 무관한 이름 | ★★★ |
> | 4 | **업그레이드 전략이 통째로 없다** | 해당 시스템 자체가 없음 | ★★★ |
> | 5 | 싸우는지 맞는지 구분이 안 된다 | `Health` 시각 반응 0건, 발사 연출 0건 | ★★★ |
> | 6 | 공격 범위를 볼 수 없다 | `Runtime/UI/` 에 range 코드 0건 | ★★★ |
> | 7 | **일시정지 메뉴가 없다** | 게임을 멈추고 나갈 방법이 없음 | ★★ |
> | 8 | 소리가 하나도 없다 | `AudioSource` 사용처 0건 | ★★ |
> | 9 | UI 가 라벨 나열 수준 | `Canvas` 아래가 Text 3개 + 버튼 4개 | ★★ |
> | 10 | 디버그 오버레이를 읽을 수 없다 | `GUI.Label` 고정 크기, 접기 불가 | ★ |
> | 11 | 스폰 예고 없음 | GDD §7 요구사항 미구현 | ★★ |
> | 12 | 모체가 밋밋하다 | `MotherSpawner.State.Burst` 가 빈 껍데기 | ★★ |
>
> ### 1·2번이 빠진 건 실수가 아니다 — 문서가 금지하고 있었다
>
> 사용자 판정: **"유닛을 배치하고 배치를 이동시킬 수 있는 게 핵심인 게임"**.
> 그런데:
> - `GDD.md` §8: *"`Deployed` — 한 번 배치되면 **스스로 움직이지 않는다.**"*
> - `GDD.md` §13 **D-05**: *"배치된 유닛을 수동으로 회수/이동할 수 있는가? → 프로토타입 기본값 **아니오**"*
>
> → **설계 결정을 뒤집어야 하는 사안**이다. G-00 의 ADR 로 처리한다.

## 여기서부터 시작하라

"어디서부터 손대야 할지 모르겠다" 에 대한 답:

```
G-00 (문서·ADR)  →  G-01 (병종 2종)  →  G-02 (선택)  →  G-03 (이동)
```

이 넷이 끝나면 **이 게임의 정체성과 핵심 동사가 화면에 존재한다.** 나머지는 그 위에 얹는다.
**A 그룹을 건너뛰고 연출·UI 부터 하지 마라.** 조작할 게 없으면 아무리 꾸며도 "완성됐다" 는 느낌은 안 온다.

---

## ⛔ 멈추지 마라 — 이 문서는 물어볼 것이 없도록 쓰였다

**이 단계를 진행하는 동안 사람에게 결정을 물어서 작업을 세우지 마라.**
선택이 갈리는 지점은 전부 **이 문서 안에서 이미 확정**되어 있다. 표에 "확정" 이라고 적힌 것은
권장이 아니라 **결정**이다. 마음에 안 들어도 그대로 하고, 이견은 G-21 에서 기록으로 남겨라.

### 확정 목록 (여기서 다시 묻지 마라)

| 항목 | 확정값 | 어디 |
|---|---|---|
| 이동 비용 모델 | **B안 — 행군 시간 + 쿨다운 3.0초** | G-03 |
| 재배치 쿨다운 종족 차등 | **없음. 둘 다 3.0초로 시작** | G-03 |
| 회수 환불 기준 | **투입 총액**(고용비 + 지불한 업그레이드비) × 0.5 | G-04 · G-05 |
| 업그레이드 적용 방식 | **즉시 적용**(행군 없음), 2단계까지, 공격력·사거리만 | G-05 |
| 사운드 음원 소스 | **절차 생성(코드로 `AudioClip.Create`)** — 외부 에셋 0개 | G-11 |
| 디버그 접기 단축키 | **F6** (F1~F5 는 이미 점유됨) | G-13 |
| 근접이 경로에 안 닿을 때 | **맵 `Buildable` 배치를 고친다. 사거리를 올리지 마라** | G-01 |
| Scout 가 근접을 통과할 때 | **의도된 압박이다.** 근접 사거리를 올리지 마라 | G-18 |
| 적 처치 보상 | **넣지 않는다** (GDD §10) | 단계 7 서문 |
| 블로킹 | **넣지 않는다** (ADR-0003) | 단계 7 서문 |

### 문서에 없는 판단이 필요해지면 — 그때도 멈추지 마라

**그건 문서의 결함이지 네가 멈출 이유가 아니다.** 아래 순서로 스스로 결정하고 진행하라:

1. `CLAUDE.md` → `GDD.md` → `.docs/adr/` 순으로 근거를 찾는다. 있으면 그대로 따른다
2. 없으면 **가장 단순하고 되돌리기 쉬운 쪽**을 고른다 (새 시스템보다 기존 것 재사용, 새 필드보다 기존 필드)
3. 고른 것과 이유를 **해당 태스크 아래에 `> 📌 현장 결정:` 한 줄로 기록**한다
4. 그대로 진행한다. 나중에 G-21 에서 사람이 뒤집을 수 있다

**예외 — 여기서만 멈춘다 (딱 세 가지):**
- 확정된 결정을 **뒤집어야만** 진행할 수 있을 때 (예: 블로킹 없이는 구현 불가라는 판단)
- **외부 에셋·패키지를 새로 도입**해야만 할 때 (CLAUDE.md §4)
- 컴파일 에러·Safe Mode 로 **CLI 가 붙지 않아** 검증이 불가능할 때

그 외에는 묻지 말고 끝까지 간다.

---

## 세계관 — 2026-08-27 확정

> 이건 이 단계에서 처음 확정된 내용이다. **`컨셉기획서.md` 와 `GDD.md` 에 반영하는 것이 G-00 의 일이다.**
> 여기 적힌 건 태스크 수행용 요약이다. 충돌하면 갱신된 GDD 가 이긴다.

- **배경:** 미래. **인류는 멸망했다.** 그 뒤를 잇는 **신인류 = 수인족**.
- **첫 만남 = 프로토타입에 등장하는 두 종족:** **고양이 수인**, **쥐 수인**.
- **적 = 로봇.** 인류가 남긴 기계. 보호대상을 쫓는 것도, 모체가 뱉는 것도 전부 로봇이다.

### 병종 정체성 (G-01 에서 구현)

| | **고양이 수인** | **쥐 수인** |
|---|---|---|
| 이미지 | 민첩하다 | 몸집이 작아 리치로는 못 이긴다 |
| 그래서 | **근접 공격** | **원거리 무기**를 다룬다 |
| 사거리 | **짧다** | **길다** |
| 공격 속도 | **빠르다** | 느리다 |
| 행군 속도 | **빠르다** (민첩) | 느리다 |
| 역할 | 경로 바로 옆에 붙여 빠르게 처리 | 뒤에서 넓게 커버 |

> **⚠️ "근접"이 블로킹을 뜻하는 게 아니다.** ADR-0003(블로킹 없음)은 **유효하다.**
> 고양이 수인도 적의 이동을 막지 않는다. **사거리가 짧을 뿐**이다.
> 적은 그대로 통과해 지나가고, 지나가는 동안 맞는다.
> 블로킹을 넣고 싶으면 ADR-0003 을 뒤집는 별도 ADR 이 필요하다. 이 단계에서 하지 마라.

### 격파 연출은 "부품이 흩어지는" 표현

GDD §12 가 이미 정해 뒀다: *"로봇 격파 연출은 폭발이 아니라 **부품이 흩어지는** 표현(톤 유지)."*
적이 로봇으로 확정됐으니 이 규정이 그대로 적용된다 → **G-08**.

---

## 이 단계에서 뒤집는 기존 결정 (전부 G-00 의 ADR 대상)

| 항목 | 기존 문서 | 변경 | 근거 |
|---|---|---|---|
| **배치 유닛 이동/회수** | GDD §8·§13 D-05 = **아니오** | **예 — 핵심 조작으로 승격** | "그게 핵심인 게임" |
| **업그레이드** | GDD 에 없음 | **도입 — 전략 요소** | "비용을 써서 유닛 업그레이드하는 전략이 누락됐다" |
| **병종 다양화** | 부록 = P-21 후 | **2종 즉시 — 세계관의 일부** | 종족이 게임 컨셉의 핵심이 됨 |
| 사운드 | 부록 = 범위 밖 | **최소 세트 도입** | "가벼운 사운드 정도는 넣어봄직하다" |
| 버스트 모드 | GDD §7 = 범위 밖 | **구현** | 모체는 "유일한 차별점" |

### ⚠️ 반대로, 뒤집지 **않는** 것 두 가지

**1. 처치 보상** — 초안에 넣었다가 **뺐다.** GDD §10 이 근거를 들어 배제한다:
> *"적 처치 보상은 프로토타입 범위 밖. (넣으면 **일부러 적을 흘려보내는** 최적화가 생겨 검증이 오염됨)"*

업그레이드(G-05)가 새 자원 소비처가 되므로 경제는 그쪽으로 푼다.

**2. 블로킹 없음 (ADR-0003)** — 고양이 수인이 근접이어도 유효하다. 위 경고 박스 참조.

### 그래도 유지되는 금지

유닛 등급/승급 · 스탠딩 일러스트 · 대화 시스템 · 세이브/로드 · 메타 진행 ·
스테이지 복수화 · 쿼터뷰 카메라 · 데이터 파이프라인 · 스팀 연동. 부록의 나머지는 유효하다.

---

## 공통 제약 (전 태스크 적용)

- **물리 금지 유지.** 클릭·hover·사거리 판정은 순수 수학 (ADR-0005).
- **`ParticleSystem` / `Animator` / `Timeline` 금지 유지.** 연출은 코드 보간(`SpriteRenderer.color`, `localScale`, `LineRenderer`).
- **런타임 UI 는 uGUI** (ADR-0006).
- **좌표 변환은 `GridSystem` 안에서만** (CLAUDE.md §4).
- **아트는 여전히 도형과 색.** 스탠딩·일러스트 금지. 단 **종족이 색과 실루엣으로 구분**되어야 한다.
- **새 수치는 전부 SO.**
- **도메인 리로드 OFF.** `static` 은 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` 로 비우고, `OnEnable` 구독은 `OnDisable` 해제.
- 태스크마다 `unity command run_tests --mode EditMode` 로 **기존 32개 통과** 확인.
- **씬/프리팹 연결까지가 완료다.**

---

## 순서

```
G-00  문서 갱신 + ADR 5장           결정과 세계관 먼저

──── A. 정체성과 핵심 조작 (★★★ 이 단계의 전부) ────
G-01  병종 2종 — 고양이(근접) / 쥐(원거리)   ← "일단 이것부터"
G-02  유닛 선택
G-03  유닛 재배치 (이동)                     ← 이 게임의 핵심 동사
G-04  유닛 회수 / 환불
G-05  업그레이드 시스템                      ← 누락됐던 전략 축

──── B. 전투가 보이게 (★★★) ────
G-06  사거리 표시
G-07  사격 연출 — 싸우는 게 보인다
G-08  피격·격파 연출 — 로봇 부품이 흩어진다
G-09  체력바 · 위협선
G-10  스폰 예고

──── C. 소리 (★★) ────
G-11  사운드 최소 세트

──── D. UI 고도화 (★★) ────
G-12  일시정지 메뉴 (우상단)                 ← 시연에 반드시 필요
G-13  디버그 오버레이 개편 (접기 + 가독성)
G-14  HUD 재설계
G-15  유닛 정보 패널
G-16  고용 패널 정보 표시

──── E. 적과 리듬 (★★) ────
G-17  스폰 테이블 (데이터화)
G-18  로봇 2종째
G-19  스폰 리듬 (묶음·휴지)
G-20  버스트 모드

──── F. 마감 ────
G-21  밸런스 재조정 + 시연 검증 패스
```

---

## `[x]` G-00. 문서 갱신 + ADR 5장 (2026-08-27 완료 — 컨셉기획서 세계관 절/PDF 재생성, GDD §2·§8·§10·§13, ADR-0008~0012, 부록 표시는 기존 반영 확인)

**목표:** 코드를 쓰기 전에 **세계관과 뒤집는 결정**을 문서에 박는다.
지금 확정된 세계관이 어디에도 안 적혀 있어서, 이대로 두면 다음 세션에 사라진다.

**갱신할 문서:**

| 파일 | 무엇 |
|---|---|
| `.docs/컨셉기획서.md` | **세계관 절 신설** — 인류 멸망 → 신인류(수인족) → 고양이·쥐 수인의 첫 만남 → 적은 로봇. 갱신 후 `pwsh .docs/build-pdf.ps1` |
| `.docs/GDD.md` §2 용어표 | 종족·병종 용어를 코드 식별자와 1:1로 (`CatFolk`, `RatFolk`, `Robot`) |
| `.docs/GDD.md` §8 | 생명 주기에 `Deployed → Marching` **재진입** 추가 |
| `.docs/GDD.md` §13 | **D-05 를 "결정됨: 예"** 로 |
| `.docs/GDD.md` §10 | 자원 소비처에 **업그레이드** 추가 |
| `.docs/TASKS-P1-prototype.md` 부록 | 앞당긴 항목에 "→ 단계 7" 표시 |

**만들 ADR:**

| 파일 | 내용 |
|---|---|
| `ADR-0008-redeploy-is-core.md` | **D-05 뒤집기.** 선택·이동·회수를 핵심 조작으로 승격 |
| `ADR-0009-unit-upgrade.md` | 업그레이드 도입. 자원의 두 번째 소비처 |
| `ADR-0010-species-identity.md` | 수인족 세계관과 병종 2종의 역할 분담 |
| `ADR-0011-minimal-audio.md` | 사운드 최소 세트. 범위와 음원 소스 방침 |
| `ADR-0012-demo-quality-gate.md` | 단계 7 자체. 합격선이 "시연 가능한가" 로 바뀐 이유 |

**ADR-0008 에 반드시 담을 것 (가장 중요):**
- 맥락: GDD §8 + §13 D-05 가 "아니오" 였다
- 뒤집는 이유: 배치와 재배치가 이 게임의 핵심 동사다
- **부작용 (여기가 핵심):** 재배치가 되면 GDD §4 의 긴장 구조가 바뀐다.
  원래 재배치 비용 = *자원 + 행군 시간* 인데, 이동이 가능하면 **자원 없이 행군 시간만** 내고 옮길 수 있다.
  → **이동은 공짜가 아니어야 한다.** G-03 에서 비용 모델을 정하고 여기 기록한다

**ADR-0010 에 반드시 담을 것:**
- **근접(고양이)이 블로킹을 뜻하지 않는다**는 것. ADR-0003 은 유효하다
- 두 종족의 역할 축이 "사거리 vs 회전율" 이라는 것

**DoD:**
- 컨셉기획서에 세계관 절이 있고 PDF 가 재생성되었다
- GDD §2·§8·§10·§13 이 갱신되었다
- ADR 5장이 있다
- 부록에서 앞당긴 항목이 표시되었다

**🚫 임의 판단 금지**
- 서사·대사·스탠딩을 쓰지 마라. **세계관 설정과 병종 역할까지**다. 서사는 기획자 영역이다.
- 이 ADR 로 다른 부록 항목을 풀지 마라. 처치 보상을 슬쩍 끼워 넣지 마라.

---

# A. 정체성과 핵심 조작

## `[x]` G-01. 병종 2종 — 고양이 수인(근접) / 쥐 수인(원거리) (2026-08-27 완료)

> 📌 현장 결정 (2026-08-27): **근접 사거리 1.6 은 원래 맵에서 경로에 전혀 닿지 않았다** — 경로 147 샘플 중 126 미커버
> (최근접 Buildable 이 도로에서 2칸 거리). 확정 처방 ① 대로 **맵 Buildable 을 도로에 인접하게 확장**했다
> (BuildableA/B/C 확장 + D/E 신규, 52→97칸, 도로 타일은 덮지 않음). 재실측 결과 **147/147 커버**. 사거리는 1.6 유지(올리지 않음).
> 소스는 `GreyboxMapData.cs` 와 씬 Tilemap_Buildable 양쪽에 반영했다.

> 📌 현장 결정 (2026-08-27): 쥐 수인의 색·실루엣 — 그레이박스 색상 규약의 파랑(아군)과 구분되게
> **Unity 기본 원형 스프라이트(Knob) + 청록**(행군 0.45/0.90/0.80, 배치 0.00/0.50/0.40), 스케일 0.4("몸집이 작다").
> 고양이 수인은 기존 규약 유지: 사각 + 연한/진한 파랑.

**검증 (CLI 플레이 모드 실측):** 마을 3개 모두 `고양이 수인/45 | 쥐 수인/110` 완비.
실제 고용 경로(DeploymentController)로 두 종족 각 1회 고용 →
고양이: 배치됨, range 1.6 / dmg 7 / interval 0.45 / hp 50 / 진한 파랑.
쥐: range 6 / dmg 26 / hp 40 / 청록 원형 — 판이 39.1초에 Victory 로 끝나(timeScale=0) 행군 중 정지까지 확인.
EditMode 32/32 통과, 컴파일 에러 0.

**선행:** G-00
**왜:** 사용자 지시: **"일단 2가지 병종부터 먼저 만들어봐."**
지금은 `Ally_Basic` 하나뿐이라 고용 패널 버튼이 **1개**다. 그건 선택이 아니라 확인이다.
게다가 이름과 형태에 **세계관이 전혀 안 실려 있다** — 시연에서 "이게 무슨 게임인지" 가 안 읽히는 큰 이유다.

**만들 파일:**
- `Data/Units/Ally_CatFolk.asset` — 고양이 수인 (기존 `Ally_Basic` 을 **개명·재조정**해서 쓴다)
- `Data/Units/Ally_RatFolk.asset` — 쥐 수인 (신규)
- 프리팹 2개 (기존 아군 프리팹 **변형**)

**시작값 — 축은 "사거리 vs 회전율"이다:**

| 항목 | **고양이 수인 (근접)** | **쥐 수인 (원거리)** |
|---|---|---|
| 사거리 | **1.6** | **6.0** |
| 공격력 | **7** | **26** |
| 공격 간격 | **0.45** | **1.8** |
| DPS | 15.6 | 14.4 |
| 행군 속도 | **3.2** (민첩) | **1.8** |
| 체력 | 50 | 40 |
| 고용 비용 | **45** | **110** |

> DPS 를 일부러 비슷하게 맞췄다. **선택의 축은 세기가 아니라
> "경로에 붙여 빠르게 도는 근접" vs "뒤에서 넓게 커버하는 원거리"** 다.
> 고양이가 빠르게 행군한다는 것은 **재배치(G-03)에 유리하다**는 뜻이기도 하다 — 정체성이 조작으로 이어진다.

**작업:**
1. `Ally_Basic` → `Ally_CatFolk` 로 **개명**한다. `unity command rename_asset` 을 쓰면 GUID 가 유지된다.
   **파일을 새로 만들고 옛것을 지우지 마라** — 프리팹·씬 참조가 전부 끊어진다
2. `Ally_RatFolk` 신규 + 프리팹 변형. **색과 실루엣으로 구분**되게 한다 (그레이박스 색상 규약 — GDD §12)
3. 씬의 `Village_1~3` `_hireableUnits` 에 **둘 다** 넣는다
4. `HirePanel` 은 이미 `HireableUnits` 를 순회한다 — **코드 변경이 없어야 정상**이다. 필요하면 그게 버그다

**DoD:**
- 고용 패널에 버튼이 2개 뜬다
- 두 종족이 화면에서 **색과 형태로 구분**된다
- 근접이 실제로 짧은 사거리에서 빠르게 때린다
- **마을 3개 모두**에서 두 종류를 고용할 수 있다
- `AffordabilityTint` 가 비싼 쪽만 비활성으로 표시한다
- 기존 씬·프리팹 참조가 하나도 끊기지 않았다 (개명 후 콘솔 에러 0)

**⚠️ 함정**
- **개명 시 GUID 를 잃으면 씬·프리팹의 참조가 전부 `Missing` 이 된다.** `rename_asset` 을 쓰고, 끝나면 콘솔을 확인하라.
- 마을 3개 `_hireableUnits` 를 **전부** 채웠는지 확인하라. 하나 빠뜨리면 "어떤 마을은 쥐가 안 나오네" 라는 유령 사양이 생긴다.
- **근접 사거리 1.6 은 셀 단위다.** 경로 바로 옆 `Buildable` 칸에서 경로에 닿는지 맵에서 실제로 확인하라.
  안 닿으면 근접 병종이 통째로 무용지물이 된다 ← **이 태스크에서 가장 위험한 지점**
  → **안 닿을 때의 처방은 확정되어 있다:**
  **① 맵의 `Buildable` 배치를 고쳐 경로에 인접한 칸을 만든다** (`Tilemap_Buildable` 편집).
  **② 사거리를 올리지 마라.** 정 안 되면 **2.4 가 절대 상한**이고, 그 이상은 원거리와 구분이 사라진다.
  ①로 해결하고, 올렸다면 그 사실과 값을 `> 📌 현장 결정:` 으로 기록하라.
- 행군이 빠른 근접은 도착 판정이 배속에서 튈 수 있다. `PathFollower` 의 도착 이벤트를 쓴다.

**🚫 임의 판단 금지**
- **병종은 2종까지다.** 3종째, 등급, 승급 금지.
- 광역·둔화·버프·관통 같은 새 메커니즘 금지. `Attacker` 를 고치는 순간 범위를 벗어난 것이다.
- **근접이라고 블로킹을 넣지 마라.** ADR-0003 은 유효하다.

---

## `[x]` G-02. 유닛 선택 (2026-08-27 완료)

**검증 (CLI 플레이 모드 실측):** 고용된 유닛 셀 판정(`FindAllyAt`) 일치, `Select` → 링 활성 + 링 위치 = 유닛 위치,
`Clear` → 해제 + 링 비활성. EditMode 32/32 통과, 컴파일 에러 0.
맵 클릭 입력 우선순위 4단계는 `SelectionController` 클래스 주석과 TASKS 양쪽에 기록. 1)단(일시정지 메뉴)은 G-12 가 채울 자리.

**선행:** G-01
**왜:** 배치된 유닛을 **클릭할 수 없다.** 선택이 안 되니 이동도, 업그레이드도, 회수도, 정보 확인도
붙을 자리가 없다. **G-03·G-04·G-05·G-15 의 공통 전제다.**

**만들 파일:**
- `Runtime/Session/SelectionController.cs`
- `Runtime/UI/SelectionRing.cs` — 선택된 액터 발밑 링 (`LineRenderer`)

**작업:**
1. 좌클릭 → 클릭 지점의 셀을 `GridSystem` 으로 구해 그 셀의 아군을 찾는다. **순수 수학** (`Collider2D` 금지)
2. `OnSelectionChanged` 이벤트를 노출한다. G-03·G-05·G-15 가 여기 붙는다
3. 빈 곳 클릭 또는 `Esc` → 해제
4. 선택 시 `SelectionRing` + **사거리 원**(G-06)
5. **입력 우선순위를 명시적으로 정한다.** 현재 `DeploymentController` 가 클릭을 먹는다:
   ```
   1) 일시정지 메뉴가 떠 있으면 → 게임 입력 전부 차단   (G-12)
   2) 배치 모드 진행 중이면      → DeploymentController
   3) 지름길 마커 근처면         → ShortcutController
   4) 그 외                      → SelectionController
   ```
   → 이 순서를 **코드 주석과 이 문서 양쪽에** 남겨라. 반드시 헷갈린다

**DoD:**
- 배치된 아군을 클릭하면 링이 뜬다
- 다른 아군을 클릭하면 선택이 옮겨간다 (둘 다 선택되지 않는다)
- 빈 곳 클릭 / `Esc` 로 해제된다
- 고용 흐름이 **기존과 똑같이** 동작한다
- 행군 중인 유닛도 선택된다

**⚠️ 함정**
- **`DeploymentController` 와 클릭 충돌**이 1순위 버그다. 위 우선순위를 안 정하면 마을을 못 고르거나 유닛을 못 고른다.
- 신 입력만: `Mouse.current.leftButton.wasPressedThisFrame`, `Mouse.current.position.ReadValue()` (CLAUDE.md §3).
- `Camera.main` 은 `Awake` 캐시.
- 선택된 유닛이 죽거나 회수되면 **선택 해제**하라. 파괴된 참조를 들고 있으면 다음 프레임에 터진다.
- 씬 단일 서비스는 4개만 허용(CLAUDE.md §4)이다. `SelectionController` 를 싱글턴으로 만들지 마라 — `[SerializeField]` 주입.

**🚫 임의 판단 금지**
- 다중 선택(드래그 박스) 금지. **한 번에 하나**다.
- 타겟 수동 지정 금지. 타겟은 자동이다 (GDD §8).

---

## `[x]` G-03. 유닛 재배치 — 이 게임의 핵심 동사 (2026-08-27 완료)

**검증 (CLI 플레이 모드 실측):**
- Deployed → Marching 재진입 후 실제 걸어감 확인 (pos (-10.5) → (-8.5) 진행, 순간이동 없음), 도착 시 사격 재개(attacker on)
- 행군 중 재명령(쿨다운 경과 후) → 현재 위치에서 재루팅 ✓
- 쿨다운 3.0초 (SO `UnitDefinition._redeployCooldown`, 양쪽 종족 동일 — 에셋이 기본값 상속 실측 `cd=3`)
- 쿨다운 중 재명령 → 거부 + 화면 표시(`RedeployCooldownNotice` TextMesh "이동 N.N초 후") + 콘솔 로그
- 슬롯 예약: 3연속 재배치 후에도 `_reservedSlots` 가 항상 **현재 목표 1개만** 유지됨

> 🐛 **실측 중 발견·수정한 버그:** 첫 구현의 `HandleLeftSlot` 이 이벤트 구독을 풀어버려서 재배치 2회째부터
> 이전 슬롯 예약 해제가 누적 누락되었다 (`_reservedSlots`에 옛 슬롯 잔존). 구독을 유지하도록 수정 —
> 함정 목록의 "슬롯 예약 해제 누락"이 실제로 터진 사례.

> 📌 현장 결정: 재배치 경로의 첫 노드는 지시서대로 `FindNearestNode(현재 위치)` 를 쓴다. 슬롯은 그래프 밖
> (경로 옆 Buildable)이라 첫 노드와의 거리가 `PathFollower` 의 경고 임계(0.05)를 넘어 **LogWarning 이 남는다** —
> 이동은 정상 진행되는 기존 로그 성격이므로(고용 행군·모체도 동일) 임계를 건드리지 않았다. G-21 에서 재검토 가능.

**선행:** G-02
**왜:** 사용자 판정 그대로 — **"유닛을 배치하고 배치를 이동시킬 수 있는 게 핵심인 게임"**.
지금은 한 번 배치하면 끝이다. 모체가 이동해 배치가 무의미해져도 **할 수 있는 게 없다.**
GDD §4 의 "모체 이동 → 기존 배치 무의미 → 다시 배치" 루프에서 **마지막 고리가 빠져 있다.**

**고칠 파일:** `Runtime/Actors/AllyUnit.cs`, `Runtime/Session/DeploymentController.cs`

**작업:**
1. 생명 주기 확장 — `Deployed` 에서 `BeginMarch(newSlot)` 재진입 가능하게:
   ```
   Queued → Marching → Deployed → (Marching) → Deployed
   ```
2. 선택된 유닛이 있을 때 배치 가능 칸을 하이라이트, 클릭하면 재행군
3. **행군 중에는 공격하지 않는다** (GDD §8 유지). 이것이 이동의 실질적 비용이다
4. 이동 중 재명령 시 **현재 위치에서** 경로를 다시 잡는다.
   `PathFollower.SetRoute(route, startPosition)` 가 이미 중간 호출을 지원한다 — 그걸 쓴다
5. 원래 슬롯 예약을 **반드시 해제**하라

**이동 비용 모델 — ADR-0008 에서 결정한 것을 구현한다**

무료로 두면 GDD §4 의 긴장이 무너진다. → **B안으로 확정한다. 다시 고민하지 마라.**

| 안 | 내용 | 판정 |
|---|---|---|
| A | 완전 무료 | 재배치 압박 소멸. **기각** |
| **B** | **행군 시간만 + 이동 쿨다운** | 자원 압박은 고용에, 시간 압박은 이동에 분리. **✅ 확정** |
| C | 고용 비용의 N% 지불 | 조작이 무거워 "핵심 동사" 답지 않음. **기각** |

**추가할 SO 필드:** `UnitDefinition._redeployCooldown` (B안 기준 **3.0**초)
→ **두 종족 모두 3.0초로 시작한다. 종족별 차등을 지금 넣지 마라.**
고양이는 행군 자체가 빨라서(3.2 vs 1.8) 이미 재배치에 유리하다 — 쿨다운까지 차등하면 이중으로 벌어진다.
차등이 필요한지는 G-21 에서만 검토한다.

**DoD:**
- 배치된 유닛을 선택하고 다른 칸을 클릭하면 그리로 걸어간다
- 도착하면 다시 사격한다 / 행군 중에는 사격하지 않는다
- 원래 슬롯이 즉시 비고 다른 유닛이 그 자리에 배치된다
- 이동 중 재명령 시 순간이동 없이 경로가 바뀐다
- 쿨다운 중에는 이동이 거부되고 그 사실이 화면에 표시된다
- **모체가 이동한 뒤 방어선을 옮겨 대응하는 플레이가 실제로 가능하다** ← 이 단계의 최종 확인점

**⚠️ 함정**
- **슬롯 예약 해제 누락**이 1순위 버그다. 이동·회수·사망 **세 경로 전부**에서 확인하라.
- `Deployed ↔ Marching` 재진입 시 `TargetRegistry` 등록/해제 짝을 맞춰라. 안 맞으면 걸어가며 쏘거나 도착해도 안 쏜다.
- `PathFollower.SetRoute` 는 `route[0]` 이 현재 위치와 멀면 `LogWarning` 을 찍는다. 재배치 경로의 첫 노드를 **현재 위치에서 가장 가까운 노드**로 잡아라.
- 아군 행군은 **지름길 엣지를 쓸 수 없다** (GDD §6 표). 통행 권한을 넘기고 있는지 확인하라.
- 도착 판정을 거리로 하면 배속 4x 에서 지나친다. `PathFollower` 도착 이벤트를 쓴다.

**🚫 임의 판단 금지**
- **순간이동 배치 금지.** 행군은 ADR-0001 의 핵심이다.
- 이동 중 사격 금지 (GDD §8).
- 드래그 앤 드롭 금지. **클릭 → 클릭**이다.

---

## `[x]` G-04. 유닛 회수 / 환불 (2026-08-27 완료)

**검증 (CLI 플레이 모드 실측):**
- 배치된 유닛 회수 → 환불 22 (= floor(45 × 0.5), SO `_refundRatio=0.5` 기본값 상속 실측) 지급, 슬롯 예약 즉시 해제(재사용 가능), 후퇴 연출(축소+페이드, unscaled) 뒤 소멸
- **행군 중 회수** ✓ — 목표 슬롯 예약도 즉시 해제
- `TargetRegistry` 해제 (`SetRegistryEnabled(false)`) + 격파와 다른 후퇴 톤 (GDD §8)
- 무한 이득 없음: 고용 45 → 환불 22 (−23 확정)

> 📌 현장 결정: 회수 단축키 = **R** (문서에 미확정. F1~F5 점유, F6 = 디버그 접기이므로 피함). 버튼은 G-15 정보 패널에 붙인다.

**선행:** G-03
**왜:** 이동만 되고 되돌릴 수 없으면 배치 실수가 영구 손실이다.
GDD §13 **D-01**("남겨진 유닛을 어떻게 하나")의 답도 회수가 있으면 달라진다.

**작업:**
1. 선택된 유닛에 회수 명령 (버튼 + 단축키) → `Retired`
2. **환불률**을 SO 로. 전액 환불이면 배치가 무위험 도박이 된다
3. 슬롯 예약 해제 + `TargetRegistry` 해제 + **격파와 다른 연출**(후퇴 느낌)
4. UI 는 G-15 정보 패널에 붙인다. 그 전까지는 단축키로 충분하다

**추가할 SO 필드:** `UnitDefinition._refundRatio` (0.5)

**DoD:**
- 회수하면 사라지고 자원이 일부 돌아온다 / 슬롯이 재사용 가능하다
- 환불액이 SO 값과 일치한다
- 행군 중인 유닛도 회수된다

**⚠️ 함정**
- **고용→회수 반복 무한 이득**이 나지 않는지 확인하라. `_refundRatio < 1` 이면 안전하다.
- 회수 연출을 격파와 같게 만들지 마라. 톤이 무너진다 (GDD §8: "사망이 아니라 후퇴/탈진").
- **환불 기준은 확정되어 있다: 투입 총액 × `_refundRatio`.**
  투입 총액 = 고용비 + 그 유닛에 지불한 업그레이드 비용 전부.
  고용비만 기준으로 하면 강화한 유닛을 회수할 때 손해가 커져 **아무도 업그레이드를 안 쓰게 된다.**
  → `AllyUnit` 이 투입 총액을 런타임 상태로 누적해야 한다 (G-05 와 같이 구현).

---

## `[x]` G-05. 업그레이드 시스템 (2026-08-27 완료)

**검증 (CLI 플레이 모드 실측, 고양이 수인):**
- 3회 연속 강화 시도 → Lv2(60) → Lv3(100) → 3차는 "이미 최대 티어" 거부. wallet 417 → 257 (−160, 3차 차감 없음)
- 강화 후 실제 능력 반영: dmg 7→11→16, range 1.6→1.8→2.0
- 티어 시각 구분: 크기 보간 (Lv3 = 초기 스케일 ×1.24)
- **티어 유지**: 재배치 후에도 lv=2, dmg 16 유지
- **환불 = 투입 총액 × 0.5**: 강화 포함 회수 → refund 102 = floor(205 × 0.5) (고용 45 + 강화 160). 무한 이득 없음(−103)
- **SO 오염 없음**: 플레이 후 `.asset` 파일 변화 없음 (git diff로 확인 — 변경은 사전 설정한 tiers 정의 +9줄뿐)

> 📌 현장 결정: 강화 단축키 = **U** (버튼 비활성 표시는 G-15 정보 패널이 담당). 티어 시각 구분은 크기(+12%/단계)로.

**선행:** G-04
**왜:** 사용자 지적: **"비용을 써서 유닛 업그레이드하는 전략도 구현 과정에서 누락되어 있다."**
그리고 **처치 보상을 넣지 않기로 한 만큼**(GDD §10) 자원의 두 번째 소비처가 필요하다.
현재 소비처는 고용과 지름길뿐이라 중반 이후 자원이 남으면 할 일이 없다.

> **프로토타입에서는 가장 단순한 형태로 간다 (ADR-0009):**
> - 배치된 유닛 1기를 **그 자리에서** 강화. 새 유닛 종류를 만들지 않는다
> - **2단계까지.** Lv1 → Lv2 → Lv3
> - 강화 축은 **공격력과 사거리 둘만.** 새 메커니즘 금지
> - **즉시 적용** (행군 없음). 시간 비용이 없는 대신 자원 비용이 크다
>   → 이것이 "새로 고용해서 보낼까 / 있는 걸 키울까" 라는 **전략적 선택**을 만든다

**작업:**
1. `UnitDefinition` 에 티어 배열: 각 티어가 `_cost`, `_attackDamage`, `_attackRange`
2. `AllyUnit` 이 현재 티어를 **런타임 상태로** 갖는다.
   **⚠️ SO 를 런타임에 수정하지 마라** (CLAUDE.md §4) — SO 는 읽기 전용 정의다
3. 정보 패널(G-15) 또는 단축키 → 자원 차감 → 티어 상승 → `Attacker` 갱신
4. 티어를 **시각적으로 구분**한다 (크기 또는 테두리 색). 안 보이면 강화한 보람이 없다
5. 사거리가 늘면 G-06 사거리 원도 같이 커져야 한다

**추가할 SO 필드:** `UnitDefinition._tiers` (배열: cost / attackDamage / attackRange)

시작값 (종족별로 다르게 — 정체성 유지):

| | Lv2 | Lv3 |
|---|---|---|
| **고양이** | 비용 60 · 공격 11 · 사거리 1.8 | 비용 100 · 공격 16 · 사거리 2.0 |
| **쥐** | 비용 140 · 공격 38 · 사거리 6.8 | 비용 230 · 공격 54 · 사거리 7.5 |

**DoD:**
- 배치된 유닛을 2단계까지 강화할 수 있다
- 자원이 차감되고 부족하면 버튼이 비활성이다
- 강화 후 실제로 더 세고 멀리 쏜다 (사거리 원으로 확인)
- 티어가 화면에서 구분된다
- **SO 에셋 파일이 플레이 후에도 변하지 않는다** ← 반드시 확인
- 최대 티어에서 버튼이 비활성이다
- 강화된 유닛을 이동시켜도 티어가 유지된다

**⚠️ 함정**
- **ScriptableObject 런타임 수정이 1순위 함정이다.** 에디터에서 값이 영구 변경되어 밸런스가 조용히 오염된다. 가변 상태는 `AllyUnit` 인스턴스가 갖는다.
- 도메인 리로드 OFF 라 오염이 **다음 플레이까지 남는다.** 플레이 후 `.asset` 을 열어 확인하라.
- **환불 기준은 투입 총액 × `_refundRatio` 로 확정되어 있다** (G-04 참조).
  `AllyUnit` 이 `_totalInvested` 를 런타임 상태로 누적하라. **SO 에 넣지 마라.**
  `_refundRatio = 0.5 < 1` 이므로 고용→강화→회수 반복으로 이득이 나지는 않는다. 그래도 한 번 확인하라.

**🚫 임의 판단 금지**
- 유닛 등급/승급 체계 금지. **수치 강화 2단계**가 전부다.
- 마을 업그레이드·전역 업그레이드·연구 트리 금지.

---

# B. 전투가 보이게

## `[x]` G-06. 사거리 표시 (2026-08-27 완료)

**검증 (CLI 플레이 모드 실측):**
- 선택된 유닛의 사거리 원: 반지름 1.80 == `Attacker.Range` 1.8 **정확 일치** (미리보기가 거짓말하지 않음 — 반지름 출처를 정의가 아닌 Attacker 로 강제). 중심 = 유닛 위치 추적
- 강화 후 원이 커짐: Lv2 range 1.8 → 원 1.80 확인 (Lv1 1.6 대비 성장)
- `LineRenderer` sortingLayer=Deploy / order=6 → 타일에 안 가려짐, 세그먼트 48
- 색 구분: 선택 = 아군 색(파랑), 배치 전·재배치 목적지 hover = 노랑 (`DeploymentController.UpdateRangeHover` — 고용 슬롯 hover / 재배치 목적지 hover 담당, 고용 패널 버튼 hover 는 G-16)

**선행:** G-02
**왜:** `Runtime/UI/` 에 사거리 코드가 **0건**이다. 근접 1.6 과 원거리 6.0 의 차이가
**화면에서 안 보이면 G-01 의 병종 설계 자체가 전달되지 않는다.**

**만들 파일:** `Runtime/UI/RangeCircle.cs` — `LineRenderer` 원 (세그먼트 48)

**작업:**
1. 중심·반지름·색을 받아 원을 그린다. `Show()` / `Hide()`
2. **표시 시점 4곳:** 슬롯 hover(고를 유닛의 사거리) / 선택된 유닛 / 재배치 목적지 hover / 고용 패널 버튼 hover(G-16)
3. 색 구분: 배치 전·재배치 = 노랑, 선택 = 아군 색

**추가할 SO 필드:** 없음 — 반지름은 `UnitDefinition.AttackRange` (티어 반영값)

**DoD:**
- 슬롯 hover 시 그 자리의 사거리가 보인다 / 선택된 유닛의 사거리가 보인다
- **원 안에 들어온 적이 실제로 피격된다** (미리보기가 거짓말하지 않는다)
- 고양이와 쥐의 원 크기 차이가 한눈에 다르다
- 업그레이드로 사거리가 늘면 원도 커진다
- 원이 타일맵에 가려지지 않는다

**⚠️ 함정**
- 반지름 출처가 `Attacker` 가 쓰는 값과 같은지 확인하라. 다르면 미리보기가 거짓말이 된다.
- 원의 중심은 `GridSystem` 이 준 **셀 중심**이다 (CLAUDE.md §4).
- `LineRenderer` 의 `sortingOrder` 를 안 주면 타일 밑에 깔린다.

---

## `[x]` G-07. 사격 연출 — 싸우고 있는 게 보인다 (2026-08-27 완료)

**검증 (CLI 플레이 모드 실측):**
- 고양이(근접) 발사 → **호(13 포지션) 파랑 선**, from 유닛 → to 타겟 (`HandleFired` 반사 호출로 검증)
- 적(로봇) 발사 → **직선(2 포지션) 빨강 선** — 아군 사격과 색으로 구분 ✓
- 액터당 ShotLine 1개 재사용 (자식 GO), 매 발사 생성 없음, 투사체 없음 (히트스캔 유지)
- `_shotLineSeconds=0.07` (SO, 게임시간 기준) — **timeScale=0(승리 동결)에서 라인 유지 = 일시정지 규약대로** 실측
- 대상 사망 시 즉시 지움 (`IsAlive` 검사)

> 📌 현장 결정: 근접/원거리 판정은 `Attacker.Range < 3` (고양이 최대 2.0 / 쥐·로봇 6.0+ — 중간값 없음). SO 에 종족 플래그를 새로 넣지 않았다.
> 📌 검증 환경 메모: 한 판이 39.1초에 승리로 끝나 교전 자체가 드물다 — 발사 연출은 `Attacker.OnFired` 구독 경로 + 합성 호출로 검증. 실전 빈도는 G-17~G-20 (스폰 테이블·리듬) 이 바꾼다.

**선행:** G-06
**왜:** 사용자 판정: **"이펙트가 없으니까 싸우고 있는 건지 공격을 받는 건지 구분도 안 간다."**
`Attacker` 는 숫자만 깎는다. 화면에서는 **아무 일도 일어나지 않는다.**

**만들 파일:** `Runtime/UI/ShotLine.cs`
**고칠 파일:** `Runtime/Combat/Attacker.cs` (발사 이벤트 노출)

**작업:**
1. `Attacker` 에 발사 이벤트 추가. **`Update` 에서 UI 를 직접 만지지 말고 이벤트로 분리**
2. `ShotLine` : `_shotLineSeconds` 동안 선을 그렸다 지운다
3. **종족별로 다르게** — 쥐(원거리)는 긴 선, 고양이(근접)는 짧은 호/번쩍임. 정체성이 전투에서도 읽혀야 한다
4. 아군 사격 = 아군 색 / 적 공격 = 붉은색
5. 액터당 하나를 **재사용**한다. 매 발사마다 생성 금지
6. 대상이 도중에 죽으면 선을 즉시 지운다

**추가할 SO 필드:** `StageDefinition._shotLineSeconds` (0.07)

**DoD:**
- 아군이 쏘는 순간 대상까지 선이 번쩍인다
- 적이 보호대상을 때리는 것도 보인다
- 아군 사격과 적 공격이 색으로 구분된다
- 근접과 원거리의 연출이 다르다
- 적 40마리 교전에서 프레임이 눈에 띄게 떨어지지 않는다
- 배속 4x·일시정지에서 정상 동작한다

**⚠️ 함정**
- `_shotLineSeconds` 는 **게임시간** 기준이어야 한다. 실시간이면 배속에서 안 보이거나 안 꺼진다.
- 투사체를 만들지 마라. 명중 판정 문제를 새로 만든다.

**🚫 임의 판단 금지**
- `ParticleSystem` 금지. `LineRenderer` + 코드 보간만.

---

## `[x]` G-08. 피격·격파 연출 — 로봇 부품이 흩어진다 (2026-08-27 완료)

**검증 (CLI 플레이 모드 실측):**
- `HitFlash` (기존 `Health.OnDamaged` 구독 — 새 이벤트 없음): 적 피격 시 스프라이트 빨강→흰색, 0.08 게임초 후 원래 색 복원 실측
- `DebrisScatter`: 격파 시 조각 5개(SO) 스폰 → 흩어짐+페이드+축소 → **자가 파괴 확인 (잔해 0)** — 순간 소멸도 폭발도 아닌 부품 흩어짐 (GDD §12)
- 상한: 100개 요청 → 동시 잔해 80개에서 캡 (`MaxActive`, static 리셋 `[RuntimeInitializeOnLoadMethod]`) — 버스트 폭주 방지
- 레지스트리: 사망 시 `Health.TakeDamage` 가 OnDied 전에 Unregister → 아군이 시체를 쏘지 않음
- 보호대상 피격의 HP 반응(붉게 깜빡)은 G-14 HUD 재설계와 함께 구현

**선행:** G-07
**왜:** `Enemy.OnDied` 가 `Destroy(gameObject)` 한 줄이다. 적이 **그냥 사라진다.**
적이 로봇으로 확정됐으므로 GDD §12 가 연출을 이미 정해 뒀다:
> *"로봇 격파 연출은 폭발이 아니라 **부품이 흩어지는** 표현 (톤 유지)."*

**만들 파일:** `Runtime/UI/HitFlash.cs`, `Runtime/UI/DebrisScatter.cs`

**작업:**
1. `HitFlash` : **기존 `Health.OnDamaged` 를 구독**한다 (새 이벤트를 만들지 마라).
   `_hitFlashSeconds` 동안 흰색 램프. `OnDisable` 에서 반드시 해제
2. `DebrisScatter` : 사망 시 작은 사각형 조각 4~6개를 만들어 바깥으로 흩뿌리며 페이드.
   **`ParticleSystem` 이 아니라 `SpriteRenderer` + 코드 보간**이다. 조각 수를 SO 로 두고 상한을 지켜라
3. 보호대상 피격은 더 크게 읽혀야 한다 — HP 바를 붉게 깜빡 (G-14 연동)
4. **연출 시작 시점에 `TargetRegistry` 에서 해제**하라

**추가할 SO 필드:** `_hitFlashSeconds`(0.08), `_debrisCount`(5), `_debrisSeconds`(0.35)

**DoD:**
- 적이 맞을 때마다 반짝인다
- 적이 죽을 때 **부품이 흩어진다** — 순간 소멸도, 폭발도 아니다
- 보호대상이 맞으면 HP 표시가 반응한다
- 아군이 **시체를 계속 쏘지 않는다**
- 100마리를 죽여도 씬에 잔해 오브젝트가 남지 않는다
- 40마리 동시 격파에서 프레임이 무너지지 않는다

**⚠️ 함정**
- 연출 중인 적이 `TargetRegistry` 에 남으면 아군이 허공을 쏜다.
- **조각 수 × 동시 격파 수**가 곱해진다. 상한을 두지 않으면 버스트(G-20)에서 터진다.
- 배속 4x 에서 연출이 안 보이면 시간 기준을 확인하라.

---

## `[x]` G-09. 체력바 · 위협선 (2026-08-27 완료)

**검증 (CLI 플레이 모드 실측):**
- 적 11마리: 데미지 입은 적 1마리만 체력바 표시, 만피 10마리 전부 숨김 (SO `_healthBarHideWhenFull=true` 기본값 상속)
- 월드 `LineRenderer` 2줄(배경+게이지) — 액터당 uGUI Canvas 없음 (함정 준수). 게이지 색 = 잔여비율(녹색→적색)
- 위협선: 적 `State.Attacking` 동안만 대상(보호대상)까지 붉은 선, 이동 시 해제
- 적이 죽으면 자식인 체력바가 함께 사라짐 / 보호대상에도 동일 체력바 (붉은 깜빡 연출은 G-14 와 함께 튜닝)

**선행:** G-08
**왜:** 누가 얼마나 위험한지 읽히지 않으면 개입 시점을 판단할 수 없다.
**G-03 재배치를 언제 할지가 이 정보에 달려 있다.**

**만들 파일:** `Runtime/UI/HealthBar.cs` (월드 `LineRenderer` 2줄)

**작업:**
1. 적과 보호대상에 체력바. **만피일 때는 숨긴다**
2. 아군은 현재 피격되지 않으므로(`_enemiesTargetAllies=0`) 붙이지 않는다. D-04 가 뒤집히면 그때
3. 적이 공격 중일 때 대상까지 위협선 (붉은 계열)
4. 모체 → 보호대상 거리를 HUD 에 (G-14 에서 정식 배치)

**추가할 SO 필드:** `StageDefinition._healthBarHideWhenFull` (true)

**DoD:**
- 피해를 입은 순간부터 체력바가 보인다 / 만피에는 없다
- 적이 죽을 때 체력바가 같이 사라진다
- 적 40마리에서 프레임이 눈에 띄게 떨어지지 않는다

**⚠️ 함정**
- **액터마다 uGUI Canvas 를 만들지 마라.** 배치 비용이 폭발한다. 월드 `LineRenderer` 로 충분하다.

---

## `[x]` G-10. 스폰 예고 (2026-08-27 완료)

**검증 (CLI 실측):**
- `SpawnTelegraph` 링 — `Show(0.6, 1.2)` → 활성 + 확산 반경 + 적색, `End()` → 즉시 비활성 (합성 호출 실측)
- 링은 **모체 자식** — 예고 중 모체가 이동해도 따라감 / 게임시간 기준 (배속 4x 인지, 일시정지 중 멈춤)
- `MoveAndSpawn` 에서 `_telegraph.End()` **직후 같은 프레임** `SpawnEnemy()` — "링이 끝나는 프레임에 적 생성" 보장 (코드 구조상)
- 예고 시간은 스폰 간격에 **포함** (첫 스폰 지연 없음 — 함정 준수), 모체 확대 연출(+15%) 병행
- 라이브 캐치 폴링은 39.1초 승리 윈도우 + 편집기 틱 지연으로 확률적이라 합성 검증으로 대체. 실전 등장은 G-21 시연에서 확인

> 📌 현장 결정: 기존의 "스폰 0.5초 전 색 플래시"(비-태스크 기능)를 새 예고(링 확산 + 스케일 펄스)로 **대체** — 이중 예고 혼란 방지.

**선행:** G-09
**왜:** GDD §7 이 **명시적으로 요구**하는데 미구현이다:
> *"유닛보다 크고, **스폰 직전 예고 연출이 있어야 한다.**"*

예고가 없으면 로봇이 갑자기 나타나고, 플레이어는 대응이 아니라 사후 수습만 한다.

**만들 파일:** `Runtime/UI/SpawnTelegraph.cs` — 링 확산

**작업:**
1. 스폰 `_spawnTelegraphSeconds` 전에 링을 띄운다
2. **링이 사라지는 그 프레임에** 적이 나온다. 어긋나면 예고의 의미가 없다
3. 모체도 예고 동안 살짝 커졌다 돌아온다
4. 링은 모체 **자식**으로 붙여라 — 예고 중 모체가 이동한다

**추가할 SO 필드:** `StageDefinition._spawnTelegraphSeconds` (0.6)

**DoD:**
- 스폰 0.6초 전부터 링이 뜬다 / 링이 끝나는 프레임에 적이 생성된다
- 배속 4x 에서도 인지된다 / 일시정지 중에는 멈춘다

**⚠️ 함정**
- 예고 시간만큼 첫 스폰이 늦어진다. `_motherSpawnDelay` 에 **포함**시켜라.

---

# C. 소리

## `[x]` G-11. 사운드 최소 세트 (2026-08-28 완료)

**검증 (CLI 플레이 모드 실측):**
- `SfxPlayer`(씬 서비스 아님 — `GameSession._sfx` 주입, 자동 탐색) + `ProceduralSfx`(절차 생성: 사인 스윕/구형파/노이즈/금속 하강음, exponential decay 엔벨로프) — **외부 에셋 0개** (ADR-0011 A안)
- 8종 클립 Awake 1회 생성 (고양이 타격/쥐 발사/로봇 격파/보호대상 피격/고용/배치 완료/승리/패배)
- `PlayOneShot` 재생 실측 (isPlaying=True), `GameSession._sfx` 자동 와이어링 확인
- **일시정지 중 새 소리 차단** ✓ (Pause 후 Play → 재생 소스 0)
- 최소 재생 간격(실시간 기준) + 동시 재생 상한(def 별 maxConcurrent) + 소스 풀 8개 라운드-robin
- 음소거 M 토글, 전역 볼륨 SO (`StageDefinition._masterVolume`) — G-12 설정 메뉴와 연결 예정
- 이벤트 연결: HandleFired(종족별)/Enemy.OnDied/Escortee.OnDamaged/고용 확정/Deploy()/EndGame(Pause 직전 — Pause 후 재생 차단 회피)

**선행:** G-10
**왜:** 사용자: **"가벼운 사운드 정도는 작업해서 넣어봄직하잖아?"**
`AudioSource` 사용처가 **0건**이다. 소리가 없는 것만으로 "미완성" 인상이 크게 온다 — 시연에서 특히.

> ## ✅ 음원 소스 = **A안(절차 생성) 으로 확정.** 묻지 말고 그대로 하라
>
> | 안 | 내용 | 판정 |
> |---|---|---|
> | **A** | **코드로 절차 생성** — `AudioClip.Create` 로 사인파·구형파·노이즈를 합성 | **✅ 확정** |
> | B | CC0 무료 음원 (Kenney 등) | **기각** — 외부 에셋 도입은 `CLAUDE.md` §4 상 별도 승인이 필요하다 |
> | C | 사용자/기획자 직접 제작 | **기각** — 이 단계를 세우는 의존이 된다 |
>
> **A 를 고른 이유:** 도형만 쓰기로 한 것과 같은 논리다. 외부 에셋이 0개라 승인 절차가 필요 없고,
> 라이선스·폴더 규약 문제도 생기지 않으며, 그레이박스 톤과 일관된다.
> 시연 품질이 아쉬우면 **P-21 통과 후** B 로 교체하는 별도 태스크를 연다 — 지금은 아니다.
>
> **절차 생성 구현 지침:**
> - `AudioClip.Create(name, samples, 1, 44100, false)` + `SetData(float[])`. 전부 코드다
> - 각 소리는 **0.05 ~ 0.4초**. 길면 겹칠 때 지저분해진다
> - 기본 재료 3개면 충분하다: **사인파**(부드러운 음) · **구형파**(건조한 타격) · **화이트 노이즈**(파편)
> - 공격은 짧은 감쇠(exponential decay) 엔벨로프를 씌워라. 안 씌우면 "삐-" 하고 끊겨 귀에 거슬린다
> - 생성은 **`Awake` 에서 한 번**. 매 재생마다 만들지 마라
> - 종족 구분: 고양이(근접) = 높고 짧게, 쥐(원거리) = 낮고 약간 길게

**만들 파일:** `Runtime/Audio/SfxPlayer.cs`, `Runtime/Data/SfxDefinition.cs`

**필요한 소리 7종 — 그 이상 만들지 마라:**

| # | 이벤트 | 성격 |
|---|---|---|
| 1 | 고양이 근접 타격 | 짧고 가볍고 빠르다. **가장 자주 난다 — 여기서 피로가 생긴다** |
| 2 | 쥐 원거리 발사 | 건조한 발사음 |
| 3 | 로봇 격파 | 금속 파편 느낌의 하강음 |
| 4 | 보호대상 피격 | 낮고 둔탁. 다른 소리와 확실히 구분 |
| 5 | 고용 확정 | 긍정적 상승음 |
| 6 | 배치 완료(도착) | 짧은 확인음 |
| 7 | 승리 / 패배 | 각 1개 |

**작업:**
1. `SfxPlayer` 가 이벤트를 구독해 재생한다. 각 액터가 `AudioSource` 를 들지 않게 하라
2. **동시 재생 상한**을 둔다. 40마리 교전에서 겹치면 소음이 된다
3. **같은 소리의 최소 재생 간격**을 둔다 (근접 타격 특히 — 공격 간격 0.45 초다)
4. 전역 볼륨을 SO 로. 음소거 단축키 하나 (설정 메뉴 G-12 와 연결)

**추가할 SO 필드:** `SfxDefinition._clip`, `_volume`, `_minInterval`, `_maxConcurrent`

**DoD:**
- 7종이 각 이벤트에서 난다
- 40마리 교전에서 소리가 뭉개지지 않는다
- 일시정지 중 새 소리가 나지 않는다
- 음소거가 동작한다
- 볼륨/간격이 SO 로 조절된다

**⚠️ 함정**
- **`Time.timeScale = 0` 에서도 `AudioSource` 는 계속 돈다.** 일시정지 처리를 따로 하라.
- 배속 4x 에서 근접 타격음이 초당 9회 난다. `_minInterval` 이 그래서 필요하다.
- `AudioListener` 는 씬에 **하나뿐**이어야 한다. 이미 `Main Camera` 에 있다.

**🚫 임의 판단 금지**
- BGM 금지. 효과음 7종까지다.
- 사운드 미들웨어(FMOD/Wwise) 도입 금지.

---

# D. UI 고도화

## `[ ]` G-12. 일시정지 메뉴 (우상단)

**선행:** 없음 (**A 그룹과 병행 가능**)
**왜:** 사용자 요구: **"우측 상단에 중지 UI 만들어주고, 누르면 계속하기 / 설정 / 게임종료 이런 거 나오게."**
지금은 게임을 멈추고 나갈 방법이 **없다.** 시연에서 이건 치명적이다 — 보여주다 멈출 수가 없다.

**만들 파일:** `Runtime/UI/PauseMenu.cs`
**고칠 파일:** `Editor/SceneParts.cs` (씬 빌더에도 넣어야 한다)

**작업:**
1. **우상단에 중지 버튼(`II`)** 을 상시 표시
2. 누르면 **`GameClock.Pause()`** 로 멈추고 메뉴 패널을 띄운다.
   **⚠️ `Time.timeScale` 을 직접 대입하지 마라** (CLAUDE.md §4)
3. 메뉴 항목 3개:
   - **계속하기** → `GameClock.Resume()` + 패널 닫기
   - **설정** → 하위 패널 (아래 참조)
   - **게임 종료** → 확인 한 번 받고 종료
4. **설정 패널 (프로토타입 범위)** — 이것만:
   - 전역 볼륨 슬라이더 (G-11 연동)
   - 음소거 토글
   - 다시 시작 (현재 스테이지 재시작 — `GameSession.RestartStage()`)
   - 닫기
5. **`Esc` 로 열고 닫기.** 단, 배치 모드 중 `Esc` 는 배치 취소가 우선이다 (G-02 입력 우선순위 1번)
6. 메뉴가 떠 있는 동안 **게임 입력 전부 차단** — 클릭이 뒤 게임판으로 새지 않게

**종료 처리:**
```csharp
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
```
→ 에디터에서 `Application.Quit()` 는 아무 일도 안 한다. 반드시 분기하라.

**DoD:**
- 우상단 버튼으로 게임이 멈추고 메뉴가 뜬다
- 계속하기로 **직전 배속 그대로** 복귀한다 (1x 로 초기화되지 않는다)
- 설정에서 볼륨·음소거가 즉시 반영된다
- 다시 시작이 동작한다
- 게임 종료가 에디터·빌드 양쪽에서 동작한다
- 메뉴가 떠 있는 동안 게임판 클릭이 먹지 않는다
- `Esc` 로 열고 닫힌다 / 배치 중에는 배치 취소가 우선이다
- `SceneParts` 로 씬을 다시 빌드해도 메뉴가 나온다

**⚠️ 함정**
- **`GameClock` 에는 UI 슬로우모션 상태(`_uiSlowActive`)가 있다** (P-18B). 일시정지 해제 시 슬로우모션 중이었는지까지 복원되는지 확인하라. 안 하면 배치하다 메뉴를 열면 속도가 꼬인다.
- `Time.timeScale = 0` 이면 `Update` 는 돌지만 `FixedUpdate` 는 안 돈다. UI 애니메이션에 `Time.deltaTime` 을 쓰면 **멈춘다** — `unscaledDeltaTime` 을 써라.
- 결과 패널(`ResultPanel`)이 떠 있을 때 일시정지 메뉴가 겹치지 않게 하라.
- `EventSystem` 이 이미 씬에 있다. 새로 만들지 마라.

**🚫 임의 판단 금지**
- 설정에 해상도·키 리매핑·그래픽 옵션을 넣지 마라. **볼륨·음소거·다시 시작**까지다.
- 메인 메뉴 씬을 만들지 마라. 씬은 `Stage_Greybox.unity` 하나다 (CLAUDE.md §4).

---

## `[ ]` G-13. 디버그 오버레이 개편 — 접기 + 가독성

**선행:** 없음 (**병행 가능. 가장 싸고 즉시 체감된다**)
**왜:** 사용자: **"우측 하단에 박아넣은 디버깅은 너무 글자가 작아서 정보를 얻을 수가 없다.
어차피 디버깅인데 접었다 폈다라도 할 수 있게 하던가."**

현재 `DebugOverlay.OnGUI` 는 `GUI.Label` 에 **고정 `Rect(420, 200)`** 하나뿐이다.
폰트 크기 지정도, 접기도, 배경도 없다.

**고칠 파일:** `Runtime/Diagnostics/DebugOverlay.cs`

**작업:**
1. **접기/펴기 = `F6` 으로 확정.** + 화면 모서리 클릭 토글 버튼도 같이 둔다.
   접으면 한 줄 요약, 펴면 전체.
   → **F1~F5 는 이미 점유되어 있다** (실측 확인):
   `F1` 오버레이 표시 토글 · `F2` 자원 +1000 · `F3` 적 전멸 · `F4` 보호대상 무적 · `F5` 강제 승리.
   `F1`(전체 숨김)과 `F6`(접기)은 **다른 기능**이다. 둘 다 남겨라.
2. **폰트 크기를 키운다.** `GUIStyle.fontSize` 명시 + 조절 가능하게
3. **반투명 검은 배경 박스**를 깐다. 지금은 배경이 없어 맵 위에서 글자가 묻힌다
4. **크기를 내용에 맞춘다.** 고정 200px 이라 줄이 늘면 잘린다 — `CalcSize`
5. 섹션 분리 — `[세션]` `[적]` `[아군]` `[경제]`
6. 접힘 상태를 `PlayerPrefs` 로 유지한다
7. **시연 중에는 기본 접힘**으로 시작하게 하라 — 시연 화면에 디버그가 떠 있으면 안 된다

**DoD:**
- 단축키로 접었다 펼 수 있다
- 펼친 상태에서 글자가 **읽힌다** (사용자가 직접 확인)
- 밝은 맵 위에서도 배경 덕에 읽힌다
- 줄이 늘어나도 잘리지 않는다
- 재생을 껐다 켜도 접힘 상태가 유지된다
- 접힌 상태에서 게임 화면을 가리지 않는다

**⚠️ 함정**
- `OnGUI` 는 프레임당 여러 번 호출된다. **`GUIStyle` 을 매번 `new` 하지 마라.** 필드에 캐시.
- 도메인 리로드 OFF: 접힘 상태를 `static` 에 담으면 다음 플레이에 샌다. `ResetStatics` 에 넣거나 인스턴스 필드로.
- `DebugHotkeys` 는 `Space`/`1`/`2`/`3`/`R` 을 쓴다. `F6` 은 어느 쪽과도 겹치지 않는다.

**🚫 임의 판단 금지**
- 디버그 오버레이를 uGUI 로 다시 만들지 마라. `OnGUI` 로 충분하고 더 싸다.

---

## `[ ]` G-14. HUD 재설계

**선행:** G-12
**왜:** 현재 `Canvas` 아래가 Text 3개 + 버튼 4개를 좌하단에 **쌓아 놓은** 상태다.
정보 위계가 없어 "지금 뭘 봐야 하는지" 가 안 읽힌다. 시연 인상을 가장 크게 좌우하는 부분이다.

**고칠 파일:** `Runtime/UI/*`, `Editor/SceneParts.cs`

**작업:**
1. **정보 위계:**
   - 최상위(크게): 보호대상 HP, 자원
   - 보조: 배속, 모체까지 거리, 다음 스폰까지
   - 임시: 선택된 유닛 정보(G-15), 결과 패널
2. 좌하단 뭉치를 **상단 바 + 좌하단 조작부**로 분리. 우상단은 일시정지(G-12)
3. 보호대상 HP 를 텍스트가 아니라 **바 + 숫자**로
4. 자원 변동 시 짧게 강조 (증가 초록 / 감소 붉게)
5. `CanvasScaler` 를 `Scale With Screen Size` 로 — 해상도가 바뀌면 지금 레이아웃은 무너진다

**DoD:**
- 보호대상 HP 와 자원이 한눈에 들어온다
- 자원이 늘고 줄 때 시각 반응이 있다
- 창 크기를 바꿔도 레이아웃이 무너지지 않는다
- `SceneParts` 로 씬을 다시 빌드해도 새 HUD 가 나온다

**⚠️ 함정**
- **`SceneParts.cs` 를 같이 안 고치면 씬 재빌드 시 옛 HUD 로 돌아간다.**
- 폰트는 `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` 유지.

**🚫 임의 판단 금지**
- UI Toolkit 금지 (ADR-0006). 아트 리소스 제작 금지 — 도형·색·기본 폰트만.

---

## `[ ]` G-15. 유닛 정보 패널

**선행:** G-14, G-02
**왜:** 유닛을 선택해도 **보여줄 곳이 없다.** G-03(이동)·G-04(회수)·G-05(업그레이드) 버튼이 붙을 자리다.

**작업:**
1. 선택 시 패널: 종족 / 티어 / 사거리 / 공격력 / 공격 간격 / DPS / 상태(`Marching`·`Deployed`·쿨다운)
2. 버튼: **이동** / **회수** / **업그레이드**
3. 선택 해제 시 닫힌다
4. DPS 는 `AttackDamage / AttackInterval` 로 **계산**한다. SO 필드를 만들지 마라 (파생값)

**DoD:**
- 유닛을 고르면 정보가 뜨고 해제하면 닫힌다
- 표시 값이 SO·현재 티어와 일치한다
- 이동·회수·업그레이드 버튼이 실제로 동작한다
- 재배치 쿨다운이 남으면 이동 버튼이 비활성이고 남은 시간이 보인다
- 최대 티어면 업그레이드 버튼이 비활성이다

---

## `[ ]` G-16. 고용 패널 정보 표시

**선행:** G-15
**왜:** 버튼이 2개여도 **이름과 가격만** 보이면 무엇이 다른지 모른다.
근접/원거리의 차이가 안 읽히면 G-01 의 설계가 전달되지 않는다.

**작업:**
1. 버튼 라벨 다중 행: `종족명` / `$비용` / `사거리 N.N · DPS NN.N`
2. hover 시 마을 주변에 **그 유닛의 사거리 원**(G-06 재사용)
3. 자원 부족 시 흐려지는 기존 `AffordabilityTint` 유지

**DoD:**
- 두 버튼의 사거리·DPS 차이가 바로 읽힌다
- DPS 에 별도 SO 필드가 없다
- hover 시 사거리 원이 뜬다

**⚠️ 함정**
- `HirePanel` 은 버튼을 **런타임 생성**한다. P-18B 에서 넣은 라벨 코드와 같은 앵커 방식을 쓰라.

---

# E. 적과 리듬

## `[ ]` G-17. 스폰 테이블 — 적 종류를 데이터로

**선행:** G-10
**왜:** `StageDefinition._motherSpawnEnemy` 가 **단일 참조**다. 로봇을 한 종류밖에 못 낸다.

**작업:**
1. `_motherSpawnEnemy` → `_spawnTable` (가중치 목록)
   ```csharp
   [System.Serializable]
   public struct SpawnEntry
   {
       [SerializeField] private EnemyDefinition _enemy;
       [SerializeField] private int _weight;
       public EnemyDefinition Enemy => _enemy;
       public int Weight => _weight;
   }
   ```
2. 가중치 추첨. 시드를 고정하지 마라
3. 버스트 전용 테이블을 만들지 마라

**DoD:**
- SO 에서 종류와 비율을 바꿀 수 있다
- 항목이 하나면 기존과 동일 동작 (회귀 없음)
- 가중치 합 0 / 빈 목록이면 `LogError`
- `_motherSpawnEnemy` 가 코드에서 사라졌다

**⚠️ 함정**
- SO 필드 교체 시 `Stage_Greybox.asset` 의 기존 값이 **조용히 날아간다.** 교체 후 에셋을 열어 다시 채워라.
- `Random.Range(int,int)` 는 상한 배타, `(float,float)` 는 상한 포함.

---

## `[ ]` G-18. 로봇 2종째

**선행:** G-17
**왜:** 적이 한 종류면 **근접(고양이)과 원거리(쥐)의 역할 분담이 시험되지 않는다.**
대비되는 위협이 있어야 두 병종을 다 쓸 이유가 생긴다.

**만들 파일:** `Data/Enemies/Robot_Scout.asset`, 프리팹 변형 1개
(기존 `Enemy_Basic` 은 `Robot_Walker` 로 **개명** — 세계관 반영)

| 항목 | **Walker** (기존) | **Scout** (신규) |
|---|---|---|
| 이동 | 2.0 | **3.4** |
| 체력 | 20 | **10** |
| 공격력 / 간격 | 5 / 1.0 | 4 / 1.0 |

스폰 테이블: Walker 70 / Scout 30

> **설계 의도:** 빠른 Scout 는 **원거리(쥐)가 넓게 커버해야** 잡히고,
> 느리고 단단한 Walker 는 **근접(고양이)이 회전율로** 녹인다. 이 대비가 G-01 의 검증이다.

**DoD:**
- 두 종류가 섞여 나오고 **색으로 구분**된다
- Scout 가 아군 사거리를 실제로 더 빨리 통과한다
- 개명 후 참조가 끊기지 않았다 (`rename_asset` 사용)
- 새 프리팹이 `Assets/_Project/Prefabs/` 아래에 있다

**⚠️ 함정**
- 적이 빨라지면 **근접(사거리 1.6)이 한 발도 못 쏠 수 있다. 이건 버그가 아니라 의도된 압박이다.**
  "근접만으로는 Scout 를 못 막는다 → 원거리를 섞어야 한다" 가 이 태스크의 설계 목적이다.
  **근접 사거리를 올려서 해결하지 마라.** 병종 정체성이 무너진다.
  → **단, 상한선이 있다:** 근접 2기가 배치된 상태에서 Scout 3마리 연속을 **한 마리도 못 잡으면** 과하다.
  그때만 G-21 에서 `Robot_Scout._moveSpeed` 를 3.4 → 3.0 으로 내린다. 그 외 조정은 하지 마라.
- 프리팹 변형은 색만 오버라이드하라.

**🚫 임의 판단 금지**
- **로봇은 2종까지다.** 특수 능력(원거리 공격·자폭·힐) 금지. 속도/체력 대비만.

---

## `[ ]` G-19. 스폰 리듬 — 묶음과 휴지

**선행:** G-18
**왜:** `_motherSpawnInterval = 3` 초 등간격이라 **긴장의 오르내림이 없다.**
몰려오고 숨 돌리는 파형이 있어야 플레이어가 **휴지에 재배치(G-03)를 하게 된다.**

**작업:**
1. **묶음(volley) + 휴지(rest)** 구조로 교체
2. 예고(G-10)는 **묶음 시작 전 한 번**
3. HUD 에 다음 묶음까지 남은 시간 게이지 — 휴지를 배치에 쓰도록 유도

**추가할 SO 필드:** `_spawnVolleyCount`(3), `_spawnVolleySpacing`(0.4), `_spawnRestSeconds`(4.0)
→ `_motherSpawnInterval` 은 역할이 사라지므로 **제거**한다.

**DoD:**
- 3마리씩 몰려오고 사이에 눈에 띄는 휴지가 있다
- 남은 시간이 HUD 에 보인다
- 평균 스폰 밀도가 기존과 크게 다르지 않다
- `_motherSpawnInterval` 이 코드·SO 어디에도 없다

**⚠️ 함정**
- 순간 동시 생존 적 수가 늘어난다. P-19(풀링) 기준 40마리에 걸릴 수 있으니 G-21 에서 재어라.
- 격파 조각(G-08)도 같이 몰린다. 조각 상한을 확인하라.

**🚫 임의 판단 금지**
- 웨이브 번호·클리어 보너스 금지. 이 게임은 웨이브제가 아니다 (GDD §4). **호흡만** 만든다.

---

## `[ ]` G-20. 버스트 모드

**선행:** G-19
**왜:** `MotherSpawner.State.Burst` 가 **빈 껍데기**다.
GDD §7: *"이 게임의 유일한 차별점이다. 여기가 밋밋하면 게임이 밋밋하다."*
시연에서 "이 게임의 한 방" 이 필요하다면 여기다.

**작업 (GDD §7 정의 그대로):**
1. **진입** — 보호대상이 지정 노드 통과. `_burstTriggerNodeIds` (예 `["N06","N10"]`)
2. **효과** — 모체 **이동 정지** + 스폰 급증
3. **지속** — `_burstDuration` 후 `Chasing` 복귀
4. **예고** — 화면 경고 표시 + 모체 색 변경
5. 같은 트리거로 두 번 진입하지 않는다

**추가할 SO 필드:** `_burstTriggerNodeIds`(string[]), `_burstDuration`(6.0), `_burstVolleyCount`(5), `_burstRestSeconds`(1.5)

**DoD:**
- 트리거 통과 시 모체가 멈추고 스폰이 급증한다
- 진입이 화면에서 명확히 읽힌다
- 지속시간 후 정상 추격으로 돌아온다
- 같은 트리거로 재진입하지 않는다
- 일시정지·배속이 정상 동작한다

**⚠️ 함정**
- **트리거를 거리 비교로 하지 마라.** 노드 *통과* 이벤트로 잡아라. 거리로 하면 배속에서 건너뛴다.
- 노드 ID 오타가 런타임까지 간다. 시작 시 조회해 **없으면 `LogError`**.
- 버스트 중 모체가 멈추므로 **플레이어에게는 유일하게 안정적으로 배치할 수 있는 구간**이다. 이 대비가 재미의 핵심이니 죽이지 마라.
- 도메인 리로드 OFF: 트리거 발동 여부를 `static` 에 담지 마라.

**🚫 임의 판단 금지**
- 모체를 공격 가능하게 만들지 마라. **처치 불가능**이다 (GDD §7).
- 버스트 전용 적 금지.

---

# F. 마감

## `[ ]` G-21. 밸런스 재조정 + 시연 검증 패스

**선행:** G-20
**왜:** 병종 2종·재배치·업그레이드·묶음 스폰이 들어가면서 P-20 밸런스는 **확실히 깨졌다.**

**작업:**
1. 한 판을 끝까지 돌려 시간을 잰다
2. 어긋나면 **SO 값만** 만진다. 코드로 맞추지 마라
3. 조정 우선순위: ① 업그레이드 비용 → ② 스폰 휴지 → ③ 보호대상 속도 → ④ 맵 길이(최후)
4. 바꾼 값과 이유를 이 태스크 아래에 표로 기록한다

**점검할 것 — 이번 단계에서 새로 생긴 축들:**
- **근접(고양이)이 실제로 쓸모 있는가.** 사거리 1.6 으로 경로에 닿는 `Buildable` 칸이 충분한가
- **원거리(쥐)만 뽑는 게 항상 정답이 되지 않는가** ← 되면 병종 설계가 죽은 것
- 재배치 쿨다운 3초가 "옮길까 말까" 를 만드는가, 그냥 성가신가
- **업그레이드와 신규 고용 중 어느 쪽이 항상 정답이 되지 않는가** ← 되면 선택이 죽은 것
- 지름길 120 이 여전히 고민되는 값인가
- 동시 생존 적 최댓값이 40 을 넘는가 → 넘으면 **P-19(풀링)를 다시 열어라**

### 최종 체크리스트 — "시연 가능한가"

| # | 항목 | 확인 |
|---|---|---|
| 1 | EditMode 32개 + 신규 테스트 전부 통과 | `run_tests` |
| 2 | 한 판 완주에 콘솔 에러/경고 0개 | `console --level warning` |
| 3 | **두 종족이 화면에서 구분되고 역할이 다르다** | 눈 |
| 4 | **배치된 유닛을 선택할 수 있다** | 눈 |
| 5 | **선택한 유닛을 다른 칸으로 이동시킬 수 있다** | 눈 |
| 6 | **모체 이동에 재배치로 대응하는 플레이가 성립한다** | 눈 |
| 7 | **비용을 써서 유닛을 강화할 수 있다** | 눈 |
| 8 | 회수·환불이 동작한다 | 눈 |
| 9 | 싸우는 것과 맞는 것이 구분된다 | 눈 |
| 10 | 로봇이 죽을 때 부품이 흩어진다 | 눈 |
| 11 | 사거리를 배치 전에 볼 수 있다 | 눈 |
| 12 | 소리가 7종 난다 | 귀 |
| 13 | **우상단 중지 → 계속하기·설정·게임종료가 동작한다** | 눈 |
| 14 | 디버그 오버레이를 접었다 펴고, 글자가 읽힌다 | 눈 |
| 15 | HUD 에서 HP·자원이 한눈에 들어온다 | 눈 |
| 16 | 스폰 예고 → 묶음 → 휴지 리듬이 있다 | 눈 |
| 17 | 버스트가 발동하고 읽힌다 | 눈 |
| 18 | SO 가 플레이 후 오염되지 않았다 | `.asset` 확인 |
| 19 | 한 판이 90~150초 (3회 측정) | 계측 |
| 20 | 승리·패배 **둘 다** 재현된다 | 각 1회 |
| 21 | `git status` 가 깨끗하다 | `git status` |
| 22 | **처음 보는 사람에게 5분간 보여줄 수 있다** | ← 이 단계의 진짜 합격선 |

**DoD:**
- 22개 전부 ✅. 하나라도 ❌ 면 **P-21 로 넘어가지 마라**
- 이 단계에서 추가된 SO 필드 목록이 문서에 정리되어 있다
- 스크린샷 5장(평시 / 재배치 중 / 업그레이드 / 버스트 / 일시정지 메뉴)이 `.docs/playtest/` 에 있다

### 실측 절차 (CLI)

```bash
unity command set_autotick --enable true --interval_ms 16 --no-banner
unity command clear_console --no-banner
unity command editor_play --no-banner
# ... 한 판 진행 ...
unity command console --level warning --no-banner
unity command capture_game_view --source screen --save_path <경로> --no-banner
unity command editor_stop --no-banner
```

포커스가 없어 얼어붙으면 `set_autotick` 이 안 먹은 것이다.
그때는 `EditorApplication.Step()` 을 `eval` 로 돌려라 — Serena 메모리 `unity_playmode_frame_freeze` 참조.

**⚠️ 함정**
- 3~17번은 **사람이 눈과 귀로 확인해야 한다.** Claude 가 스크린샷만 보고 "확인됨" 이라고 쓸 수 없다. `CLAUDE.md` §5 가 여기서도 적용된다.

**🚫 임의 판단 금지**
- 재미가 없게 느껴져도 **여기서 새 기능을 넣지 마라.** 그 판정은 P-21 의 일이다.
- 체크리스트를 통과시키려고 항목을 고쳐 쓰지 마라. 미달이면 미달이라고 적어라.


---

## `[ ]` P-21. 플레이 테스트 세션

> **선행이 바뀌었다 (2026-08-27): 단계 7 전체(G-00~G-21)을 먼저 끝낸다.**
> 지금 상태로는 게임의 형태가 안 나와서, 재미가 없다는 결과가 나와도
> **핵심 루프 탓인지 완성도 탓인지 구분할 수 없다.**

**목표:** **재미를 판정한다.** 이 프로젝트에서 가장 중요한 태스크다.

**선행:** ~~P-20~~ → **G-21** (단계 7 전체)

**진행 방법:**
1. 개발자와 기획자가 **각자 5판 이상** 플레이한다
2. 매 판 후 아래를 기록한다 (`.docs/playtest/YYYY-MM-DD.md`)
3. 코드를 고치기 전에 **먼저 다 기록한다**

> **⚠️ 이건 사람이 하는 태스크다.** CLI 로 자동화하지 마라.
> 판정 대상이 "재미" 이므로 Claude 가 대신 판정할 수 없다.
> Claude 의 역할은 기록 문서 틀을 만들고, 수치를 뽑아 주고, 결정을 ADR 로 옮기는 것까지다.

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
  → `Stage_Greybox.asset` 의 `_alliesCanDieWhileMarching` (현재 `0`)
- **D-06 (모체 경로 추종 vs 직진)**: 토글을 바꿔 각 3판. 어느 쪽이 재배치 압박이 큰가?
  → `_motherFollowsPath` (현재 `1` = 경로 추종)
- **D-01 (뒤에 남은 유닛)**: 남겨진 유닛이 몇 개나 되는가? 신경 쓰이는가?

### D. P-18B 에서 들어온 비-태스크 기능도 같이 판정한다
태스크에 없이 추가된 것들이다. 재미에 기여하지 않으면 **덜어내는 것도 결정**이다.
- **배치 UI 슬로우모션 0.1x** — 정밀 조작에 도움이 되는가, 아니면 흐름을 끊는가? 값이 적절한가?
- **마을 선택 즉시 하이라이트** — 유닛을 고르기 전에 보이는 게 도움이 되는가?
- **배속 버튼 4개** — 키보드만으로 충분했는가?

**DoD:**
- 플레이 테스트 기록 문서가 `.docs/playtest/` 에 있다
- 통과 조건 4개 각각에 ✅/❌ 판정이 있다
- D-01, D-03, D-06 에 대한 결정이 내려지고 **ADR로 기록**되었다
- D 항목(비-태스크 기능) 3개에 유지/삭제/튜닝 결정이 있다

> ### ⛔ 게이트
> **통과 조건 4개 중 2개 이상이 ❌ 면, 다음 단계로 가지 마라.**
> 종족을 열 개 넣어도 재미없다. 기획서로 돌아가 핵심 루프를 다시 짜라.
> 여기서 몇 주 쓰는 것이 몇 달을 아낀다.


---
---

## 부록. 프로토타입 범위 밖 (지금 만들지 마라)

이것들을 "있으면 좋을 것 같아서" 만들지 마라. **전부 P-21 통과 후다.**

- ~~버스트 모드~~ → **단계 7 G-20 으로 이동** (ADR-0012)
- 마을 복수화, 유닛 등급/승급 — (병종 2종만 **단계 7 G-01 로 이동**, 종족 체계는 여전히 범위 밖)
- 스탠딩 일러스트, 대화 시스템, 서사, 튜토리얼
- ~~사운드~~ → **단계 7 G-11 로 이동** (효과음 7종만). 파티클·애니메이션은 **여전히 금지**
- 세이브/로드, 메타 진행, 스테이지 선택
- 데이터 파이프라인 (구글 시트 → SO 임포터) ← **P-21 통과 직후 1순위**
- 스팀 연동, 빌드 파이프라인
- 쿼터뷰 카메라 기울임
- 오브젝트 풀링 (적 외 다른 것)
- 해상도 대응, 키 리매핑, 로컬라이제이션 — (일시정지 메뉴의 **볼륨·음소거·다시 시작**만 **단계 7 G-12 로 이동**)
