using AutoInvest.Core;
using AutoInvest.Data;
using AutoInvest.Data.DAO;
using AutoInvest.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AutoInvest.Controllers
{
    /// <summary>
    /// 수동 주문 트리거 API.
    /// 예약 시각 외에 즉시 스마트 주문을 실행할 수 있습니다.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly SessionManager _session;

        public OrderController(SessionManager session)
        {
            _session = session;
        }

        /// <summary>
        /// 현재 활성 전략 기반으로 스마트 주문을 즉시 실행합니다.
        /// </summary>
        [HttpPost("execute")]
        public async Task<IActionResult> ExecuteSmartOrders()
        {
            try
            {
                var client = _session.GetClient();
                if (!client.IsLoggedIn)
                {
                    var loginOk = await client.LoginAsync();
                    if (!loginOk)
                    {
                        return StatusCode(503, new { error = "브로커 로그인 실패" });
                    }
                }

                var strategyName = AppConfigManager.Get("ACTIVE_STRATEGY", "사용자정의");
                var strategies = StrategyDAO.GetStrategy(strategyName);
                if (strategies.Count == 0)
                {
                    return BadRequest(new { error = $"전략 '{strategyName}'에 종목이 없습니다." });
                }

                var amountStr = AppConfigManager.Get("INVEST_AMOUNT_KRW", "1000000");
                decimal investAmount = decimal.Parse(amountStr);

                var engine = new SmartOrderEngine(client, _session.GetAnalyzer());
                var results = await engine.ExecuteSmartOrdersAsync(strategies, investAmount);

                Logger.Info($"[Order] 수동 스마트 주문 실행 완료: {results.Count}건");

                var summary = results.Select(r => new
                {
                    r.Ticker,
                    Signal = r.Signal.ToString(),
                    r.Reason,
                    Price = r.PriceRange?.Current ?? 0m
                });

                return Ok(new
                {
                    message = $"스마트 주문 {results.Count}건 실행 완료",
                    results = summary
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Order] 수동 주문 실행 실패: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 단일 종목 분석 결과만 조회합니다 (주문 실행 없이).
        /// </summary>
        /// <param name="ticker">종목 코드</param>
        /// <param name="strategy">전략 유형 (기본: MEAN_REVERSION)</param>
        [HttpGet("analyze/{ticker}")]
        public async Task<IActionResult> AnalyzeTicker(string ticker, [FromQuery] string strategy = "MEAN_REVERSION")
        {
            try
            {
                var client = _session.GetClient();
                if (!client.IsLoggedIn)
                {
                    await client.LoginAsync();
                }

                var engine = new SmartOrderEngine(client, _session.GetAnalyzer());
                var result = await engine.AnalyzeAsync(ticker, strategy);

                return Ok(new
                {
                    result.Ticker,
                    Signal = result.Signal.ToString(),
                    result.Reason,
                    result.DecisionReason,
                    Price = result.PriceRange?.Current ?? 0m,
                    Indicators = result.Indicators != null ? new
                    {
                        result.Indicators.Position,
                        result.Indicators.Rsi14,
                        result.Indicators.MacdLine,
                        result.Indicators.MacdSignal,
                        result.Indicators.MacdHistogram,
                        result.Indicators.BbUpper,
                        result.Indicators.BbMiddle,
                        result.Indicators.BbLower
                    } : null,
                    Conditions = result.QuantConditions
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Order] 종목 분석 실패 ({ticker}): {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
