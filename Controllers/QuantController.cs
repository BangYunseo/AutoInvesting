using AutoInvest.Core;
using AutoInvest.Core.Quant;
using AutoInvest.Data;
using AutoInvest.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AutoInvest.Controllers
{
    /// <summary>
    /// 실시간 종목 퀀트 분석 API.
    /// 현재가와 120일 OHLCV를 기반으로 실시간 전문가 분석 의견을 제공합니다.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class QuantController : ControllerBase
    {
        private readonly SessionManager _session;

        public QuantController(SessionManager session)
        {
            _session = session;
        }

        /// <summary>
        /// 특정 종목의 현재 상태를 실시간 분석합니다.
        /// </summary>
        [HttpGet("analyze/{ticker}")]
        public async Task<IActionResult> AnalyzeTicker(string ticker, [FromQuery] string strategyType = "MEAN_REVERSION")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ticker))
                    return BadRequest(new { error = "종목 코드(Ticker)는 필수입니다." });

                var client = _session.GetClient();
                if (!client.IsLoggedIn)
                {
                    await client.LoginAsync();
                }

                // 1. 현재가 조회 (옵션 - 필요시 사용)
                var currentPrice = await client.GetCurrentPriceAsync(ticker);

                // 2. OHLCV 조회 (지표 계산용, 기본 120일)
                var ohlcv = await client.GetOhlcvAsync(ticker, 120);
                if (ohlcv == null || ohlcv.Count == 0)
                {
                    return NotFound(new { error = $"종목 {ticker}의 데이터를 불러오지 못했습니다." });
                }

                // 3. 지표 계산
                var closes = ohlcv.Select(x => x.Close).ToList();
                var recent20 = closes.Skip(Math.Max(0, closes.Count - 20)).ToList();
                decimal high20d = recent20.Any() ? recent20.Max() : currentPrice;
                decimal low20d = recent20.Any() ? recent20.Min() : currentPrice;
                
                var indicators = QuantIndicator.CalculateAll(ticker, ohlcv, currentPrice, high20d, low20d);

                // 4. 전문가 어투 판단 로직 수행
                var buyResult = QuantFilter.CheckBuyCondition(indicators, strategyType);
                var sellResult = QuantFilter.CheckSellCondition(indicators, strategyType);

                return Ok(new
                {
                    ticker = ticker.ToUpper(),
                    currentPrice = currentPrice,
                    strategyType = strategyType,
                    indicators = new
                    {
                        rsi14 = indicators.Rsi14,
                        position = indicators.Position,
                        macdLine = indicators.MacdLine,
                        macdHistogram = indicators.MacdHistogram
                    },
                    analysis = new
                    {
                        buyPassed = buyResult.Passed,
                        buySummary = buyResult.Summary,
                        sellPassed = sellResult.Passed,
                        sellSummary = sellResult.Summary
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"[QuantAnalysis] {ticker} 분석 실패: {ex.Message}");
                return StatusCode(500, new { error = "분석 중 서버 오류가 발생했습니다. 잠시 후 다시 시도해주세요." });
            }
        }
    }
}
