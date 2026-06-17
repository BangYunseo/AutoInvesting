using System;
using System.Collections.Generic;
using Npgsql;
using AutoInvest.Utils;
using Microsoft.Extensions.Configuration;

namespace AutoInvest.Data
{
    /// <summary>
    /// 애플리케이션 설정값을 통합 관리합니다.
    /// 우선순위: 환경변수 → PostgreSQL DB (TB_APP_CONFIG) → appsettings.json
    /// 민감 정보(KIS_APP_KEY, KIS_APP_SECRET, KIS_ACCOUNT_NO)는 환경변수 전용입니다.
    /// DB는 런타임에 UI로 저장한 값을 보관하며, appsettings.json의 기본값을 항상 덮어씁니다.
    /// </summary>
    public static class AppConfigManager
    {
        private static IConfiguration? _configuration;

        /// <summary>
        /// ASP.NET Core IConfiguration을 주입합니다. Program.cs에서 호출.
        /// </summary>
        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
            Logger.Info("[Config] AppConfigManager 초기화 완료 (appsettings.json 연동)");
        }

        /// <summary>
        /// 설정값 조회. 우선순위: 환경변수 → DB(TB_APP_CONFIG) → appsettings.json → 기본값.
        /// UI에서 저장한 DB 값이 appsettings.json 기본값을 항상 덮어씁니다.
        /// </summary>
        public static string Get(string key, string defaultValue = "")
        {
            try
            {
                // 1. 환경변수 (최우선 — 민감 정보용)
                string? envValue = Environment.GetEnvironmentVariable(key);
                if (!string.IsNullOrEmpty(envValue)) return envValue;

                // 2. PostgreSQL DB (런타임 수정 가능한 설정 — 저장 시 항상 덮어쓰기)
                //    DB 장애 시 null을 반환해 appsettings.json 기본값으로 폴백한다.
                string? dbValue = TryGetFromDb(key);
                if (!string.IsNullOrEmpty(dbValue)) return dbValue;

                // 3. appsettings.json (IConfiguration) — DB에 저장된 적 없는 키의 초기 기본값
                if (_configuration != null)
                {
                    // 평탄화된 키 매핑: IS_PAPER_TRADING → Trading:IsPaperTrading 등
                    string? configValue = ResolveFromConfiguration(key);
                    if (!string.IsNullOrEmpty(configValue)) return configValue;
                }

                // 4. 기본값
                return defaultValue;
            }
            catch (Exception ex)
            {
                Logger.Error($"Config 조회 실패 [{key}]: {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// TB_APP_CONFIG에서 설정값을 조회합니다.
        /// 행이 없거나 DB 오류가 발생하면 null을 반환해 상위 호출부가
        /// appsettings.json 기본값으로 폴백하도록 합니다.
        /// </summary>
        private static string? TryGetFromDb(string key)
        {
            try
            {
                using (var conn = DBManager.Instance.GetConnection())
                using (var cmd = new NpgsqlCommand(
                    "SELECT CONFIG_VALUE FROM TB_APP_CONFIG WHERE CONFIG_KEY=@k", conn))
                {
                    cmd.Parameters.AddWithValue("@k", key);
                    return cmd.ExecuteScalar()?.ToString();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"[Config] DB 조회 실패, 기본 설정으로 폴백 [{key}]: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 설정값 저장 (PostgreSQL DB에 저장).
        /// </summary>
        public static void Set(string key, string value)
        {
            try
            {
                using (var conn = DBManager.Instance.GetConnection())
                using (var cmd = new NpgsqlCommand(
                    "UPDATE TB_APP_CONFIG SET CONFIG_VALUE=@v WHERE CONFIG_KEY=@k", conn))
                {
                    cmd.Parameters.AddWithValue("@v", value);
                    cmd.Parameters.AddWithValue("@k", key);
                    int affected = cmd.ExecuteNonQuery();

                    if (affected == 0)
                    {
                        // 키가 없으면 INSERT
                        using var insertCmd = new NpgsqlCommand(
                            "INSERT INTO TB_APP_CONFIG (CONFIG_KEY, CONFIG_VALUE) VALUES (@k, @v)", conn);
                        insertCmd.Parameters.AddWithValue("@k", key);
                        insertCmd.Parameters.AddWithValue("@v", value);
                        insertCmd.ExecuteNonQuery();
                    }
                }
                Logger.Info($"[Config] 저장: {key} = {value}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[Config] 저장 실패 [{key}]: {ex.Message}");
            }
        }

        /// <summary>
        /// appsettings.json의 특정 섹션을 키/값 딕셔너리로 조회합니다 (예: "FxAdvisor:HedgeMap").
        /// 값이 있는 직속 하위 항목만 포함하며, 섹션이 없으면 빈 딕셔너리를 반환합니다.
        /// </summary>
        /// <param name="path">섹션 경로 (콜론 구분)</param>
        public static Dictionary<string, string> GetMap(string path)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var section = _configuration?.GetSection(path);
                if (section == null) return map;

                foreach (var child in section.GetChildren())
                {
                    if (!string.IsNullOrEmpty(child.Value))
                        map[child.Key] = child.Value;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[Config] 섹션 조회 실패 [{path}]: {ex.Message}");
            }
            return map;
        }

        /// <summary>
        /// 레거시 키명을 appsettings.json의 계층 구조 키로 매핑합니다.
        /// </summary>
        private static string? ResolveFromConfiguration(string key)
        {
            // 레거시 키 → appsettings.json 경로 매핑
            string? mappedPath = key switch
            {
                "IS_PAPER_TRADING"      => "Trading:IsPaperTrading",
                "INVEST_AMOUNT_KRW"     => "Trading:InvestAmountKrw",
                "ACTIVE_STRATEGY"       => "Trading:ActiveStrategy",
                "STRATEGY_TYPE"         => "Trading:StrategyType",
                "ORDER_SCHEDULE"        => "Trading:OrderSchedule",
                "REBALANCE_ENABLED"     => "Rebalance:Enabled",
                "REBALANCE_PERIOD"      => "Rebalance:Period",
                "REBALANCE_THRESHOLD"   => "Rebalance:Threshold",
                "LAST_REBALANCE_DATE"   => null, // DB 전용
                "KIS_SERVER"            => "Kis:Server",
                "KIS_ACCOUNT_PROD"      => "Kis:AccountProd",
                "KIS_APP_KEY"           => "Kis:AppKey",
                "KIS_APP_SECRET"        => "Kis:AppSecret",
                "KIS_ACCOUNT_NO"        => "Kis:AccountNo",
                "AI_PROVIDER"           => "Ai:Provider",
                "GEMINI_API_KEY"        => "Ai:GeminiApiKey",
                "RESEND_API_KEY"        => "Resend:ApiKey",
                "API_ACCESS_KEY"        => "Security:ApiAccessKey",
                "QUANT_WEIGHT"          => "Consensus:QuantWeight",
                "CHART_AI_WEIGHT"       => "Consensus:ChartAiWeight",
                "FUND_AI_WEIGHT"        => "Consensus:FundAiWeight",
                "BUY_THRESHOLD"         => "Consensus:BuyThreshold",
                "SELL_THRESHOLD"        => "Consensus:SellThreshold",
                "FX_ADVISOR_ENABLED"    => "FxAdvisor:Enabled",
                "FX_LOOKBACK_DAYS"      => "FxAdvisor:LookbackDays",
                "FX_HIGH_PERCENTILE"    => "FxAdvisor:HighPercentile",
                _ => null
            };

            if (mappedPath == null) return null;

            string? value = _configuration?[mappedPath];

            // bool → "1"/"0" 변환 (레거시 호환)
            if (value != null && (key == "IS_PAPER_TRADING" || key == "REBALANCE_ENABLED" || key == "FX_ADVISOR_ENABLED"))
            {
                if (bool.TryParse(value, out bool boolVal))
                {
                    return boolVal ? "1" : "0";
                }
            }

            return value;
        }
    }
}