using System.Collections.Generic;
using System.Threading.Tasks;
using AutoInvest.Controllers;
using AutoInvest.Core;
using AutoInvest.Data.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace AutoInvest.Tests
{
    /// <summary>
    /// PriceController의 현재가 캐시 검증.
    ///
    /// 적립설정 화면은 진입할 때마다 저장된 종목 전부를 다시 검증하고, 현재가 조회는 거래소를
    /// 차례로 찔러보는 구조라 종목당 최소 400ms가 든다. 같은 종목을 짧은 간격으로 다시 묻는
    /// 경우가 대부분이라 캐시를 두었다. 실계좌·네트워크 없이 FakeBrokerClient의 호출 횟수로
    /// "정말 브로커를 건너뛰는지"를 확인한다.
    /// </summary>
    public class PriceControllerCacheTests
    {
        private const decimal Price = 123.45m;
        private const decimal Rate = 1_300m;

        private static PriceController BuildController(FakeBrokerClient broker, IMemoryCache cache)
            => new PriceController(new SessionManager(broker), cache);

        private static IMemoryCache NewCache() => new MemoryCache(new MemoryCacheOptions());

        /// <summary>같은 티커를 두 번 물으면 브로커 조회는 한 번만 나가야 한다.</summary>
        [Fact]
        public async Task GetPrice_같은티커_두번째는_브로커를_치지_않는다()
        {
            var broker = new FakeBrokerClient(new List<HoldingDto>(), Price, Rate);
            var controller = BuildController(broker, NewCache());

            await controller.GetPrice("SPY");
            await controller.GetPrice("SPY");

            Assert.Equal(1, broker.PriceCallCount);
        }

        /// <summary>캐시에서 준 값도 조회했을 때와 같아야 한다(표시가 달라지면 캐시가 의미 없다).</summary>
        [Fact]
        public async Task GetPrice_캐시적중이어도_같은값을_돌려준다()
        {
            var broker = new FakeBrokerClient(new List<HoldingDto>(), Price, Rate);
            var controller = BuildController(broker, NewCache());

            await controller.GetPrice("SPY");
            var second = Assert.IsType<OkObjectResult>(await controller.GetPrice("SPY"));

            Assert.Equal(Price, GetProp<decimal>(second.Value!, "priceUsd"));
            Assert.Equal("SPY", GetProp<string>(second.Value!, "ticker"));
        }

        /// <summary>다른 티커는 캐시를 공유하지 않고 각각 조회해야 한다.</summary>
        [Fact]
        public async Task GetPrice_다른티커는_각각_조회한다()
        {
            var broker = new FakeBrokerClient(new List<HoldingDto>(), Price, Rate);
            var controller = BuildController(broker, NewCache());

            await controller.GetPrice("SPY");
            await controller.GetPrice("QQQM");

            Assert.Equal(2, broker.PriceCallCount);
        }

        /// <summary>대소문자가 달라도 같은 종목이므로 캐시가 적중해야 한다.</summary>
        [Fact]
        public async Task GetPrice_대소문자가_달라도_같은_캐시를_쓴다()
        {
            var broker = new FakeBrokerClient(new List<HoldingDto>(), Price, Rate);
            var controller = BuildController(broker, NewCache());

            await controller.GetPrice("spy");
            await controller.GetPrice("SPY");

            Assert.Equal(1, broker.PriceCallCount);
        }

        /// <summary>
        /// 조회 실패(0)는 캐시하지 않고 다음 요청에서 다시 시도해야 한다.
        ///
        /// 실패를 캐시하면 Rate limit 같은 일시적 실패가 캐시 시간 동안 "존재하지 않는 티커"로
        /// 굳어, 멀쩡한 종목을 사람이 설정에서 빼게 만든다.
        /// </summary>
        [Fact]
        public async Task GetPrice_조회실패는_캐시하지_않는다()
        {
            var broker = new FakeBrokerClient(new List<HoldingDto>(), currentPrice: 0m, exchangeRate: Rate);
            var controller = BuildController(broker, NewCache());

            Assert.IsType<NotFoundObjectResult>(await controller.GetPrice("ZZZZ"));
            Assert.IsType<NotFoundObjectResult>(await controller.GetPrice("ZZZZ"));

            Assert.Equal(2, broker.PriceCallCount);
        }

        /// <summary>익명 응답 객체에서 프로퍼티 값을 리플렉션으로 꺼낸다.</summary>
        private static T GetProp<T>(object payload, string name)
            => (T)payload.GetType().GetProperty(name)!.GetValue(payload)!;
    }
}
