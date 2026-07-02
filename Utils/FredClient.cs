using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AutoInvest.Data;
using AutoInvest.Data.DTO;

namespace AutoInvest.Utils
{
    /// <summary>
    /// FRED(미 세인트루이스 연은) 공식 API에서 거시경제 수치를 조회합니다.
    /// 물가(CPI·근원PCE)·유가(WTI)·금리(10년 국채)·고용(실업률)을 다룹니다.
    ///
    /// 지수형(CPI·PCE)은 '지수값'으로 오므로 전년 동월 대비 상승률(YoY)을 직접 계산하고,
    /// 값형(유가·금리·실업률)은 직전 관측치 대비 등락을 계산합니다.
    ///
    /// ⚠️ 표시/보고 전용 데이터 소스입니다 — 매수 판단에 사용하지 않습니다.
    /// FRED_API_KEY는 소스에 두지 않고 환경변수/DB에서 읽습니다(security.md).
    /// </summary>
    public static class FredClient
    {
        private const string BaseUrl = "https://api.stlouisfed.org/fred/series/observations";

        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        // ── 지표 카탈로그 — 추가하려면 여기에 한 줄 더하면 카드·해설에 자동 반영 ──
        private static readonly Dictionary<string, (string SeriesId, string Label, string Kind, string Unit)> Catalog =
            new()
            {
                ["CPI"]     = ("CPIAUCSL",   "소비자물가지수(CPI)",   "index", "%"),
                ["CorePCE"] = ("PCEPILFE",   "근원 PCE 물가지수",     "index", "%"),
                ["WTI"]     = ("DCOILWTICO", "WTI 국제유가",          "level", "$"),
                ["RATE10Y"] = ("DGS10",      "미국 10년 국채금리",     "level", "%"),
                ["UNEMP"]   = ("UNRATE",     "미국 실업률",           "level", "%"),
            };

        /// <summary>화면·해설에서 사용하는 지표 표시 순서.</summary>
        public static readonly string[] DisplayOrder = { "CPI", "CorePCE", "WTI", "RATE10Y", "UNEMP" };

        // ── 조회 결과 캐시 (FRED는 자주 안 바뀌므로 1시간 캐싱 — recommended_rules 성능 규칙) ──
        private static Dictionary<string, MacroIndicatorDto>? _cache;
        private static DateTime _cacheTime = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

        /// <summary>
        /// 카탈로그의 모든 지표를 조회합니다 (1시간 캐싱).
        /// 개별 지표가 실패해도 예외를 던지지 않고 <see cref="MacroIndicatorDto.Error"/>에 담아 반환합니다.
        /// </summary>
        public static async Task<Dictionary<string, MacroIndicatorDto>> GetAllAsync()
        {
            if (_cache != null && DateTime.Now - _cacheTime < CacheDuration)
                return _cache;

            string apiKey = AppConfigManager.Get("FRED_API_KEY", "");

            var result = new Dictionary<string, MacroIndicatorDto>();
            foreach (var key in DisplayOrder)
                result[key] = await GetIndicatorAsync(key, apiKey);

            // 하나라도 성공했을 때만 캐싱 (전부 실패는 일시적 장애일 수 있어 캐싱하지 않음)
            if (result.Values.Any(v => v.Error == null))
            {
                _cache = result;
                _cacheTime = DateTime.Now;
            }
            return result;
        }

        /// <summary>
        /// 지표 하나를 조회해 <see cref="MacroIndicatorDto"/>로 돌려줍니다. 실패해도 예외를 던지지 않습니다.
        /// </summary>
        private static async Task<MacroIndicatorDto> GetIndicatorAsync(string key, string apiKey)
        {
            if (!Catalog.TryGetValue(key, out var meta))
                return new MacroIndicatorDto { Key = key, Label = key, Error = "알 수 없는 지표 키" };

            var dto = new MacroIndicatorDto { Key = key, Label = meta.Label, Unit = meta.Unit };

            if (string.IsNullOrEmpty(apiKey))
            {
                dto.Error = "FRED API 키가 설정되지 않았습니다.";
                return dto;
            }

            try
            {
                var obs = await FetchObservationsAsync(meta.SeriesId, apiKey);
                if (obs.Count == 0)
                {
                    dto.Error = "데이터 없음";
                    return dto;
                }

                dto.LatestValue = obs[0].Value;
                dto.LatestDate = obs[0].Date;

                if (meta.Kind == "index")
                {
                    // 지수형: 최신 YoY와 직전 YoY로 가속(▲)/둔화(▼) 판정
                    dto.YoyPercent = YoyFor(obs, 0);
                    decimal? prevYoy = YoyFor(obs, 1);
                    if (dto.YoyPercent.HasValue && prevYoy.HasValue)
                    {
                        dto.YoyDelta = Math.Round(dto.YoyPercent.Value - prevYoy.Value, 2);
                        dto.Direction = DirectionOf(dto.YoyDelta);
                    }
                }
                else
                {
                    // 값형: 직전 관측치 대비 등락 금액·비율·방향
                    if (obs.Count > 1)
                    {
                        decimal prev = obs[1].Value;
                        dto.PrevValue = prev;
                        dto.ChangeAbs = Math.Round(dto.LatestValue.Value - prev, 2);
                        if (prev != 0)
                            dto.ChangePct = Math.Round((dto.LatestValue.Value - prev) / prev * 100, 2);
                        dto.Direction = DirectionOf(dto.ChangeAbs);
                    }
                }

                return dto;
            }
            catch (Exception ex)
            {
                Logger.Warn($"[FRED] '{key}'({meta.SeriesId}) 조회 실패: {ex.Message}");
                dto.Error = $"{ex.GetType().Name}: {ex.Message}";
                return dto;
            }
        }

        /// <summary>
        /// 특정 시리즈의 관측치를 최신순으로 조회합니다. 결측치(".")는 제거합니다.
        /// </summary>
        private static async Task<List<(string Date, decimal Value)>> FetchObservationsAsync(string seriesId, string apiKey)
        {
            string url = $"{BaseUrl}?series_id={Uri.EscapeDataString(seriesId)}" +
                         $"&api_key={Uri.EscapeDataString(apiKey)}&file_type=json&sort_order=desc&limit=400";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "AutoInvesting/1.0");

            using var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();

            var list = new List<(string, decimal)>();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("observations", out var observations))
            {
                foreach (var obs in observations.EnumerateArray())
                {
                    string? date = obs.TryGetProperty("date", out var d) ? d.GetString() : null;
                    string? valueStr = obs.TryGetProperty("value", out var v) ? v.GetString() : null;
                    if (string.IsNullOrEmpty(date) || string.IsNullOrEmpty(valueStr) || valueStr == ".")
                        continue;
                    if (decimal.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                        list.Add((date, value));
                }
            }
            return list;
        }

        /// <summary>
        /// observations[anchorIdx]를 기준으로 전년 동월 대비(YoY) 상승률(%)을 계산합니다.
        /// observations는 최신순(desc) 정렬 상태를 가정합니다.
        /// </summary>
        private static decimal? YoyFor(List<(string Date, decimal Value)> obs, int anchorIdx)
        {
            if (anchorIdx >= obs.Count)
                return null;

            var anchor = obs[anchorIdx];
            if (anchor.Date.Length < 7)
                return null;

            int anchorYear = int.Parse(anchor.Date.Substring(0, 4));
            string anchorMonth = anchor.Date.Substring(5, 2);
            string targetYear = (anchorYear - 1).ToString();

            (string Date, decimal Value)? yearAgo = null;
            for (int i = anchorIdx + 1; i < obs.Count; i++)
            {
                if (obs[i].Date.Length >= 7
                    && obs[i].Date.Substring(0, 4) == targetYear
                    && obs[i].Date.Substring(5, 2) == anchorMonth)
                {
                    yearAgo = obs[i];
                    break;
                }
            }
            // 같은 달이 없으면 12칸 뒤(월간 기준 약 1년 전)로 근사
            if (yearAgo == null && obs.Count > anchorIdx + 12)
                yearAgo = obs[anchorIdx + 12];
            if (yearAgo == null || yearAgo.Value.Value == 0)
                return null;

            return Math.Round((anchor.Value - yearAgo.Value.Value) / yearAgo.Value.Value * 100, 2);
        }

        /// <summary>변화량의 부호로 상승/하락/보합을 정합니다.</summary>
        private static string DirectionOf(decimal? delta)
        {
            if (delta == null) return "flat";
            if (delta > 0) return "up";
            if (delta < 0) return "down";
            return "flat";
        }
    }
}
