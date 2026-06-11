using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoInvest.Core;
using AutoInvest.Core.Quant;
using AutoInvest.Data;
using AutoInvest.Data.DAO;
using AutoInvest.Data.DTO;
using Microsoft.AspNetCore.Mvc;

using AutoInvest.Utils; 

namespace AutoInvest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly SmartOrderEngine _engine;
        private readonly SessionManager _sessionManager;

        public TestController(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
            var broker = sessionManager.GetClient();
            var analyzer = sessionManager.GetAnalyzer();
            _engine = new SmartOrderEngine(broker, analyzer);
        }

        [HttpPost("inject-mock")]
        public IActionResult InjectMockData()
        {
            using (var conn = DBManager.Instance.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM TB_MARKET_SNAPSHOT WHERE TICKER = 'QQQ'";
                cmd.ExecuteNonQuery();
            }

            for (int i = 1; i <= 30; i++)
            {
                decimal prob = 0.50m + (i * 0.01m); // 0.51 to 0.80
                var dto = new MarketSnapshotDto
                {
                    SnapDate = DateTime.Now.AddDays(-i),
                    Ticker = "QQQ",
                    Price = 200m,
                    Position20d = 0.1m,
                    Rsi14 = 30m,
                    MacdValue = 0m,
                    MacdSignal = 0m,
                    BbUpper = 210m,
                    BbLower = 190m,
                    Signal = "BUY",
                    BuyProbability = prob,
                    SellProbability = 0m,
                    ChartAiScore = 0.6m,
                    FundAiScore = 0.6m
                };
                MarketSnapshotDAO.Insert(dto);
            }
            return Ok("Mock data injected. Range: 0.51 ~ 0.80. Expected 70th Percentile ~ 0.71");
        }

        [HttpGet("test-adaptive")]
        public async Task<IActionResult> TestAdaptive(string ticker = "QQQ")
        {
            var (threshold, reason) = AdaptiveThresholdEngine.GetBuyThreshold(ticker);
            var result = await _engine.AnalyzeAsync(ticker, "MEAN_REVERSION");

            return Ok(new
            {
                AdaptiveThreshold = threshold,
                ThresholdReason = reason,
                AnalysisResult = result
            });
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

        [HttpPost("send-report")]
        public async Task<IActionResult> SendDailyReport()
        {
            try
            {
                // 1. 토큰 사용량
                int totalTokens = TokenUsageDAO.GetTodayTotalTokens();
                
                // 2. AI 성과
                var (perfCount, avgWinRate) = AiPerformanceDAO.GetOverallPerformance();

                string htmlBody = $@"
                    <h2>AutoInvesting 일일 운용 보고서 (테스트 발송)</h2>
                    <hr/>
                    <h3>1. 금일 매매 내역</h3>
                    <p>테스트 발송이므로 매매 내역은 생략됩니다.</p>
                    <br/>
                    <h3>2. AI 성과 요약</h3>
                    <ul>
                        <li>현재까지 평가 완료된 신호 건수: {perfCount}건</li>
                        <li><strong>AI 누적 적중률(Win Rate): {avgWinRate:P1}</strong></li>
                    </ul>
                    <br/>
                    <h3>3. AI API 토큰 소모량</h3>
                    <ul>
                        <li>금일 사용 토큰 합계: <strong>{totalTokens:N0} tokens</strong></li>
                    </ul>
                    <hr/>
                    <p style='color: gray; font-size: 12px;'>본 메일은 TestController에 의해 발송되었습니다.</p>";

                await AutoInvest.Utils.NotificationService.SendEmailAsync("일일 운용 보고서 (테스트)", htmlBody);
                
                return Ok(new { message = "테스트 일일 보고서 메일 발송 성공" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
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
