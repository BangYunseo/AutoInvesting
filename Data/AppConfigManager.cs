using System.Configuration;
using System.Data.SQLite;
using AutoInvest.Utils;

namespace AutoInvest.Data
{
    public static class AppConfigManager
    {
        public static string Get(string key, string defaultValue = "")
        {
            try
            {
                // 1. App.config 또는 환경변수 우선 확인 (보안 규칙)
                string? envValue = System.Environment.GetEnvironmentVariable(key);
                if (!string.IsNullOrEmpty(envValue)) return envValue;

                string? appSettingValue = ConfigurationManager.AppSettings[key];
                if (!string.IsNullOrEmpty(appSettingValue)) return appSettingValue;

                // 2. DB 조회 (기존 방식)
                using (var conn = DBManager.Instance.GetConnection())
                using (var cmd = new SQLiteCommand(
                    "SELECT CONFIG_VALUE FROM TB_APP_CONFIG WHERE CONFIG_KEY=@k", conn))
                {
                    cmd.Parameters.AddWithValue("@k", key);
                    var result = cmd.ExecuteScalar();
                    return result?.ToString() ?? defaultValue;
                }
            }
            catch (System.Exception ex)
            {
                Logger.Error($"Config 조회 실패 [{key}]: {ex.Message}");
                return defaultValue;
            }
        }

        public static void Set(string key, string value)
        {
            try
            {
                using (var conn = DBManager.Instance.GetConnection())
                using (var cmd = new SQLiteCommand(
                    "UPDATE TB_APP_CONFIG SET CONFIG_VALUE=@v WHERE CONFIG_KEY=@k", conn))
                {
                    cmd.Parameters.AddWithValue("@v", value);
                    cmd.Parameters.AddWithValue("@k", key);
                    cmd.ExecuteNonQuery();
                }
                Logger.Info($"Config 저장: {key} = {value}");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"Config 저장 실패 [{key}]: {ex.Message}");
            }
        }
    }
}