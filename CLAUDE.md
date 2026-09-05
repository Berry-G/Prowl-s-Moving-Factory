# CLAUDE.md — Prowl's Moving Factory

이 파일은 **모든 세션에서 자동으로 읽히는 강제 규칙**이다.
여기 적힌 것과 충돌하는 판단을 하지 마라. 충돌하면 작업을 멈추고 사용자에게 물어라.

---

## 0. 이 프로젝트가 무엇인가 (한 문장)

> **"이동하는 적 스폰 지점 때문에 방어선을 계속 다시 짜야 하는, 호위형 타워 디펜스."**

현재 단계는 **그레이박스 프로토타입**이다. 아트 에셋은 만들지 않는다. 도형만 쓴다.

---

## 1. 환경

| 항목 | 값 | 비고 |
|---|---|---|
| Unity | **6000.5.7f1** | 절대 다른 버전으로 프로젝트를 열거나 업그레이드 제안하지 말 것 |
| 렌더 파이프라인 | URP 17.6 / **2D Renderer** | |
| 입력 | **Input System 1.20** (신 입력) | 구 `Input` API 금지 (§3) |
| 대상 플랫폼 | Windows (IL2CPP) / Linux(예정) | Android·iOS·WebGL·Mac 전부 제외 |
| UI | **uGUI** | UI Toolkit 런타임 UI 금지 (ADR-0006) |
| 언어 | C# / 네임스페이스 루트 `PMF` | |

---

## 2. 확정된 설계 결정 (뒤집지 마라)

이 6개는 사용자와 합의가 끝났다. **"더 나은 방법이 있는데요"라며 임의로 바꾸지 마라.**
바꾸고 싶으면 코드를 쓰기 전에 물어라. 근거는 `.docs/adr/` 에 있다.

1. **배치 모델 = 마을 고용 + 행군.** 유닛은 마을에서 고용되어 목적지까지 *실제로 걸어간다*. 즉시 설치가 아니다. → ADR-0001
2. **격자 = 직교(Rectangular) Tilemap.** Isometric Tilemap을 쓰지 마라. 쿼터뷰 느낌은 나중에 *카메라 각도와 스프라이트*로만 낸다. → ADR-0002
3. **블로킹 없음.** 아군 유닛은 적의 진행을 물리적으로 막지 않는다. 사거리 안의 적을 쏘기만 한다. → ADR-0003
4. **적 이동 = 경로 그래프 위 고정 경로.** A*/NavMesh 그리드 패스파인딩을 도입하지 마라. 노드 단위 그래프 탐색만 쓴다. → ADR-0004
5. **지름길 기믹.** 보호대상은 자원을 지불하고 지름길 엣지를 탈 수 있다. **적은 지름길 엣지를 통행할 수 없다.** 그래서 경로는 선형 웨이포인트가 아니라 **분기 그래프 + 통행 권한 플래그**다. → ADR-0004
6. **물리 엔진 미사용.** Rigidbody2D / Collider2D / Physics2D 를 쓰지 않는다. 사거리·충돌·클릭 판정은 전부 순수 수학이다. → ADR-0005

---

## 3. Unity 6 API — 틀리기 쉬운 것들

낡은 예제 코드를 그대로 옮기면 여기서 깨진다. **반드시 오른쪽 것을 써라.**

> 이 표는 **6000.5.7f1 컴파일러가 실제로 뱉은 경고**로 확인했다 (2026-08-30).
> Unity 를 올리면 또 낡는다 — `CS0618` 경고가 보이면 표를 의심하고 고쳐라.
> 한 판 완주에 콘솔 경고 0개가 기준선이므로(TASKS G-21 체크리스트 2번) 폐기 API를 남겨 두면 안 된다.

| 쓰지 마라 (구식/Obsolete) | 써라 |
|---|---|
| `Input.GetKeyDown`, `Input.mousePosition`, `Input.GetAxis` | `Keyboard.current.xKey.wasPressedThisFrame`, `Mouse.current.position.ReadValue()` (`using UnityEngine.InputSystem;`) |
| `FindObjectOfType<T>()` | `FindFirstObjectByType<T>()` / `FindAnyObjectByType<T>()` |
| `FindObjectsOfType<T>()`, `FindObjectsByType<T>(FindObjectsSortMode.None)` | `FindObjectsByType<T>()` — **인자 없이.** 비활성까지 찾으려면 `FindObjectsByType<T>(FindObjectsInactive.Include)` |
| `FindObjectsSortMode` (열거형 자체) | **쓰지 마라.** 이 열거형이 통째로 폐기됐다 — 인자 없는 오버로드가 정렬하지 않는 기본 동작이다 |
| `Object.GetInstanceID()` | `GetEntityId()` — 반환형 `EntityId` 를 `int` 로 캐스팅하는 것도 폐기 예정이니 그대로 써라 |
| `Rigidbody2D.velocity` | `Rigidbody2D.linearVelocity` — **단, 이 프로젝트는 Rigidbody2D 자체를 안 쓴다** |
| `Camera.main` 을 매 프레임 호출 | `Awake()` 에서 한 번 캐시 |
| `Grid.CellToWorld(c)` 로 유닛 위치 계산 | `Grid.GetCellCenterWorld(c)` — `CellToWorld` 는 셀의 **좌하단 모서리**를 준다 |
| `yield return new WaitForSeconds(x)` 를 매 틱 새로 할당 | 필드에 캐시하거나 `Update` 에서 타이머 누산 |
| `async void` | 프로토타입에서는 `async` 자체를 쓰지 마라. `Update` + 타이머로 충분하다 |

### Enter Play Mode Options (도메인 리로드 OFF)

이 프로젝트는 **Reload Domain 을 끈다**(빠른 재생). 그 결과:

- **`static` 필드가 플레이 종료 후에도 값을 유지한다.** 다음 플레이 시작 시 이전 판의 쓰레기 값이 남아 있다.
- **`static` 이벤트 구독이 누적된다.** 콜백이 2번, 3번 호출되는 버그의 원인 1위.

→ **`static` 상태를 가진 클래스는 반드시 아래를 넣어라.**

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
private static void ResetStatics()
{
    _instances.Clear();
    OnSomething = null;
}
```

→ `OnEnable` 에서 구독했으면 **`OnDisable` 에서 반드시 해제**하라. 예외 없다.

---

## 4. 코드 규칙

### 폴더 / 어셈블리

```
Assets/_Project/
  Scripts/
    Runtime/     → PMF.Runtime.asmdef   (네임스페이스 PMF.*)
    Editor/      → PMF.Editor.asmdef    (Editor 플랫폼 전용, Runtime 참조)
    Tests/       → PMF.Tests.asmdef     (EditMode)
  Data/          → ScriptableObject .asset 파일
  Prefabs/
  Scenes/
  Art/           → 프로토타입 단계에서는 비워 둔다
```

- `Assets/` 최상위에 새 폴더를 만들지 마라. 전부 `Assets/_Project/` 아래다.
- Unity 기본 생성물(`Assets/Scenes/SampleScene.unity`, `Assets/Settings/`)은 건드리지 마라.

### 네이밍

| 대상 | 규칙 | 예 |
|---|---|---|
| 클래스/메서드/프로퍼티 | PascalCase | `PathGraph`, `TryFindRoute` |
| private 필드 | `_camelCase` | `_currentRoute` |
| `[SerializeField]` private 필드 | `_camelCase` | `[SerializeField] private float _moveSpeed;` |
| public 필드 | **금지.** 프로퍼티나 `[SerializeField]` 를 써라 | |
| 상수 | PascalCase | `MaxUnits` |
| ScriptableObject 클래스 | `~Definition` | `UnitDefinition`, `EnemyDefinition` |
| 인터페이스 | `I~` | `IDamageable` |
| MonoBehaviour 파일명 | 클래스명과 **정확히** 일치 | |

### 하지 말 것

- **싱글턴 남발 금지.** 씬 단일 서비스는 `GridSystem`, `PathGraph`, `GameSession`, `GameClock` **4개만** 허용. 그 외에는 `[SerializeField]` 참조 주입.
- **`Find`/`GetComponent` 를 `Update` 안에서 호출 금지.** 전부 `Awake`/`Start` 에 캐시.
- **`Time.timeScale` 직접 대입 금지.** `GameClock` 을 통해서만 바꾼다.
- **윈도우형 UI 를 띄우면 반드시 0.1배속을 건다.** 아래 §4-1 을 읽어라. 이 규칙은 세 번 새어 나갔다.
- **ScriptableObject 필드를 런타임에 수정 금지.** 에디터에서 영구 변경되어 밸런스가 조용히 오염된다. SO는 읽기 전용 정의(Definition)이고, 가변 상태는 런타임 인스턴스 클래스가 따로 갖는다.
- **매직 넘버 금지.** 밸런스 수치는 전부 ScriptableObject 로 뺀다. 예외: 0, 1, 그리고 명백한 수학 상수.
- **외부 에셋/패키지 임의 도입 금지.** DOTween, Odin, A* Pathfinding Project 등. 필요하면 물어라.
- **씬 임의 생성 금지.** 프로토타입 씬은 `Stage_Greybox.unity` 하나다.
- **아트 에셋 생성 금지.** 그레이박스는 Unity 기본 스프라이트(흰 사각/원)와 색상만 쓴다.
- **`.meta` 파일을 손으로 만들거나 지우지 마라.** Unity 에디터에 맡긴다.

### 4-1. 윈도우형 UI = 0.1배속 (예외 없음)

**화면에 창이 뜨면 게임은 반드시 0.1배속으로 느려진다.** 2026-08-30 확정 규칙 6이다.

정밀 조작을 요구하는 창을 띄워 놓고 게임이 제 속도로 굴러가면, 플레이어는 **창을 읽는 대가로
판을 잃는다.** 그러면 창을 안 보게 되고, 그 창은 만든 의미가 없어진다.

**적용 대상 — "버튼을 눌러야 하는 창"이면 전부다.**

| 창 | 담당 |
|---|---|
| 고용 패널 (마을 클릭 ~ 슬롯 선택) | `DeploymentController` |
| 지름길 구매 확인 | `ShortcutPanel` |
| 유닛 정보 패널 (이동·회수·강화) | `UnitInfoPanel` |
| 일시정지 메뉴 · 설정 · 난이도 확인 | `GameClock.Pause()` — **0배속이라 이미 더 강하다** |

체력바·자원 표시·디버그 오버레이 같은 **HUD 는 해당 없다.** 누를 것이 없으면 창이 아니다.

**지키는 방법 — 새 창을 만들 때 이 3줄을 복사해 넣어라.**

```csharp
private bool _slowMotionHeld;   // 이 창이 들고 있는 토큰. 소유자당 하나뿐이다.

private void AcquireSlowMotion()
{
    if (_slowMotionHeld) return;          // 두 번 열려도 Enter 는 한 번
    _slowMotionHeld = true;
    GameClock.Instance?.EnterUiSlowMotion();
}

private void ReleaseSlowMotion()
{
    if (!_slowMotionHeld) return;
    _slowMotionHeld = false;
    GameClock.Instance?.ExitUiSlowMotion();
}

private void OnDisable() => ReleaseSlowMotion();   // 씬이 내려갈 때 걸린 채 남지 않게
```

- `GameClock` 의 슬로우모션은 **참조 계수**다. 창이 겹쳐도 마지막 창이 닫힐 때만 속도가 돌아온다.
- 그래서 **Enter 와 Exit 의 짝이 생명이다.** 짝이 어긋나면 `ExitUiSlowMotion` 이 `LogError` 를 뱉는다.
  콘솔에 그 에러가 보이면 어딘가에서 창을 여닫는 경로 하나가 빠진 것이다.
- 닫는 경로를 **전부** 세어라: 확인 / 취소 / ESC / 우클릭 / 대상 소멸 / 선택 변경 / `OnDisable`.

근거: ADR-0019, `.docs/HANDOFF-2026-08-30.md` §4 확정 규칙 6.

### 좌표 규칙 (가장 중요)

**격자 좌표 ↔ 월드 좌표 변환은 오직 `GridSystem` 안에서만 한다.**
다른 어떤 스크립트도 `transform.position` 에서 셀 인덱스를 직접 계산하면 안 된다.

이유: 나중에 쿼터뷰(카메라 기울임)를 도입할 때, 변환 지점이 한 곳이면 그 파일만 고치면 되고, 흩어져 있으면 전부 다시 짜야 한다.

---

## 5. 작업 방식

- **한 번에 하나의 태스크만.** `.docs/TASKS-P1-prototype.md` 의 태스크 ID 순서대로 진행한다.
- 태스크를 시작하기 전에 그 태스크의 **수용 조건(DoD)** 을 먼저 읽어라.
- 태스크에 명시되지 않은 기능을 "있으면 좋을 것 같아서" 추가하지 마라.
- 스크립트를 만들었으면 **씬/프리팹 연결까지가 그 태스크의 완료**다. 코드만 두고 "완료"라고 하지 마라.
- 에디터가 열려 있지 않아 컴파일 검증이 불가능하면, **그 사실을 명시**하고 "동작 확인됨"이라고 쓰지 마라.
- **Unity CLI 가 붙어 있으면** 코드를 쓴 뒤 컴파일 결과·콘솔·테스트를 **직접 확인하고** 보고하라. 확인 없이 "완료"라고 쓰지 마라.
  ```bash
  unity status --no-banner                                   # state 가 "ready" 여야 한다
  unity command recompile --no-banner                        # → recompile_status 로 폴링
  unity command console --tail 50 --level error --no-banner
  unity command run_tests --mode EditMode --no-banner
  ```
  붙지 않으면 에디터가 꺼져 있거나 **Safe Mode**(컴파일 에러로 부팅 실패)다. 파일 직접 편집으로 도망가지 마라.
- **씬·프리팹 연결은 CLI 로 한다.** `.unity` / `.prefab` YAML 을 손으로 만지지 마라.
  (`create_gameobject`, `attach_script`, `set_serialized_field`, `instantiate_prefab`, `save_prefab_contents` …)
- **그 외 쓰기는 태스크에 명시된 것만.** 검증 목적이라며 씬이나 에셋을 임의로 고치지 마라.
- 기존 Unity **MCP 7개 도구**는 보조로 남아 있다. 새 작업의 기본 수단으로 고르지 마라.
- 결정을 내렸으면 `.docs/adr/` 에 한 장 남겨라. 짧게.

---

## 6. 문서 지도

| 파일 | 내용 |
|---|---|
| `.docs/컨셉기획서.md` | 컨셉·차별점·포지셔닝·로드맵 (외부 설명용). **PDF는 여기서 생성되는 파생물** |
| `.docs/GDD.md` | 게임 규칙과 용어의 **단일 진실 원천** |
| `.docs/ARCHITECTURE.md` | 시스템 구조, **확정된 클래스/API 시그니처** |
| `.docs/HANDOFF-2026-08-30.md` | **세션 인수인계. 새 세션이면 이것부터 읽어라** — 최근 변경·확정 규칙·함정 |
| `.docs/TASKS-P1-prototype.md` | 프로토타입 실행 태스크 (순서대로). **단계 7(G-00~G-21)이 P-21 게이트보다 먼저다** |
| `.docs/TASKS-P2.md` | P2 태스크 (Q-01~Q-14). **P-21 게이트 통과 전 착수 금지** |
| `.docs/회의록/*.md` | 회의 기록과 확정 결정. **날짜별. 결정 번호로 참조한다** |
| `.docs/PLAN-authoring-pipeline.md` | **기획자 저작 파이프라인 계획** — 맵·스폰을 텍스트 파일로 옮기는 단계별 계획 (S-01~S-05) |
| `.docs/PMF-todo.md` | 팀·사업·로드맵 레벨 할 일 |
| `.docs/adr/*.md` | 결정과 그 이유 |

**용어가 헷갈리면 GDD의 용어표를, 함수 이름이 헷갈리면 ARCHITECTURE의 시그니처를 따르라. 새로 지어내지 마라.**

### 문서를 고칠 때

- **마크다운이 원본이다.** PDF는 파생물이니 직접 편집하지 마라.
- 컨셉기획서를 고쳤으면 `pwsh .docs/build-pdf.ps1` 로 PDF를 다시 뽑는다.
- `.docs/무빙 타워 컨셉 기획서_old_20260820.pdf` 는 **기획자가 쓴 원본 백업**이다. 내용이 낡았으니 근거로 삼지 마라. 참고만 하고 수정하지 마라.
