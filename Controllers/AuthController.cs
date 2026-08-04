using AutoInvest.Data;
using AutoInvest.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AutoInvest.Controllers
{
    /// <summary>
    /// 단일 관리자 로그인 API. 비밀번호 검증 후 서명된 세션 토큰(7일)을 발급합니다.
    ///
    /// <b>인증 면제는 <c>status</c>·<c>login</c>에만 붙입니다(닭-달걀 방지).</b>
    /// <c>setup</c>은 전역 인증 필터를 그대로 받습니다 — 예전처럼 컨트롤러 전체를
    /// <see cref="PublicEndpointAttribute"/>로 열어두면, 관리자 자리가 비어 보이는 순간
    /// (최초 배포 직후이거나 DB 조회 실패로 오판했을 때) 누구나 먼저 관리자를 선점하고
    /// 그 토큰으로 실주문까지 낼 수 있습니다. 최초 설정은 <c>x-api-key</c>를 붙여 호출하세요.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private const string UserKey = "ADMIN_USERNAME";
        private const string HashKey = "ADMIN_PASSWORD_HASH";
        private const int TokenDays = 7;

        /// <summary>실패한 로그인 1건마다 넣는 고정 지연. 자동화된 추측의 회전 속도를 늦춘다.</summary>
        private static readonly TimeSpan FailureDelay = TimeSpan.FromSeconds(1);

        /// <summary>
        /// 저장된 관리자 비밀번호 해시를 읽고 <b>"판정이 가능한지"</b>를 반환합니다.
        ///
        /// 이 값이 비었는지 여부로 "관리자 미설정"을 판단하는 곳이 status·setup·login 세 곳인데,
        /// <see cref="AppConfigManager.Get"/>은 조회 실패와 값 없음을 모두 기본값으로 뭉갭니다.
        /// 그대로 쓰면 DB가 잠깐 안 읽히는 순간 "계정이 없다"로 오판해, 소유자에게 로그인 거부와
        /// "계정을 새로 만들라"는 잘못된 안내를 내보냅니다. 세 곳이 같은 판정을 쓰도록 여기로 모읍니다.
        /// </summary>
        /// <param name="hash">저장된 해시 (없으면 빈 문자열)</param>
        /// <returns>DB 조회에 성공해 판정할 수 있으면 true, 조회 실패면 false</returns>
        private static bool TryGetAdminHash(out string hash)
        {
            hash = string.Empty;
            if (!AppConfigManager.TryReadDb(HashKey, out string? row)) return false;

            hash = row ?? string.Empty;

            // DB에 행이 없을 때만 환경변수/appsettings 폴백을 본다. Login이 Get으로 읽는 값과
            // 판정 소스를 일치시키기 위함이다(여기만 DB 전용이면 "설정됨"을 놓치고 덮어쓴다).
            if (string.IsNullOrWhiteSpace(hash)) hash = AppConfigManager.Get(HashKey, "");

            return true;
        }

        /// <summary>
        /// 인증 상태를 반환합니다. 최초 비밀번호 설정이 필요한지(needsSetup) 프론트가 분기합니다.
        /// 설정 여부를 확인할 수 없으면 needsSetup을 추측하지 않고 503을 반환합니다.
        /// </summary>
        [HttpGet("status")]
        [PublicEndpoint]
        public IActionResult GetStatus()
        {
            if (!TryGetAdminHash(out string hash))
            {
                Logger.Warn("[Auth] 설정 저장소 조회 실패 — needsSetup 판정 불가");
                return StatusCode(503, new { error = "설정 저장소를 확인할 수 없습니다. 잠시 후 다시 시도하세요." });
            }

            return Ok(new { needsSetup = string.IsNullOrWhiteSpace(hash) });
        }

        /// <summary>
        /// 최초 1회 관리자 계정을 설정합니다. 이미 설정되었거나 설정 여부를 확인할 수 없으면 거부합니다.
        /// 전역 인증 필터가 적용되므로 Bearer 토큰 또는 <c>x-api-key</c>가 필요합니다.
        /// </summary>
        [HttpPost("setup")]
        public IActionResult Setup([FromBody] CredentialRequest req)
        {
            // 설정 여부를 확인할 수 없으면 거부한다(fail-closed). 조회 실패를 "미설정"으로 오판하면
            // 기존 계정을 덮어쓰게 된다.
            if (!TryGetAdminHash(out string existingHash))
            {
                Logger.Error("[Auth] 관리자 설정 여부를 확인할 수 없어 최초 설정을 거부했습니다 (DB 조회 실패).");
                return StatusCode(503, new { error = "설정 저장소를 확인할 수 없어 요청을 거부했습니다. 잠시 후 다시 시도하세요." });
            }

            if (!string.IsNullOrWhiteSpace(existingHash))
                return Conflict(new { error = "이미 관리자 계정이 설정되어 있습니다." });

            if (req == null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { error = "아이디와 비밀번호를 입력하세요." });

            if (req.Password.Length < 8)
                return BadRequest(new { error = "비밀번호는 8자 이상이어야 합니다." });

            // 사용자명 → 해시 순서를 지킨다. 도중에 실패해도 해시가 비어 있어 이 엔드포인트로 재시도할 수
            // 있다(순서를 뒤집으면 해시만 남아 setup은 409, 로그인은 사용자명 공백으로 거부되는 잠김이 된다).
            bool userSaved = AppConfigManager.Set(UserKey, req.Username.Trim());
            bool hashSaved = AppConfigManager.Set(HashKey, CryptoUtil.HashPassword(req.Password));

            if (!userSaved || !hashSaved)
            {
                Logger.Error("[Auth] 관리자 계정 저장 실패 — 설정이 반영되지 않았습니다.");
                return StatusCode(500, new { error = "설정을 저장하지 못했습니다. 잠시 후 다시 시도하세요." });
            }

            Logger.Info("[Auth] 관리자 계정 최초 설정 완료");

            return Ok(new { message = "관리자 계정이 설정되었습니다. 로그인하세요." });
        }

        /// <summary>
        /// 로그인. 성공 시 7일 만료 세션 토큰을 발급합니다.
        /// 실패가 <see cref="LoginThrottle"/>의 창당 상한을 넘기면 그 창이 끝날 때까지 429로 거부합니다.
        /// </summary>
        [HttpPost("login")]
        [PublicEndpoint]
        public async Task<IActionResult> Login([FromBody] CredentialRequest req)
        {
            DateTime now = DateTime.UtcNow;

            // 상한 검사는 비밀번호 검증(PBKDF2 12만 회)보다 반드시 앞에 둔다 — 뒤에 두면
            // 거부되는 요청도 CPU를 태우므로 추측 차단과 자원 소모 공격 차단이 둘 다 무너진다.
            // 유효한 x-api-key를 가진 소유자는 면제한다(공격자는 위조할 수 없고, 공격 중에도
            // 소유자가 들어올 통로가 남는다).
            if (!HasValidApiKey() && LoginThrottle.IsRateLimited(now, out TimeSpan wait))
            {
                Logger.Warn($"[Auth] 로그인 시도 거부 — 실패 상한 초과, {Math.Ceiling(wait.TotalSeconds)}초 후 재시도 가능");
                Response.Headers["Retry-After"] = ((int)Math.Ceiling(wait.TotalSeconds)).ToString();
                return StatusCode(429, new
                {
                    error = $"로그인 시도가 너무 많습니다. {Math.Ceiling(wait.TotalSeconds)}초 후 다시 시도하세요."
                });
            }

            if (!TryGetAdminHash(out string storedHash))
            {
                Logger.Warn("[Auth] 설정 저장소 조회 실패 — 로그인 판정 불가");
                return StatusCode(503, new { error = "설정 저장소를 확인할 수 없습니다. 잠시 후 다시 시도하세요." });
            }

            if (string.IsNullOrWhiteSpace(storedHash))
                return BadRequest(new { error = "관리자 계정이 아직 설정되지 않았습니다.", needsSetup = true });

            if (req == null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { error = "아이디와 비밀번호를 입력하세요." });

            string storedUser = AppConfigManager.Get(UserKey, "");

            // 아이디가 틀려도 해시 검증을 수행한다(단축평가 금지) — 응답 시간 차이로 아이디 존재 여부가
            // 새어나가지 않게 하기 위함이다. 비용은 위 속도 상한이 이미 묶어 두었다.
            bool userOk = string.Equals(req.Username.Trim(), storedUser, StringComparison.Ordinal);
            bool passOk = CryptoUtil.VerifyPassword(req.Password, storedHash);

            if (!userOk || !passOk)
            {
                LoginThrottle.RegisterFailure(now);
                Logger.Warn("[Auth] 로그인 실패 (자격증명 불일치)");
                await Task.Delay(FailureDelay);
                return Unauthorized(new { error = "아이디 또는 비밀번호가 올바르지 않습니다." });
            }

            LoginThrottle.Reset();

            DateTime expires = DateTime.UtcNow.AddDays(TokenDays);
            string? token = CryptoUtil.IssueToken(storedUser, expires);

            if (token == null)
            {
                Logger.Error("[Auth] 토큰 서명 키 부재 — MASTER_KEY 또는 AUTH_TOKEN_SECRET 설정 필요");
                return StatusCode(500, new { error = "서버에 토큰 서명 키가 설정되지 않았습니다. (MASTER_KEY)" });
            }

            Logger.Info("[Auth] 로그인 성공, 토큰 발급");
            return Ok(new { token, expiresAt = expires });
        }

        /// <summary>
        /// 요청이 서버에 설정된 <c>x-api-key</c>를 그대로 들고 왔는지 확인합니다.
        /// 로그인은 <see cref="PublicEndpointAttribute"/>라 전역 필터를 타지 않으므로 여기서 직접 봅니다.
        /// 실패 상한에 걸린 동안에도 소유자가 들어올 통로를 남기는 용도입니다(키가 없으면 면제도 없음).
        /// </summary>
        private bool HasValidApiKey()
        {
            string serverKey = AppConfigManager.Get("API_ACCESS_KEY", "");
            if (string.IsNullOrWhiteSpace(serverKey)) return false;

            return Request.Headers.TryGetValue("x-api-key", out var provided)
                && serverKey.Equals(provided.ToString(), StringComparison.Ordinal);
        }

        /// <summary>로그인 요청 본문.</summary>
        public class CredentialRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
    }
}
