using AutoInvest.Data.DAO;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoInvest.Core
{
    /// <summary>
    /// 적립식(DCA) 자동 매수 엔진.
    ///
    /// 퀀트/AI 판단을 일절 하지 않습니다. 백테스트 결과 "타이밍 판단은 잘해야 본전,
    /// 실제로는 손해"로 검증되었기에, 이 엔진은 오직 다음만 수행합니다:
    ///   사람이 종목별로 지정한 "고정 매수 주수"를 매 사이클 그대로 매수 + 거래 기록.
    ///
    /// 비중(%)·매수금액은 사람이 정하지 않습니다 — 수량×현재가로 환산해 화면에서 보여주는
    /// 표시용 값일 뿐입니다. 예산은 초과 여부를 경고하는 상한일 뿐 수량을 줄이지 않습니다.
    ///
    /// 매수 계획(PlanPurchases)은 순수 함수(외부 I/O 없음)로 분리되어 단위 검증이 가능합니다.
    /// </summary>
    public class DcaAccumulationEngine
    {
        private readonly IBrokerClient _broker;

        /// <param name="broker">증권사 클라이언트 (Sim 또는 KIS)</param>
        public DcaAccumulationEngine(IBrokerClient broker)
        {
            _broker = broker;
        }

        /// <summary>
        /// 고정 수량 매수 계획을 산출합니다 (순수 함수 — 외부 I/O 없음, 검증 대상).
        /// 현재가가 있는 종목만 설정 수량 그대로 계획에 포함하고, 총 매수금액(원)을 함께 반환합니다.
        /// 예산은 여기서 고려하지 않습니다 — 초과 경고는 호출부(AccumulateAsync)에서 처리합니다.
        /// </summary>
        /// <param name="quantities">종목별 매수 수량 (예: QQQM=2, SPLG=3)</param>
        /// <param name="exchangeRate">USD→KRW 환율</param>
        /// <param name="priceUsd">종목별 현재가 (USD)</param>
        /// <param name="totalCostKrw">계획 전체의 매수금액 합계 (원)</param>
        /// <returns>종목별 매수 수량 (현재가가 있고 수량이 1주 이상인 종목만)</returns>
        public static Dictionary<string, int> PlanPurchases(
            IReadOnlyDictionary<string, int> quantities,
            decimal exchangeRate,
            IReadOnlyDictionary<string, decimal> priceUsd,
            out decimal totalCostKrw)
        {
            var plan = new Dictionary<string, int>();
            totalCostKrw = 0m;

            foreach (var kv in quantities)
            {
                int qty = kv.Value;
                if (qty <= 0) continue;
                if (!priceUsd.TryGetValue(kv.Key, out decimal px) || px <= 0) continue;

                plan[kv.Key] = qty;
                totalCostKrw += qty * px * exchangeRate;
            }

            return plan;
        }

        /// <summary>
        /// 설정한 종목별 고정 수량을 매수합니다.
        /// 주문 체결분은 TB_TRADE_HISTORY에 기록되며, 체결 내역 목록을 반환합니다.
        /// </summary>
        /// <param name="quantities">종목별 매수 수량 맵 (예: QQQM=2, SPLG=3)</param>
        /// <param name="budgetKrw">이번 사이클 예산 (원, 초과 경고용 상한)</param>
        /// <returns>체결된 매수 내역 목록 (없으면 빈 목록)</returns>
        public async Task<List<TradeHistoryDto>> AccumulateAsync(
            Dictionary<string, int> quantities,
            decimal budgetKrw)
        {
            var filled = new List<TradeHistoryDto>();

            if (quantities == null || quantities.Count == 0)
            {
                Logger.Warn("[DCA] 매수 수량(quantities)이 비어 있어 매수를 건너뜁니다.");
                return filled;
            }

            decimal exchangeRate = await _broker.GetExchangeRateAsync();
            if (exchangeRate <= 0)
            {
                Logger.Error("[DCA] 환율 조회 실패(0 이하) — 매수 중단");
                return filled;
            }

            // ── 현재가 수집 ──
            var priceUsd = new Dictionary<string, decimal>();
            foreach (var ticker in quantities.Keys)
            {
                decimal px = await _broker.GetCurrentPriceAsync(ticker);
                if (px <= 0)
                {
                    Logger.Warn($"[DCA] {ticker} 현재가 조회 실패(0 이하) — 이 종목은 제외");
                    continue;
                }
                priceUsd[ticker] = px;
            }

            if (priceUsd.Count == 0)
            {
                Logger.Error("[DCA] 유효한 현재가가 있는 종목이 없어 매수 중단");
                return filled;
            }

            // ── 순수 매수 계획 산출 (고정 수량) ──
            var plan = PlanPurchases(quantities, exchangeRate, priceUsd, out decimal totalCostKrw);

            Logger.Info($"[DCA] === 적립식(고정수량) 매수 시작 (예산 {budgetKrw:N0}원, 환율 {exchangeRate:N0}, 종목 {plan.Count}개) ===");

            // 예산 초과 시 경고만 (수량은 그대로 진행)
            if (budgetKrw > 0 && totalCostKrw > budgetKrw)
            {
                string msg = $"총 매수금액 {totalCostKrw:N0}원이 예산 {budgetKrw:N0}원을 초과합니다 " +
                    $"(초과 {totalCostKrw - budgetKrw:N0}원). 설정 수량 그대로 진행합니다.";
                Logger.Warn($"[DCA] ⚠ {msg}");
                _ = NotificationService.SendEmailAsync("DCA 예산 초과 경고", msg);
            }

            // ── 계획대로 주문 실행 + 기록 ──
            foreach (var (ticker, qty) in plan)
            {
                decimal price = priceUsd[ticker];
                try
                {
                    var orderNo = await _broker.PlaceBuyOrderAsync(ticker, qty, price);

                    var trade = new TradeHistoryDto
                    {
                        TradeDate = DateTime.Now,
                        Ticker = ticker,
                        OrderType = "BUY",
                        Qty = qty,
                        Price = price,
                        Status = "FILLED",
                        OrderNo = orderNo
                    };
                    TradeHistoryDAO.Insert(trade);
                    filled.Add(trade);

                    Logger.Info($"[DCA] 매수 완료: {ticker} {qty}주 @ ${price} (주문번호: {orderNo})");
                }
                catch (Exception ex)
                {
                    Logger.Error($"[DCA] {ticker} 매수 실패: {ex.Message}");
                    _ = NotificationService.SendEmailAsync($"DCA 매수 실패: {ticker}", $"수량: {qty}주\n오류: {ex.Message}");
                }
            }

            var perTicker = plan.Select(kv => $"{kv.Key} {kv.Value}주");
            Logger.Info($"[DCA] === 매수 완료: 총 {plan.Values.Sum()}주 ({string.Join(", ", perTicker)}), " +
                $"총 매수금액 {totalCostKrw:N0}원 ===");

            return filled;
        }
    }
}
