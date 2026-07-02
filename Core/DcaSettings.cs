using AutoInvest.Data;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace AutoInvest.Core
{
    /// <summary>
    /// 적립식(DCA) 설정의 단일 읽기/쓰기 지점.
    ///
    /// 여러 개의 "매수 템플릿"(명명된 예산+수량 구성)을 두고, 1~12월에 템플릿을 배정합니다.
    /// 적립 사이클은 현재(KST) 월에 배정된 템플릿대로 매수합니다. 월배정이 비어 있으면 첫
    /// 템플릿을 매월 사용(기존 단일 설정 동작 유지), 배정된 월에 템플릿이 없으면 매수를 스킵합니다.
    ///
    /// 우선순위: DB(TB_APP_CONFIG: DCA_TEMPLATES / DCA_MONTH_MAP) → 레거시 단일 설정
    /// (DCA_QTYS/DCA_BUDGET_KRW) → appsettings.json(Dca 섹션). 레거시 설정은 자동으로 "기본"
    /// 템플릿 하나로 이관되어 읽힙니다(저장 시 템플릿 형식으로 기록).
    /// </summary>
    public static class DcaSettings
    {
        /// <summary>DB 키 — 템플릿 목록 JSON.</summary>
        public const string TemplatesKey = "DCA_TEMPLATES";

        /// <summary>DB 키 — 월(1~12)→템플릿Id 배정 JSON.</summary>
        public const string MonthMapKey = "DCA_MONTH_MAP";

        /// <summary>DB 키 — 레거시 단일 설정 수량 JSON (마이그레이션 폴백).</summary>
        public const string QuantitiesKey = "DCA_QTYS";

        /// <summary>DB 키 — 레거시 단일 설정 예산(원) (마이그레이션 폴백).</summary>
        public const string BudgetKey = "DCA_BUDGET_KRW";

        /// <summary>기본 예산 (설정이 전혀 없을 때).</summary>
        public const decimal DefaultBudgetKrw = 1_000_000m;

        /// <summary>
        /// 현재(KST) 월에 적용할 종목별 매수 수량과 예산을 반환합니다 (엔진 진입점).
        /// 적용할 템플릿이 없으면 빈 수량(예산 0)을 반환해 호출부가 매수를 스킵하게 합니다.
        /// </summary>
        public static (Dictionary<string, int> Quantities, decimal BudgetKrw) Load()
        {
            var templates = LoadTemplates();
            var monthMap = LoadMonthMap();
            int month = KstNow().Month;

            DcaTemplate? chosen = SelectTemplate(templates, monthMap, month);

            if (chosen == null)
            {
                Logger.Warn($"[DcaSettings] {month}월에 배정된 템플릿이 없어 이번 사이클 매수를 스킵합니다.");
                return (new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase), 0m);
            }

            var qtys = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in chosen.Quantities)
                if (kv.Value > 0) qtys[kv.Key] = kv.Value;

            decimal budget = chosen.BudgetKrw > 0 ? chosen.BudgetKrw : DefaultBudgetKrw;
            Logger.Info($"[DcaSettings] {month}월 적용 템플릿='{chosen.Name}' (종목 {qtys.Count}개, 예산 {budget:N0}원)");
            return (qtys, budget);
        }

        /// <summary>
        /// 주어진 월(1~12)에 적용할 템플릿을 고릅니다 (순수 함수 — 외부 I/O 없음, 검증 대상).
        /// 규칙:
        ///   · 해당 월이 월배정에 있으면 그 Id의 템플릿을 선택(Id가 목록에 없으면 null → 스킵).
        ///   · 월배정이 비어 있으면 첫(기본) 템플릿을 매월 사용(기존 단일 설정 동작 유지).
        ///   · 월배정은 있으나 해당 월에 배정이 없으면 null → 매수 스킵.
        /// </summary>
        /// <param name="templates">템플릿 목록</param>
        /// <param name="monthMap">월(1~12)→템플릿Id 배정</param>
        /// <param name="month">적용할 월(1~12)</param>
        /// <returns>선택된 템플릿, 없으면 null(매수 스킵)</returns>
        public static DcaTemplate? SelectTemplate(
            IReadOnlyList<DcaTemplate> templates,
            IReadOnlyDictionary<int, string> monthMap,
            int month)
        {
            if (templates == null || templates.Count == 0) return null;

            if (monthMap != null && monthMap.TryGetValue(month, out var tid) && !string.IsNullOrWhiteSpace(tid))
                return templates.FirstOrDefault(t => t.Id == tid);

            if (monthMap == null || monthMap.Count == 0)
                return templates.FirstOrDefault(); // 스케줄 미설정 → 첫(기본) 템플릿을 매월 사용

            return null; // 월배정은 있으나 이번 달 배정 없음 → 스킵
        }

        /// <summary>템플릿 목록을 반환합니다 (DB → 없으면 레거시 단일 설정을 '기본' 템플릿으로 이관).</summary>
        public static List<DcaTemplate> LoadTemplates()
        {
            string json = AppConfigManager.Get(TemplatesKey, "");
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<List<DcaTemplate>>(json);
                    if (parsed != null)
                    {
                        var clean = parsed.Where(t => t != null && !string.IsNullOrWhiteSpace(t.Id)).ToList();
                        if (clean.Count > 0) return clean;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"[DcaSettings] DCA_TEMPLATES 파싱 실패 — 레거시/appsettings로 폴백: {ex.Message}");
                }
            }

            // 마이그레이션: 레거시 단일 설정(DCA_QTYS/appsettings)을 '기본' 템플릿 하나로
            return new List<DcaTemplate>
            {
                new DcaTemplate
                {
                    Id = "default",
                    Name = "기본",
                    BudgetKrw = LoadLegacyBudget(),
                    Quantities = LoadLegacyQuantities()
                }
            };
        }

        /// <summary>월(1~12)→템플릿Id 배정을 반환합니다 (비어 있으면 첫 템플릿을 매월 사용).</summary>
        public static Dictionary<int, string> LoadMonthMap()
        {
            var map = new Dictionary<int, string>();
            string json = AppConfigManager.Get(MonthMapKey, "");
            if (string.IsNullOrWhiteSpace(json)) return map;

            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (parsed != null)
                    foreach (var kv in parsed)
                        if (int.TryParse(kv.Key, out int m) && m >= 1 && m <= 12 && !string.IsNullOrWhiteSpace(kv.Value))
                            map[m] = kv.Value;
            }
            catch (Exception ex)
            {
                Logger.Error($"[DcaSettings] DCA_MONTH_MAP 파싱 실패: {ex.Message}");
            }
            return map;
        }

        /// <summary>
        /// 템플릿 목록과 월배정을 DB에 저장합니다 (다음 사이클부터 반영).
        /// </summary>
        public static void SaveConfig(List<DcaTemplate> templates, Dictionary<int, string> monthMap)
        {
            var cleanTemplates = (templates ?? new List<DcaTemplate>())
                .Where(t => t != null && !string.IsNullOrWhiteSpace(t.Id))
                .Select(t => new DcaTemplate
                {
                    Id = t.Id.Trim(),
                    Name = string.IsNullOrWhiteSpace(t.Name) ? t.Id.Trim() : t.Name.Trim(),
                    BudgetKrw = t.BudgetKrw,
                    Quantities = (t.Quantities ?? new Dictionary<string, int>())
                        .Where(kv => !string.IsNullOrWhiteSpace(kv.Key) && kv.Value > 0)
                        .ToDictionary(kv => kv.Key.Trim().ToUpper(), kv => kv.Value)
                })
                .ToList();

            var ids = new HashSet<string>(cleanTemplates.Select(t => t.Id));
            var cleanMap = (monthMap ?? new Dictionary<int, string>())
                .Where(kv => kv.Key >= 1 && kv.Key <= 12 && !string.IsNullOrWhiteSpace(kv.Value) && ids.Contains(kv.Value))
                .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);

            AppConfigManager.Set(TemplatesKey, JsonSerializer.Serialize(cleanTemplates));
            AppConfigManager.Set(MonthMapKey, JsonSerializer.Serialize(cleanMap));
            Logger.Info($"[DcaSettings] DCA 템플릿 저장 — 템플릿 {cleanTemplates.Count}개, 월배정 {cleanMap.Count}건");
        }

        // ── 레거시 단일 설정 로딩 (마이그레이션용) ──
        private static Dictionary<string, int> LoadLegacyQuantities()
        {
            var qtys = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            string dbJson = AppConfigManager.Get(QuantitiesKey, "");
            if (!string.IsNullOrWhiteSpace(dbJson))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<Dictionary<string, int>>(dbJson);
                    if (parsed != null)
                    {
                        foreach (var kv in parsed)
                            if (kv.Value > 0) qtys[kv.Key] = kv.Value;
                        if (qtys.Count > 0) return qtys;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"[DcaSettings] 레거시 DCA_QTYS 파싱 실패: {ex.Message}");
                }
            }

            foreach (var kv in AppConfigManager.GetMap("Dca:Quantities"))
                if (int.TryParse(kv.Value, out int q) && q > 0) qtys[kv.Key] = q;

            return qtys;
        }

        private static decimal LoadLegacyBudget()
        {
            string dbVal = AppConfigManager.Get(BudgetKey, "");
            if (!string.IsNullOrWhiteSpace(dbVal) && decimal.TryParse(dbVal, out decimal b) && b > 0)
                return b;

            var cfg = AppConfigManager.GetMap("Dca");
            if (cfg.TryGetValue("MonthlyBudgetKrw", out var mb) && decimal.TryParse(mb, out var mv) && mv > 0)
                return mv;

            return DefaultBudgetKrw;
        }

        private static DateTime KstNow() => DateTime.UtcNow.AddHours(9);
    }
}
