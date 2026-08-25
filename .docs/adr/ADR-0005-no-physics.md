# ADR-0005. 물리 엔진을 쓰지 않는다

- 상태: 채택
- 날짜: 2026-08-25

## 결정

`Rigidbody2D`, `Collider2D`, `Physics2D` 를 쓰지 않는다.

- 사거리 판정 → `TargetRegistry` + `sqrMagnitude` 비교
- 클릭 판정 → `WorldToCell` 로 셀 역변환
- 이동 → `transform.position` 직접 대입

## 이유

- 타워 디펜스에 물리 시뮬레이션이 필요한 지점이 하나도 없다. 충돌 반응도, 중력도, 관성도 쓰지 않는다.
- `OverlapCircle` 계열은 레이어 마스크 설정, 트리거/논트리거 구분, `Rigidbody2D` 부착 여부에 따라 조용히 동작이 달라진다. 원인 찾기 어려운 버그의 주요 출처다.
- 결정성이 확보된다. 같은 입력이면 같은 결과가 나와 밸런스 테스트가 신뢰할 수 있다.
- **Unity 6.x 는 2D 물리 백엔드가 교체되는 과도기다.** (`com.unity.modules.physicscore2d` 의 등장) 물리에 의존하지 않으면 버전 업그레이드 리스크에서 자유롭다. 이 프로젝트는 6.5 → 6.6 → 6.7 LTS 로 올라갈 예정이므로 실질적인 이득이다.
- 물리를 안 쓰면 `FixedUpdate` / `Update` 이원화가 사라져 배속(`Time.timeScale`) 처리가 단순해진다.

## 대가

- `TargetRegistry` 를 직접 관리해야 하고, `OnEnable`/`OnDisable` 짝이 어긋나면 유령 타겟이 생긴다. (P-09, P-19 의 함정 항목)
- 대상 수가 수백을 넘으면 선형 탐색이 부담이 된다. 그때 공간 분할(격자 버킷)을 도입한다 — 그 전에는 하지 않는다.
