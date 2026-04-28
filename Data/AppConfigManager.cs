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