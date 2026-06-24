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
    /// 예약 시각 외에 즉시 스마트 주문을 실행할 수 있습니다.
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
        /// 외부 스케줄러(운영 환경에서는 GitHub Actions 워크플로우)에서 매일 한 번 호출하여 전체 일일 사이클을 실행합니다.
        /// (매매, 리밸런싱, AI 평가, 메일 리포트 발송 포함)
        ///
        /// 사이클은 KIS 로그인 + 종목별 Gemini 호출 + SMTP까지 수십 초 이상 걸릴 수 있어
        /// 크론의 타임아웃(예: 30초)을 넘기기 쉽습니다. 따라서 사이클을 백그라운드에서 실행하고
        /// 호출자에게는 즉시 202를 반환하여 타임아웃/응답 과대(output too large)를 방지합니다.
        /// </summary>
        [HttpPost("daily-run")]
        public IActionResult RunDailyCycle()
        {
            // Scoped 서비스(DailyExecutionService)를 요청 수명과 분리해 사용하기 위해
            // 백그라운드 작업 내부에서 별도 DI 스코프를 생성한다.
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dailyService = scope.ServiceProvider.GetRequiredService<DailyExecutionService>();
                    await dailyService.RunDailyCycleAsync();
                }
                catch (Exception ex)
                {
                    Logger.Error($"[Order] 백그라운드 일일 사이클 실행 실패: {ex.Message}");
                }
            });

            Logger.Info("[Order] 일일 사이클을 백그라운드로 시작했습니다 (즉시 202 반환).");
            return Accepted(new { message = "일일 사이클을 시작했습니다. 처리 결과는 서버 로그와 이메일로 확인하세요." });
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

                // 가격 미지정 시 현재가 사용
                decimal price = req.Price ?? await client.GetCurrentPriceAsync(ticker);
                if (price <= 0)
                {
                    return BadRequest(new { error = $"'{ticker}'의 가격을 확인할 수 없습니다. price를 직접 지정해 주세요." });
                }

                string orderNo = orderType == "BUY"
                    ? await client.PlaceBuyOrderAsync(ticker, req.Qty, price)
                    : await client.PlaceSellOrderAsync(ticker, req.Qty, price);

                if (string.IsNullOrEmpty(orderNo))
                {
                    Logger.Warn($"[Order] 수동 {orderType} 주문 미체결/실패: {ticker} {req.Qty}주");
                    return StatusCode(502, new { error = "주문이 거부되었거나 주문번호를 받지 못했습니다. 서버 로그를 확인하세요." });
                }

                TradeHistoryDAO.Insert(new TradeHistoryDto
                {
                    TradeDate = DateTime.Now,
                    Ticker = ticker,
                    OrderType = orderType,
                    Qty = req.Qty,
                    Price = price,
                    Status = "FILLED",
                    OrderNo = orderNo
                });

                Logger.Info($"[Order] 수동 {orderType} 주문 실행: {ticker} {req.Qty}주 @ ${price} (주문번호: {orderNo})");
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
                    Conditions = result.QuantConditions,
                    AdvisoryNotes = result.AdvisoryNotes.Select(n => new
                    {
                        n.Source,
                        Severity = n.Severity.ToString(),
                        n.Title,
                        n.Message,
                        n.SuggestedAlternatives
                    })
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Order] 종목 분석 실패 ({ticker}): {ex.Message}");
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
    }
}
