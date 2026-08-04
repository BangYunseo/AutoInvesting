using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoInvest.Controllers;
using AutoInvest.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace AutoInvest.Tests
{
    /// <summary>
    /// 전역 인증 필터를 면제받는 엔드포인트가 의도한 두 개뿐인지 고정합니다.
    ///
    /// <see cref="PublicEndpointAttribute"/>를 컨트롤러 클래스에 붙이면 그 안의 모든 액션이
    /// 한꺼번에 열립니다. 과거 <see cref="AuthController"/>가 그런 상태여서 <c>setup</c>까지
    /// 미인증 공개였고, 관리자 자리가 비어 보이는 순간 아무나 관리자를 선점할 수 있었습니다.
    /// 같은 실수가 되살아나면 이 테스트가 깨지도록 목록을 못 박아 둡니다.
    /// </summary>
    public class PublicEndpointExposureTests
    {
        /// <summary>인증 없이 열려 있어도 되는 액션 (컨트롤러명.액션명).</summary>
        private static readonly HashSet<string> AllowedPublicActions = new(StringComparer.Ordinal)
        {
            "AuthController.GetStatus",
            "AuthController.Login",
        };

        private static IEnumerable<Type> ControllerTypes()
            => typeof(AuthController).Assembly.GetTypes()
                .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);

        [Fact]
        public void 인증면제_액션은_허용목록과_정확히_일치한다()
        {
            var actual = new HashSet<string>(StringComparer.Ordinal);

            foreach (Type t in ControllerTypes())
            {
                bool classPublic = t.GetCustomAttribute<PublicEndpointAttribute>() != null;

                foreach (MethodInfo m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    // 액션으로 라우팅되는 메서드만 대상 (HTTP 동사 어트리뷰트 보유)
                    if (m.GetCustomAttributes().OfType<HttpMethodAttribute>().Any() == false) continue;

                    if (classPublic || m.GetCustomAttribute<PublicEndpointAttribute>() != null)
                        actual.Add($"{t.Name}.{m.Name}");
                }
            }

            Assert.Equal(AllowedPublicActions.OrderBy(x => x), actual.OrderBy(x => x));
        }

        [Fact]
        public void 관리자_최초설정은_인증면제가_아니다()
        {
            Assert.Null(typeof(AuthController).GetCustomAttribute<PublicEndpointAttribute>());

            MethodInfo? setup = typeof(AuthController).GetMethod(nameof(AuthController.Setup));
            Assert.NotNull(setup);
            Assert.Null(setup!.GetCustomAttribute<PublicEndpointAttribute>());
        }

        [Fact]
        public void 주문_엔드포인트는_인증면제가_아니다()
        {
            Assert.Null(typeof(OrderController).GetCustomAttribute<PublicEndpointAttribute>());

            foreach (MethodInfo m in typeof(OrderController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                Assert.Null(m.GetCustomAttribute<PublicEndpointAttribute>());
            }
        }
    }
}
