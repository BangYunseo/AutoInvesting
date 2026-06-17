using AutoInvest.Data;
using AutoInvest.Data.DAO;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoInvest.Core
{
    /// <summary>
    /// Google Gemini API를 사용하는 다중 에이전트 AI 시장 분석 엔진 구현체 (Phase 4-d).
    ///
    /// 투자 위원회(Investment Committee) 구조:
    ///   1. 차트 기술 애널리스트 에이전트 — RSI·MACD·BB·OHLCV 기반 기술적 판단
    ///   2. 거시경제·펀더멘털 애널리스트 에이전트 — 섹터·금리·달러 환경 기반 거시 판단
    ///
    /// 두 에이전트를 Task.WhenAll로 병렬 실행하여 레이턴시를 최소화합니다.
    /// 각 에이전트의 결과를 MultiAgentAnalysisResult에 분리 보관하여 SmartOrderEngine이
    /// 퀀트 신호와 함께 3자 만장일치 합의를 수행하도록 합니다.
    /// </summary>
    public class GeminiMarketAnalyzer : IMarketAnalyzer
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(45)
        };

        private readonly string _apiKey;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

        // 사용할 Gemini 모델명은 GEMINI_MODEL 설정값으로 관리합니다.
        // (모델이 폐기되면 코드 수정 없이 환경변수만 교체하면 됩니다.)
        // 사용 가능한 모델은 ListModels API로 확인: GET /v1beta/models?key=...
        private const string DefaultModel = "gemini-2.0-flash";
        private readonly string _modelEndpoint;

        /// <param name="apiKey">Gemini API 키 (AI Studio에서 발급)</param>
        public GeminiMarketAnalyzer(string apiKey)
        {
            _apiKey = apiKey;

            // ── 모델명 로드 (설정 없으면 기본 모델 사용) ──
            string model = AppConfigManager.Get("GEMINI_MODEL", DefaultModel);
            _modelEndpoint =
                $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
            Logger.Info($"[GeminiAI] 사용 모델: {model}");

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
        /// 차트 + 펀더멘털 두 관점을 단일 Gemini 호출로 동시에 분석하여 MultiAgentAnalysisResult를 반환합니다.
        /// (무료 티어 호출 한도(429) 절감을 위해 종목당 호출을 2회 → 1회로 통합)
        /// 호출/파싱 실패 시 두 에이전트 모두 HOLD fallback으로 대체됩니다.
        /// </summary>
        /// <param name="ticker">종목 코드</param>
        /// <param name="indicators">퀀트 지표 (RSI, MACD 등)</param>
        /// <param name="ohlcv">최근 OHLCV 데이터 (차트 분석 입력용)</param>
        public async Task<MultiAgentAnalysisResult> AnalyzeAsync(
            string ticker,
            IndicatorDto indicators,
            List<OhlcvDto>? ohlcv = null)
        {
            Logger.Info($"[GeminiAI] {ticker} — 통합 분석 시작 (차트 + 펀더멘털 단일 호출)");

            // ── 투자 철학 로드 ──
            string philosophy = AppConfigManager.Get("AI_PHILOSOPHY", "");

            // ── 통합 프롬프트 조립 (두 관점을 한 번에) ──
            string systemPrompt = PromptBuilder.BuildCombinedSystemPrompt(philosophy);
            string userPrompt   = PromptBuilder.BuildCombinedUserPrompt(ticker, indicators, ohlcv);

            var (chartResult, fundamentalResult) = await CallGeminiCombinedAsync(systemPrompt, userPrompt, ticker);

            // ── 두 의견 모두 정상 응답했는지 판단 ──
            bool isFull = chartResult.ConfidenceScore > 0m && fundamentalResult.ConfidenceScore > 0m;

            Logger.Info($"[GeminiAI] {ticker} — 의견 수집 완료 | " +
                $"차트: {chartResult.Signal}({chartResult.ConfidenceScore:F2}) | " +
                $"펀더멘털: {fundamentalResult.Signal}({fundamentalResult.ConfidenceScore:F2}) | " +
                $"전체응답: {isFull}");

            return new MultiAgentAnalysisResult
            {
                ChartAgent       = chartResult,
                FundamentalAgent = fundamentalResult,
                IsFullConsensus  = isFull
            };
        }

        /// <summary>
        /// 통합 Gemini API 호출을 1회 수행하여 차트/펀더멘털 두 의견을 함께 받아옵니다.
        /// 실패 시 두 의견 모두 HOLD fallback을 반환하여 합의 흐름을 보호합니다.
        /// </summary>
        /// <param name="systemPrompt">두 분석가 역할을 정의한 통합 System Prompt</param>
        /// <param name="userPrompt">공통 종목 데이터 User Prompt</param>
        /// <param name="ticker">종목 코드 (로그용)</param>
        private async Task<(AiAnalysisResult Chart, AiAnalysisResult Fundamental)> CallGeminiCombinedAsync(
            string systemPrompt,
            string userPrompt,
            string ticker)
        {
            try
            {
                // ── Gemini API 요청 Body (두 의견을 담기 위해 출력 토큰 상향) ──
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
                        // 통합 응답(chart+fundamental 두 의견 + 한국어 reason)이 잘리지 않도록 충분히 확보.
                        // 2.5 계열 thinking 모델은 사고 토큰까지 차감되므로 여유를 더 둔다.
                        maxOutputTokens = 1024
                    }
                };

                string jsonBody = JsonSerializer.Serialize(requestBody);

                // ── HTTP 호출 (Polly 재시도 포함) ──
                var response = await _retryPolicy.ExecuteAsync(() =>
                    _httpClient.SendAsync(new HttpRequestMessage(
                        HttpMethod.Post, $"{_modelEndpoint}?key={_apiKey}")
                    {
                        Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
                    }));

                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Warn($"[GeminiAI] {ticker} API 오류 HTTP {(int)response.StatusCode}: " +
                        $"{responseBody[..Math.Min(200, responseBody.Length)]}");
                    return (BuildFallback("차트"), BuildFallback("펀더멘털"));
                }

                return ParseCombinedResponse(responseBody, ticker);
            }
            catch (Exception ex)
            {
                Logger.Warn($"[GeminiAI] {ticker} 통합 호출 실패 — HOLD fallback: {ex.Message}");
                return (BuildFallback("차트"), BuildFallback("펀더멘털"));
            }
        }

        /// <summary>
        /// 통합 Gemini 응답 JSON(chart/fundamental 두 객체)을 파싱합니다.
        /// </summary>
        private (AiAnalysisResult Chart, AiAnalysisResult Fundamental) ParseCombinedResponse(string responseBody, string ticker)
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

                // ── Token Usage 파싱 및 DB 기록 (단일 호출이므로 1건 합산 기록) ──
                if (doc.RootElement.TryGetProperty("usageMetadata", out var usageProp))
                {
                    int promptTokens = usageProp.TryGetProperty("promptTokenCount", out var p) && p.ValueKind == JsonValueKind.Number ? p.GetInt32() : 0;
                    int compTokens = usageProp.TryGetProperty("candidatesTokenCount", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetInt32() : 0;
                    int totalTokens = usageProp.TryGetProperty("totalTokenCount", out var t) && t.ValueKind == JsonValueKind.Number ? t.GetInt32() : 0;

                    TokenUsageDAO.Insert(new TokenUsageDto
                    {
                        Ticker = ticker,
                        AgentType = "COMBINED_AI",
                        PromptTokens = promptTokens,
                        CompletionTokens = compTokens,
                        TotalTokens = totalTokens
                    });
                }

                // ── JSON 안전 파싱 (마크다운 코드 블록 제거) ──
                string cleanJson = ExtractJson(rawText);
                using var resultDoc = JsonDocument.Parse(cleanJson);
                var root = resultDoc.RootElement;

                var chart       = ParseAgentObject(root, "chart", ticker, "차트");
                var fundamental = ParseAgentObject(root, "fundamental", ticker, "펀더멘털");
                return (chart, fundamental);
            }
            catch (Exception ex)
            {
                Logger.Warn($"[GeminiAI] {ticker} 통합 응답 파싱 실패: {ex.Message}. " +
                    $"원문: {responseBody[..Math.Min(300, responseBody.Length)]}");
                _ = NotificationService.SendEmailAsync(
                    "AI 통합 응답 파싱 실패 (HOLD Fallback)",
                    $"종목: {ticker}\n\n원문:\n{responseBody}");
                return (BuildFallback("차트"), BuildFallback("펀더멘털"));
            }
        }

        /// <summary>
        /// 통합 응답 JSON에서 지정 키("chart"/"fundamental")의 의견 객체를 AiAnalysisResult로 파싱합니다.
        /// 키가 없거나 필드가 비면 HOLD fallback을 반환합니다.
        /// </summary>
        private static AiAnalysisResult ParseAgentObject(JsonElement root, string key, string ticker, string agentLabel)
        {
            if (!root.TryGetProperty(key, out var obj) || obj.ValueKind != JsonValueKind.Object)
            {
                Logger.Warn($"[GeminiAI-{agentLabel}] {ticker} 응답에 '{key}' 항목 누락 — HOLD fallback");
                return BuildFallback(agentLabel);
            }

            string signalStr = obj.TryGetProperty("signal", out var s) ? (s.GetString() ?? "HOLD") : "HOLD";
            decimal confidence = obj.TryGetProperty("confidence", out var c) && c.ValueKind == JsonValueKind.Number
                ? c.GetDecimal() : 0m;
            string reason = obj.TryGetProperty("reason", out var r) ? (r.GetString() ?? string.Empty) : string.Empty;

            SmartOrderSignal signal = signalStr.ToUpper() switch
            {
                "BUY"  => SmartOrderSignal.BUY,
                "SELL" => SmartOrderSignal.SELL,
                _      => SmartOrderSignal.HOLD
            };

            Logger.Info($"[GeminiAI-{agentLabel}] {ticker} → {signal} (확신도: {confidence:F2}) | {reason}");

            return new AiAnalysisResult
            {
                Signal          = signal,
                ConfidenceScore = confidence,
                Reason          = reason
            };
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
                int end   = text.LastIndexOf('}');
                if (start >= 0 && end > start)
                    return text[start..(end + 1)];
            }
            return text;
        }

        /// <summary>
        /// API 실패 시 반환하는 안전한 기본값 (HOLD, 확신도 0).
        /// CombineSignals()에서 자동으로 만장일치 불성립 → 퀀트 신호를 보호합니다.
        /// </summary>
        private static AiAnalysisResult BuildFallback(string agentLabel) => new AiAnalysisResult
        {
            Signal          = SmartOrderSignal.HOLD,
            ConfidenceScore = 0m,
            Reason          = $"[{agentLabel} 에이전트] AI 응답 불가 — 만장일치 불성립으로 퀀트 신호 보호"
        };
    }
}
