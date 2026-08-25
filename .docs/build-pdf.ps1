<#
.SYNOPSIS
  .docs 의 마크다운 문서를 PDF로 변환한다.

.DESCRIPTION
  마크다운이 원본이고 PDF는 파생물이다. 문서를 고칠 때는 .md 를 고치고 이 스크립트를 다시 돌린다.
  PDF를 직접 편집하지 마라.

  파이프라인: Markdown --(pandoc)--> HTML --(Chrome headless)--> PDF
  Chrome을 쓰는 이유: LaTeX 계열 PDF 엔진은 한글 폰트 설정이 번거롭다.
  Chrome은 시스템 폰트를 그대로 써서 한글이 깨지지 않는다.

.EXAMPLE
  pwsh .docs\build-pdf.ps1
  pwsh .docs\build-pdf.ps1 -Source 컨셉기획서.md
#>
param(
    [string[]] $Source = @('컨셉기획서.md')
)

$ErrorActionPreference = 'Stop'
$docs = $PSScriptRoot

# --- 의존성 확인 ---
$pandoc = @(
    "$env:LOCALAPPDATA\Pandoc\pandoc.exe",
    "$env:ProgramFiles\Pandoc\pandoc.exe",
    (Get-Command pandoc -ErrorAction SilentlyContinue).Source
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1

if (-not $pandoc) {
    throw "pandoc 을 찾을 수 없습니다. 설치: winget install --id JohnMacFarlane.Pandoc -e"
}

$chrome = @(
    "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
    "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
    "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe",
    "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $chrome) { throw "Chrome 또는 Edge 를 찾을 수 없습니다." }

$css = Join-Path $docs 'doc-print.css'
if (-not (Test-Path $css)) { throw "doc-print.css 가 없습니다: $css" }

$tmp = Join-Path ([System.IO.Path]::GetTempPath()) ("pmf-doc-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tmp | Out-Null

try {
    foreach ($name in $Source) {
        $md = Join-Path $docs $name
        if (-not (Test-Path $md)) { Write-Warning "건너뜀 (없음): $md"; continue }

        $base  = [System.IO.Path]::GetFileNameWithoutExtension($name)
        $html  = Join-Path $tmp "$base.html"
        $pdfTmp = Join-Path $tmp "$base.pdf"

        # 문서의 첫 h1 을 제목이자 출력 파일명으로 쓴다 (em-dash 는 공백으로)
        $title = (Select-String -Path $md -Pattern '^#\s+(.+)$' | Select-Object -First 1).Matches.Groups[1].Value
        if (-not $title) { $title = $base }
        $fileTitle = ($title -replace '\s*—\s*', ' ').Trim()

        Write-Host "[1/2] pandoc  : $name"
        & $pandoc $md `
            --standalone --embed-resources `
            --toc --toc-depth=2 `
            --css $css `
            --metadata title="$title" `
            --metadata lang=ko `
            -f gfm -t html5 `
            -o $html
        if ($LASTEXITCODE -ne 0) { throw "pandoc 실패: $name" }

        Write-Host "[2/2] chrome  : $base.pdf"
        $uri = ([uri]$html).AbsoluteUri
        & $chrome --headless --disable-gpu --no-sandbox --no-pdf-header-footer `
                  --virtual-time-budget=15000 --print-to-pdf="$pdfTmp" $uri 2>&1 | Out-Null

        if (-not (Test-Path $pdfTmp)) { throw "PDF 생성 실패: $base" }

        # 버전 표기가 있으면 파일명에 반영
        $ver = (Select-String -Path $md -Pattern '버전\s+([0-9]+\.[0-9]+)' | Select-Object -First 1)
        $suffix = if ($ver) { " v" + $ver.Matches.Groups[1].Value } else { "" }
        $out = Join-Path $docs ("$fileTitle$suffix.pdf")

        Copy-Item $pdfTmp $out -Force
        $kb = [math]::Round((Get-Item $out).Length / 1KB, 1)
        Write-Host "  ✔ $out  ($kb KB)" -ForegroundColor Green
    }
}
finally {
    Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
}
