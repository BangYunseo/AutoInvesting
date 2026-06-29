using AutoInvest.Data;
using AutoInvest.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace AutoInvest.Core
{
    /// <summary>
    /// 적립식(DCA) 설정(종목별 고정 매수 수량·예산)의 단일 읽기/쓰기 지점.
    ///
    /// 우선순위: DB(TB_APP_CONFIG, 런타임 편집 가능) → appsettings.json(Dca 섹션, 초기 기본값).
    /// 사람이 종목별 "매 사이클 고정 매수 주수"를 직접 지정합니다. 비중(%)은 저장 대상이 아니라
    /// 화면에서 수량×현재가로 환산해 보여주기만 합니다. 예산은 초과 경고용 상한입니다.
    /// </summary>
    public static class DcaSettings
    {
        /// <summary>DB 설정 키 — 종목별 고정 매수 수량 JSON (예: {"QQQM":2,"SPLG":3}).</summary>
        public const string QuantitiesKey = "DCA_QTYS";

        /// <summary>DB 설정 키 — 월 예산(원, 초과 경고용 상한).</summary>
        public const string BudgetKey = "DCA_BUDGET_KRW";

        /// <summary>기본 예산 (설정이 전혀 없을 때).</summary>
        public const decimal DefaultBudgetKrw = 1_000_000m;

        /// <summary>
        /// 현재 적용할 종목별 매수 수량과 예산을 반환합니다 (DB 우선, 없으면 appsettings).
        /// </summary>
        public static (Dictionary<string, int> Quantities, decimal BudgetKrw) Load()
        {
            return (LoadQuantities(), LoadBudget());
        }

        /// <summary>종목별 고정 매수 수량을 반환합니다 (DB JSON 우선, 없으면 appsettings Dca:Quantities).</summary>
        public static Dictionary<string, int> LoadQuantities()
        {
            var qtys = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // 1. DB 저장값 (JSON)
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
                    Logger.Error($"[DcaSettings] DCA_QTYS JSON 파싱 실패 — appsettings로 폴백: {ex.Message}");
                }
            }

            // 2. appsettings.json (Dca:Quantities)
            foreach (var kv in AppConfigManager.GetMap("Dca:Quantities"))
                if (int.TryParse(kv.Value, out int q) && q > 0)
                    qtys[kv.Key] = q;

            return qtys;
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
        /// 종목별 매수 수량과 예산을 DB에 저장합니다 (다음 사이클부터 반영).
        /// </summary>
        public static void Save(Dictionary<string, int> quantities, decimal budgetKrw)
        {
            var clean = quantities
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Key) && kv.Value > 0)
                .ToDictionary(kv => kv.Key.Trim().ToUpper(), kv => kv.Value);

            AppConfigManager.Set(QuantitiesKey, JsonSerializer.Serialize(clean));
            AppConfigManager.Set(BudgetKey, budgetKrw.ToString("0.##"));
            Logger.Info($"[DcaSettings] DCA 설정 저장 — 종목 {clean.Count}개, 예산 {budgetKrw:N0}원");
        }
    }
}
