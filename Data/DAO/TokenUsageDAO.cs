using System;
using Npgsql;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using System.Collections.Generic;

namespace AutoInvest.Data.DAO
{
    public static class TokenUsageDAO
    {
        public static void Insert(TokenUsageDto dto)
        {
            try
            {
                using (var conn = DBManager.Instance.GetConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT INTO TB_TOKEN_USAGE 
                        (TICKER, AGENT_TYPE, PROMPT_TOKENS, COMP_TOKENS, TOTAL_TOKENS) 
                        VALUES 
                        (@ticker, @agentType, @promptTokens, @compTokens, @totalTokens)";
                    
                    cmd.Parameters.AddWithValue("@ticker", dto.Ticker);
                    cmd.Parameters.AddWithValue("@agentType", dto.AgentType);
                    cmd.Parameters.AddWithValue("@promptTokens", dto.PromptTokens);
                    cmd.Parameters.AddWithValue("@compTokens", dto.CompletionTokens);
                    cmd.Parameters.AddWithValue("@totalTokens", dto.TotalTokens);
                    
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[DAO] TB_TOKEN_USAGE Insert 에러: {ex.Message}");
            }
        }

        public static int GetTodayTotalTokens()
        {
            try
            {
                using (var conn = DBManager.Instance.GetConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT SUM(TOTAL_TOKENS)
                        FROM TB_TOKEN_USAGE
                        WHERE DATE(CREATED_AT) = CURRENT_DATE";

                    var result = cmd.ExecuteScalar();
                    if (result != DBNull.Value && result != null)
                    {
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[DAO] TB_TOKEN_USAGE Select 에러: {ex.Message}");
            }
            return 0;
        }

        /// <summary>
        /// 최근 N일간 프롬프트/완성 토큰 합계를 반환합니다 (비용 추정용).
        /// </summary>
        public static (long PromptTokens, long CompletionTokens) GetTokenSums(int days)
        {
            try
            {
                using (var conn = DBManager.Instance.GetConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT COALESCE(SUM(PROMPT_TOKENS), 0), COALESCE(SUM(COMP_TOKENS), 0)
                        FROM TB_TOKEN_USAGE
                        WHERE CREATED_AT >= CURRENT_DATE - (@days * INTERVAL '1 day')";

                    cmd.Parameters.AddWithValue("@days", days);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            long prompt = reader.IsDBNull(0) ? 0L : Convert.ToInt64(reader[0]);
                            long comp = reader.IsDBNull(1) ? 0L : Convert.ToInt64(reader[1]);
                            return (prompt, comp);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[DAO] TB_TOKEN_USAGE GetTokenSums 에러: {ex.Message}");
            }
            return (0L, 0L);
        }

        /// <summary>
        /// 최근 N일간 에이전트 유형별 토큰 사용량 집계를 반환합니다.
        /// </summary>
        public static List<AgentTokenSummaryDto> GetUsageByAgent(int days)
        {
            var list = new List<AgentTokenSummaryDto>();
            try
            {
                using (var conn = DBManager.Instance.GetConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT AGENT_TYPE,
                               COUNT(*) AS CALL_COUNT,
                               COALESCE(SUM(PROMPT_TOKENS), 0) AS PROMPT_SUM,
                               COALESCE(SUM(COMP_TOKENS), 0) AS COMP_SUM,
                               COALESCE(SUM(TOTAL_TOKENS), 0) AS TOTAL_SUM
                        FROM TB_TOKEN_USAGE
                        WHERE CREATED_AT >= CURRENT_DATE - (@days * INTERVAL '1 day')
                        GROUP BY AGENT_TYPE
                        ORDER BY TOTAL_SUM DESC";

                    cmd.Parameters.AddWithValue("@days", days);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new AgentTokenSummaryDto
                            {
                                AgentType = reader["AGENT_TYPE"].ToString() ?? "",
                                CallCount = Convert.ToInt32(reader["CALL_COUNT"]),
                                PromptTokens = Convert.ToInt64(reader["PROMPT_SUM"]),
                                CompletionTokens = Convert.ToInt64(reader["COMP_SUM"]),
                                TotalTokens = Convert.ToInt64(reader["TOTAL_SUM"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[DAO] TB_TOKEN_USAGE GetUsageByAgent 에러: {ex.Message}");
            }
            return list;
        }

        /// <summary>
        /// 최근 N일간 일자별 토큰 사용량 집계를 반환합니다 (최신순).
        /// </summary>
        public static List<DailyTokenUsageDto> GetDailyUsage(int days)
        {
            var list = new List<DailyTokenUsageDto>();
            try
            {
                using (var conn = DBManager.Instance.GetConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT TO_CHAR(DATE(CREATED_AT), 'YYYY-MM-DD') AS USE_DATE,
                               COUNT(*) AS CALL_COUNT,
                               COALESCE(SUM(PROMPT_TOKENS), 0) AS PROMPT_SUM,
                               COALESCE(SUM(COMP_TOKENS), 0) AS COMP_SUM,
                               COALESCE(SUM(TOTAL_TOKENS), 0) AS TOTAL_SUM
                        FROM TB_TOKEN_USAGE
                        WHERE CREATED_AT >= CURRENT_DATE - (@days * INTERVAL '1 day')
                        GROUP BY DATE(CREATED_AT)
                        ORDER BY DATE(CREATED_AT) DESC";

                    cmd.Parameters.AddWithValue("@days", days);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new DailyTokenUsageDto
                            {
                                Date = reader["USE_DATE"].ToString() ?? "",
                                CallCount = Convert.ToInt32(reader["CALL_COUNT"]),
                                PromptTokens = Convert.ToInt64(reader["PROMPT_SUM"]),
                                CompletionTokens = Convert.ToInt64(reader["COMP_SUM"]),
                                TotalTokens = Convert.ToInt64(reader["TOTAL_SUM"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[DAO] TB_TOKEN_USAGE GetDailyUsage 에러: {ex.Message}");
            }
            return list;
        }
    }
}
