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
    /// 설정의 유일한 출처는 DB(TB_APP_CONFIG: DCA_TEMPLATES / DCA_MONTH_MAP)입니다.
    /// 읽지 못하면 종목이 빈 "기본" 템플릿이 되어 그 사이클은 매수를 건너뜁니다 — 조회 실패가
    /// 곧 "의도하지 않은 종목·수량을 실계좌에 매수"로 이어지지 않게 하기 위한 fail-closed입니다.
    /// (2026-06-29 템플릿 이관 완료 후 남아 있던 레거시 단일 설정 폴백 DCA_QTYS/DCA_BUDGET_KRW는
    ///  2026-09-23에 제거했습니다. 낡은 바스켓을 되살려 매수할 위험만 남아 있었습니다.)
    /// </summary>
    public static class DcaSettings
    {
        /// <summary>DB 키 — 템플릿 목록 JSON.</summary>
        public const string TemplatesKey = "DCA_TEMPLATES";

        /// <summary>DB 키 — 월(1~12)→템플릿Id 배정 JSON.</summary>
        public const string MonthMapKey = "DCA_MONTH_MAP";

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
                    Logger.Error($"[DCA] DCA_TEMPLATES 파싱 실패 : 종목 없는 기본 템플릿으로 폴백(매수 스킵): {ex.Message}");
                }
            }

            // 기본 템플릿 — 종목을 일부러 비워 둔다. 여기에 수량이 채워지면 DB 조회 실패만으로
            // 의도하지 않은 바스켓이 실계좌에 매수된다. 종목은 적립설정 화면에서만 들어온다.
            return new List<DcaTemplate>
            {
                new DcaTemplate
                {
                    Id = "default",
                    Name = "기본",
                    BudgetKrw = DefaultBudgetKrw,
                    Quantities = new Dictionary<string, int>()
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

        private static DateTime KstNow() => DateTime.UtcNow.AddHours(9);
    }
}
