using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoInvest.Core;
using AutoInvest.Core.Quant;
using AutoInvest.Data;
using AutoInvest.Data.DAO;
using AutoInvest.Data.DTO;
using Microsoft.AspNetCore.Mvc;

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
    }
}
