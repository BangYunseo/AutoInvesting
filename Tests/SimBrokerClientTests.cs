using System.Linq;
using System.Threading.Tasks;
using AutoInvest.Core;
using Xunit;

namespace AutoInvest.Tests
{
    /// <summary>
    /// SimBrokerClient(모의투자 브로커)의 결정적 동작을 못박는 스모크 테스트.
    /// 4순위 DcaAccumulationEngine 검증이 이 시뮬 브로커에 의존하므로, 시뮬의 계약
    /// (가중평균 평단 / 매도 차감·전량 매도 제거 / 미등록 티커 폴백 / 상수 반환)을 먼저 고정한다.
    ///
    /// 주의: 기준가·환율은 "최근 스냅샷(수동 갱신)"이라 값이 바뀔 수 있으므로 정확값을 단정하지 않고
    /// 양수·결정성만 검증한다. 폴백 $100·산식 등 시뮬 내부 계약은 정확히 검증한다.
    /// </summary>
    public class SimBrokerClientTests
    {
        /// <summary>로그인은 항상 성공하고 IsLoggedIn이 켜져야 한다.</summary>
        [Fact]
        public async Task LoginAsync_항상_성공하고_상태를_켠다()
        {
            var broker = new SimBrokerClient();
            Assert.False(broker.IsLoggedIn);

            bool ok = await broker.LoginAsync();

            Assert.True(ok);
            Assert.True(broker.IsLoggedIn);
        }

        /// <summary>등록된 티커의 현재가는 양수이고, 같은 티커는 매번 같은 값(결정적)이어야 한다.</summary>
        [Fact]
        public async Task GetCurrentPriceAsync_등록티커는_양수이고_결정적이다()
        {
            var broker = new SimBrokerClient();

            decimal first = await broker.GetCurrentPriceAsync("SPLG");
            decimal second = await broker.GetCurrentPriceAsync("SPLG");

            Assert.True(first > 0);
            Assert.Equal(first, second); // 랜덤 없음 — 스냅샷 값을 그대로 반환
        }

        /// <summary>표에 없는 티커는 $100으로 폴백해야 한다(시뮬 내부 계약).</summary>
        [Fact]
        public async Task GetCurrentPriceAsync_미등록티커는_100으로_폴백한다()
        {
            var broker = new SimBrokerClient();

            decimal price = await broker.GetCurrentPriceAsync("UNKNOWN_TICKER");

            Assert.Equal(100.00m, price);
        }

        /// <summary>환율은 양수를 반환해야 한다(정확값은 스냅샷이라 단정하지 않음).</summary>
        [Fact]
        public async Task GetExchangeRateAsync_양수를_반환한다()
        {
            var broker = new SimBrokerClient();

            decimal rate = await broker.GetExchangeRateAsync();

            Assert.True(rate > 0);
        }

        /// <summary>예수금은 고정 상수($10,000)를 반환해야 한다(시뮬 내부 계약).</summary>
        [Fact]
        public async Task GetCashBalanceAsync_고정상수를_반환한다()
        {
            var broker = new SimBrokerClient();

            decimal cash = await broker.GetCashBalanceAsync();

            Assert.Equal(10000.00m, cash);
        }

        /// <summary>신규 매수는 가상 잔고에 체결가를 평단으로 기록해야 한다.</summary>
        [Fact]
        public async Task PlaceBuyOrderAsync_신규매수는_체결가가_평단이_된다()
        {
            var broker = new SimBrokerClient();

            await broker.PlaceBuyOrderAsync("SPLG", 2, 80m);
            var holding = (await broker.GetHoldingsAsync()).Single(h => h.Ticker == "SPLG");

            Assert.Equal(2, holding.Qty);
            Assert.Equal(80m, holding.AvgPrice);
        }

        /// <summary>연속 매수는 가중평균으로 평단을 재계산해야 한다.</summary>
        [Fact]
        public async Task PlaceBuyOrderAsync_연속매수는_가중평균_평단이다()
        {
            var broker = new SimBrokerClient();

            await broker.PlaceBuyOrderAsync("SPLG", 2, 80m);   // 원가 160
            await broker.PlaceBuyOrderAsync("SPLG", 2, 100m);  // 원가 200
            var holding = (await broker.GetHoldingsAsync()).Single(h => h.Ticker == "SPLG");

            Assert.Equal(4, holding.Qty);
            Assert.Equal(90m, holding.AvgPrice); // (160 + 200) / 4 = 90
        }

        /// <summary>일부 매도는 수량만 차감하고 평단은 유지해야 한다.</summary>
        [Fact]
        public async Task PlaceSellOrderAsync_일부매도는_수량만_차감한다()
        {
            var broker = new SimBrokerClient();
            await broker.PlaceBuyOrderAsync("SPLG", 5, 80m);

            await broker.PlaceSellOrderAsync("SPLG", 2, 100m);
            var holding = (await broker.GetHoldingsAsync()).Single(h => h.Ticker == "SPLG");

            Assert.Equal(3, holding.Qty);
            Assert.Equal(80m, holding.AvgPrice); // 평단 유지
        }

        /// <summary>전량 매도(보유수량 이상)는 종목을 잔고에서 제거해야 한다.</summary>
        [Fact]
        public async Task PlaceSellOrderAsync_전량매도는_잔고에서_제거한다()
        {
            var broker = new SimBrokerClient();
            await broker.PlaceBuyOrderAsync("SPLG", 3, 80m);

            await broker.PlaceSellOrderAsync("SPLG", 3, 100m);
            var holdings = await broker.GetHoldingsAsync();

            Assert.DoesNotContain(holdings, h => h.Ticker == "SPLG");
        }

        /// <summary>보유가 없어도 매도는 예외 없이 주문번호를 반환하고 잔고는 그대로여야 한다.</summary>
        [Fact]
        public async Task PlaceSellOrderAsync_보유없어도_예외없이_처리한다()
        {
            var broker = new SimBrokerClient();

            string orderNo = await broker.PlaceSellOrderAsync("SPLG", 1, 80m);

            Assert.False(string.IsNullOrEmpty(orderNo));
            Assert.Empty(await broker.GetHoldingsAsync());
        }
    }
}
