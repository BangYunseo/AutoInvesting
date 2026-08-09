using AutoInvest.Core;
using AutoInvest.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AutoInvest.Controllers
{
    /// <summary>
    /// 종목 현재가 조회 API.
    /// 적립 설정 화면에서 티커 실재 여부 검증 + 실시간 가격 표시에 사용합니다.
    /// 현재가가 0 이하이면 존재하지 않는 티커(또는 조회 실패)로 간주해 404를 반환합니다.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PriceController : ControllerBase
    {
        private readonly SessionManager _session;

        public PriceController(SessionManager session)
        {
            _session = session;
        }

        /// <summary>
        /// 지정한 티커의 현재가(USD)와 환율 환산 원화가를 반환합니다.
        /// </summary>
        /// <param name="ticker">종목 코드 (예: QQQ)</param>
        [HttpGet("{ticker}")]
        public async Task<IActionResult> GetPrice(string ticker)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ticker))
                {
                    return BadRequest(new { error = "티커는 필수입니다." });
                }

                ticker = ticker.Trim().ToUpper();

                var client = _session.GetClient();
                if (!client.IsLoggedIn)
                {
                    await client.LoginAsync();
                }

                decimal priceUsd = await client.GetCurrentPriceAsync(ticker);
                if (priceUsd <= 0)
                {
                    return NotFound(new
                    {
                        error = $"'{ticker}' 현재가를 확인할 수 없습니다. 존재하지 않는 티커이거나 조회에 실패했습니다.",
                        ticker
                    });
                }

                decimal exchangeRate = await client.GetExchangeRateAsync();

                return Ok(new
                {
                    ticker,
                    priceUsd,
                    exchangeRate,
                    priceKrw = priceUsd * exchangeRate
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Price] {ticker} 현재가 조회 실패: {ex.Message}");
                return StatusCode(500, new { error = "현재가 조회 중 오류가 발생했습니다." });
            }
        }
    }
}
