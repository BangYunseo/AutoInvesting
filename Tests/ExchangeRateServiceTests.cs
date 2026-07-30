using System;
using AutoInvest.Utils;
using Xunit;

namespace AutoInvest.Tests
{
    /// <summary>
    /// ExchangeRateService.ParseKrwRate(순수 함수 — 외부 I/O 없음)의 단위 검증.
    /// 두 환율 API(Frankfurter / ExchangeRate-API)가 같은 rates.KRW 형태로 응답하므로
    /// 파서 하나가 양쪽을 처리하는지, 잘못된 응답에서 조용히 0을 돌려주지 않는지 확인합니다.
    /// </summary>
    public class ExchangeRateServiceTests
    {
        /// <summary>Frankfurter 응답 형태에서 KRW 환율을 읽는다.</summary>
        [Fact]
        public void Frankfurter_응답에서_환율을_읽는다()
        {
            string json = """{"amount":1.0,"base":"USD","date":"2026-07-30","rates":{"KRW":1448.1}}""";

            Assert.Equal(1448.1m, ExchangeRateService.ParseKrwRate(json));
        }

        /// <summary>ExchangeRate-API 응답 형태(다른 통화가 섞여 있음)에서도 KRW만 정확히 읽는다.</summary>
        [Fact]
        public void ExchangeRateApi_응답에서_환율을_읽는다()
        {
            string json = """{"result":"success","rates":{"JPY":157.2,"KRW":1447.35,"EUR":0.92}}""";

            Assert.Equal(1447.35m, ExchangeRateService.ParseKrwRate(json));
        }

        /// <summary>KRW 필드가 없으면 예외로 알려 호출부가 다음 소스로 넘어가게 한다.</summary>
        [Fact]
        public void KRW_없으면_예외를_던진다()
        {
            string json = """{"result":"success","rates":{"JPY":157.2}}""";

            Assert.Throws<InvalidOperationException>(() => ExchangeRateService.ParseKrwRate(json));
        }

        /// <summary>0 이하 환율은 조용히 통과시키지 않는다 (0으로 나눈 매수금액 산출 방지).</summary>
        [Fact]
        public void 환율이_0이하면_예외를_던진다()
        {
            string json = """{"rates":{"KRW":0}}""";

            Assert.Throws<InvalidOperationException>(() => ExchangeRateService.ParseKrwRate(json));
        }
    }
}
