using AutoInvest.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;

namespace AutoInvest.Utils
{
    /// <summary>
    /// 글로벌 인증 필터.
    /// 사람(브라우저)은 로그인으로 발급받은 Bearer 세션 토큰으로, 크론/머신은 기존 x-api-key로 통과합니다.
    /// 둘 중 하나만 유효하면 허용하며, <see cref="PublicEndpointAttribute"/>가 붙은 액션은 검사를 면제합니다.
    /// </summary>
    public class ApiKeyAuthAttribute : ActionFilterAttribute
    {
        private const string API_KEY_HEADER = "x-api-key";
        private const string AUTH_HEADER = "Authorization";

        /// <summary>
        /// 통과한 자격증명 종류를 담는 <c>HttpContext.Items</c> 키. 값은 <see cref="AuthKindSession"/>
        /// 또는 <see cref="AuthKindApiKey"/>다.
        ///
        /// 두 자격증명은 권한이 같지 않다 — 세션 토큰은 사람이 비밀번호로 얻지만, x-api-key는
        /// GitHub Actions Secret에 있어 노출 표면이 다르다. 시크릿 평문 열람처럼 사람만 해야 하는
        /// 동작은 이 표식으로 구분한다. 표식이 없으면(필터를 타지 않았으면) 사람이 아닌 것으로 본다.
        /// </summary>
        public const string AuthKindItemKey = "AuthKind";

        /// <summary>사람이 로그인해 받은 Bearer 세션 토큰으로 통과.</summary>
        public const string AuthKindSession = "session";

        /// <summary>크론·머신이 x-api-key로 통과.</summary>
        public const string AuthKindApiKey = "apikey";

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // ── 공개 엔드포인트(로그인/설정/상태)는 인증 면제 ──
            var endpoint = context.HttpContext.GetEndpoint();
            if (endpoint?.Metadata.GetMetadata<PublicEndpointAttribute>() != null)
            {
                await next();
                return;
            }

            // ── 1) Bearer 세션 토큰 (브라우저) ──
            if (context.HttpContext.Request.Headers.TryGetValue(AUTH_HEADER, out var authHeader))
            {
                string raw = authHeader.ToString();
                if (raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    string token = raw.Substring("Bearer ".Length).Trim();
                    if (CryptoUtil.TryValidateToken(token, out _))
                    {
                        context.HttpContext.Items[AuthKindItemKey] = AuthKindSession;
                        await next();
                        return;
                    }
                }
            }

            // ── 2) x-api-key (크론/머신) ──
            if (context.HttpContext.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey))
            {
                var serverApiKey = AppConfigManager.Get("API_ACCESS_KEY", "");
                if (!string.IsNullOrWhiteSpace(serverApiKey) && serverApiKey.Equals(extractedApiKey.ToString()))
                {
                    context.HttpContext.Items[AuthKindItemKey] = AuthKindApiKey;
                    await next();
                    return;
                }
            }

            // ── 둘 다 실패 ──
            context.Result = new UnauthorizedObjectResult(new { error = "인증이 필요합니다. 로그인하거나 유효한 API 키를 제공하세요." });
        }
    }
}
