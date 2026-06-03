using AutoInvest.Core;
using AutoInvest.Core.Quant;
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
    /// 백테스트 실행 API
    /// 과거 데이터 기반 전략 수익성 검증
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class BacktestController : ControllerBase
    {
        private readonly SessionManager _session;

        public BacktestController(SessionManager session)
        {
            _session = session;
        }

        /// <summary>
        /// 특정 종목에 대해 백테스트를 실행합니다.
        /// </summary>
        /// <param name="request">백테스트 요청 파라미터</param>
        [HttpPost("run")]
        public async Task<IActionResult> RunBacktest([FromBody] BacktestRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Ticker))
                {
                    return BadRequest(new { error = "종목 코드(Ticker)는 필수입니다." });
                }

                var client = _session.GetClient();
                if (!client.IsLoggedIn)
                {
                    await client.LoginAsync();
                }

                int days = request.Days > 0 ? request.Days : 120;
                string strategyType = request.StrategyType ?? "MEAN_REVERSION";
                decimal initialCapital = request.InitialCapital > 0 ? request.InitialCapital : 10000m;

                // 백테스트 실행
                var engine = new BacktestEngine(
                    client,
                    initialAmount: initialCapital,
                    buyThreshold: request.BuyThreshold > 0 ? request.BuyThreshold : 0.10m,
                    sellThreshold: request.SellThreshold > 0 ? request.SellThreshold : 0.90m);

                var result = await engine.RunAsync(request.Ticker, "API테스트", strategyType, days);

                Logger.Info($"[Backtest] {request.Ticker} 백테스트 완료: " +
                    $"수익률={result.ReturnRate:F2}%, MDD={result.MaxDrawdown:F2}%, 승률={result.WinRate:F1}%");

                return Ok(new
                {
                    ticker = request.Ticker,
                    strategy = strategyType,
                    days = days,
                    initialCapital,
                    finalCapital = result.FinalAmount,
                    totalReturnPct = result.ReturnRate,
                    maxDrawdownPct = result.MaxDrawdown,
                    winRatePct = result.WinRate,
                    totalTrades = result.TotalTrades,
                    trades = result.Trades.Select(t => new
                    {
                        t.Date,
                        Type = t.Action,
                        t.Price,
                        t.Qty,
                        ProfitLoss = t.ProfitLoss
                    })
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Backtest] 실행 실패: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    /// <summary>
    /// 백테스트 요청 DTO.
    /// </summary>
    public class BacktestRequest
    {
        /// <summary>종목 코드</summary>
        public string Ticker { get; set; } = string.Empty;

        /// <summary>전략 유형 (MEAN_REVERSION / MOMENTUM / MIXED)</summary>
        public string? StrategyType { get; set; }

        /// <summary>백테스트 기간 (일, 기본 120)</summary>
        public int Days { get; set; } = 120;

        /// <summary>초기 투자금 (USD, 기본 10000)</summary>
        public decimal InitialCapital { get; set; } = 10000m;

        /// <summary>매수 임계값 (기본 0.10)</summary>
        public decimal BuyThreshold { get; set; } = 0.10m;

        /// <summary>매도 임계값 (기본 0.90)</summary>
        public decimal SellThreshold { get; set; } = 0.90m;
    }
}
