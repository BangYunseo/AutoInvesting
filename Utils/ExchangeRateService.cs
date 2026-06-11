using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
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

        // ── 환율 시계열(백분위 분석용) 캐시 ──
        private static List<decimal>? _seriesCache;
        private static int _seriesCacheDays;
        private static DateTime _seriesCacheTime = DateTime.MinValue;

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
                Logger.Info($"[환율] Frankfurter API 조회 성공: 1 USD = {rate:N1} KRW");
                return rate;
            }
            catch (Exception ex)
            {
                Logger.Warn($"[환율] Frankfurter API 실패: {ex.Message}");
            }

            // 2차: ExchangeRate-API (fallback)
            try
            {
                var rate = await FetchFromExchangeRateApiAsync();
                _cachedRate = rate;
                _cacheTime = DateTime.Now;
                Logger.Info($"[환율] ExchangeRate-API 조회 성공: 1 USD = {rate:N1} KRW");
                return rate;
            }
            catch (Exception ex)
            {
                Logger.Warn($"[환율] ExchangeRate-API 실패: {ex.Message}");
            }

            // 모두 실패 시 캐시 또는 기본값 반환
            if (_cachedRate > 0)
                return _cachedRate;

            Logger.Warn("[환율] 모든 API 실패 — 기본값 1,350원 사용");
            return 1350m;
        }

        /// <summary>
        /// 최근 <paramref name="lookbackDays"/>일간 USD/KRW 분포 대비 현재 환율의 상대 위치를 계산합니다 (Phase 5-e).
        /// Frankfurter 시계열 API를 1회 호출해 백분위 순위를 산출하며, 결과 시계열은 1시간 캐싱합니다.
        /// 표본이 부족하거나 조회 실패 시 <c>IsHigh=false</c>로 안전하게 반환합니다.
        /// </summary>
        /// <param name="lookbackDays">분포 산출 기간(일)</param>
        /// <param name="highPercentile">고환율 판정 분위수 (예: 0.80 = 상위 20%)</param>
        /// <returns>(현재환율, 백분위순위 0~1, 상위경계값, 고환율여부, 표본수)</returns>
        public static async Task<(decimal Current, decimal PercentileRank, decimal HighThreshold, bool IsHigh, int SampleCount)>
            GetUsdKrwContextAsync(int lookbackDays = 60, decimal highPercentile = 0.80m)
        {
            decimal current = await GetUsdKrwAsync();

            try
            {
                List<decimal> series;
                if (_seriesCache != null && _seriesCacheDays == lookbackDays
                    && DateTime.Now - _seriesCacheTime < CacheDuration)
                {
                    series = _seriesCache;
                }
                else
                {
                    series = await FetchUsdKrwSeriesAsync(lookbackDays);
                    if (series.Count >= 5)
                    {
                        series.Sort();
                        _seriesCache = series;
                        _seriesCacheDays = lookbackDays;
                        _seriesCacheTime = DateTime.Now;
                    }
                }

                if (series.Count >= 5)
                {
                    int leCount = series.Count(v => v <= current);
                    decimal rank = (decimal)leCount / series.Count;
                    decimal highThreshold = Percentile(series, highPercentile);
                    bool isHigh = current >= highThreshold;

                    Logger.Info($"[환율] 분포 분석 — 현재 {current:N1}, 백분위 {rank:P0}, " +
                                $"상위{highPercentile:P0}경계 {highThreshold:N1}, 고환율={isHigh} (표본 {series.Count})");
                    return (current, rank, highThreshold, isHigh, series.Count);
                }

                Logger.Warn($"[환율] 시계열 표본 부족({series.Count}) — 고환율 판정 생략");
            }
            catch (Exception ex)
            {
                Logger.Warn($"[환율] 시계열 분석 실패: {ex.Message}");
            }

            return (current, 0m, 0m, false, 0);
        }

        /// <summary>
        /// Frankfurter 시계열 API에서 최근 N일간 USD/KRW 일별 환율 목록을 조회합니다.
        /// 예: https://api.frankfurter.app/2024-04-01..2024-06-10?from=USD&to=KRW
        /// </summary>
        private static async Task<List<decimal>> FetchUsdKrwSeriesAsync(int lookbackDays)
        {
            var end = DateTime.Today;
            var start = end.AddDays(-Math.Max(7, lookbackDays));
            string url = $"https://api.frankfurter.app/{start:yyyy-MM-dd}..{end:yyyy-MM-dd}?from=USD&to=KRW";

            var json = await _http.GetStringAsync(url);
            var values = new List<decimal>();

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("rates", out var rates))
            {
                foreach (var day in rates.EnumerateObject())
                {
                    if (day.Value.TryGetProperty("KRW", out var krw) && krw.TryGetDecimal(out var v))
                        values.Add(v);
                }
            }
            return values;
        }

        /// <summary>
        /// 정렬된 목록에서 선형 보간 방식으로 분위수 값을 계산합니다 (p: 0.0~1.0).
        /// </summary>
        private static decimal Percentile(List<decimal> sorted, decimal p)
        {
            if (sorted.Count == 0) return 0m;
            if (sorted.Count == 1) return sorted[0];

            decimal idx = p * (sorted.Count - 1);
            int lo = (int)Math.Floor(idx);
            int hi = (int)Math.Ceiling(idx);
            if (lo == hi) return sorted[lo];

            decimal frac = idx - lo;
            return sorted[lo] + (sorted[hi] - sorted[lo]) * frac;
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
