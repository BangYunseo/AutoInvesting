using AutoInvest.Core;
using AutoInvest.Data;
using AutoInvest.Data.DAO;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AutoInvest.Controllers
{
    /// <summary>
    /// 수동 주문 트리거 API.
    /// 예약 시각 외에 즉시 적립식 매수 또는 수동 주문을 실행할 수 있습니다.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly SessionManager _session;
        private readonly IServiceScopeFactory _scopeFactory;

        public OrderController(SessionManager session, IServiceScopeFactory scopeFactory)
        {
            _session = session;
            _scopeFactory = scopeFactory;
        }

        /// <summary>
        /// 적립식(DCA) 자동 매수 사이클을 실행합니다 (판단 없는 단순 자동화).
        /// 외부 크론잡에서 매수 주기(예: 매월 첫 거래일)에 호출합니다.
        /// 백그라운드에서 실행하고 즉시 202를 반환합니다.
        /// </summary>
        [HttpPost("dca-run")]
        public IActionResult RunDcaCycle()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dailyService = scope.ServiceProvider.GetRequiredService<DailyExecutionService>();
                    await dailyService.RunDcaCycleAsync();
                }
                catch (Exception ex)
                {
                    Logger.Error($"[Order] 백그라운드 적립식 사이클 실행 실패: {ex.Message}");
                }
            });

            Logger.Info("[Order] 적립식 매수 사이클을 백그라운드로 시작했습니다 (즉시 202 반환).");
            return Accepted(new { message = "적립식 매수 사이클을 시작했습니다. 처리 결과는 서버 로그와 이메일로 확인하세요." });
        }

        /// <summary>
        /// 신호 판단을 거치지 않고 즉시 매수/매도 주문을 실행합니다 (KIS 모의계좌 연동 검증용).
        /// 퀀트/AI 합의와 무관하게 동작하므로 실거래 환경에서는 사용에 주의하세요.
        /// </summary>
        /// <param name="req">주문 요청 (종목, 수량, 매수/매도, 가격(생략 시 현재가))</param>
        [HttpPost("manual")]
        public async Task<IActionResult> PlaceManualOrder([FromBody] ManualOrderRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.Ticker))
                {
                    return BadRequest(new { error = "종목 코드(ticker)는 필수입니다." });
                }
                if (req.Qty <= 0)
                {
                    return BadRequest(new { error = "수량(qty)은 1 이상이어야 합니다." });
                }

                string orderType = (req.OrderType ?? "BUY").Trim().ToUpper();
                if (orderType != "BUY" && orderType != "SELL")
                {
                    return BadRequest(new { error = "orderType은 'BUY' 또는 'SELL'이어야 합니다." });
                }

                string ticker = req.Ticker.Trim().ToUpper();

                var client = _session.GetClient();
                if (!client.IsLoggedIn)
                {
                    var loginOk = await client.LoginAsync();
                    if (!loginOk)
                    {
                        return StatusCode(503, new { error = "브로커 로그인 실패" });
                    }
                }

                // ── 매도 안전가드: 실제 보유 종목·수량 범위 내에서만 허용 ──
                // (프론트 우회·실수와 무관하게 서버에서 오발주를 차단)
                decimal sellAvgPriceUsd = 0m; // 세금 가드에서 재사용할 취득 평균단가
                if (orderType == "SELL")
                {
                    var holdings = await client.GetHoldingsAsync();
                    var held = holdings.FirstOrDefault(h =>
                        string.Equals(h.Ticker, ticker, StringComparison.OrdinalIgnoreCase));
                    if (held == null || held.Qty <= 0)
                    {
                        return BadRequest(new { error = $"보유하지 않은 종목('{ticker}')은 매도할 수 없습니다." });
                    }
                    if (req.Qty > held.Qty)
                    {
                        return BadRequest(new { error = $"보유 수량({held.Qty}주)을 초과해 매도할 수 없습니다." });
                    }
                    sellAvgPriceUsd = held.AvgPrice;
                }

                // 가격 미지정 시 현재가 사용
                decimal price = req.Price ?? await client.GetCurrentPriceAsync(ticker);
                if (price <= 0)
                {
                    return BadRequest(new { error = $"'{ticker}'의 가격을 확인할 수 없습니다. price를 직접 지정해 주세요." });
                }

                // ── 절세 가드: 과세가 예상되는 매도인데 사용자가 세금을 확인(acknowledge)하지 않았으면 차단 ──
                // (판단/타이밍 아님 — 세금 산수 기반 정보 제공. 취득가 불명 시엔 계산 신뢰 불가라 가드를 건너뜀)
                if (orderType == "SELL" && sellAvgPriceUsd > 0m)
                {
                    decimal fx = await client.GetExchangeRateAsync();
                    var est = TaxEstimator.Estimate(
                        ticker, sellAvgPriceUsd, price, req.Qty, fx, req.YtdRealizedGainKrw, TaxSettings.Load());

                    if (est.IsTaxable && !req.AcknowledgeTax)
                    {
                        Logger.Info($"[Order] 과세 매도 사전 차단(미확인): {ticker} {req.Qty}주, " +
                            $"예상세금 {est.EstimatedTaxKrw:N0}원 (확인 시 acknowledgeTax=true로 재요청)");
                        return Conflict(new
                        {
                            error = "이 매도는 양도소득세가 예상됩니다. 예상 세금을 확인한 뒤 다시 시도하세요.",
                            taxEstimate = est
                        });
                    }
                }

                string orderNo = orderType == "BUY"
                    ? await client.PlaceBuyOrderAsync(ticker, req.Qty, price)
                    : await client.PlaceSellOrderAsync(ticker, req.Qty, price);

                if (string.IsNullOrEmpty(orderNo))
                {
                    Logger.Warn($"[Order] 수동 {orderType} 주문 미체결/실패: {ticker} {req.Qty}주");
                    return StatusCode(502, new { error = "주문이 거부되었거나 주문번호를 받지 못했습니다. 서버 로그를 확인하세요." });
                }

                // 접수 성공까지만 확인된 상태다(지정가 주문이므로 미체결로 끝날 수 있다).
                // 체결 확인 후 FILLED로 갱신하는 것은 별도 대사 경로가 담당한다.
                TradeHistoryDAO.Insert(new TradeHistoryDto
                {
                    TradeDate = DateTime.Now,
                    Ticker = ticker,
                    OrderType = orderType,
                    Qty = req.Qty,
                    Price = price,
                    Status = "PENDING",
                    OrderNo = orderNo
                });

                Logger.Info($"[Order] 수동 {orderType} 주문 접수: {ticker} {req.Qty}주 @ ${price} (주문번호: {orderNo})");
                return Ok(new
                {
                    message = $"수동 {orderType} 주문이 실행되었습니다.",
                    ticker,
                    orderType,
                    qty = req.Qty,
                    price,
                    orderNo
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Order] 수동 주문 실행 실패: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 매도 예정 정보로 예상 양도소득세·수수료를 미리 계산합니다 (주문 실행 없음, 정보 제공).
        /// 프론트가 매도 전에 호출해 "이 매도가 과세 구간인지 / 세금이 얼마인지"를 보여주는 용도입니다.
        /// </summary>
        /// <param name="ticker">종목 코드 (보유 종목이어야 함)</param>
        /// <param name="qty">매도 예정 수량(주)</param>
        /// <param name="price">매도 단가(USD). 생략 시 현재가 사용.</param>
        /// <param name="ytd">올해 이미 실현한 양도차익 합계(원). 남은 공제 계산용(수동 입력, 기본 0).</param>
        [HttpGet("sell-preview")]
        public async Task<IActionResult> PreviewSell(
            [FromQuery] string ticker,
            [FromQuery] int qty,
            [FromQuery] decimal? price,
            [FromQuery] decimal ytd = 0m)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ticker))
                {
                    return BadRequest(new { error = "종목 코드(ticker)는 필수입니다." });
                }
                if (qty <= 0)
                {
                    return BadRequest(new { error = "수량(qty)은 1 이상이어야 합니다." });
                }

                ticker = ticker.Trim().ToUpper();

                var client = _session.GetClient();
                if (!client.IsLoggedIn)
                {
                    var loginOk = await client.LoginAsync();
                    if (!loginOk)
                    {
                        return StatusCode(503, new { error = "브로커 로그인 실패" });
                    }
                }

                var holdings = await client.GetHoldingsAsync();
                var held = holdings.FirstOrDefault(h =>
                    string.Equals(h.Ticker, ticker, StringComparison.OrdinalIgnoreCase));
                if (held == null || held.Qty <= 0)
                {
                    return BadRequest(new { error = $"보유하지 않은 종목('{ticker}')은 매도 세금을 계산할 수 없습니다." });
                }

                decimal p = price ?? await client.GetCurrentPriceAsync(ticker);
                if (p <= 0)
                {
                    return BadRequest(new { error = $"'{ticker}'의 가격을 확인할 수 없습니다." });
                }

                decimal fx = await client.GetExchangeRateAsync();
                var est = TaxEstimator.Estimate(ticker, held.AvgPrice, p, qty, fx, ytd, TaxSettings.Load());
                return Ok(est);
            }
            catch (Exception ex)
            {
                Logger.Error($"[Order] 매도 세금 프리뷰 실패: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    /// <summary>
    /// 수동 주문 요청 본문.
    /// </summary>
    public class ManualOrderRequest
    {
        /// <summary>종목 코드 (예: QQQM)</summary>
        public string Ticker { get; set; } = string.Empty;

        /// <summary>주문 수량 (1 이상)</summary>
        public int Qty { get; set; } = 1;

        /// <summary>주문 유형: "BUY" 또는 "SELL"</summary>
        public string OrderType { get; set; } = "BUY";

        /// <summary>주문 가격 (USD). 생략 시 현재가로 주문.</summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// (매도 전용) 과세가 예상되는 매도임을 사용자가 확인했는지 여부.
        /// 과세 매도인데 이 값이 false면 서버가 409로 차단합니다.
        /// </summary>
        public bool AcknowledgeTax { get; set; } = false;

        /// <summary>
        /// (매도 전용) 올해 이미 실현한 양도차익 합계(원). 남은 공제 계산용(수동 입력, 기본 0).
        /// </summary>
        public decimal YtdRealizedGainKrw { get; set; } = 0m;
    }
}
