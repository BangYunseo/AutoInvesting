using System;
using System.Collections.Generic;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;

namespace AutoInvest.Core
{
    public class AllocationResult
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public decimal Price { get; set; } // 현재가 (원화 환산)
        public int Qty { get; set; } // 매수 수량
        public decimal Amount { get; set; } // 실제 투자금액
    }

    public static class AllocationEngine
    {
        /// <summary>
        /// 투자금액과 전략 비중으로 종목별 매수 수량 계산
        /// </summary>
        /// <param name="investAmountKrw">총 투자금액 (원)</param>
        /// <param name="exchangeRate">환율 (원/달러)</param>
        /// <param name="strategies">전략 목록 (비중 합계 1.0)</param>
        /// <param name="prices">종목별 현재가 (달러)</param>
        public static List<AllocationResult> Calculate(
            decimal investAmountKrw,
            decimal exchangeRate,
            List<StrategyDto> strategies,
            Dictionary<string, decimal> prices)
        {
            var results = new List<AllocationResult>();

            foreach (var s in strategies)
            {
                if (!prices.TryGetValue(s.Ticker, out var priceUsd))
                {
                    Logger.Warn($"현재가 없음: {s.Ticker}");
                    continue;
                }

                decimal allocKrw = investAmountKrw * (decimal)s.Weight;
                decimal priceKrw = priceUsd * exchangeRate;
                int qty = (int)Math.Floor(allocKrw / priceKrw); // 소수점 버림
                decimal actualAmt = qty * priceKrw;

                results.Add(new AllocationResult
                {
                    Ticker = s.Ticker,
                    Weight = (decimal)s.Weight,
                    Price = priceKrw,
                    Qty = qty,
                    Amount = actualAmt
                });

                Logger.Info($"배분 계산: {s.Ticker} " +
                    $"비중={s.Weight:P0} 단가={priceKrw:N0}원 수량={qty}주 금액={actualAmt:N0}원");
            }

            return results;
        }
    }
}