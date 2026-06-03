using AutoInvest.Data;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using Polly;
using Polly.Retry;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoInvest.Core
{
    /// <summary>
    /// Google Gemini API를 사용하는 실물 AI 시장 분석 엔진 구현체 (Phase 4).
    /// 실패 시 Polly 재시도 + HOLD fallback으로 기존 퀀트 신호를 보호합니다.
    /// </summary>
    public class GeminiMarketAnalyzer : IMarketAnalyzer
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        private readonly string _apiKey;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

        // Gemini 1.5 Flash — 무료 티어: 분당 15회, 일 1,500회
        private const string ModelEndpoint =
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";

        /// <param name="apiKey">Gemini API 키 (AI Studio에서 발급)</param>
        public GeminiMarketAnalyzer(string apiKey)
        {
            _apiKey = apiKey;

            // ── Polly 재시도: 429/5xx 발생 시 최대 3회, 지수 백오프 ──
            _retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(r =>
                    (int)r.StatusCode == 429 || (int)r.StatusCode >= 500)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (result, delay, attempt, _) =>
                        Logger.Warn($"[GeminiAI] HTTP {(int)result.Result.StatusCode} 응답, {delay.TotalSeconds}초 후 재시도 ({attempt}회차)")
                );
        }

        /// <summary>
        /// 종목 지표를 Gemini에 전달하여 BUY/SELL/HOLD + 확신도를 반환합니다.
        /// API 호출 실패 시 HOLD fallback을 반환하여 퀀트 신호를 보호합니다.
        /// </summary>
        public async Task<AiAnalysisResult> AnalyzeAsync(string ticker, IndicatorDto indicators)
        {
            Logger.Info($"[GeminiAI] {ticker} 분석 요청...");

            try
            {
                // ── 프롬프트 조립 ──
                string systemPrompt = PromptBuilder.BuildSystemPrompt();
                string userPrompt = PromptBuilder.BuildUserPrompt(ticker, indicators);

                // ── Gemini API 요청 Body ──
                var requestBody = new
                {
                    system_instruction = new
                    {
                        parts = new[] { new { text = systemPrompt } }
                    },
                    contents = new[]
                    {
                        new { parts = new[] { new { text = userPrompt } } }
                    },
                    generationConfig = new
                    {
                        temperature = 0.3,
                        maxOutputTokens = 256
                    }
                };

                string jsonBody = JsonSerializer.Serialize(requestBody);
                var request = new HttpRequestMessage(HttpMethod.Post, $"{ModelEndpoint}?key={_apiKey}")
                {
                    Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
                };
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // ── HTTP 호출 (Polly 재시도 포함) ──
                var response = await _retryPolicy.ExecuteAsync(() =>
                    _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"{ModelEndpoint}?key={_apiKey}")
                    {
                        Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
                    }));

                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Warn($"[GeminiAI] API 오류 HTTP {(int)response.StatusCode}: {responseBody[..Math.Min(200, responseBody.Length)]}");
                    return BuildFallback();
                }

                return ParseResponse(responseBody, ticker);
            }
            catch (Exception ex)
            {
                Logger.Warn($"[GeminiAI] {ticker} 분석 실패 — 퀀트 신호 우선 사용: {ex.Message}");
                return BuildFallback();
            }
        }

        /// <summary>
        /// Gemini 응답 JSON을 파싱하여 AiAnalysisResult로 변환합니다.
        /// </summary>
        private AiAnalysisResult ParseResponse(string responseBody, string ticker)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseBody);

                // Gemini 응답 구조: candidates[0].content.parts[0].text
                string rawText = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? string.Empty;

                // ── JSON 안전 파싱 (마크다운 코드 블록 제거) ──
                string cleanJson = ExtractJson(rawText);
                using var resultDoc = JsonDocument.Parse(cleanJson);
                var root = resultDoc.RootElement;

                string signalStr = root.GetProperty("signal").GetString() ?? "HOLD";
                decimal confidence = root.GetProperty("confidence").GetDecimal();
                string reason = root.GetProperty("reason").GetString() ?? string.Empty;

                SmartOrderSignal signal = signalStr.ToUpper() switch
                {
                    "BUY"  => SmartOrderSignal.BUY,
                    "SELL" => SmartOrderSignal.SELL,
                    _      => SmartOrderSignal.HOLD
                };

                Logger.Info($"[GeminiAI] {ticker} → {signal} (확신도: {confidence:F2}) | {reason}");

                return new AiAnalysisResult
                {
                    Signal = signal,
                    ConfidenceScore = confidence,
                    Reason = reason
                };
            }
            catch (Exception ex)
            {
                Logger.Warn($"[GeminiAI] 응답 파싱 실패: {ex.Message}. 원문: {responseBody[..Math.Min(300, responseBody.Length)]}");
                return BuildFallback();
            }
        }

        /// <summary>
        /// 마크다운 코드 블록(```json ... ```)을 제거하고 순수 JSON만 추출합니다.
        /// </summary>
        private static string ExtractJson(string text)
        {
            text = text.Trim();
            if (text.StartsWith("```"))
            {
                int start = text.IndexOf('{');
                int end = text.LastIndexOf('}');
                if (start >= 0 && end > start)
                    return text[start..(end + 1)];
            }
            return text;
        }

        /// <summary>
        /// API 실패 시 반환하는 안전한 기본값 (HOLD, 확신도 0).
        /// CombineSignals()에서 자동으로 퀀트 신호를 우선 사용하게 됩니다.
        /// </summary>
        private static AiAnalysisResult BuildFallback() => new AiAnalysisResult
        {
            Signal = SmartOrderSignal.HOLD,
            ConfidenceScore = 0m,
            Reason = "AI 분석 엔진 응답 불가 — 퀀트 지표 판단으로 대체"
        };
    }
}
