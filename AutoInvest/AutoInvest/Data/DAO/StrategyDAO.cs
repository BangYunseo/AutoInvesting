using AutoInvest.Data.DTO;
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
                "SELECT STRATEGY_ID, STRATEGY_NAME, TICKER, WEIGHT " +
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
                            Weight = rdr.GetDouble(3)
                        });
            }
            return list;
        }
    }
}