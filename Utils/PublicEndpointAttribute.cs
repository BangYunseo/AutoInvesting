using System;

namespace AutoInvest.Utils
{
    /// <summary>
    /// 전역 인증 필터(<see cref="ApiKeyAuthAttribute"/>)를 면제하는 마커 어트리뷰트입니다.
    /// 로그인·상태조회처럼 인증 없이 접근해야 하는 엔드포인트에 부착합니다(닭-달걀 방지).
    ///
    /// 액션 메서드에만 붙일 수 있습니다. 컨트롤러 클래스에 붙이면 그 안의 모든 액션이 한꺼번에
    /// 열리며, 과거 <c>AuthController</c>가 그 상태여서 <c>setup</c>까지 미인증 공개였습니다
    /// (2026-08-04 수정). 같은 실수가 컴파일 단계에서 막히도록 대상을 메서드로 좁혀 둡니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class PublicEndpointAttribute : Attribute
    {
    }
}
