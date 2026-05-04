using AutoInvest.Data;
using AutoInvest.Data.DAO;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoInvest.Core.Quant
{
    /// <summary>
    /// 리밸런싱 주문 결과
    /// </summary>
    public class RebalanceOrder
    {
        public string Ticker { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // BUY / SELL
        public int Qty { get; set; }
        public decimal CurrentWeight { get; set; }
        public decimal TargetWeight { get; set; }
        public decimal Deviation { get; set; }
    }

    /// <summary>
    /// 리밸런싱 엔진.
    /// 현재 보유 비중과 목표 비중의 편차를 계산하고,
    /// 임계값을 초과하는 종목에 대해 조정 주문을 생성합니다.
    ///
    /// 예시:
    ///   목표: QQQM 50%, SPLG 30%, GLD 20%
    ///   현재: QQQM 60%, SPLG 25%, GLD 15%
    ///   → QQQM 10% 초과 → 일부 매도
    ///   → GLD 5% 미달 → 추가 매수
    /// </summary>
    public class RebalancingEngine
    {
        private readonly IBrokerClient _broker;
        private readonly decimal _threshold;

        /// <param name="broker">증권사 클라이언트</param>
        /// <param name="threshold">리밸런싱 편차 임계값 (기본 5%)</param>
        public RebalancingEngine(IBrokerClient broker, decimal threshold = 0.05m)
        {
            _broker = broker;
            _threshold = threshold;
        }

        /// <summary>
        /// 리밸런싱 분석 + 주문 실행
        /// </summary>
        /// <param name="strategies">목표 전략 (종목별 비중)</param>
        /// <returns>조정 주문 목록</returns>
        public async Task<List<RebalanceOrder>> ExecuteAsync(List<StrategyDto> strategies)
        {
            var orders = new List<RebalanceOrder>();
            var holdings = await _broker.GetHoldingsAsync();
            var exchangeRate = await _broker.GetExchangeRateAsync();

            if (holdings.Count == 0)
            {
                Logger.Info("[Rebalance] 보유 종목 없음 — 리밸런싱 생략");
                return orders;
            }

            // 현재 포트폴리오 총 평가금액 (KRW)
            decimal totalValue = holdings.Sum(h => h.CurrentPrice * h.Qty * exchangeRate);
            if (totalValue <= 0)
            {
                Logger.Warn("[Rebalance] 포트폴리오 평가금액 0 — 리밸런싱 생략");
                return orders;
            }

            Logger.Info($"[Rebalance] === 리밸런싱 분석 시작 (총 평가: {totalValue:N0}원, 임계값: {_threshold:P0}) ===");

            foreach (var strategy in strategies)
            {
                var holding = holdings.Find(h => h.Ticker == strategy.Ticker);
                decimal currentWeight = 0m;
                decimal currentValue = 0m;

                if (holding != null && holding.Qty > 0)
                {
                    currentValue = holding.CurrentPrice * holding.Qty * exchangeRate;
                    currentWeight = currentValue / totalValue;
                }

                decimal targetWeight = (decimal)strategy.Weight;
                decimal deviation = currentWeight - targetWeight;

                Logger.Info($"[Rebalance] {strategy.Ticker}: " +
                    $"현재 {currentWeight:P1} → 목표 {targetWeight:P1} (편차 {deviation:P1})");

                // 편차가 임계값을 초과할 때만 조정
                if (Math.Abs(deviation) <= _threshold)
                    continue;

                decimal adjustAmount = Math.Abs(deviation) * totalValue;
                decimal currentPrice = holding?.CurrentPrice ?? await _broker.GetCurrentPriceAsync(strategy.Ticker);
                decimal priceKrw = currentPrice * exchangeRate;
                int adjustQty = (int)Math.Floor(adjustAmount / priceKrw);

                if (adjustQty <= 0)
                    continue;

                var order = new RebalanceOrder
                {
                    Ticker = strategy.Ticker,
                    CurrentWeight = currentWeight,
                    TargetWeight = targetWeight,
                    Deviation = deviation
                };

                if (deviation > 0)
                {
                    // 초과 → 매도
                    int sellQty = Math.Min(adjustQty, holding?.Qty ?? 0);
                    if (sellQty > 0)
                    {
                        order.Action = "SELL";
                        order.Qty = sellQty;
                        await _broker.PlaceSellOrderAsync(strategy.Ticker, sellQty, currentPrice);

                        TradeHistoryDAO.Insert(new TradeHistoryDto
                        {
                            TradeDate = DateTime.Now,
                            Ticker = strategy.Ticker,
                            OrderType = "SELL",
                            Qty = sellQty,
                            Price = currentPrice,
                            Status = "FILLED",
                            OrderNo = $"REBAL-{DateTime.Now:yyyyMMdd}"
                        });

                        Logger.Info($"[Rebalance] 리밸런싱 매도: {strategy.Ticker} {sellQty}주 " +
                            $"(비중 {currentWeight:P1} → {targetWeight:P1})");
                    }
                }
                else
                {
                    // 부족 → 매수
                    order.Action = "BUY";
                    order.Qty = adjustQty;
                    await _broker.PlaceBuyOrderAsync(strategy.Ticker, adjustQty, currentPrice);

                    TradeHistoryDAO.Insert(new TradeHistoryDto
                    {
                        TradeDate = DateTime.Now,
                        Ticker = strategy.Ticker,
                        OrderType = "BUY",
                        Qty = adjustQty,
                        Price = currentPrice,
                        Status = "FILLED",
                        OrderNo = $"REBAL-{DateTime.Now:yyyyMMdd}"
                    });

                    Logger.Info($"[Rebalance] 리밸런싱 매수: {strategy.Ticker} {adjustQty}주 " +
                        $"(비중 {currentWeight:P1} → {targetWeight:P1})");
                }

                orders.Add(order);
            }

            // 마지막 리밸런싱 날짜 기록
            AppConfigManager.Set("LAST_REBALANCE_DATE", DateTime.Now.ToString("yyyy-MM-dd"));

            Logger.Info($"[Rebalance] === 리밸런싱 완료 (조정 {orders.Count}건) ===");
            return orders;
        }

        /// <summary>
        /// 리밸런싱 주기가 도래했는지 확인합니다.
        /// </summary>
        public static bool IsDue()
        {
            var enabled = AppConfigManager.Get("REBALANCE_ENABLED", "0");
            if (enabled != "1") return false;

            var lastDateStr = AppConfigManager.Get("LAST_REBALANCE_DATE", "");
            if (string.IsNullOrEmpty(lastDateStr)) return true; // 한 번도 실행 안 함

            if (!DateTime.TryParse(lastDateStr, out var lastDate))
                return true;

            var period = AppConfigManager.Get("REBALANCE_PERIOD", "MONTHLY");
            return period switch
            {
                "WEEKLY" => (DateTime.Now - lastDate).TotalDays >= 7,
                "MONTHLY" => (DateTime.Now - lastDate).TotalDays >= 30,
                _ => (DateTime.Now - lastDate).TotalDays >= 30
            };
        }
    }
}
