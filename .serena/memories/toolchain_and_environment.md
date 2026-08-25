# 도구·환경 제약 (코드만 봐서는 알 수 없는 것들)

## PowerShell 7 은 반드시 MSI 판(Program Files)이어야 한다

**2026-08-26 해결.** 이 PC는 원래 PowerShell 7 이 **Store(MSIX)** 판만 있었다
(`%LOCALAPPDATA%\Microsoft\WindowsApps\pwsh.exe` = 앱 실행 별칭 스텁).

그 결과 VS Code 확장 **Cline**(`saoudrizwan.claude-dev`)이 터미널을 못 띄웠다:
> 셸 실행 파일 "C:\Program Files\PowerShell\7\pwsh.exe"의 경로가 없습니다.

- 이 경로는 **어떤 VS Code 설정에도 없었다.** 확장이 pwsh 의 표준 설치 위치를 스스로 가정한다.
- 앱 실행 별칭 스텁은 `Test-Path` 로는 존재하고 셸에서 실행도 되지만, **확장이 프로세스를 직접 생성할 때 실패**한다. 개발 도구가 깨지는 전형적 원인이다.
- `winget install --id Microsoft.PowerShell` 은 이제 **msixbundle 만** 제공한다(2026-08-14 릴리스 기준). `--installer-type msi` 는 "적용 가능한 설치 관리자를 찾을 수 없습니다"(exit 16)로 실패한다.
- **해결:** GitHub 릴리스에서 MSI 직접 설치
  `https://github.com/PowerShell/PowerShell/releases/download/v7.6.5/PowerShell-7.6.5-win-x64.msi`
  → `msiexec /i <msi> /qb /norestart ADD_PATH=1 REGISTER_MANIFEST=1` (**UAC 승인 필요** — 이 세션은 관리자 권한 없음)
- MSIX 판은 지우지 않고 공존시켰다. Machine PATH 의 `C:\Program Files\PowerShell\7` 가 우선한다.

**VS Code 프로필도 후보 배열로 바꿔 뒀다** (`%APPDATA%\Code\User\settings.json`).
VS Code 는 `path` 가 배열이면 **존재하는 첫 항목**을 쓴다. 특정 경로에 묶이지 않는다.
백업: `settings.json.bak-20260826-003942`

## 문서 → PDF 파이프라인

**마크다운이 원본, PDF는 파생물이다.** 재생성: `pwsh .docs/build-pdf.ps1`

- 파이프라인: Markdown → **pandoc** → HTML → **Chrome 헤드리스 `--print-to-pdf`** → PDF
- **pandoc 의 `--pdf-engine` 을 쓰려고 하지 마라.** LaTeX 계열 엔진이 없고, 있어도 한글 CJK 폰트 설정이 번거롭다. Chrome은 시스템 폰트를 그대로 써서 한글이 깨지지 않는다.
- 스타일: `.docs/doc-print.css` (A4, 표·인용·코드블록 인쇄 스타일)
- 출력 파일명은 마크다운의 **첫 h1** 과 본문의 `버전 X.Y` 에서 자동 생성

## 설치 경로 (기본 위치가 아닌 것들)

| 도구 | 경로 | 비고 |
|---|---|---|
| pandoc 3.10.2 | `%LOCALAPPDATA%\Pandoc\pandoc.exe` | winget이 **user scope** 로 설치. `C:\Program Files\Pandoc` 에 **없다** |
| pwsh 7.6.5 | `C:\Program Files\PowerShell\7\pwsh.exe` | MSI 판(위 참조). MSIX 판도 WindowsApps 에 공존 |
| Chrome | `C:\Program Files\Google\Chrome\Application\chrome.exe` | |
| pdftotext | `/mingw64/bin/pdftotext.exe` (Git Bash) | **pdftoppm / pdfinfo 는 없다** — PDF를 이미지로 렌더 불가 |
| python | `C:\Program Files\Python312\python` (3.12.9) | `markdown`, `pypdf` 등 서드파티 없음 |

→ Read 툴로 PDF를 열면 pdftoppm 부재로 실패한다. **`pdftotext -enc UTF-8` 로 텍스트를 뽑아라.**
→ **winget 설치 후 실행파일 경로를 가정하지 마라.** user scope 로 갈 수 있으니 매번 찾아라.

## ⚠️ 백슬래시 함정 세 가지 (전부 실제로 당함)

**1. Bash 툴 heredoc** — 명령 문자열이 JSON 을 거치며 `\\` → `\` 로 줄어든다.
`.docs\\build-pdf.ps1` 이 Python 에서 `\b`(백스페이스 0x08)로 해석되어 문서에 제어문자가 박혔다.

**2. PowerShell `-replace`** — 패턴이 정규식이라 `'\'` 하나는 "유효하지 않은 정규식" 에러.
경로→URI 변환은 `([uri]$path).AbsoluteUri` 를 써라. 백슬래시가 아예 필요 없다.

**3. Python `re.sub` 의 치환 문자열** — 치환부의 백슬래시도 **다시** 이스케이프 해석된다.
JSON 에 `\\` 를 넣으려고 `\\` 를 넣으면 `\` 가 나와 파일이 깨진다.
→ **치환을 함수로 넘겨라.** 함수 반환값은 이스케이프 처리되지 않는다. 값 생성은 `json.dumps()` 에 맡겨라.

**공통 대처:**
- 경로는 **슬래시**로 쓴다
- 백슬래시가 꼭 필요하면 Python `chr(92)`
- 긴 스크립트는 heredoc 대신 **Write 툴로 파일을 만들어** 실행한다
- 설정 파일을 고쳤으면 **쓰기 전에 파싱 검증**하고, 제어문자를 검사한다:
  `[c for c in text if ord(c)<32 and c not in chr(10)+chr(9)]`
- 남의 설정 파일을 고칠 땐 **먼저 백업**한다

## Unity 검증 제약

- **Unity 에디터를 이 세션에서 연 적이 없다.** 컴파일·플레이 검증은 수행되지 않았다.
- 스크립트를 만들었다면 "동작 확인됨"이라고 쓰지 마라. `CLAUDE.md` §5 의 규칙이 이것 때문이다.
- 프로젝트에 git 이 아직 없다. 첫 태스크 P-00 이 `git init` 이다.

관련: `mem:project_overview`
