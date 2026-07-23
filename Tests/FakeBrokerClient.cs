using System.Collections.Generic;
using System.Threading.Tasks;
using AutoInvest.Core;
using AutoInvest.Data.DTO;

namespace AutoInvest.Tests
{
    /// <summary>
    /// 테스트 전용 가짜 브로커(IBrokerClient 구현).
    /// 실계좌·네트워크·DB 없이 지정한 보유종목·현재가·환율을 결정적으로 반환합니다.
    /// OrderController의 절세 가드 배선(취득가→세금계산→409 차단)을 검증하는 데 사용합니다.
    ///
    /// 원칙: 테스트 데이터를 실제 계좌에 만들지 않는다 — 인터페이스로 가짜 객체를 주입한다.
    /// (Documents/[2026-07-02] 03_절세기능 테스트 실행계획서.md B계층)
    /// </summary>
    public class FakeBrokerClient : IBrokerClient
    {
        private readonly List<HoldingDto> _holdings;
        private readonly decimal _price;
        private readonly decimal _fx;

        /// <summary>
        /// PlaceSellOrderAsync가 호출된 횟수. 절세 가드가 "주문·기록 전에" 차단했는지를
        /// 이 값이 0인지로 검증합니다(가드가 통과시켰다면 1 이상).
        /// </summary>
        public int SellOrderCallCount { get; private set; }

        /// <summary>
        /// 가짜 브로커를 만듭니다.
        /// </summary>
        /// <param name="holdings">GetHoldingsAsync가 반환할 보유종목(시드값)</param>
        /// <param name="currentPrice">GetCurrentPriceAsync가 반환할 고정 현재가(USD)</param>
        /// <param name="exchangeRate">GetExchangeRateAsync가 반환할 고정 환율(USD→KRW)</param>
        public FakeBrokerClient(List<HoldingDto> holdings, decimal currentPrice, decimal exchangeRate)
        {
            _holdings = holdings ?? new List<HoldingDto>();
            _price = currentPrice;
            _fx = exchangeRate;
            IsLoggedIn = true;
        }

        public bool IsLoggedIn { get; private set; }

        public Task<bool> LoginAsync()
        {
            IsLoggedIn = true;
            return Task.FromResult(true);
        }

        public Task<decimal> GetCurrentPriceAsync(string ticker) => Task.FromResult(_price);

        public Task<decimal> GetExchangeRateAsync() => Task.FromResult(_fx);

        public Task<List<HoldingDto>> GetHoldingsAsync() => Task.FromResult(_holdings);

        public Task<decimal> GetCashBalanceAsync() => Task.FromResult(0m);

        public Task<string> PlaceBuyOrderAsync(string ticker, int qty, decimal price)
            => Task.FromResult("FAKE-BUY-0001");

        public Task<string> PlaceSellOrderAsync(string ticker, int qty, decimal price)
        {
            SellOrderCallCount++;
            return Task.FromResult("FAKE-SELL-0001");
        }
    }
}
