using System;
using System.Threading.Tasks;
using AutoInvest.Core;
using Microsoft.AspNetCore.Mvc;

using AutoInvest.Utils;

namespace AutoInvest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly SessionManager _sessionManager;

        public TestController(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        [HttpPost("buy")]
        public async Task<IActionResult> Buy(string ticker = "QQQM", int qty = 1)
        {
            try
            {
                var broker = _sessionManager.GetClient();
                if (!broker.IsLoggedIn)
                {
                    await broker.LoginAsync();
                }

                // 현재가 조회 후 시장가(또는 현재가)로 매수
                decimal price = await broker.GetCurrentPriceAsync(ticker);
                if (price <= 0) return BadRequest($"현재가를 조회할 수 없습니다: {ticker}");

                string orderNo = await broker.PlaceBuyOrderAsync(ticker, qty, price);
                return Ok(new { message = "매수 주문 성공", orderNo, ticker, qty, price });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("send-test-email")]
        public async Task<IActionResult> SendTestEmail()
        {
            string subject = "AutoInvesting 테스트 이메일";
            string body = "<p>이것은 <b>AutoInvesting 시스템</b>에서 보낸 <b>테스트 이메일</b>입니다.<br/>이 메일이 성공적으로 도착했다면 SMTP 설정이 올바르게 작동하는 것입니다.</p>";
            try
            {
                await NotificationService.SendEmailAsync(subject, body);
                return Ok("테스트 이메일 발송 시도 완료. Render.com 로그를 확인하거나 이메일 수신 여부를 확인하세요.");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[TestController] 테스트 이메일 발송 중 오류 발생: {ex.Message}");
                return StatusCode(500, $"테스트 이메일 발송 중 오류 발생: {ex.Message}");
            }
        }
    }
}
