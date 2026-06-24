using AutoInvest.Data;
using AutoInvest.Utils;
using Microsoft.AspNetCore.Mvc;
using System;

namespace AutoInvest.Controllers
{
    /// <summary>
    /// 단일 관리자 로그인 API. 비밀번호 검증 후 서명된 세션 토큰(7일)을 발급합니다.
    /// 모든 액션은 <see cref="PublicEndpointAttribute"/>로 전역 인증 필터를 면제받습니다(닭-달걀 방지).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [PublicEndpoint]
    public class AuthController : ControllerBase
    {
        private const string UserKey = "ADMIN_USERNAME";
        private const string HashKey = "ADMIN_PASSWORD_HASH";
        private const int TokenDays = 7;

        /// <summary>
        /// 인증 상태를 반환합니다. 최초 비밀번호 설정이 필요한지(needsSetup) 프론트가 분기합니다.
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            bool needsSetup = string.IsNullOrWhiteSpace(AppConfigManager.Get(HashKey, ""));
            return Ok(new { needsSetup });
        }

        /// <summary>
        /// 최초 1회 관리자 계정을 설정합니다. 이미 설정된 경우 거부합니다.
        /// </summary>
        [HttpPost("setup")]
        public IActionResult Setup([FromBody] CredentialRequest req)
        {
            if (!string.IsNullOrWhiteSpace(AppConfigManager.Get(HashKey, "")))
                return Conflict(new { error = "이미 관리자 계정이 설정되어 있습니다." });

            if (req == null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { error = "아이디와 비밀번호를 입력하세요." });

            if (req.Password.Length < 8)
                return BadRequest(new { error = "비밀번호는 8자 이상이어야 합니다." });

            AppConfigManager.Set(UserKey, req.Username.Trim());
            AppConfigManager.Set(HashKey, CryptoUtil.HashPassword(req.Password));
            Logger.Info("[Auth] 관리자 계정 최초 설정 완료");

            return Ok(new { message = "관리자 계정이 설정되었습니다. 로그인하세요." });
        }

        /// <summary>
        /// 로그인. 성공 시 7일 만료 세션 토큰을 발급합니다.
        /// </summary>
        [HttpPost("login")]
        public IActionResult Login([FromBody] CredentialRequest req)
        {
            string storedUser = AppConfigManager.Get(UserKey, "");
            string storedHash = AppConfigManager.Get(HashKey, "");

            if (string.IsNullOrWhiteSpace(storedHash))
                return BadRequest(new { error = "관리자 계정이 아직 설정되지 않았습니다.", needsSetup = true });

            if (req == null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { error = "아이디와 비밀번호를 입력하세요." });

            bool userOk = string.Equals(req.Username.Trim(), storedUser, StringComparison.Ordinal);
            bool passOk = CryptoUtil.VerifyPassword(req.Password, storedHash);

            if (!userOk || !passOk)
            {
                Logger.Warn("[Auth] 로그인 실패 (자격증명 불일치)");
                return Unauthorized(new { error = "아이디 또는 비밀번호가 올바르지 않습니다." });
            }

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

        /// <summary>로그인 요청 본문.</summary>
        public class CredentialRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
    }
}
