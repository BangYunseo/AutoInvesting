using AutoInvest.Core;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoInvest.Controllers
{
    /// <summary>
    /// 적립식(DCA) 설정 조회·저장 API.
    /// 여러 매수 템플릿(예산+종목별 수량)과 월(1~12)별 템플릿 배정을 UI에서 편집합니다.
    /// 적립 사이클은 현재(KST) 월에 배정된 템플릿대로 매수합니다.
    /// 저장값은 DB(TB_APP_CONFIG)에 기록되며 다음 사이클부터 반영됩니다.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DcaController : ControllerBase
    {
        /// <summary>
        /// 현재 적립 설정(템플릿 목록 + 월배정 + 현재 월/활성 템플릿)을 반환합니다.
        /// </summary>
        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            try
            {
                var templates = DcaSettings.LoadTemplates();
                var monthMap = DcaSettings.LoadMonthMap();
                int currentMonth = DateTime.UtcNow.AddHours(9).Month;

                string? activeId = monthMap.TryGetValue(currentMonth, out var tid)
                    ? tid
                    : (monthMap.Count == 0 ? templates.FirstOrDefault()?.Id : null);

                return Ok(new
                {
                    templates,
                    monthMap = monthMap.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
                    currentMonth,
                    activeTemplateId = activeId
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Dca] 설정 조회 실패: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 적립 설정(템플릿 목록 + 월배정)을 저장합니다. 다음 사이클부터 반영됩니다.
        /// </summary>
        [HttpPut("config")]
        public IActionResult UpdateConfig([FromBody] DcaConfigRequest req)
        {
            try
            {
                if (req?.Templates == null || req.Templates.Count == 0)
                {
                    return BadRequest(new { error = "템플릿(templates)이 최소 1개 이상이어야 합니다." });
                }

                var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var t in req.Templates)
                {
                    string label = string.IsNullOrWhiteSpace(t.Name) ? t.Id : t.Name;
                    if (string.IsNullOrWhiteSpace(t.Id))
                    {
                        return BadRequest(new { error = "템플릿 식별자(id)가 비어 있습니다." });
                    }
                    if (!seenIds.Add(t.Id.Trim()))
                    {
                        return BadRequest(new { error = $"중복된 템플릿 id가 있습니다: {t.Id}" });
                    }
                    if (t.BudgetKrw <= 0)
                    {
                        return BadRequest(new { error = $"'{label}' 템플릿의 예산은 0보다 커야 합니다." });
                    }
                    if (t.Quantities == null || t.Quantities.Count == 0)
                    {
                        return BadRequest(new { error = $"'{label}' 템플릿에 종목이 최소 1개 이상 필요합니다." });
                    }
                    foreach (var kv in t.Quantities)
                    {
                        if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                        if (kv.Value <= 0)
                        {
                            return BadRequest(new { error = $"'{label}' 템플릿의 '{kv.Key}' 수량은 1 이상이어야 합니다." });
                        }
                    }
                }

                var monthMap = new Dictionary<int, string>();
                if (req.MonthMap != null)
                {
                    foreach (var kv in req.MonthMap)
                        if (int.TryParse(kv.Key, out int m))
                            monthMap[m] = kv.Value;
                }

                DcaSettings.SaveConfig(req.Templates, monthMap);

                Logger.Info($"[Dca] 적립 설정 저장 완료 — 템플릿 {req.Templates.Count}개, 월배정 {monthMap.Count}건");
                return Ok(new
                {
                    message = "적립 설정이 저장되었습니다. 다음 사이클부터 반영됩니다.",
                    templates = req.Templates,
                    monthMap = req.MonthMap
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
    /// 적립 설정 저장 요청 본문 (템플릿 목록 + 월배정).
    /// </summary>
    public class DcaConfigRequest
    {
        /// <summary>매수 템플릿 목록.</summary>
        public List<DcaTemplate> Templates { get; set; } = new();

        /// <summary>월(문자열 "1"~"12")→템플릿Id 배정.</summary>
        public Dictionary<string, string> MonthMap { get; set; } = new();
    }
}
