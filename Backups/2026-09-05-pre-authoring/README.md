# 저작 파이프라인 착수 전 스냅샷 (2026-09-05)

## 왜 있나

`.toon` 임포터가 곧 `Stage_Greybox.asset` 과 맵 데이터를 **덮어쓰기 시작한다.**
그 전의 손으로 맞춘 맵·스폰·밸런스가 진실이므로 여기에 원본을 둔다.

`Assets/` **밖**에 두는 이유: Unity 가 임포트하지 않아야 한다.
`.asset` 사본이 프로젝트 안에 있으면 GUID 가 겹쳐 참조가 꼬인다.

## 무엇이 들어 있나

| 파일 | 무엇의 진실인가 |
|---|---|
| `Stage_Greybox.asset` | 스폰 리듬·버스트·경제·난이도·연출 등 31개 필드 |
| `Stage_Greybox.asset.meta` | 위 에셋의 GUID (참조를 살리려면 함께 되돌려야 한다) |
| `Stage_Greybox.unity` | 씬 (배치된 노드·마을·타일) |
| `GreyboxMapData.cs` | 32×18 맵 셀 데이터 — 현재 맵의 진실 |
| `Ally_CatFolk.asset` / `Ally_RatFolk.asset` | 아군 유닛 수치 |

## 되돌리는 법

**방법 1 — git (권장).** 이 시점이 태그로 박혀 있다.

    git show pre-authoring-baseline:Assets/_Project/Data/Stages/Stage_Greybox.asset > Assets/_Project/Data/Stages/Stage_Greybox.asset

전부 되돌리려면:

    git checkout pre-authoring-baseline -- Assets/_Project/Data Assets/_Project/Scenes Assets/_Project/Scripts/Editor/GreyboxMapData.cs

**방법 2 — 이 폴더에서 직접 복사.** git 이 꼬였을 때의 마지막 수단이다.

    cp Backups/2026-09-05-pre-authoring/Stage_Greybox.asset      Assets/_Project/Data/Stages/
    cp Backups/2026-09-05-pre-authoring/Stage_Greybox.asset.meta Assets/_Project/Data/Stages/
    cp Backups/2026-09-05-pre-authoring/Stage_Greybox.unity      Assets/_Project/Scenes/
    cp Backups/2026-09-05-pre-authoring/GreyboxMapData.cs        Assets/_Project/Scripts/Editor/

되돌린 뒤 Unity 에서 Reimport 하고, **수입 천장을 재측정하라** (SDD-05 §9).

## 이 폴더를 지워도 되는 때

임포터 왕복(툴 → .toon → 임포트 → 씬 재생성 → 플레이)이 확인되고,
한 사이클 더 지난 뒤. 그전에는 지우지 마라.
