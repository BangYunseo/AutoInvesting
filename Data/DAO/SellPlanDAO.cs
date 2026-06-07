using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using System;
using System.Collections.Generic;
using Npgsql;

namespace AutoInvest.Data.DAO
{
    public static class SellPlanDAO
    {
        public static List<SellPlanDto> GetAllActivePlans()
        {
            var list = new List<SellPlanDto>();
            string sql = "SELECT * FROM TB_SELL_PLAN WHERE STATUS = 'ACTIVE'";

            try
            {
                using var conn = DBManager.Instance.GetConnection();
                using var cmd = new NpgsqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(MapFromReader(reader));
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[SellPlanDAO] GetAllActivePlans 에러: {ex.Message}");
            }

            return list;
        }

        public static List<SellPlanDto> GetPlansByTicker(string ticker)
        {
            var list = new List<SellPlanDto>();
            string sql = "SELECT * FROM TB_SELL_PLAN WHERE TICKER = @ticker ORDER BY CREATED_AT DESC";

            try
            {
                using var conn = DBManager.Instance.GetConnection();
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ticker", ticker);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(MapFromReader(reader));
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[SellPlanDAO] GetPlansByTicker 에러: {ex.Message}");
            }

            return list;
        }

        public static int Insert(SellPlanDto dto)
        {
            string sql = @"
                INSERT INTO TB_SELL_PLAN (TICKER, STRATEGY_TYPE, TARGET_QTY, SOLD_QTY, STATUS, PARAMS)
                VALUES (@ticker, @strategyType, @targetQty, @soldQty, @status, @params)
                RETURNING PLAN_ID;";

            try
            {
                using var conn = DBManager.Instance.GetConnection();
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ticker", dto.Ticker);
                cmd.Parameters.AddWithValue("@strategyType", dto.StrategyType);
                cmd.Parameters.AddWithValue("@targetQty", dto.TargetQty);
                cmd.Parameters.AddWithValue("@soldQty", dto.SoldQty);
                cmd.Parameters.AddWithValue("@status", dto.Status);
                cmd.Parameters.AddWithValue("@params", dto.Params);
                
                var result = cmd.ExecuteScalar();
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Logger.Error($"[SellPlanDAO] Insert 에러: {ex.Message}");
                return 0;
            }
        }

        public static void Update(SellPlanDto dto)
        {
            string sql = @"
                UPDATE TB_SELL_PLAN 
                SET SOLD_QTY = @soldQty, STATUS = @status, PARAMS = @params
                WHERE PLAN_ID = @planId";

            try
            {
                using var conn = DBManager.Instance.GetConnection();
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@soldQty", dto.SoldQty);
                cmd.Parameters.AddWithValue("@status", dto.Status);
                cmd.Parameters.AddWithValue("@params", dto.Params);
                cmd.Parameters.AddWithValue("@planId", dto.PlanId);
                
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.Error($"[SellPlanDAO] Update 에러: {ex.Message}");
            }
        }

        private static SellPlanDto MapFromReader(NpgsqlDataReader reader)
        {
            return new SellPlanDto
            {
                PlanId = Convert.ToInt32(reader["PLAN_ID"]),
                Ticker = reader["TICKER"].ToString() ?? "",
                StrategyType = reader["STRATEGY_TYPE"].ToString() ?? "",
                TargetQty = Convert.ToInt32(reader["TARGET_QTY"]),
                SoldQty = Convert.ToInt32(reader["SOLD_QTY"]),
                Status = reader["STATUS"].ToString() ?? "",
                Params = reader["PARAMS"].ToString() ?? "{}",
                CreatedAt = DateTime.Parse(reader["CREATED_AT"].ToString() ?? DateTime.Now.ToString())
            };
        }
    }
}
