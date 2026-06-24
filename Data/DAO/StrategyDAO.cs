using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using System.Collections.Generic;
using Npgsql;

namespace AutoInvest.Data.DAO
{
    public static class StrategyDAO
    {
        public static List<StrategyDto> GetStrategy(string strategyName)
        {
            var list = new List<StrategyDto>();
            using (var conn = DBManager.Instance.GetConnection())
            using (var cmd = new NpgsqlCommand(
                "SELECT STRATEGY_ID, STRATEGY_NAME, TICKER, CAST(WEIGHT AS INTEGER) AS QTY, " +
                "COALESCE(STRATEGY_TYPE, 'MEAN_REVERSION') AS STRATEGY_TYPE " +
                "FROM TB_INVEST_STRATEGY WHERE STRATEGY_NAME=@name", conn))
            {
                cmd.Parameters.AddWithValue("@name", strategyName);
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        list.Add(new StrategyDto
                        {
                            StrategyId = rdr.GetInt32(0),
                            StrategyName = rdr.GetString(1),
                            Ticker = rdr.GetString(2),
                            Qty = rdr.GetInt32(3),
                            StrategyType = rdr.GetString(4)
                        });
            }
            return list;
        }

        public static List<StrategySummaryDto> GetStrategySummaries()
        {
            var list = new List<StrategySummaryDto>();
            using (var conn = DBManager.Instance.GetConnection())
            using (var cmd = new NpgsqlCommand(
                "SELECT STRATEGY_NAME, MAX(STRATEGY_TYPE), COUNT(TICKER) " +
                "FROM TB_INVEST_STRATEGY GROUP BY STRATEGY_NAME", conn))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    list.Add(new StrategySummaryDto
                    {
                        StrategyName = rdr.GetString(0),
                        StrategyType = rdr.IsDBNull(1) ? "MEAN_REVERSION" : rdr.GetString(1),
                        TickerCount = rdr.GetInt32(2)
                    });
                }
            }
            return list;
        }

        /// <summary>
        /// 자산 마스터(TB_ASSET_MASTER)의 전체 종목 목록을 조회합니다. 전략에 편입 가능한 허용 종목입니다.
        /// </summary>
        public static List<AssetMasterDto> GetAssetMaster()
        {
            var list = new List<AssetMasterDto>();
            using (var conn = DBManager.Instance.GetConnection())
            using (var cmd = new NpgsqlCommand(
                "SELECT TICKER, NAME, CURRENCY, IS_ACTIVE FROM TB_ASSET_MASTER ORDER BY TICKER", conn))
            using (var rdr = cmd.ExecuteReader())
                while (rdr.Read())
                    list.Add(new AssetMasterDto
                    {
                        Ticker = rdr.GetString(0),
                        Name = rdr.GetString(1),
                        Currency = rdr.GetString(2),
                        IsActive = rdr.GetInt32(3) == 1
                    });
            return list;
        }

        /// <summary>
        /// 전략을 저장합니다. 동일 이름의 기존 전략을 삭제 후 새로 INSERT.
        /// </summary>
        public static void SaveStrategy(string strategyName, List<StrategyDto> items)
        {
            using (var conn = DBManager.Instance.GetConnection())
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    // 기존 전략 삭제
                    using (var delCmd = new NpgsqlCommand(
                        "DELETE FROM TB_INVEST_STRATEGY WHERE STRATEGY_NAME=@name", conn, tx))
                    {
                        delCmd.Parameters.AddWithValue("@name", strategyName);
                        delCmd.ExecuteNonQuery();
                    }

                    // 새 전략 INSERT
                    foreach (var item in items)
                    {
                        // ── 자산 마스터 선등록 (FK 제약 충족: TB_INVEST_STRATEGY.TICKER → TB_ASSET_MASTER.TICKER) ──
                        //    종목이 자산 마스터에 없으면 INSERT가 FK 위반(23503)으로 실패하므로, 먼저 upsert로 보장한다.
                        //    표시명(NAME)을 알 수 없으므로 우선 티커로 채운다(NOT NULL 충족).
                        using (var assetCmd = new NpgsqlCommand(
                            "INSERT INTO TB_ASSET_MASTER (TICKER, NAME) VALUES (@ticker, @name) " +
                            "ON CONFLICT (TICKER) DO NOTHING", conn, tx))
                        {
                            assetCmd.Parameters.AddWithValue("@ticker", item.Ticker);
                            assetCmd.Parameters.AddWithValue("@name", item.Ticker);
                            assetCmd.ExecuteNonQuery();
                        }

                        using (var insCmd = new NpgsqlCommand(
                            "INSERT INTO TB_INVEST_STRATEGY (STRATEGY_NAME, TICKER, WEIGHT, STRATEGY_TYPE) " +
                            "VALUES (@name, @ticker, @qty, @type)", conn, tx))
                        {
                            insCmd.Parameters.AddWithValue("@name", strategyName);
                            insCmd.Parameters.AddWithValue("@ticker", item.Ticker);
                            insCmd.Parameters.AddWithValue("@qty", item.Qty);
                            insCmd.Parameters.AddWithValue("@type", item.StrategyType ?? "MEAN_REVERSION");
                            insCmd.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                    Logger.Info($"전략 저장 완료: {strategyName} ({items.Count}종목)");
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// 전략을 삭제합니다.
        /// </summary>
        public static void DeleteStrategy(string strategyName)
        {
            using (var conn = DBManager.Instance.GetConnection())
            using (var cmd = new NpgsqlCommand(
                "DELETE FROM TB_INVEST_STRATEGY WHERE STRATEGY_NAME=@name", conn))
            {
                cmd.Parameters.AddWithValue("@name", strategyName);
                cmd.ExecuteNonQuery();
            }
            Logger.Info($"전략 삭제: {strategyName}");
        }
    }
}