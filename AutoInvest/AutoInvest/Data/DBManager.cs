using System;
using System.Data.SQLite;
using System.IO;
using AutoInvest.Utils;

namespace AutoInvest.Data
{
    public sealed class DBManager
    {
        private static readonly Lazy<DBManager> _instance =
            new Lazy<DBManager>(() => new DBManager());
        public static DBManager Instance => _instance.Value;

        private readonly string _dbPath;
        private readonly string _connStr;

        private DBManager()
        {
            _dbPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "AutoETF.db");
            _connStr = $"Data Source={_dbPath};Version=3;";
            InitializeDatabase();
        }

        public SQLiteConnection GetConnection()
        {
            var conn = new SQLiteConnection(_connStr);
            conn.Open();
            return conn; 
        }

        private void InitializeDatabase()
        {
            try
            {
                using (var conn = GetConnection())
                {
                    var sqlPath = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "Data", "sql", "create_tables.sql");

                    var sql = File.ReadAllText(sqlPath);
                    using (var cmd = new SQLiteCommand(sql, conn))
                        cmd.ExecuteNonQuery();
                }
                Logger.Info("DB 초기화 완료");
            }
            catch (Exception ex)
            {
                Logger.Fatal($"DB 초기화 실패: {ex.Message}");
                throw;
            }
        }
    }
}