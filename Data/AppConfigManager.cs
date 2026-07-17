using System;
using System.Collections.Generic;
using Npgsql;
using AutoInvest.Utils;
using Microsoft.Extensions.Configuration;

namespace AutoInvest.Data
{
    /// <summary>
    /// 애플리케이션 설정값 통합 관리
    /// 우선순위 : 환경변수 > DB 테이블(TB_APP_CONFIG) > appsettings.json
    /// 민감정보 : (KIS_APP_KEY, KIS_APP_SECRET, KIS_ACCOUNT_NO) -> 환경변수 전용
    /// DB 테이블 : 런타임에 UI 저장값 보관, appsettings.json 기본값 덮어쓰기(Always)
    /// </summary>
    public static class AppConfigManager
    {
        private static IConfiguration? _configuration;

        /// <summary>
        /// DB 테이블(TB_APP_CONFIG) 민감 키 목록
        /// (1) 저장 시 암호화 
        /// (2) 조회 시 복호화
        /// 환경변수는 그대로 사용(암호화/복호화 불필요)
        /// </summary>
        private static readonly HashSet<string> SensitiveKeys = new(StringComparer.Ordinal)
        {
            "KIS_APP_KEY",
            "KIS_APP_SECRET",
            "KIS_ACCOUNT_NO",
            "RESEND_API_KEY",
            "API_ACCESS_KEY"
        };

        /// <summary>
        /// ASP.NET Core IConfiguration 주입
        /// </summary>
        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
            if (configuration != null)
            {
                Logger.Info("[AppConfig] AppConfigManager 초기화 완료 : appsettings.json/IConfiguration 설정 등록");
            }
            else
            {
                Logger.Warn("[AppConfig] AppConfigManager 초기화 경고 : appsettings.json/IConfiguration 설정 등록 실패");
            }
        }

        /// <summary>
        /// 설정값 조회
        /// 우선순위 : 환경변수 > DB 테이블(TB_APP_CONFIG) > appsettings.json > 기본값.
        /// </summary>
        public static string Get(string key, string defaultValue = "")
        {
            try
            {
                // 환경변수 (최우선 — 배포 환경 실제 값)
                string? envValue = Environment.GetEnvironmentVariable(key);
                if (!string.IsNullOrEmpty(envValue)) return envValue;

                // DB 테이블(TB_APP_CONFIG) — DB 장애 시 null 반환 -> appsettings.json 기본값 폴백
                string? dbValue = TryGetFromDb(key);
                if (!string.IsNullOrEmpty(dbValue)) return dbValue;

                // appsettings.json (키 매핑: IS_PAPER_TRADING → Trading:IsPaperTrading 등)
                if (_configuration != null)
                {
                    string? configValue = ResolveFromConfiguration(key);
                    if (!string.IsNullOrEmpty(configValue)) return configValue;
                }

                // 기본값
                return defaultValue;
            }
            catch (Exception ex)
            {
                Logger.Error($"[AppConfig] 조회 실패 [{key}]: {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// TB_APP_CONFIG 설정값 조회
        /// 특정 상황[(1), (2)] 발생 시 null 반환 -> appsettings.json 기본값으로 폴백
        /// (1) 행 없음 
        /// (2) DB 오류
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
                    string? value = cmd.ExecuteScalar()?.ToString();

                    // (1) 암호문(enc:v1:...) -> 복호화 
                    // (2) 평문/비민감 키 -> 통과
                    if (CryptoUtil.IsEncrypted(value ?? string.Empty)) 
                    { 
                        return CryptoUtil.DecryptSecret(value!);
                    }

                    return value;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"[AppConfig] DB 조회 실패 -> 기본 설정 폴백 [{key}]: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 설정값 저장 (TB_APP_CONFIG)
        /// </summary>
        public static void Set(string key, string value)
        {
            try
            {
                // 민감 키의 경우 저장 직전 암호화
                string storedValue = value;
                if (SensitiveKeys.Contains(key) && !string.IsNullOrEmpty(value))
                {
                    if (CryptoUtil.IsConfigured)
                    {
                        storedValue = CryptoUtil.EncryptSecret(value);
                    }
                    else
                    {
                        Logger.Warn($"[AppConfig] MASTER_KEY 미설정 \n\t: 민감 키 평문 저장 [{key}]\n\tMASTER_KEY 설정 요청");
                    }
                }

                using (var conn = DBManager.Instance.GetConnection())
                using (var cmd = new NpgsqlCommand(
                    "UPDATE TB_APP_CONFIG SET CONFIG_VALUE=@v WHERE CONFIG_KEY=@k", conn))
                {
                    cmd.Parameters.AddWithValue("@v", storedValue);
                    cmd.Parameters.AddWithValue("@k", key);
                    int affected = cmd.ExecuteNonQuery();

                    if (affected == 0)
                    {
                        // 키가 없을 경우 INSERT
                        using var insertCmd = new NpgsqlCommand(
                            "INSERT INTO TB_APP_CONFIG (CONFIG_KEY, CONFIG_VALUE) VALUES (@k, @v)", conn);
                        insertCmd.Parameters.AddWithValue("@k", key);
                        insertCmd.Parameters.AddWithValue("@v", storedValue);
                        insertCmd.ExecuteNonQuery();
                    }
                }

                // 민감 키 : 키 이름만 기록
                if (SensitiveKeys.Contains(key))
                    Logger.Info($"[AppConfig] 저장: {key} = ****...");
                else
                    Logger.Info($"[AppConfig] 저장: {key} = {value}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[AppConfig] 저장 실패 [{key}]: {ex.Message}");
            }
        }

        /// <summary>
        /// appsettings.json의 특정 섹션 조회(키/값 딕셔너리)
        /// 값이 있는 직속 하위 항목만 포함
        /// 섹션이 없을 경우 빈 딕셔너리 반환
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
                    {
                        map[child.Key] = child.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[AppConfig] 섹션 조회 실패 [{path}]: {ex.Message}");
            }
            return map;
        }

        /// <summary>
        /// 레거시 키 -> 계층 구조 키 매핑
        /// </summary>
        private static string? ResolveFromConfiguration(string key)
        {
            // 레거시 키 → appsettings.json 경로 매핑
            string? mappedPath = key switch
            {
                "IS_PAPER_TRADING"      => "Trading:IsPaperTrading",
                "KIS_SERVER"            => "Kis:Server",
                "KIS_ACCOUNT_PROD"      => "Kis:AccountProd",
                "KIS_APP_KEY"           => "Kis:AppKey",
                "KIS_APP_SECRET"        => "Kis:AppSecret",
                "KIS_ACCOUNT_NO"        => "Kis:AccountNo",
                "RESEND_API_KEY"        => "Resend:ApiKey",
                "API_ACCESS_KEY"        => "Security:ApiAccessKey",
                _ => null
            };

            if (mappedPath == null) return null;

            string? value = _configuration?[mappedPath];

            // bool → "1"/"0" 변환 (레거시 호환)
            if (value != null && key == "IS_PAPER_TRADING")
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