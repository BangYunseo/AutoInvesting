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
                    activeTemplateId = activeId,
                    runDay = DcaSettings.LoadRunDay(),   // 0 = 미설정(월초부터 시도)
                    maxRunDay = DcaSettings.MaxRunDay
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Dca] 설정 조회 실패: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 적립 설정을 저장합니다. 다음 사이클부터 반영됩니다.
        /// 템플릿 목록과 월배정은 서로 독립적으로 저장됩니다 — 본문에 담아 보낸 쪽만 기록하고,
        /// 빠뜨린 쪽은 손대지 않습니다(둘 다 없으면 400).
        /// </summary>
        [HttpPut("config")]
        public IActionResult UpdateConfig([FromBody] DcaConfigRequest req)
        {
            try
            {
                if (req == null || (req.Templates == null && req.MonthMap == null && req.RunDay == null))
                {
                    return BadRequest(new { error = "저장할 내용이 없습니다 (templates·monthMap·runDay 중 하나는 필요)." });
                }

                if (req.RunDay != null && (req.RunDay < 0 || req.RunDay > DcaSettings.MaxRunDay))
                {
                    return BadRequest(new
                    {
                        error = $"적립 지정일은 1~{DcaSettings.MaxRunDay} 사이여야 합니다(0은 해제)."
                    });
                }

                if (req.Templates != null)
                {
                    var invalid = ValidateTemplates(req.Templates);
                    if (invalid != null) return BadRequest(new { error = invalid });
                }

                Dictionary<int, string>? monthMap = null;
                if (req.MonthMap != null)
                {
                    monthMap = new Dictionary<int, string>();
                    foreach (var kv in req.MonthMap)
                        if (int.TryParse(kv.Key, out int m))
                            monthMap[m] = kv.Value;

                    // 배정 대상은 "이번에 함께 저장하는 템플릿" 또는 "이미 저장된 템플릿"에 있어야 한다.
                    // 조용히 버리면 그 달 매수가 스킵되므로, 모르는 Id면 저장하지 않고 되돌려준다.
                    var known = new HashSet<string>(
                        (req.Templates ?? DcaSettings.LoadTemplates()).Select(t => t.Id.Trim()),
                        StringComparer.OrdinalIgnoreCase);
                    var unknown = monthMap
                        .Where(kv => !string.IsNullOrWhiteSpace(kv.Value) && !known.Contains(kv.Value.Trim()))
                        .Select(kv => $"{kv.Key}월")
                        .ToList();
                    if (unknown.Count > 0)
                    {
                        return BadRequest(new
                        {
                            error = $"아직 저장되지 않은 템플릿에 배정된 달이 있습니다({string.Join(", ", unknown)}). 매수 템플릿을 먼저 저장하세요."
                        });
                    }
                }

                if (req.Templates != null) DcaSettings.SaveTemplates(req.Templates);
                if (monthMap != null) DcaSettings.SaveMonthMap(monthMap);

                // 지정일 기록이 조용히 실패하면 크론이 월초부터 매수해 사람이 고른 날보다 이르게
                // 실자금이 나간다. 실패는 삼키지 않고 그대로 알린다.
                if (req.RunDay != null && !DcaSettings.SaveRunDay(req.RunDay.Value))
                {
                    return StatusCode(500, new
                    {
                        error = "적립 지정일을 저장하지 못했습니다. 지금 상태로 두면 크론이 월초부터 매수합니다."
                    });
                }

                var saved = new List<string>();
                if (req.Templates != null) saved.Add("매수 템플릿");
                if (monthMap != null) saved.Add("월별 배정");
                if (req.RunDay != null) saved.Add("적립 지정일");
                string what = saved.Count == 3 ? "적립 설정" : string.Join("·", saved);

                Logger.Info($"[Dca] {what} 저장 완료 — 템플릿 {req.Templates?.Count ?? 0}개, 월배정 {monthMap?.Count ?? 0}건, 지정일 {req.RunDay?.ToString() ?? "변경없음"}");
                return Ok(new
                {
                    message = $"{what}이 저장되었습니다. 다음 사이클부터 반영됩니다.",
                    templates = req.Templates,
                    monthMap = req.MonthMap,
                    runDay = req.RunDay
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Dca] 설정 저장 실패: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 템플릿 목록을 검증합니다. 문제가 없으면 null, 있으면 사용자에게 보여줄 메시지를 반환합니다.
        /// </summary>
        /// <param name="templates">검증할 템플릿 목록</param>
        private static string? ValidateTemplates(List<DcaTemplate> templates)
        {
            if (templates.Count == 0)
            {
                return "템플릿(templates)이 최소 1개 이상이어야 합니다.";
            }

            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in templates)
            {
                string label = string.IsNullOrWhiteSpace(t.Name) ? t.Id : t.Name;
                if (string.IsNullOrWhiteSpace(t.Id))
                {
                    return "템플릿 식별자(id)가 비어 있습니다.";
                }
                if (!seenIds.Add(t.Id.Trim()))
                {
                    return $"중복된 템플릿 id가 있습니다: {t.Id}";
                }
                if (t.BudgetKrw <= 0)
                {
                    return $"'{label}' 템플릿의 예산은 0보다 커야 합니다.";
                }
                if (t.Quantities == null || t.Quantities.Count == 0)
                {
                    return $"'{label}' 템플릿에 종목이 최소 1개 이상 필요합니다.";
                }
                foreach (var kv in t.Quantities)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                    if (kv.Value <= 0)
                    {
                        return $"'{label}' 템플릿의 '{kv.Key}' 수량은 1 이상이어야 합니다.";
                    }
                }
            }

            return null;
        }
    }

    /// <summary>
    /// 적립 설정 저장 요청 본문.
    /// 두 항목은 독립적이다 — 담아 보낸 쪽만 저장되고, null인 쪽은 기존 저장값을 유지한다.
    /// </summary>
    public class DcaConfigRequest
    {
        /// <summary>매수 템플릿 목록. null이면 템플릿을 저장하지 않는다.</summary>
        public List<DcaTemplate>? Templates { get; set; }

        /// <summary>월(문자열 "1"~"12")→템플릿Id 배정. null이면 월배정을 저장하지 않는다.</summary>
        public Dictionary<string, string>? MonthMap { get; set; }

        /// <summary>매월 적립을 시작할 날짜(KST, 1~28). 0은 해제(월초부터 시도), null이면 변경하지 않는다.</summary>
        public int? RunDay { get; set; }
    }
}
