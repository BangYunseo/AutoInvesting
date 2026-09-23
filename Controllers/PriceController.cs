using AutoInvest.Core;
using AutoInvest.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace AutoInvest.Controllers
{
    /// <summary>
    /// 종목 현재가 조회 API.
    /// 적립 설정 화면에서 티커 실재 여부 검증 + 실시간 가격 표시에 사용합니다.
    /// 현재가가 0 이하이면 존재하지 않는 티커(또는 조회 실패)로 간주해 404를 반환합니다.
    ///
    /// ⚠️ 여기서 돌려주는 가격은 <b>표시·검증 전용</b>이며 짧게 캐시됩니다(<see cref="CacheSeconds"/>초).
    /// 실제 주문가는 이 값을 쓰지 않습니다 — 적립 사이클은 <c>DcaAccumulationEngine</c>이,
    /// 수동 주문은 <c>OrderController</c>가 주문 시점에 각각 직접 조회합니다. 화면이 보낸 가격이
    /// 주문에 쓰이는 경우는 사람이 지정가를 손으로 입력했을 때뿐입니다.
    /// 캐시를 <c>KisBrokerClient.GetCurrentPriceAsync</c> 안에 넣으면 그 경계가 무너져
    /// 낡은 가격으로 실주문이 나가므로, 캐시는 반드시 이 컨트롤러에만 둡니다.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PriceController : ControllerBase
    {
        /// <summary>현재가 캐시 유지 시간(초). 표시용이므로 짧게 두고, 만료되면 다시 조회한다.</summary>
        private const int CacheSeconds = 60;

        private readonly SessionManager _session;
        private readonly IMemoryCache _cache;

        public PriceController(SessionManager session, IMemoryCache cache)
        {
            _session = session;
            _cache = cache;
        }

        /// <summary>
        /// 지정한 티커의 현재가(USD)와 환율 환산 원화가를 반환합니다.
        ///
        /// 현재가 조회는 거래소(EXCD)를 차례로 찔러보는 구조라 종목당 최소 400ms가 들고,
        /// 적립설정 화면은 진입할 때마다 저장된 종목 전부를 다시 검증합니다. 같은 종목을 짧은 간격으로
        /// 다시 묻는 경우가 대부분이라 <see cref="CacheSeconds"/>초간 캐시합니다.
        /// 조회에 <b>성공한 값만</b> 캐시합니다 — 실패(0)를 캐시하면 Rate limit 같은 일시적 실패가
        /// 그 시간 동안 "존재하지 않는 티커"로 굳어 사람에게 거짓을 말하게 됩니다.
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

                string cacheKey = $"price:{ticker}";
                if (!_cache.TryGetValue(cacheKey, out decimal priceUsd))
                {
                    priceUsd = await client.GetCurrentPriceAsync(ticker);

                    if (priceUsd > 0)
                    {
                        _cache.Set(cacheKey, priceUsd, TimeSpan.FromSeconds(CacheSeconds));
                    }
                }

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
