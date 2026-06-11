# secret-guard.ps1
# PreToolUse 훅: 시크릿/개인정보가 커밋에 포함되는 것을 차단한다.
# 조직 보안정책(DB 접속정보·개인정보) 및 .agents/rules/security.md 를 강제하는 가드레일.
#
# 동작:
#   - stdin 으로 도구 호출 JSON 을 받는다.
#   - 명령어 문자열 자체에 개인정보/시크릿 패턴이 있으면 차단.
#   - git commit 시 스테이징된 파일명/변경분에 시크릿이 있으면 차단.
# 종료코드 2 = 차단(메시지를 Claude 에 전달), 0 = 통과.

$ErrorActionPreference = 'Stop'

$raw = [Console]::In.ReadToEnd()
if ([string]::IsNullOrWhiteSpace($raw)) { exit 0 }

try { $payload = $raw | ConvertFrom-Json } catch { exit 0 }

$cmd = ""
if ($payload.tool_input -and $payload.tool_input.command) {
    $cmd = [string]$payload.tool_input.command
}
if ([string]::IsNullOrWhiteSpace($cmd)) { exit 0 }

# ── 차단 대상 패턴 ──
$patterns = @(
    @{ name = '주민등록번호';        re = '\b\d{6}-\d{7}\b' },
    @{ name = '휴대폰번호';          re = '\b01[016789]-?\d{3,4}-?\d{4}\b' },
    @{ name = 'KIS AppKey/Secret(추정)'; re = '(?i)(appkey|appsecret)\s*[":=]\s*["'']?[A-Za-z0-9+/=]{30,}' },
    @{ name = 'Access Token(추정)';  re = '(?i)(bearer|access[_-]?token)\s*[":=]?\s*["'']?[A-Za-z0-9._\-]{30,}' }
)

function Test-Forbidden {
    param([string]$Text, [string]$Where)
    if ([string]::IsNullOrEmpty($Text)) { return }
    foreach ($p in $patterns) {
        if ($Text -match $p.re) {
            [Console]::Error.WriteLine("[secret-guard] 차단: $Where 에서 '$($p.name)' 패턴이 감지되었습니다.")
            [Console]::Error.WriteLine("조직 보안정책상 개인정보/시크릿은 커밋·전송할 수 없습니다. 값을 제거하고 appsettings.local.json 또는 환경변수로 분리하세요.")
            exit 2
        }
    }
}

# 1) 명령어 자체 검사 (예: git commit -m "...01012345678...")
Test-Forbidden -Text $cmd -Where '명령어'

# 2) git commit 시 스테이징 내용 검사
if ($cmd -match 'git\s+commit') {
    try { $staged = & git diff --cached --name-only } catch { $staged = @() }

    $forbiddenFiles = $staged | Where-Object {
        $_ -match '(appsettings\.local\.json$|\.secrets\.json$|\.local\.json$|\.key$|\.env$|\.db$)'
    }
    if ($forbiddenFiles) {
        [Console]::Error.WriteLine("[secret-guard] 차단: 시크릿 파일이 스테이징되었습니다 → $($forbiddenFiles -join ', ')")
        [Console]::Error.WriteLine(".gitignore 를 확인하고 'git restore --staged <파일>' 로 제외하세요.")
        exit 2
    }

    try { $diff = (& git diff --cached | Out-String) } catch { $diff = "" }
    Test-Forbidden -Text $diff -Where '스테이징된 변경분'
}

exit 0
