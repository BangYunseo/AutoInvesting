using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using System.Collections.Generic;
using System.Data.SQLite;

namespace AutoInvest.Data.DAO
{
    public static class StrategyDAO
    {
        public static List<StrategyDto> GetStrategy(string strategyName)
        {
            var list = new List<StrategyDto>();
            using (var conn = DBManager.Instance.GetConnection())
            using (var cmd = new SQLiteCommand(
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
                    using (var delCmd = new SQLiteCommand(
                        "DELETE FROM TB_INVEST_STRATEGY WHERE STRATEGY_NAME=@name", conn, tx))
                    {
                        delCmd.Parameters.AddWithValue("@name", strategyName);
                        delCmd.ExecuteNonQuery();
                    }

                    // 새 전략 INSERT
                    foreach (var item in items)
                    {
                        using (var insCmd = new SQLiteCommand(
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
            using (var cmd = new SQLiteCommand(
                "DELETE FROM TB_INVEST_STRATEGY WHERE STRATEGY_NAME=@name", conn))
            {
                cmd.Parameters.AddWithValue("@name", strategyName);
                cmd.ExecuteNonQuery();
            }
            Logger.Info($"전략 삭제: {strategyName}");
        }
    }
}