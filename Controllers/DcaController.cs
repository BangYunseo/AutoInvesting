using AutoInvest.Core;
using AutoInvest.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoInvest.Controllers
{
    /// <summary>
    /// 적립식(DCA) 설정 조회·저장 API.
    /// 목표비중(Targets)과 예산(MonthlyBudgetKrw)을 UI에서 편집할 수 있게 합니다.
    /// 저장값은 DB(TB_APP_CONFIG)에 기록되며 다음 적립 사이클부터 반영됩니다.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DcaController : ControllerBase
    {
        /// <summary>
        /// 현재 적용 중인 적립 설정(목표비중·예산)을 반환합니다.
        /// </summary>
        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            try
            {
                var (targets, budget) = DcaSettings.Load();
                return Ok(new
                {
                    budgetKrw = budget,
                    targets,
                    totalWeight = targets.Values.Sum()
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Dca] 설정 조회 실패: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 적립 설정(목표비중·예산)을 저장합니다. 다음 사이클부터 반영됩니다.
        /// </summary>
        /// <param name="req">목표비중 맵과 예산</param>
        [HttpPut("config")]
        public IActionResult UpdateConfig([FromBody] DcaConfigRequest req)
        {
            try
            {
                if (req == null || req.Targets == null || req.Targets.Count == 0)
                {
                    return BadRequest(new { error = "목표비중(targets)은 최소 1개 이상이어야 합니다." });
                }
                if (req.BudgetKrw <= 0)
                {
                    return BadRequest(new { error = "예산(budgetKrw)은 0보다 커야 합니다." });
                }

                // 유효 항목만 추리고 검증 (티커 비어있지 않고, 비중 양수)
                var clean = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in req.Targets)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                    if (kv.Value <= 0)
                    {
                        return BadRequest(new { error = $"'{kv.Key}'의 비중은 0보다 커야 합니다." });
                    }
                    clean[kv.Key.Trim().ToUpper()] = kv.Value;
                }

                if (clean.Count == 0)
                {
                    return BadRequest(new { error = "유효한 목표비중이 없습니다." });
                }

                DcaSettings.Save(clean, req.BudgetKrw);

                Logger.Info($"[Dca] 적립 설정 저장 완료 — 종목 {clean.Count}개, 예산 {req.BudgetKrw:N0}원");
                return Ok(new
                {
                    message = "적립 설정이 저장되었습니다. 다음 사이클부터 반영됩니다.",
                    budgetKrw = req.BudgetKrw,
                    targets = clean,
                    totalWeight = clean.Values.Sum()
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Dca] 설정 저장 실패: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    /// <summary>
    /// 적립 설정 저장 요청 본문.
    /// </summary>
    public class DcaConfigRequest
    {
        /// <summary>월 예산 (원).</summary>
        public decimal BudgetKrw { get; set; }

        /// <summary>종목별 목표비중 (예: SPLG=0.4).</summary>
        public Dictionary<string, decimal> Targets { get; set; } = new();
    }
}
