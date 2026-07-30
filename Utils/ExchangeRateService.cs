using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoInvest.Utils
{
    /// <summary>
    /// 무료 환율 API를 통해 USD/KRW 환율을 조회합니다.
    /// 기본: Frankfurter API (ECB 데이터, API 키 불필요)
    /// 대안: ExchangeRate-API (Open Access)
    ///
    /// 두 API 모두 <c>{"rates":{"KRW":1448.1,...}}</c> 형태로 응답하므로 파서(<see cref="ParseKrwRate"/>)를
    /// 하나만 두고 URL만 바꿔 순차 시도합니다.
    /// </summary>
    public static class ExchangeRateService
    {
        /// <summary>조회 순서대로의 (표시 이름, 엔드포인트). 앞이 실패하면 다음으로 넘어갑니다.</summary>
        private static readonly (string Name, string Url)[] Sources =
        {
            ("Frankfurter API", "https://api.frankfurter.app/latest?from=USD&to=KRW"),
            ("ExchangeRate-API", "https://open.er-api.com/v6/latest/USD"),
        };

        /// <summary>모든 API와 캐시가 모두 실패했을 때 사용할 최후 기본값 (원).</summary>
        private const decimal FallbackRate = 1350m;

        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        private static decimal _cachedRate;
        private static DateTime _cacheTime = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

        /// <summary>
        /// USD/KRW 환율을 조회합니다 (1시간 캐싱).
        /// 모든 소스가 실패하면 만료된 캐시라도 우선 쓰고, 그것도 없으면 기본값을 반환합니다.
        /// </summary>
        public static async Task<decimal> GetUsdKrwAsync()
        {
            // 캐시 유효 시 캐시 반환
            if (_cachedRate > 0 && DateTime.Now - _cacheTime < CacheDuration)
                return _cachedRate;

            foreach (var (name, url) in Sources)
            {
                try
                {
                    decimal rate = ParseKrwRate(await _http.GetStringAsync(url));
                    _cachedRate = rate;
                    _cacheTime = DateTime.Now;
                    Logger.Info($"[ExchangeRate] {name} 조회 성공: 1 USD = {rate:N1} KRW");
                    return rate;
                }
                catch (Exception ex)
                {
                    Logger.Warn($"[ExchangeRate] {name} 실패: {ex.Message}");
                }
            }

            // 모두 실패 시 만료 캐시 → 기본값
            if (_cachedRate > 0)
            {
                Logger.Warn($"[ExchangeRate] 모든 API 실패 — 만료된 캐시값 {_cachedRate:N1}원 사용");
                return _cachedRate;
            }

            Logger.Warn($"[ExchangeRate] 모든 API 실패 — 기본값 {FallbackRate:N0}원 사용");
            return FallbackRate;
        }

        /// <summary>
        /// 응답 JSON에서 <c>rates.KRW</c>를 읽어 환율을 반환합니다 (순수 함수 — 외부 I/O 없음, 검증 대상).
        /// 필드가 없거나 값이 0 이하면 예외를 던져, 호출부가 잘못된 환율을 쓰지 않고 다음 소스로 넘어가게 합니다.
        /// </summary>
        /// <param name="json">환율 API 응답 본문</param>
        /// <exception cref="InvalidOperationException">rates.KRW가 없거나 유효한 양수가 아닐 때</exception>
        public static decimal ParseKrwRate(string json)
        {
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("rates", out var rates)
                || !rates.TryGetProperty("KRW", out var krw)
                || !krw.TryGetDecimal(out decimal rate))
            {
                throw new InvalidOperationException("응답에서 rates.KRW를 읽을 수 없습니다.");
            }

            if (rate <= 0)
            {
                throw new InvalidOperationException($"환율이 유효한 양수가 아닙니다: {rate}");
            }

            return rate;
        }
    }
}
