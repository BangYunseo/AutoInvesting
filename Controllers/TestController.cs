using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using AutoInvest.Utils;

namespace AutoInvest.Controllers
{
    /// <summary>
    /// 운영 점검용 API.
    ///
    /// 실주문 경로는 두지 않습니다 — 매수/매도는 보유 검증·절세 가드·거래이력 기록이 있는
    /// <c>/api/order/manual</c>만 사용합니다. (과거 <c>POST /api/test/buy</c>는 manual에서
    /// 가드만 뺀 중복 경로였고 실전 모드에서는 스스로를 403으로 차단하고 있어 제거했습니다.)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        /// <summary>
        /// [점검용] 이메일 발송 설정(Resend)이 실제로 동작하는지 확인 메일을 1통 보냅니다.
        /// 실패 원인을 응답으로 확인해야 하므로, 예외를 삼키지 않는
        /// <see cref="NotificationService.SendEmailOrThrowAsync"/>를 사용합니다.
        /// </summary>
        [HttpGet("send-test-email")]
        public async Task<IActionResult> SendTestEmail()
        {
            string subject = "AutoInvesting 테스트 이메일";
            string body = "<p>이것은 <b>AutoInvesting 시스템</b>에서 보낸 <b>테스트 이메일</b>입니다.<br/>이 메일이 성공적으로 도착했다면 발송 설정이 올바르게 작동하는 것입니다.</p>";
            try
            {
                await NotificationService.SendEmailOrThrowAsync(subject, body);
                return Ok("테스트 이메일 발송 완료. 수신 여부를 확인하세요.");
            }
            catch (Exception ex)
            {
                Logger.Error($"[Test] 테스트 이메일 발송 중 오류 발생: {ex.Message}");
                return StatusCode(500, $"테스트 이메일 발송 중 오류 발생: {ex.Message}");
            }
        }
    }
}
