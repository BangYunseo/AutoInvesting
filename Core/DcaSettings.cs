using AutoInvest.Data;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;
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

        /// <summary>
        /// DB 키 — 매월 적립을 시작할 날짜(KST, 1~31). 비어 있으면 월초부터 시도(기존 동작).
        /// </summary>
        public const string RunDayKey = "DCA_RUN_DAY";

        /// <summary>
        /// 지정 가능한 최대 일자. 29~31도 허용한다 — 그 날이 없는 달에는 말일로 당겨 판정하므로
        /// (<see cref="DailyExecutionService.IsOnOrAfterRunDay"/>) 적립이 빠지는 달은 없다.
        /// 31을 고르면 사실상 "매월 말일부터"가 된다.
        /// </summary>
        public const int MaxRunDay = 31;

        /// <summary>기본 예산 (설정이 전혀 없을 때).</summary>
        public const decimal DefaultBudgetKrw = 1_000_000m;

        /// <summary>
        /// 종목별 매수 수량, 예산 반환(월 단위)
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
        /// 적용할 템플릿 선택(월 단위)
        /// </summary>
        /// <param name="templates">템플릿 목록</param>
        /// <param name="monthMap">템플릿 Id</param>
        /// <param name="month">적용 월</param>
        /// <returns>선택된 템플릿이 없으면 null(매수 스킵)</returns>
        public static DcaTemplate? SelectTemplate(
            IReadOnlyList<DcaTemplate> templates,
            IReadOnlyDictionary<int, string> monthMap,
            int month)
        {
            if (templates == null || templates.Count == 0) return null;

            if (monthMap != null && monthMap.TryGetValue(month, out var tid) && !string.IsNullOrWhiteSpace(tid))
            {
                return templates.FirstOrDefault(t => t.Id == tid);
            }

            if (monthMap == null || monthMap.Count == 0)
            {
                return templates.FirstOrDefault();
            } 

            return null; 
        }

        /// <summary>
        /// 템플릿 목록 반환(DB → 없으면 '기본' 템플릿)
        /// </summary>
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
                        var clean = new List<DcaTemplate>(); 
                        foreach (var t in parsed)
                        {
                            if (t != null && !string.IsNullOrWhiteSpace(t.Id)) clean.Add(t);
                        }
                        if (clean.Count > 0) return clean;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"[DCA] DCA_TEMPLATES 파싱 실패 : 레거시/appsettings로 폴백: {ex.Message}");
                }
            }

            // 기본 템플릿
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
        /// 템플릿 목록만 저장합니다 (월배정은 건드리지 않음 — 다음 사이클부터 반영).
        /// 다만 삭제된 템플릿을 가리키던 월배정은 함께 지웁니다. 그대로 두면 그 달에 배정된
        /// 템플릿이 없는 상태가 되어 매수가 조용히 스킵됩니다.
        /// </summary>
        /// <param name="templates">저장할 템플릿 목록</param>
        public static void SaveTemplates(List<DcaTemplate> templates)
        {
            var clean = CleanTemplates(templates);
            AppConfigManager.Set(TemplatesKey, JsonSerializer.Serialize(clean));

            var ids = new HashSet<string>(clean.Select(t => t.Id));
            var stored = LoadMonthMap();
            var kept = stored.Where(kv => ids.Contains(kv.Value)).ToDictionary(kv => kv.Key, kv => kv.Value);
            if (kept.Count != stored.Count)
            {
                WriteMonthMap(kept);
                Logger.Warn($"[DcaSettings] 삭제된 템플릿을 가리키던 월배정 {stored.Count - kept.Count}건을 정리했습니다.");
            }

            Logger.Info($"[DcaSettings] 매수 템플릿 저장 — 템플릿 {clean.Count}개");
        }

        /// <summary>
        /// 월(1~12)→템플릿Id 배정만 저장합니다 (템플릿 목록은 건드리지 않음).
        /// 저장된 템플릿에 없는 Id를 가리키는 배정은 버립니다.
        /// </summary>
        /// <param name="monthMap">월(1~12)→템플릿Id 배정</param>
        public static void SaveMonthMap(Dictionary<int, string> monthMap)
        {
            var ids = new HashSet<string>(LoadTemplates().Select(t => t.Id));
            var kept = (monthMap ?? new Dictionary<int, string>())
                .Where(kv => kv.Key >= 1 && kv.Key <= 12 && !string.IsNullOrWhiteSpace(kv.Value) && ids.Contains(kv.Value))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            WriteMonthMap(kept);
            Logger.Info($"[DcaSettings] 월별 배정 저장 — {kept.Count}건");
        }

        /// <summary>
        /// 적립 시작할 날짜(KST) 반환
        /// </summary>
        public static int LoadRunDay()
        {
            string raw = AppConfigManager.Get(RunDayKey, "");
            if (int.TryParse(raw, out int day) && day >= 1 && day <= MaxRunDay)
            {
                return day;
            }
            return 0;
        }

        /// <summary>
        /// 매월 적립을 시작할 날짜를 저장합니다. 0 이하를 넘기면 지정을 해제합니다(월초부터 시도).
        /// </summary>
        /// <param name="day">1~31, 또는 해제용 0</param>
        /// <returns>DB 기록 성공 여부. 실패를 삼키면 사람이 고른 날짜보다 이르게 매수될 수 있어 그대로 돌려준다.</returns>
        public static bool SaveRunDay(int day)
        {
            string value = day >= 1 && day <= MaxRunDay ? day.ToString() : "";
            bool ok = AppConfigManager.Set(RunDayKey, value);
            Logger.Info($"[DcaSettings] 적립 지정일 저장 — {(value.Length == 0 ? "해제(월초부터)" : value + "일")} (성공={ok})");
            return ok;
        }

        private static void WriteMonthMap(Dictionary<int, string> map)
        {
            AppConfigManager.Set(
                MonthMapKey,
                JsonSerializer.Serialize(map.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value)));
        }

        private static List<DcaTemplate> CleanTemplates(List<DcaTemplate> templates)
        {
            return (templates ?? new List<DcaTemplate>())
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
