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
    }
}
