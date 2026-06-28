using AutoInvest.Data;
using AutoInvest.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace AutoInvest.Core
{
    /// <summary>
    /// 적립식(DCA) 설정(목표비중·예산)의 단일 읽기/쓰기 지점.
    ///
    /// 우선순위: DB(TB_APP_CONFIG, 런타임 편집 가능) → appsettings.json(Dca 섹션, 초기 기본값).
    /// UI(DcaController)에서 저장하면 DB에 기록되어 다음 사이클부터 반영됩니다.
    /// DB 값이 없으면 appsettings의 기본 목표비중/예산을 사용합니다.
    /// </summary>
    public static class DcaSettings
    {
        /// <summary>DB 설정 키 — 목표비중 JSON (예: {"SPLG":0.4,"QQQM":0.3}).</summary>
        public const string TargetsKey = "DCA_TARGETS";

        /// <summary>DB 설정 키 — 월 예산(원).</summary>
        public const string BudgetKey = "DCA_BUDGET_KRW";

        /// <summary>기본 예산 (설정이 전혀 없을 때).</summary>
        public const decimal DefaultBudgetKrw = 1_000_000m;

        /// <summary>
        /// 현재 적용할 목표비중과 예산을 반환합니다 (DB 우선, 없으면 appsettings).
        /// </summary>
        public static (Dictionary<string, decimal> Targets, decimal BudgetKrw) Load()
        {
            return (LoadTargets(), LoadBudget());
        }

        /// <summary>목표비중을 반환합니다 (DB JSON 우선, 없으면 appsettings Dca:Targets).</summary>
        public static Dictionary<string, decimal> LoadTargets()
        {
            var targets = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            // 1. DB 저장값 (JSON)
            string dbJson = AppConfigManager.Get(TargetsKey, "");
            if (!string.IsNullOrWhiteSpace(dbJson))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<Dictionary<string, decimal>>(dbJson);
                    if (parsed != null)
                    {
                        foreach (var kv in parsed)
                            if (kv.Value > 0) targets[kv.Key] = kv.Value;
                        if (targets.Count > 0) return targets;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"[DcaSettings] DCA_TARGETS JSON 파싱 실패 — appsettings로 폴백: {ex.Message}");
                }
            }

            // 2. appsettings.json (Dca:Targets)
            foreach (var kv in AppConfigManager.GetMap("Dca:Targets"))
                if (decimal.TryParse(kv.Value, out decimal w) && w > 0)
                    targets[kv.Key] = w;

            return targets;
        }

        /// <summary>예산(원)을 반환합니다 (DB 우선, 없으면 appsettings, 그래도 없으면 기본값).</summary>
        public static decimal LoadBudget()
        {
            string dbVal = AppConfigManager.Get(BudgetKey, "");
            if (!string.IsNullOrWhiteSpace(dbVal) && decimal.TryParse(dbVal, out decimal dbBudget) && dbBudget > 0)
                return dbBudget;

            var dcaCfg = AppConfigManager.GetMap("Dca");
            if (dcaCfg.TryGetValue("MonthlyBudgetKrw", out var b) && decimal.TryParse(b, out var bv) && bv > 0)
                return bv;

            return DefaultBudgetKrw;
        }

        /// <summary>
        /// 목표비중과 예산을 DB에 저장합니다 (다음 사이클부터 반영).
        /// </summary>
        public static void Save(Dictionary<string, decimal> targets, decimal budgetKrw)
        {
            var clean = targets
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Key) && kv.Value > 0)
                .ToDictionary(kv => kv.Key.Trim().ToUpper(), kv => kv.Value);

            AppConfigManager.Set(TargetsKey, JsonSerializer.Serialize(clean));
            AppConfigManager.Set(BudgetKey, budgetKrw.ToString("0.##"));
            Logger.Info($"[DcaSettings] DCA 설정 저장 — 종목 {clean.Count}개, 예산 {budgetKrw:N0}원");
        }
    }
}
