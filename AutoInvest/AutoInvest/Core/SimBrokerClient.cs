using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoInvest.Core
{
    /// <summary>
    /// 시뮬레이션 브로커 클라이언트.
    /// LS증권 API 키 없이도 SmartOrderEngine, SchedulerModule 등
    /// 전체 엔진 로직을 테스트할 수 있도록 가상 데이터를 반환합니다.
    ///
    /// TODO [Phase 3] LsBrokerClient 구현 시 이 클래스를 참조 구현으로 활용
    /// TODO [Phase 4] AI 학습 데이터 생성 시, 시뮬레이션 결과를 학습 데이터로 저장하는 기능 추가
    /// </summary>
    public class SimBrokerClient : IBrokerClient
    {
        private bool _isLoggedIn;
        private readonly Random _rng = new Random();

        /// <summary>
        /// ETF별 기준가 (USD). 시뮬레이션 현재가는 이 값 ±3% 범위에서 생성.
        /// </summary>
        private readonly Dictionary<string, decimal> _basePrices = new Dictionary<string, decimal>
        {
            { "SCHD",  27.50m },
            { "QQQM", 200.00m },
            { "GLD",  195.00m },
            { "JEPI",  56.00m },
            { "SPLG",  62.00m }
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
            // TODO [Phase 3] LS증권 API 연동 시 실제 시세 조회로 교체
            Logger.Info($"[SimBroker] 현재가 조회: {ticker} = ${price}");
            return Task.FromResult(price);
        }

        public Task<(decimal High, decimal Low)> GetPriceRangeAsync(string ticker, int days)
        {
            decimal basePrice = GetBasePrice(ticker);
            // 기준가 ±10% 범위
            decimal high = Math.Round(basePrice * 1.10m, 2);
            decimal low = Math.Round(basePrice * 0.90m, 2);
            Logger.Info($"[SimBroker] {days}일 가격범위: {ticker} High=${high} Low=${low}");
            return Task.FromResult((high, low));
        }

        public Task<decimal> GetExchangeRateAsync()
        {
            const decimal rate = 1350.00m;
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
