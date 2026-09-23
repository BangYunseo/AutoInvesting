using System.Collections.Generic;
using System.Linq;
using AutoInvest.Core;
using Xunit;

namespace AutoInvest.Tests
{
    /// <summary>
    /// KisBrokerClient.OrderExchanges(순수 함수 — 외부 I/O 없음)의 단위 검증.
    ///
    /// 현재가 조회는 종목이 어느 거래소(EXCD)에 있는지 몰라 NAS→NYS→AMS를 차례로 찔러보고,
    /// 각 시도 앞에 Rate limit 방어용 400ms 지연이 있습니다. 이전에 확인된 거래소를 먼저 시도하면
    /// 헛조회가 줄지만, 그 과정에서 어느 거래소가 빠지면 조회되던 종목이 조회되지 않게 됩니다.
    /// 그 회귀를 막기 위해 "순서는 바뀌어도 후보는 전부 남는다"를 못 박습니다.
    /// </summary>
    public class KisExchangeOrderTests
    {
        /// <summary>현재가 조회가 시도해야 할 전체 거래소 집합.</summary>
        private static readonly string[] All = { "NAS", "NYS", "AMS" };

        /// <summary>확인된 거래소가 없으면 기존 순서(NAS→NYS→AMS) 그대로여야 한다.</summary>
        [Fact]
        public void OrderExchanges_캐시가_없으면_기존순서를_유지한다()
        {
            var order = KisBrokerClient.OrderExchanges(null).ToList();

            Assert.Equal(All, order);
        }

        /// <summary>빈 문자열도 "모름"으로 취급해 기존 순서를 유지해야 한다.</summary>
        [Fact]
        public void OrderExchanges_빈문자열은_모름으로_취급한다()
        {
            var order = KisBrokerClient.OrderExchanges("").ToList();

            Assert.Equal(All, order);
        }

        /// <summary>확인된 거래소는 맨 앞에 오고, 중복 없이 나머지가 뒤따라야 한다.</summary>
        [Fact]
        public void OrderExchanges_확인된거래소를_먼저_시도한다()
        {
            var order = KisBrokerClient.OrderExchanges("AMS").ToList();

            Assert.Equal(new[] { "AMS", "NAS", "NYS" }, order);
        }

        /// <summary>이미 첫 번째인 거래소를 넘겨도 순서와 개수가 그대로여야 한다(중복 금지).</summary>
        [Fact]
        public void OrderExchanges_이미_첫번째면_그대로다()
        {
            var order = KisBrokerClient.OrderExchanges("NAS").ToList();

            Assert.Equal(All, order);
        }

        /// <summary>
        /// 목록에 없는 값이 들어와도 모든 거래소는 그대로 시도해야 한다.
        /// 캐시에 낡은 값이 남아 특정 거래소가 빠지면, 조회되던 종목이 조용히 조회 실패가 된다.
        /// </summary>
        [Fact]
        public void OrderExchanges_모르는값이어도_모든거래소를_포함한다()
        {
            var order = KisBrokerClient.OrderExchanges("ZZZ").ToList();

            Assert.Equal("ZZZ", order[0]);
            foreach (string excd in All)
            {
                Assert.Contains(excd, order);
            }
        }

        /// <summary>어떤 입력에도 알려진 거래소가 중복 시도되지 않아야 한다(불필요한 400ms 방지).</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("NAS")]
        [InlineData("NYS")]
        [InlineData("AMS")]
        public void OrderExchanges_알려진거래소는_한번씩만_나온다(string? known)
        {
            var order = KisBrokerClient.OrderExchanges(known).ToList();

            foreach (string excd in All)
            {
                Assert.Equal(1, order.Count(e => e == excd));
            }
        }
    }
}
