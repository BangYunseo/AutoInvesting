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

                    // Phase 2.5 마이그레이션: STRATEGY_TYPE 컬럼 추가
                    RunMigration(conn,
                        "ALTER TABLE TB_INVEST_STRATEGY ADD COLUMN STRATEGY_TYPE TEXT DEFAULT 'MEAN_REVERSION'");

                    // Phase 4-e 마이그레이션: 확률 기반 합의 점수 컬럼 추가
                    RunMigration(conn,
                        "ALTER TABLE TB_MARKET_SNAPSHOT ADD COLUMN BUY_PROBABILITY REAL DEFAULT 0");
                    RunMigration(conn,
                        "ALTER TABLE TB_MARKET_SNAPSHOT ADD COLUMN SELL_PROBABILITY REAL DEFAULT 0");
                    RunMigration(conn,
                        "ALTER TABLE TB_MARKET_SNAPSHOT ADD COLUMN CHART_AI_SCORE REAL DEFAULT 0");
                    RunMigration(conn,
                        "ALTER TABLE TB_MARKET_SNAPSHOT ADD COLUMN FUND_AI_SCORE REAL DEFAULT 0");
                }
                Logger.Info("DB 초기화 완료");
            }
            catch (Exception ex)
            {
                Logger.Fatal($"DB 초기화 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// DB 마이그레이션 쿼리를 실행합니다. 이미 적용된 경우 무시합니다.
        /// </summary>
        private void RunMigration(SQLiteConnection conn, string sql)
        {
            try
            {
                using (var cmd = new SQLiteCommand(sql, conn))
                    cmd.ExecuteNonQuery();
            }
            catch (SQLiteException)
            {
                // 이미 컬럼이 존재하는 경우 등 — 무시
            }
        }
    }
}