using System;
using System.Data.SQLite;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using System.Collections.Generic;

namespace AutoInvest.Data.DAO
{
    public static class AiPerformanceDAO
    {
        public static void Insert(AiPerformanceDto dto)
        {
            try
            {
                using (var conn = DBManager.Instance.GetConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT INTO TB_AI_PERFORMANCE 
                        (TICKER, SIGNAL, PRICE_AT_SIGNAL) 
                        VALUES 
                        (@ticker, @signal, @priceAtSignal)";
                    
                    cmd.Parameters.AddWithValue("@ticker", dto.Ticker);
                    cmd.Parameters.AddWithValue("@signal", dto.Signal);
                    cmd.Parameters.AddWithValue("@priceAtSignal", dto.PriceAtSignal);
                    
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[DAO] TB_AI_PERFORMANCE Insert 에러: {ex.Message}");
            }
        }

        public static List<AiPerformanceDto> GetUnevaluated(int daysOld = 7)
        {
            var list = new List<AiPerformanceDto>();
            try
            {
                using (var conn = DBManager.Instance.GetConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT PERF_ID, TICKER, SIGNAL, PRICE_AT_SIGNAL, CREATED_AT 
                        FROM TB_AI_PERFORMANCE 
                        WHERE EVALUATED_AT IS NULL 
                          AND CREATED_AT <= date('now', 'localtime', @daysModifier)";
                    
                    cmd.Parameters.AddWithValue("@daysModifier", $"-{daysOld} days");

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new AiPerformanceDto
                            {
                                PerfId = Convert.ToInt32(reader["PERF_ID"]),
                                Ticker = reader["TICKER"].ToString() ?? "",
                                Signal = reader["SIGNAL"].ToString() ?? "",
                                PriceAtSignal = Convert.ToDecimal(reader["PRICE_AT_SIGNAL"]),
                                CreatedAt = Convert.ToDateTime(reader["CREATED_AT"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[DAO] TB_AI_PERFORMANCE GetUnevaluated 에러: {ex.Message}");
            }
            return list;
        }

        public static void UpdateEvaluation(int perfId, decimal priceLater, decimal winRate)
        {
            try
            {
                using (var conn = DBManager.Instance.GetConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        UPDATE TB_AI_PERFORMANCE 
                        SET PRICE_LATER = @priceLater, 
                            WIN_RATE = @winRate, 
                            EVALUATED_AT = datetime('now', 'localtime') 
                        WHERE PERF_ID = @perfId";
                    
                    cmd.Parameters.AddWithValue("@priceLater", priceLater);
                    cmd.Parameters.AddWithValue("@winRate", winRate);
                    cmd.Parameters.AddWithValue("@perfId", perfId);
                    
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[DAO] TB_AI_PERFORMANCE UpdateEvaluation 에러: {ex.Message}");
            }
        }
        
        public static (int Total, decimal AverageWinRate) GetOverallPerformance()
        {
            try
            {
                using (var conn = DBManager.Instance.GetConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT COUNT(*), AVG(WIN_RATE) 
                        FROM TB_AI_PERFORMANCE 
                        WHERE EVALUATED_AT IS NOT NULL";
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int total = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader[0]);
                            decimal avg = reader.IsDBNull(1) ? 0m : Convert.ToDecimal(reader[1]);
                            return (total, avg);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[DAO] TB_AI_PERFORMANCE GetOverallPerformance 에러: {ex.Message}");
            }
            return (0, 0m);
        }
    }
}
