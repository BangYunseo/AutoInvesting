using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoInvest.Core
{
    /// <summary>
    /// 시뮬레이션 브로커 클라이언트.
    /// 한국투자증권(KIS) API 키 없이도 DcaAccumulationEngine, DailyExecutionService 등
    /// 전체 적립 사이클 로직을 검증할 수 있도록 가상 데이터를 반환합니다.
    /// </summary>
    public class SimBrokerClient : IBrokerClient
    {
        // 로그인 상태 여부
        private bool _isLoggedIn;

        /// <summary>
        /// ETF별 기준가 (USD). <see cref="GetCurrentPriceAsync"/>가 이 값을 그대로(랜덤 없이) 반환한다.
        /// 실거래 없이 적립 사이클을 실제처럼 검증하기 위한 대략적 최근 스냅샷(2026-07 기준, 수동 갱신)이며
        /// 실시간 시세가 아니다. 표에 없는 티커는 <see cref="GetBasePrice"/>에서 $100으로 폴백한다.
        /// </summary>
        private readonly Dictionary<string, decimal> _basePrices = new Dictionary<string, decimal>
        {
            { "VTI",  32.39m },
            { "QQQ", 293.42m },
            { "GLD",  378.13m },
            { "JEPI",  56.71m },
            { "SPLG",  80.00m }
        };

        /// <summary>시뮬레이션 보유 잔고</summary>
        private readonly Dictionary<string, (int Qty, decimal AvgPrice)> _holdings
            = new Dictionary<string, (int, decimal)>();

        public bool IsLoggedIn => _isLoggedIn;

        public Task<bool> LoginAsync()
        {
            _isLoggedIn = true;
            Logger.Info("[SimBroker] 시뮬레이션 로그인 성공");
            return Task.FromResult(true);
        }

        public Task<decimal> GetCurrentPriceAsync(string ticker)
        {
            decimal price = GetBasePrice(ticker);
            Logger.Info($"[SimBroker] 현재가 조회: {ticker} = ${price}");
            return Task.FromResult(price);
        }

        public Task<decimal> GetExchangeRateAsync()
        {
            // 대략적 최근 스냅샷(2026-07 기준, 수동 갱신) — 실시간 환율이 아니다.
            const decimal rate = 1530.00m;
            Logger.Info($"[SimBroker] 환율 조회: 1 USD = {rate:N0} KRW");
            return Task.FromResult(rate);
        }

        public Task<List<HoldingDto>> GetHoldingsAsync()
        {
            var list = new List<HoldingDto>();
            foreach (var kv in _holdings)
            {
                decimal current = GetBasePrice(kv.Key);
                list.Add(new HoldingDto
                {
                    Ticker = kv.Key,
                    Qty = kv.Value.Qty,
                    AvgPrice = kv.Value.AvgPrice,
                    CurrentPrice = current,
                    ProfitRate = kv.Value.AvgPrice > 0
                        ? (current - kv.Value.AvgPrice) / kv.Value.AvgPrice
                        : 0
                });
            }
            Logger.Info($"[SimBroker] 보유 종목 {list.Count}건 조회");
            return Task.FromResult(list);
        }

        public Task<decimal> GetCashBalanceAsync()
        {
            const decimal cashBalance = 10000.00m;
            Logger.Info($"[SimBroker] 예수금 조회: ${cashBalance:N2}");
            return Task.FromResult(cashBalance);
        }

        public Task<string> PlaceBuyOrderAsync(string ticker, int qty, decimal price)
        {
            string orderNo = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();

            // 시뮬레이션 잔고 반영
            if (_holdings.ContainsKey(ticker))
            {
                var (prevQty, prevAvg) = _holdings[ticker];
                decimal totalCost = prevAvg * prevQty + price * qty;
                int newQty = prevQty + qty;
                _holdings[ticker] = (newQty, Math.Round(totalCost / newQty, 2));
            }
            else
            {
                _holdings[ticker] = (qty, price);
            }

            Logger.Info($"[SimBroker] 매수 주문 체결: {ticker} {qty}주 @ ${price} (주문번호: {orderNo})");
            return Task.FromResult(orderNo);
        }

        public Task<string> PlaceSellOrderAsync(string ticker, int qty, decimal price)
        {
            string orderNo = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();

            // 시뮬레이션 잔고 반영
            if (_holdings.ContainsKey(ticker))
            {
                var (prevQty, prevAvg) = _holdings[ticker];
                int remaining = prevQty - qty;
                if (remaining <= 0)
                    _holdings.Remove(ticker);
                else
                    _holdings[ticker] = (remaining, prevAvg);
            }

            Logger.Info($"[SimBroker] 매도 주문 체결: {ticker} {qty}주 @ ${price} (주문번호: {orderNo})");
            return Task.FromResult(orderNo);
        }

        private decimal GetBasePrice(string ticker)
        {
            return _basePrices.ContainsKey(ticker) ? _basePrices[ticker] : 100.00m;
        }
    }
}

