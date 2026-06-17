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
        /// 차트 에이전트와 펀더멘털 에이전트를 병렬 실행하여 MultiAgentAnalysisResult를 반환합니다.
        /// 하나 이상의 에이전트가 실패하면 해당 에이전트는 HOLD fallback으로 대체됩니다.
        /// </summary>
        /// <param name="ticker">종목 코드</param>
        /// <param name="indicators">퀀트 지표 (RSI, MACD 등)</param>
        /// <param name="ohlcv">최근 OHLCV 데이터 (차트 에이전트 입력용)</param>
        public async Task<MultiAgentAnalysisResult> AnalyzeAsync(
            string ticker,
            IndicatorDto indicators,
            List<OhlcvDto>? ohlcv = null)
        {
            Logger.Info($"[GeminiAI] {ticker} — 다중 에이전트 분석 시작 (차트 + 펀더멘털 병렬 실행)");

            // ── 투자 철학 로드 ──
            string philosophy = AppConfigManager.Get("AI_PHILOSOPHY", "");

            // ── 프롬프트 조립 (에이전트별 분리) ──
            string chartSystemPrompt      = PromptBuilder.BuildSystemPrompt(philosophy);
            string chartUserPrompt        = PromptBuilder.BuildUserPrompt(ticker, indicators, ohlcv);
            string fundamentalSystemPrompt = PromptBuilder.BuildFundamentalSystemPrompt(philosophy);
            string fundamentalUserPrompt   = PromptBuilder.BuildFundamentalUserPrompt(ticker, indicators);

            // ── Task.WhenAll: 두 에이전트 병렬 실행 ──
            var chartTask       = CallGeminiAsync(chartSystemPrompt, chartUserPrompt, ticker, "차트");
            var fundamentalTask = CallGeminiAsync(fundamentalSystemPrompt, fundamentalUserPrompt, ticker, "펀더멘털");

            AiAnalysisResult[] results = await Task.WhenAll(chartTask, fundamentalTask);

            AiAnalysisResult chartResult       = results[0];
            AiAnalysisResult fundamentalResult = results[1];

            // ── 두 에이전트 모두 정상 응답했는지 판단 ──
            bool isFull = chartResult.ConfidenceScore > 0m && fundamentalResult.ConfidenceScore > 0m;

            Logger.Info($"[GeminiAI] {ticker} — 에이전트 의견 수집 완료 | " +
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
        /// 단일 Gemini API 호출을 수행합니다 (에이전트 1개).
        /// 실패 시 HOLD fallback을 반환하여 다른 에이전트와의 합의 흐름을 보호합니다.
        /// </summary>
        /// <param name="systemPrompt">에이전트 역할 정의 프롬프트</param>
        /// <param name="userPrompt">분석 요청 프롬프트</param>
        /// <param name="ticker">종목 코드 (로그용)</param>
        /// <param name="agentLabel">에이전트 식별자 ("차트" 또는 "펀더멘털", 로그용)</param>
        private async Task<AiAnalysisResult> CallGeminiAsync(
            string systemPrompt,
            string userPrompt,
            string ticker,
            string agentLabel)
        {
            try
            {
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
                    Logger.Warn($"[GeminiAI-{agentLabel}] {ticker} API 오류 HTTP {(int)response.StatusCode}: " +
                        $"{responseBody[..Math.Min(200, responseBody.Length)]}");
                    return BuildFallback(agentLabel);
                }

                return ParseResponse(responseBody, ticker, agentLabel);
            }
            catch (Exception ex)
            {
                Logger.Warn($"[GeminiAI-{agentLabel}] {ticker} 에이전트 호출 실패 — HOLD fallback: {ex.Message}");
                return BuildFallback(agentLabel);
            }
        }

        /// <summary>
        /// Gemini 응답 JSON을 파싱하여 AiAnalysisResult로 변환합니다.
        /// </summary>
        private AiAnalysisResult ParseResponse(string responseBody, string ticker, string agentLabel)
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

                // ── Token Usage 파싱 및 DB 기록 ──
                if (doc.RootElement.TryGetProperty("usageMetadata", out var usageProp))
                {
                    int promptTokens = usageProp.TryGetProperty("promptTokenCount", out var p) && p.ValueKind == JsonValueKind.Number ? p.GetInt32() : 0;
                    int compTokens = usageProp.TryGetProperty("candidatesTokenCount", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetInt32() : 0;
                    int totalTokens = usageProp.TryGetProperty("totalTokenCount", out var t) && t.ValueKind == JsonValueKind.Number ? t.GetInt32() : 0;

                    TokenUsageDAO.Insert(new TokenUsageDto
                    {
                        Ticker = ticker,
                        AgentType = agentLabel == "차트" ? "CHART_AI" : "FUND_AI",
                        PromptTokens = promptTokens,
                        CompletionTokens = compTokens,
                        TotalTokens = totalTokens
                    });
                }

                // ── JSON 안전 파싱 (마크다운 코드 블록 제거) ──
                string cleanJson = ExtractJson(rawText);
                using var resultDoc = JsonDocument.Parse(cleanJson);
                var root = resultDoc.RootElement;

                string signalStr   = root.GetProperty("signal").GetString() ?? "HOLD";
                decimal confidence = root.GetProperty("confidence").GetDecimal();
                string reason      = root.GetProperty("reason").GetString() ?? string.Empty;

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
            catch (Exception ex)
            {
                Logger.Warn($"[GeminiAI-{agentLabel}] {ticker} 응답 파싱 실패: {ex.Message}. " +
                    $"원문: {responseBody[..Math.Min(300, responseBody.Length)]}");
                _ = NotificationService.SendEmailAsync(
                    $"AI [{agentLabel}] 응답 파싱 실패 (HOLD Fallback)",
                    $"종목: {ticker}\n에이전트: {agentLabel}\n\n원문:\n{responseBody}");
                return BuildFallback(agentLabel);
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
