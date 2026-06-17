using AutoInvest.Core.Quant;
using AutoInvest.Data.DAO;
using AutoInvest.Utils;
using Microsoft.AspNetCore.Mvc;
using System;

namespace AutoInvest.Controllers
{
    /// <summary>
    /// AI 판단 성과 및 토큰 사용량/비용을 조회하는 모니터링 API (Phase 5-b).
    /// 데이터 수집은 SmartOrderEngine/DailyExecutionService에서 수행되며,
    /// 본 컨트롤러는 축적된 데이터를 대시보드에서 조회·시각화하기 위한 읽기 전용 엔드포인트를 제공합니다.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MonitoringController : ControllerBase
    {
        // Gemini 2.0 Flash 공식 단가 (USD / 1M tokens) — 현재 기본 모델 gemini-2.0-flash 기준
        // 참고: https://ai.google.dev/pricing — 입력 $0.10, 출력 $0.40
        private const decimal INPUT_USD_PER_MILLION = 0.10m;
        private const decimal OUTPUT_USD_PER_MILLION = 0.40m;

        private static decimal EstimateCostUsd(long promptTokens, long completionTokens)
        {
            decimal inputCost = promptTokens / 1_000_000m * INPUT_USD_PER_MILLION;
            decimal outputCost = completionTokens / 1_000_000m * OUTPUT_USD_PER_MILLION;
            return Math.Round(inputCost + outputCost, 6);
        }

        /// <summary>
        /// 모니터링 요약 카드용 핵심 지표를 반환합니다.
        /// </summary>
        /// <param name="days">비용 추정 집계 기간 (기본 30일)</param>
        [HttpGet("summary")]
        public IActionResult GetSummary([FromQuery] int days = 30)
        {
            try
            {
                int todayTotalTokens = TokenUsageDAO.GetTodayTotalTokens();
                var (perfCount, avgWinRate) = AiPerformanceDAO.GetOverallPerformance();
                var (promptSum, compSum) = TokenUsageDAO.GetTokenSums(days);

                return Ok(new
                {
                    todayTotalTokens,
                    evaluatedCount = perfCount,
                    averageWinRate = Math.Round(avgWinRate, 4),
                    periodDays = days,
                    periodPromptTokens = promptSum,
                    periodCompletionTokens = compSum,
                    periodTotalTokens = promptSum + compSum,
                    estPeriodCostUsd = EstimateCostUsd(promptSum, compSum)
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Monitoring] 요약 조회 실패: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 최근 AI 판단 성과 기록을 반환합니다.
        /// </summary>
        /// <param name="limit">최대 조회 건수 (기본 50)</param>
        [HttpGet("performance")]
        public IActionResult GetPerformance([FromQuery] int limit = 50)
        {
            try
            {
                var list = AiPerformanceDAO.GetRecent(limit);
                Logger.Info($"[Monitoring] AI 성과 {list.Count}건 조회");
                return Ok(list);
            }
            catch (Exception ex)
            {
                Logger.Error($"[Monitoring] AI 성과 조회 실패: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 에이전트 유형별 토큰 사용량 + 비용 추정을 반환합니다.
        /// </summary>
        /// <param name="days">집계 기간 (기본 30일)</param>
        [HttpGet("tokens/by-agent")]
        public IActionResult GetTokensByAgent([FromQuery] int days = 30)
        {
            try
            {
                var rows = TokenUsageDAO.GetUsageByAgent(days);
                var result = rows.ConvertAll(r => new
                {
                    r.AgentType,
                    r.CallCount,
                    r.PromptTokens,
                    r.CompletionTokens,
                    r.TotalTokens,
                    estCostUsd = EstimateCostUsd(r.PromptTokens, r.CompletionTokens)
                });
                return Ok(new { periodDays = days, agents = result });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Monitoring] 에이전트별 토큰 조회 실패: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 일자별 토큰 사용량 + 비용 추정을 반환합니다 (최신순).
        /// </summary>
        /// <param name="days">집계 기간 (기본 14일)</param>
        [HttpGet("tokens/daily")]
        public IActionResult GetTokensDaily([FromQuery] int days = 14)
        {
            try
            {
                var rows = TokenUsageDAO.GetDailyUsage(days);
                var result = rows.ConvertAll(r => new
                {
                    r.Date,
                    r.CallCount,
                    r.PromptTokens,
                    r.CompletionTokens,
                    r.TotalTokens,
                    estCostUsd = EstimateCostUsd(r.PromptTokens, r.CompletionTokens)
                });
                return Ok(new { periodDays = days, daily = result });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Monitoring] 일자별 토큰 조회 실패: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Phase 5-d: 에이전트(퀀트/차트AI/펀더멘털AI)별 실측 적중률을 반환합니다.
        /// </summary>
        /// <param name="horizonDays">신호 이후 성과를 측정할 경과 일수 (기본 7일)</param>
        [HttpGet("agent-accuracy")]
        public IActionResult GetAgentAccuracy([FromQuery] int horizonDays = 7)
        {
            try
            {
                var rows = PerformanceFeedbackEngine.GetAgentAccuracy(horizonDays);
                Logger.Info($"[Monitoring] 에이전트 적중률 조회 (horizon={horizonDays}d)");
                return Ok(new { horizonDays, agents = rows });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Monitoring] 에이전트 적중률 조회 실패: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Phase 5-d: 합의 가중치 조합별 가상 매수 성과(A/B 백테스트)를 반환합니다.
        /// ⚠️ 검증용 리포트 — 실제 매매 가중치에 자동 반영되지 않습니다.
        /// </summary>
        /// <param name="horizonDays">매수 이후 성과를 측정할 경과 일수 (기본 7일)</param>
        [HttpGet("weight-abtest")]
        public IActionResult GetWeightAbTest([FromQuery] int horizonDays = 7)
        {
            try
            {
                var rows = PerformanceFeedbackEngine.RunWeightAbTest(horizonDays);
                Logger.Info($"[Monitoring] 가중치 A/B 백테스트 조회 (horizon={horizonDays}d)");
                return Ok(new { horizonDays, note = "검증용 리포트 — 실제 매매 가중치에 자동 반영되지 않음", schemes = rows });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Monitoring] 가중치 A/B 백테스트 조회 실패: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Phase 5-d: 특정 종목의 현재 적응형 매수/매도 임계값과 산출 근거를 반환합니다.
        /// </summary>
        /// <param name="ticker">종목 코드 (예: QQQM)</param>
        [HttpGet("adaptive-threshold")]
        public IActionResult GetAdaptiveThreshold([FromQuery] string ticker)
        {
            if (string.IsNullOrWhiteSpace(ticker))
            {
                return BadRequest(new { error = "ticker 파라미터가 필요합니다." });
            }
            try
            {
                var (buyThreshold, buyReason) = AdaptiveThresholdEngine.GetBuyThreshold(ticker);
                var (sellThreshold, sellReason) = AdaptiveThresholdEngine.GetSellThreshold(ticker);
                return Ok(new
                {
                    ticker,
                    buyThreshold,
                    buyReason,
                    sellThreshold,
                    sellReason
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Monitoring] 적응형 임계값 조회 실패: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
