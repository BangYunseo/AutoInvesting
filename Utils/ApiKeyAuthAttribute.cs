using AutoInvest.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace AutoInvest.Utils
{
    /// <summary>
    /// 글로벌 API 키 인증 필터.
    /// 외부 무단 접근을 차단하기 위해 모든 API 엔드포인트에 x-api-key 헤더를 필수화합니다.
    /// </summary>
    public class ApiKeyAuthAttribute : ActionFilterAttribute
    {
        private const string API_KEY_HEADER = "x-api-key";

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey))
            {
                context.Result = new UnauthorizedObjectResult(new { error = "API 키가 누락되었습니다. (헤더에 'x-api-key' 포함 필요)" });
                return;
            }

            var serverApiKey = AppConfigManager.Get("API_ACCESS_KEY", "");

            // 서버 측에 키 설정이 없는 경우, 보안상 접근 거부
            if (string.IsNullOrWhiteSpace(serverApiKey))
            {
                context.Result = new UnauthorizedObjectResult(new { error = "서버 측에 API Access Key가 설정되지 않았습니다. 관리자에게 문의하세요." });
                return;
            }

            // 키 불일치
            if (!serverApiKey.Equals(extractedApiKey.ToString()))
            {
                context.Result = new UnauthorizedObjectResult(new { error = "권한이 없습니다. 유효하지 않은 API 키입니다." });
                return;
            }

            await next();
        }
    }
}
