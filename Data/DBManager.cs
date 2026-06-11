using System;
using Npgsql;
using System.IO;
using AutoInvest.Utils;

namespace AutoInvest.Data
{
    public sealed class DBManager
    {
        private static readonly Lazy<DBManager> _instance =
            new Lazy<DBManager>(() => new DBManager());
        public static DBManager Instance => _instance.Value;

        private readonly string _connStr;

        private DBManager()
        {
            _connStr = GetConnectionString();
            InitializeDatabase();
        }

        private string GetConnectionString()
        {
            var envUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
            if (string.IsNullOrEmpty(envUrl))
            {
                // 로컬 개발용 기본 접속 정보
                return "Host=localhost;Username=postgres;Password=postgres;Database=autoinvest";
            }

            // Render.com 등에서 제공하는 URI 형식 (postgres://user:pass@host:port/db) 파싱
            if (envUrl.StartsWith("postgres://") || envUrl.StartsWith("postgresql://"))
            {
                var uri = new Uri(envUrl);
                var userInfo = uri.UserInfo.Split(':');
                return $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};Username={userInfo[0]};Password={(userInfo.Length > 1 ? userInfo[1] : "")};Database={uri.LocalPath.TrimStart('/')};SslMode=Require;Trust Server Certificate=true;";
            }

            return envUrl;
        }

        public NpgsqlConnection GetConnection()
        {
            var conn = new NpgsqlConnection(_connStr);
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
                    using (var cmd = new NpgsqlCommand(sql, conn))
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

                    // Phase 5-d 마이그레이션: 에이전트별 방향 신호 컬럼 추가 (적중률/가중치 A/B 분석용)
                    RunMigration(conn,
                        "ALTER TABLE TB_MARKET_SNAPSHOT ADD COLUMN QUANT_SIGNAL TEXT DEFAULT ''");
                    RunMigration(conn,
                        "ALTER TABLE TB_MARKET_SNAPSHOT ADD COLUMN CHART_AI_SIGNAL TEXT DEFAULT ''");
                    RunMigration(conn,
                        "ALTER TABLE TB_MARKET_SNAPSHOT ADD COLUMN FUND_AI_SIGNAL TEXT DEFAULT ''");
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
        private void RunMigration(NpgsqlConnection conn, string sql)
        {
            try
            {
                using (var cmd = new NpgsqlCommand(sql, conn))
                    cmd.ExecuteNonQuery();
            }
            catch (PostgresException)
            {
                // 이미 컬럼이 존재하는 경우 등 — 무시
            }
        }
    }
}