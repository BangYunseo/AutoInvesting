using System;
using System.Collections.Generic;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;

namespace AutoInvest.Core
{
    public class AllocationResult
    {
        // 종목 코드 (예: QQQM)
        public string Ticker { get; set; } = string.Empty;
        
        // 목표 비중 (0.0 ~ 1.0)
        public decimal Weight { get; set; }
        
        // 현재가 (원화 환산 기준, USD * 환율)
        public decimal Price { get; set; }
        
        // 매수 수량 (주 단위)
        public int Qty { get; set; }
        
        // 실제 투자금액 (원화, 단가 * 수량)
        public decimal Amount { get; set; }
    }

    public static class AllocationEngine
    {
        /// <summary>
        /// 전략의 종목별 수량과 현재가로 배분 결과를 계산합니다.
        /// </summary>
        /// <param name="exchangeRate">환율 (원/달러)</param>
        /// <param name="strategies">전략 목록 (종목별 수량)</param>
        /// <param name="prices">종목별 현재가 (달러)</param>
        public static List<AllocationResult> Calculate(
            decimal exchangeRate,
            List<StrategyDto> strategies,
            Dictionary<string, decimal> prices)
        {
            var results = new List<AllocationResult>();

            // 전체 수량 합계 (비중 계산용)
            int totalQty = 0;
            foreach (var s in strategies)
                totalQty += s.Qty;
            if (totalQty <= 0) totalQty = 1;

            foreach (var s in strategies)
            {
                if (!prices.TryGetValue(s.Ticker, out var priceUsd))
                {
                    Logger.Warn($"현재가 없음: {s.Ticker}");
                    continue;
                }

                decimal priceKrw = priceUsd * exchangeRate;
                decimal actualAmt = s.Qty * priceKrw;
                decimal weight = (decimal)s.Qty / totalQty;

                results.Add(new AllocationResult
                {
                    Ticker = s.Ticker,
                    Weight = weight,
                    Price = priceKrw,
                    Qty = s.Qty,
                    Amount = actualAmt
                });

                Logger.Info($"배분 계산: {s.Ticker} " +
                    $"수량={s.Qty}주 단가={priceKrw:N0}원 금액={actualAmt:N0}원");
            }

            return results;
        }
    }
}