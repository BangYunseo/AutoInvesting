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
                    await next();
                    return;
                }
            }

            // ── 둘 다 실패 ──
            context.Result = new UnauthorizedObjectResult(new { error = "인증이 필요합니다. 로그인하거나 유효한 API 키를 제공하세요." });
        }
    }
}
