using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AutoInvest.Utils
{
    /// <summary>
    /// 무료 환율 API를 통해 USD/KRW 환율을 조회합니다.
    /// 기본: Frankfurter API (ECB 데이터, API 키 불필요)
    /// 대안: ExchangeRate-API (Open Access)
    /// </summary>
    public static class ExchangeRateService
    {
        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        private static decimal _cachedRate;
        private static DateTime _cacheTime = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

        /// <summary>
        /// USD/KRW 환율을 조회합니다 (1시간 캐싱).
        /// </summary>
        public static async Task<decimal> GetUsdKrwAsync()
        {
            // 캐시 유효 시 캐시 반환
            if (_cachedRate > 0 && DateTime.Now - _cacheTime < CacheDuration)
                return _cachedRate;

            // 1차: Frankfurter API
            try
            {
                var rate = await FetchFromFrankfurterAsync();
                _cachedRate = rate;
                _cacheTime = DateTime.Now;
                Logger.Info($"[ExchangeRate] Frankfurter API 조회 성공: 1 USD = {rate:N1} KRW");
                return rate;
            }
            catch (Exception ex)
            {
                Logger.Warn($"[ExchangeRate] Frankfurter API 실패: {ex.Message}");
            }

            // 2차: ExchangeRate-API (fallback)
            try
            {
                var rate = await FetchFromExchangeRateApiAsync();
                _cachedRate = rate;
                _cacheTime = DateTime.Now;
                Logger.Info($"[ExchangeRate] ExchangeRate-API 조회 성공: 1 USD = {rate:N1} KRW");
                return rate;
            }
            catch (Exception ex)
            {
                Logger.Warn($"[ExchangeRate] ExchangeRate-API 실패: {ex.Message}");
            }

            // 모두 실패 시 캐시 또는 기본값 반환
            if (_cachedRate > 0)
                return _cachedRate;

            Logger.Warn("[ExchangeRate] 모든 API 실패 — 기본값 1,350원 사용");
            return 1350m;
        }

        /// <summary>
        /// Frankfurter API: https://api.frankfurter.app/latest?from=USD&to=KRW
        /// 응답: {"amount":1.0,"base":"USD","date":"...","rates":{"KRW":1448.1}}
        /// </summary>
        private static async Task<decimal> FetchFromFrankfurterAsync()
        {
            var json = await _http.GetStringAsync(
                "https://api.frankfurter.app/latest?from=USD&to=KRW");

            // 간단한 JSON 파싱 (외부 라이브러리 없이)
            var krwKey = "\"KRW\":";
            int idx = json.IndexOf(krwKey, StringComparison.Ordinal);
            if (idx < 0) throw new Exception("KRW 필드를 찾을 수 없습니다.");

            idx += krwKey.Length;
            int endIdx = json.IndexOfAny(new[] { '}', ',' }, idx);
            var valueStr = json.Substring(idx, endIdx - idx).Trim();

            if (decimal.TryParse(valueStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var rate))
                return rate;

            throw new Exception($"KRW 값 파싱 실패: {valueStr}");
        }

        /// <summary>
        /// ExchangeRate-API: https://open.er-api.com/v6/latest/USD
        /// 응답: {"result":"success","rates":{"KRW":1447.35,...}}
        /// </summary>
        private static async Task<decimal> FetchFromExchangeRateApiAsync()
        {
            var json = await _http.GetStringAsync(
                "https://open.er-api.com/v6/latest/USD");

            var krwKey = "\"KRW\":";
            int idx = json.IndexOf(krwKey, StringComparison.Ordinal);
            if (idx < 0) throw new Exception("KRW 필드를 찾을 수 없습니다.");

            idx += krwKey.Length;
            int endIdx = json.IndexOfAny(new[] { '}', ',' }, idx);
            var valueStr = json.Substring(idx, endIdx - idx).Trim();

            if (decimal.TryParse(valueStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var rate))
                return rate;

            throw new Exception($"KRW 값 파싱 실패: {valueStr}");
        }
    }
}
