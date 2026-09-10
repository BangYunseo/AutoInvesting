using AutoInvest.Data.DAO;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;

namespace AutoInvest.Core
{
    /// <summary>
    /// 적립식(DCA) 자동 매수 엔진
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
        /// 고정 수량 매수 계획 산출
        /// </summary>
        /// <param name="quantities">종목별 매수 수량</param>
        /// <param name="exchangeRate">환율</param>
        /// <param name="priceUsd">종목별 현재가</param>
        /// <param name="totalCostKrw">계획 전체의 매수금액 합계</param>
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
        /// 고정 수량 매수 주문 후 객체들을 종합 보고서로 발송
        /// </summary>
        /// <param name="quantities">종목별 매수 수량 맵</param>
        /// <param name="budgetKrw">이번 사이클 예산</param>
        /// <returns>체결·실패·예산경고를 담은 사이클 결과</returns>
        public async Task<DcaCycleResult> AccumulateAsync(
            Dictionary<string, int> quantities,
            decimal budgetKrw)
        {
            var result = new DcaCycleResult();

            if (quantities == null || quantities.Count == 0)
            {
                Logger.Warn("[DCA] 매수 수량이 없어 매수를 건너뜁니다.");
                return result;
            }

            decimal exchangeRate = await _broker.GetExchangeRateAsync();
            if (exchangeRate <= 0)
            {
                Logger.Error("[DCA] 환율 조회에 실패해 매수를 건너뜁니다.");
                return result;
            }

            // 현재가
            var priceUsd = new Dictionary<string, decimal>();
            foreach (var ticker in quantities.Keys)
            {
                decimal px = await _broker.GetCurrentPriceAsync(ticker);
                if (px <= 0)
                {
                    Logger.Warn($"[DCA] {ticker} 조회 실패 : {ticker}는 매수를 건너뜁니다.");
                    continue;
                }
                priceUsd[ticker] = px;
            }

            if (priceUsd.Count == 0)
            {
                Logger.Error("[DCA] 유효한 현재가가 있는 종목이 없어 매수를 건너뜁니다.");
                return result;
            }

            // 순수 매수 계획 산출 (고정 수량)
            // ponytail: totalCostKrw는 조회 시점 추정치
            // 지정가라 미체결이면 0원 / 환율은 주문 시각 vs 환전 시점이 다름(실제 비용 차이 발생) / 수수료·제세금 제외
            // 실제 비용 필요 시 체결 대사에서 체결가를 받은 뒤 TB_TRADE_HISTORY 데이터 저장
            var plan = PlanPurchases(quantities, exchangeRate, priceUsd, out decimal totalCostKrw);

            // 보고서 표시용으로만 결과에 담는다(주문 결정에는 관여하지 않음).
            result.TotalCostKrw = totalCostKrw;
            result.ExchangeRate = exchangeRate;

            Logger.Info($"[DCA] ===== 매수 진행 (예산 {budgetKrw:N0}원, 환율 {exchangeRate:N0}, 종목 {plan.Count}개) =====");

            // 예산 초과 시 경고
            if (budgetKrw > 0 && totalCostKrw > budgetKrw)
            {
                string msg = $"총 매수금액 {totalCostKrw:N0}원이 예산 {budgetKrw:N0}원을 초과합니다.\n" +
                    $"(초과 {totalCostKrw - budgetKrw:N0}원)\n";
                Logger.Warn($"[DCA] ⚠ {msg}");
                result.BudgetWarning = msg;
            }

            // 주문 실행 및 기록
            foreach (var (ticker, qty) in plan)
            {
                decimal price = priceUsd[ticker];
                try
                {
                    var orderNo = await _broker.PlaceBuyOrderAsync(ticker, qty, price);

                    // 접수 성공까지만 확인(지정가 주문은 미체결로 끝날 수 있어 PENDING 기록 / 체결 후 FILLED 갱신)
                    var trade = new TradeHistoryDto
                    {
                        TradeDate = DateTime.Now,
                        Ticker = ticker,
                        OrderType = "BUY",
                        Qty = qty,
                        Price = price,
                        Status = "PENDING",
                        OrderNo = orderNo
                    };
                    TradeHistoryDAO.Insert(trade);
                    result.Accepted.Add(trade);

                    if (string.IsNullOrEmpty(orderNo))
                        Logger.Warn($"[DCA] 주문 접수: {ticker} {qty}주 @ ${price} — 주문번호 미수신, 체결 대사 불가");
                    else
                        Logger.Info($"[DCA] 주문 접수: {ticker} {qty}주 @ ${price} (주문번호: {orderNo})");
                }
                catch (Exception ex)
                {
                    Logger.Error($"[DCA] {ticker} 매수 실패: {ex.Message}");
                    // 결과 적재
                    result.Failures.Add(new DcaBuyFailure {
                        Ticker = ticker,
                        Qty = qty,
                        Error = ex.Message
                    });
                }
            }

            var perTicker = plan.Select(kv => $"{kv.Key} {kv.Value}주");
            Logger.Info($"[DCA] === 매수 완료: 총 {plan.Values.Sum()}주 ({string.Join(", ", perTicker)}), " +
                $"총 매수금액 {totalCostKrw:N0}원 ===");

            return result;
        }
    }
}
