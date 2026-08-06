using AutoInvest.Core;
using System;
using Xunit;

namespace AutoInvest.Tests
{
    /// <summary>
    /// DailyExecutionService.IsOnOrAfterRunDay(순수 함수 — 외부 I/O 없음)의 단위 검증.
    ///
    /// 이 판정이 틀리면 실자금이 사람이 고른 날짜보다 이르게 나가거나(게이트가 안 막힘),
    /// 그 달 적립이 통째로 빠진다(게이트가 계속 막음). 월 1회 멱등 가드와 맞물려 동작하므로
    /// "지정일에만"이 아니라 "지정일부터"라는 성질을 특히 고정해 둔다.
    /// </summary>
    public class DcaRunDayTests
    {
        private static DateTime Kst(int day) => new DateTime(2026, 8, day, 0, 10, 0);

        /// <summary>지정일 미설정(0)이면 월초부터 매일 시도해야 한다(기존 동작 유지).</summary>
        [Fact]
        public void 지정일_미설정이면_어느_날이든_시도()
        {
            Assert.True(DailyExecutionService.IsOnOrAfterRunDay(Kst(1), 0));
            Assert.True(DailyExecutionService.IsOnOrAfterRunDay(Kst(15), 0));
            Assert.True(DailyExecutionService.IsOnOrAfterRunDay(Kst(28), 0));
        }

        /// <summary>지정일 전날까지는 크론이 흘려보내야 한다.</summary>
        [Fact]
        public void 지정일_전이면_시도하지_않음()
        {
            Assert.False(DailyExecutionService.IsOnOrAfterRunDay(Kst(1), 10));
            Assert.False(DailyExecutionService.IsOnOrAfterRunDay(Kst(9), 10));
        }

        /// <summary>지정일 당일은 시도해야 한다.</summary>
        [Fact]
        public void 지정일_당일이면_시도()
        {
            Assert.True(DailyExecutionService.IsOnOrAfterRunDay(Kst(10), 10));
        }

        /// <summary>
        /// 지정일 이후에도 계속 시도해야 한다 — 지정일이 주말·휴장이면 그날은 접수 0건이 되고,
        /// 월 1회 마커가 남지 않아 다음 영업일에 1회 집행되는 경로가 이 성질에 의존한다.
        /// </summary>
        [Fact]
        public void 지정일_이후에도_시도해_다음_영업일_이월이_가능()
        {
            Assert.True(DailyExecutionService.IsOnOrAfterRunDay(Kst(11), 10));
            Assert.True(DailyExecutionService.IsOnOrAfterRunDay(Kst(28), 10));
        }

        /// <summary>
        /// 31일을 골라도 그 날이 없는 달에는 말일로 당겨 판정해야 한다 — 당기지 않으면
        /// 2·4·6·9·11월 적립이 통째로 빠진다.
        /// </summary>
        [Fact]
        public void 없는_날짜를_고르면_말일로_당겨_판정()
        {
            // 2026년 2월은 28일까지 → 31 지정은 28일부터
            Assert.False(DailyExecutionService.IsOnOrAfterRunDay(new DateTime(2026, 2, 27), 31));
            Assert.True(DailyExecutionService.IsOnOrAfterRunDay(new DateTime(2026, 2, 28), 31));

            // 2026년 4월은 30일까지 → 31 지정은 30일부터
            Assert.False(DailyExecutionService.IsOnOrAfterRunDay(new DateTime(2026, 4, 29), 31));
            Assert.True(DailyExecutionService.IsOnOrAfterRunDay(new DateTime(2026, 4, 30), 31));

            // 31일까지 있는 달은 그대로 31일부터
            Assert.False(DailyExecutionService.IsOnOrAfterRunDay(new DateTime(2026, 8, 30), 31));
            Assert.True(DailyExecutionService.IsOnOrAfterRunDay(new DateTime(2026, 8, 31), 31));
        }

        /// <summary>윤년 2월(29일)에 29 지정은 그대로 29일부터여야 한다.</summary>
        [Fact]
        public void 윤년_2월은_29일이_그대로_유효()
        {
            Assert.False(DailyExecutionService.IsOnOrAfterRunDay(new DateTime(2028, 2, 28), 29));
            Assert.True(DailyExecutionService.IsOnOrAfterRunDay(new DateTime(2028, 2, 29), 29));

            // 평년 2월에는 28일로 당겨진다
            Assert.True(DailyExecutionService.IsOnOrAfterRunDay(new DateTime(2026, 2, 28), 29));
        }
    }
}
