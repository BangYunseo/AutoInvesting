using System;
using System.Threading.Tasks;
using AutoInvest.Controllers;
using AutoInvest.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AutoInvest.Tests
{
    /// <summary>
    /// <see cref="AuthController.Login"/>이 실제로 <see cref="LoginThrottle"/>을 <b>호출하는지</b> 고정한다.
    ///
    /// <see cref="LoginThrottleTests"/>는 헬퍼를 고립 상태로만 검증하므로, 컨트롤러에서 그 호출을 지워도
    /// 전부 통과한다(뮤테이션으로 확인됨). 배선이 끊기면 브루트포스 방어가 통째로 사라지므로
    /// 호출 지점 자체를 별도로 못 박는다.
    ///
    /// 상한 검사는 자격증명·DB 조회보다 앞에 있어야 하므로, DB가 없는 테스트 환경에서도 429가 나와야 한다.
    /// 이 테스트가 503이나 401을 받으면 검사 순서가 뒤로 밀린 것이다(=PBKDF2와 DB 왕복이 먼저 돈다).
    ///
    /// 참고: Setup의 fail-closed(503) 경로는 여기서 검증하지 않는다. 호출하면 실제 DB에 연결될 수 있고,
    /// 그 DB에 관리자 행이 없으면 테스트가 관리자 계정을 써버린다. DB를 스텁으로 바꾸기 전까지는
    /// 리플렉션 계약(<see cref="PublicEndpointExposureTests"/>)까지만 자동 검증한다.
    /// </summary>
    [Collection("LoginThrottle")]
    public class AuthControllerThrottleWiringTests
    {
        private static AuthController NewController()
            => new AuthController
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

        [Fact]
        public async Task 실패_상한을_넘기면_로그인이_429를_반환한다()
        {
            LoginThrottle.Reset();
            try
            {
                for (int i = 0; i < LoginThrottle.MaxFailsPerWindow; i++)
                    LoginThrottle.RegisterFailure(DateTime.UtcNow);

                AuthController controller = NewController();
                IActionResult result = await controller.Login(
                    new AuthController.CredentialRequest { Username = "someone", Password = "whatever" });

                var objectResult = Assert.IsType<ObjectResult>(result);
                Assert.Equal(429, objectResult.StatusCode);
                Assert.True(controller.Response.Headers.ContainsKey("Retry-After"));
            }
            finally
            {
                LoginThrottle.Reset();
            }
        }

        [Fact]
        public async Task 상한_이하에서는_상한_때문에_거부되지_않는다()
        {
            LoginThrottle.Reset();
            try
            {
                for (int i = 0; i < LoginThrottle.MaxFailsPerWindow - 1; i++)
                    LoginThrottle.RegisterFailure(DateTime.UtcNow);

                AuthController controller = NewController();
                IActionResult result = await controller.Login(
                    new AuthController.CredentialRequest { Username = "someone", Password = "whatever" });

                // 429가 아니기만 하면 된다 — 그 뒤 결과(503/400/401)는 DB 상태에 달려 있어 고정하지 않는다.
                if (result is ObjectResult objectResult)
                    Assert.NotEqual(429, objectResult.StatusCode);
            }
            finally
            {
                LoginThrottle.Reset();
            }
        }
    }
}
