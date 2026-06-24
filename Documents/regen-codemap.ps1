<#
.SYNOPSIS
    소스 코드의 XML <summary> 주석을 추출하여 CODE_MAP.md(전체 파일 색인)를 재생성한다.

.DESCRIPTION
    이 스크립트는 "어느 파일에 어느 코드가 있는지"를 한눈에 보는 색인을 자동 생성한다.
    단일 진실 원천(SSOT)은 각 .cs 파일의 클래스 선언 바로 위 XML <summary> 주석이다.
    (code-style-guide.md: 모든 public 클래스/메서드에 XML 주석 의무)

    - 요약이 있는 파일 → 첫 문장을 "책임 요약"으로 표기
    - 요약이 없는 파일 → ⚠️ 마커로 표기 (코드에 <summary> 보강을 유도)

.USAGE
    Documents 폴더 기준 또는 리포 루트에서:
        pwsh Documents/regen-codemap.ps1
    결과: Documents/CODE_MAP.md 덮어쓰기
#>

$ErrorActionPreference = 'Stop'

# ── 경로 설정 ──────────────────────────────────────────
$RepoRoot = Split-Path -Parent $PSScriptRoot   # Documents/ 의 상위 = 리포 루트
$OutFile  = Join-Path $PSScriptRoot 'CODE_MAP.md'

# 스캔 대상 폴더 → 표 섹션 제목 (출력 순서대로)
$Sections = [ordered]@{
    'Program.cs'      = @{ Title = '진입점 (Entry Point)';        Path = $RepoRoot;                    Recurse = $false; Filter = 'Program.cs' }
    'Core'            = @{ Title = 'Core — 비즈니스 로직';        Path = (Join-Path $RepoRoot 'Core');           Recurse = $false }
    'Core/Quant'      = @{ Title = 'Core/Quant — 퀀트 분석';      Path = (Join-Path $RepoRoot 'Core\Quant');     Recurse = $false }
    'Core/Advisors'   = @{ Title = 'Core/Advisors — 컨텍스트 조언'; Path = (Join-Path $RepoRoot 'Core\Advisors'); Recurse = $false }
    'Controllers'     = @{ Title = 'Controllers — REST API';      Path = (Join-Path $RepoRoot 'Controllers');    Recurse = $false }
    'Data/DTO'        = @{ Title = 'Data/DTO — 데이터 전송 객체';  Path = (Join-Path $RepoRoot 'Data\DTO');       Recurse = $false }
    'Data/DAO'        = @{ Title = 'Data/DAO — DB 접근';          Path = (Join-Path $RepoRoot 'Data\DAO');       Recurse = $false }
    'Data'            = @{ Title = 'Data — DB/설정 관리';         Path = (Join-Path $RepoRoot 'Data');           Recurse = $false }
    'Utils'           = @{ Title = 'Utils — 유틸리티/통신';       Path = (Join-Path $RepoRoot 'Utils');          Recurse = $false }
}

# 기본 타입 선언 정규식: public/internal (한정자) class|interface|enum|record|struct 이름
$TypeDeclRe   = '^\s*(?:public|internal)\s+(?:static\s+|abstract\s+|sealed\s+|partial\s+)*(class|interface|enum|record|struct)\s+([A-Za-z_]\w*)'
# public 메서드 선언 (async 포함, 생성자/프로퍼티 제외 위해 괄호 필수)
$MethodRe     = '^\s*public\s+(?:static\s+|async\s+|virtual\s+|override\s+|sealed\s+)*[A-Za-z_][\w<>,\.\?\[\]\s]*\s+([A-Za-z_]\w*)\s*\('

# ── 한 파일에서 (타입종류, 타입명, 요약, public 메서드[]) 추출 ──
function Get-FileInfo {
    param([string]$FilePath)

    # 소스는 UTF-8(BOM 없음) → 한글 깨짐 방지 위해 명시적 UTF8 디코딩
    $lines = Get-Content -LiteralPath $FilePath -Encoding UTF8

    # 모든 타입 선언을 수집한 뒤, 파일명과 같은 타입을 우선 선택
    # (헬퍼 enum/class가 주 클래스보다 먼저 나오는 파일 대응 — 예: SmartOrderEngine.cs)
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($FilePath)
    $candidates = New-Object System.Collections.Generic.List[object]
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $m = [regex]::Match($lines[$i], $TypeDeclRe)
        if ($m.Success) {
            $candidates.Add([pscustomobject]@{ Idx = $i; Kind = $m.Groups[1].Value; Name = $m.Groups[2].Value })
        }
    }
    if ($candidates.Count -eq 0) {
        return [pscustomobject]@{ TypeName = '(타입 없음)'; TypeKind = ''; Summary = $null; Methods = @() }
    }
    $chosen = $candidates | Where-Object { $_.Name -eq $baseName } | Select-Object -First 1
    if (-not $chosen) { $chosen = $candidates[0] }
    $typeKind = $chosen.Kind; $typeName = $chosen.Name; $typeLineIdx = $chosen.Idx

    # ── 타입 선언 바로 위로 거슬러 올라가며 /// 주석 블록 수집 (어트리뷰트/빈줄 건너뜀) ──
    $commentLines = New-Object System.Collections.Generic.List[string]
    for ($j = $typeLineIdx - 1; $j -ge 0; $j--) {
        $t = $lines[$j].Trim()
        if ($t -like '///*')      { $commentLines.Insert(0, $t.Substring(3).Trim()); continue }
        if ($t -like '[[]*[]]')   { continue }   # [ApiController] 등 어트리뷰트
        if ($t -eq '')            { continue }   # 빈 줄
        break                                     # 그 외 코드 → 블록 종료
    }

    # <summary> 내용 추출
    $summary = $null
    if ($commentLines.Count -gt 0) {
        $joined = ($commentLines -join ' ')
        $sm = [regex]::Match($joined, '<summary>(.*?)</summary>', 'Singleline')
        $raw = if ($sm.Success) { $sm.Groups[1].Value } else { $joined }
        $raw = [regex]::Replace($raw, '<[^>]+>', '')          # 잔여 XML 태그 제거
        $raw = [regex]::Replace($raw, '\s+', ' ').Trim()      # 공백 정리
        # 첫 문장만 (마침표/개행 기준)
        $first = [regex]::Match($raw, '^(.*?[\.。])(\s|$)')
        if ($first.Success) { $raw = $first.Groups[1].Value }
        if ($raw.Length -gt 90) { $raw = $raw.Substring(0, 88) + '…' }
        if ($raw) { $summary = $raw }
    }

    # public 메서드 수집 (최대 5개)
    $methods = New-Object System.Collections.Generic.List[string]
    foreach ($ln in $lines) {
        $mm = [regex]::Match($ln, $MethodRe)
        if ($mm.Success) {
            $name = $mm.Groups[1].Value
            if ($name -ne $typeName -and -not $methods.Contains($name)) { $methods.Add($name) }
        }
    }
    $methodList = @($methods | Select-Object -First 5)

    return [pscustomobject]@{
        TypeName = $typeName
        TypeKind = $typeKind
        Summary  = $summary
        Methods  = $methodList
    }
}

function Escape-Md { param([string]$s) if ($null -eq $s) { return '' } $s -replace '\|', '\|' }

# ── 본문 생성 ──────────────────────────────────────────
$sb = New-Object System.Text.StringBuilder
$null = $sb.AppendLine('# 🗺️ AutoInvesting 코드 맵 (전체 파일 색인)')
$null = $sb.AppendLine()
$null = $sb.AppendLine('> "어느 파일에 어느 코드가 있는지" 한눈에 찾는 자동 생성 색인입니다.')
$null = $sb.AppendLine('> **이 파일을 직접 수정하지 마세요.** 각 소스 파일의 XML `<summary>` 주석이 진실 원천이며,')
$null = $sb.AppendLine('> `pwsh Documents/regen-codemap.ps1` 실행으로 재생성됩니다.')
$null = $sb.AppendLine('>')
$null = $sb.AppendLine('> ⚠️ 표시 = 해당 파일에 클래스 `<summary>` 주석이 없습니다 → 코드에 추가하면 다음 재생성 때 채워집니다.')
$null = $sb.AppendLine()

$missing = New-Object System.Collections.Generic.List[string]
$total = 0

foreach ($key in $Sections.Keys) {
    $sec = $Sections[$key]
    if (-not (Test-Path $sec.Path)) { continue }

    $filterArgs = @{ LiteralPath = $sec.Path; Filter = '*.cs'; File = $true }
    if ($sec.ContainsKey('Filter')) { $filterArgs.Filter = $sec.Filter }
    $files = Get-ChildItem @filterArgs | Sort-Object Name

    if (-not $files) { continue }

    $null = $sb.AppendLine("## $($sec.Title)")
    $null = $sb.AppendLine()
    $null = $sb.AppendLine('| 파일 | 타입 | 책임 요약 | 핵심 멤버 |')
    $null = $sb.AppendLine('|------|------|-----------|-----------|')

    foreach ($f in $files) {
        $info = Get-FileInfo -FilePath $f.FullName
        $total++
        $rel = $key
        $summaryCell = if ($info.Summary) { Escape-Md $info.Summary } else { '⚠️ (요약 없음)' }
        if (-not $info.Summary) { $missing.Add("$rel/$($f.Name)") }
        $membersCell = if ($info.Methods.Count -gt 0) { '`' + ($info.Methods -join '`, `') + '`' } else { '—' }
        $null = $sb.AppendLine("| ``$($f.Name)`` | $($info.TypeKind) | $summaryCell | $membersCell |")
    }
    $null = $sb.AppendLine()
}

# ── 요약 통계 ──
$null = $sb.AppendLine('---')
$null = $sb.AppendLine()
$null = $sb.AppendLine("**총 ${total}개 파일** · 요약 없는 파일 **$($missing.Count)개**")
if ($missing.Count -gt 0) {
    $null = $sb.AppendLine()
    $null = $sb.AppendLine('<details><summary>⚠️ XML &lt;summary&gt; 보강이 필요한 파일</summary>')
    $null = $sb.AppendLine()
    foreach ($m in $missing) { $null = $sb.AppendLine("- ``$m``") }
    $null = $sb.AppendLine()
    $null = $sb.AppendLine('</details>')
}

# UTF-8 (BOM 없이) 저장 — 한글 깨짐 방지
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($OutFile, $sb.ToString(), $utf8NoBom)

Write-Host "✅ CODE_MAP.md 생성 완료: $OutFile"
Write-Host "   총 ${total}개 파일, 요약 없음 $($missing.Count)개"
