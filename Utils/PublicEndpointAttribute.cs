using System;

namespace AutoInvest.Utils
{
    /// <summary>
    /// 전역 인증 필터(<see cref="ApiKeyAuthAttribute"/>)를 면제하는 마커 어트리뷰트입니다.
    /// 로그인·최초설정·상태조회처럼 인증 없이 접근해야 하는 엔드포인트에 부착합니다(닭-달걀 방지).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class PublicEndpointAttribute : Attribute
    {
    }
}
