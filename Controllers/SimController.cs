using AutoInvest.Core.Quant;
using AutoInvest.Data;
using AutoInvest.Data.DAO;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AutoInvest.Controllers
{
    /// <summary>
    /// Phase 6-a: 시뮬레이션 학습데이터 생성·검증 API.
    /// SimBroker 기반으로 라벨링된 스냅샷(DATA_SOURCE='SIM')을 대량 생성하고,
    /// 그 데이터로 피드백 분석(적중률 / 가중치 A/B)을 검증합니다. 실데이터(REAL)는 건드리지 않습니다.
    /// </summary>
    [ApiController]
    [Route("api/sim")]
    public class SimController : ControllerBase
    {
        /// <summary>
        /// SimBroker 시뮬레이션으로 AI 학습데이터를 대량 생성하여 SIM 출처로 저장합니다.
        /// </summary>
        /// <param name="req">생성 요청 (종목 목록, 종목당 스냅샷 수, 전략 유형)</param>
        [HttpPost("generate-training-data")]
        public async Task<IActionResult> GenerateTrainingData([FromBody] SimTrainingDataGenerator.GenerateRequest req)
        {
            try
            {
                req ??= new SimTrainingDataGenerator.GenerateRequest();
                var result = await SimTrainingDataGenerator.GenerateAsync(req);

                Logger.Info($"[Sim] 학습데이터 생성 완료: {result.InsertedCount}건");
                return Ok(new
                {
                    message = $"시뮬레이션 학습데이터 {result.InsertedCount}건 생성 완료 (SIM)",
                    insertedCount = result.InsertedCount,
                    tickerCount = result.TickerCount,
                    perTicker = result.PerTicker
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Sim] 학습데이터 생성 실패: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 생성된 SIM 학습데이터만 대상으로 에이전트별 실측 적중률과 가중치 A/B 결과를 산출합니다 (검증용).
        /// </summary>
        /// <param name="horizonDays">신호 이후 성과를 측정할 경과 일수 (기본 7일)</param>
        [HttpGet("verify-training-data")]
        public IActionResult VerifyTrainingData([FromQuery] int horizonDays = 7)
        {
            try
            {
                var snaps = MarketSnapshotDAO.GetRecentAll(5000, "SIM");
                decimal buyThreshold = decimal.TryParse(AppConfigManager.Get("BUY_THRESHOLD", "0.65"), out decimal t) ? t : 0.65m;

                var accuracy = PerformanceFeedbackEngine.GetAgentAccuracy(snaps, horizonDays);
                var abtest = PerformanceFeedbackEngine.RunWeightAbTest(snaps, buyThreshold, horizonDays);

                return Ok(new
                {
                    dataSource = "SIM",
                    snapshotCount = snaps.Count,
                    horizonDays,
                    agentAccuracy = accuracy,
                    weightAbTest = abtest
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Sim] 학습데이터 검증 실패: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
