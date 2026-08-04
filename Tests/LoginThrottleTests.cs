using System;
using AutoInvest.Utils;
using Xunit;

namespace AutoInvest.Tests
{
    /// <summary>
    /// <see cref="LoginThrottle"/>은 프로세스 전역 카운터 하나를 쓰므로, 이 카운터를 건드리는
    /// 테스트는 모두 같은 컬렉션에 묶어 순차 실행시키고 각 테스트 시작 시 초기화한다.
    /// </summary>
    [CollectionDefinition("LoginThrottle")]
    public class LoginThrottleCollection { }

    /// <summary>로그인 실패 속도 상한 검증.</summary>
    [Collection("LoginThrottle")]
    public class LoginThrottleTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 8, 4, 12, 0, 0, DateTimeKind.Utc);

        public LoginThrottleTests() => LoginThrottle.Reset();

        [Fact]
        public void 상한_미만이면_제한하지_않는다()
        {
            for (int i = 0; i < LoginThrottle.MaxFailsPerWindow - 1; i++)
                LoginThrottle.RegisterFailure(T0);

            Assert.False(LoginThrottle.IsRateLimited(T0, out TimeSpan retryAfter));
            Assert.Equal(TimeSpan.Zero, retryAfter);
        }

        [Fact]
        public void 상한에_도달하면_제한한다()
        {
            for (int i = 0; i < LoginThrottle.MaxFailsPerWindow; i++)
                LoginThrottle.RegisterFailure(T0);

            Assert.True(LoginThrottle.IsRateLimited(T0, out TimeSpan retryAfter));
            Assert.True(retryAfter > TimeSpan.Zero);
            Assert.True(retryAfter <= LoginThrottle.Window);
        }

        [Fact]
        public void 창이_지나면_제한이_풀린다()
        {
            for (int i = 0; i < LoginThrottle.MaxFailsPerWindow; i++)
                LoginThrottle.RegisterFailure(T0);

            Assert.True(LoginThrottle.IsRateLimited(T0, out _));
            Assert.False(LoginThrottle.IsRateLimited(T0 + LoginThrottle.Window, out _));
        }

        [Fact]
        public void 창이_지난_뒤의_실패는_새_창에서_다시_센다()
        {
            for (int i = 0; i < LoginThrottle.MaxFailsPerWindow; i++)
                LoginThrottle.RegisterFailure(T0);

            DateTime next = T0 + LoginThrottle.Window;

            // 새 창의 첫 실패 — 상한까지 여유가 다시 생겨야 한다.
            LoginThrottle.RegisterFailure(next);
            Assert.False(LoginThrottle.IsRateLimited(next, out _));

            // 새 창에서도 상한에 도달하면 다시 제한된다.
            for (int i = 1; i < LoginThrottle.MaxFailsPerWindow; i++)
                LoginThrottle.RegisterFailure(next);
            Assert.True(LoginThrottle.IsRateLimited(next, out _));
        }

        [Fact]
        public void 로그인_성공으로_초기화하면_제한이_풀린다()
        {
            for (int i = 0; i < LoginThrottle.MaxFailsPerWindow; i++)
                LoginThrottle.RegisterFailure(T0);
            Assert.True(LoginThrottle.IsRateLimited(T0, out _));

            LoginThrottle.Reset();

            Assert.False(LoginThrottle.IsRateLimited(T0, out _));
        }

        [Fact]
        public void 남은_시간은_창이_끝나는_시점까지다()
        {
            for (int i = 0; i < LoginThrottle.MaxFailsPerWindow; i++)
                LoginThrottle.RegisterFailure(T0);

            DateTime mid = T0 + TimeSpan.FromSeconds(20);
            Assert.True(LoginThrottle.IsRateLimited(mid, out TimeSpan retryAfter));
            Assert.Equal(LoginThrottle.Window - TimeSpan.FromSeconds(20), retryAfter);
        }
    }
}
