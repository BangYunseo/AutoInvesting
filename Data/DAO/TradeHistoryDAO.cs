using AutoInvest.Data.DTO;
using System;
using System.Collections.Generic;
using Npgsql;

namespace AutoInvest.Data.DAO
{
    public static class TradeHistoryDAO
    {
        public static void Insert(TradeHistoryDto dto)
        {
            using (var conn = DBManager.Instance.GetConnection())
            using (var cmd = new NpgsqlCommand(@"
                INSERT INTO TB_TRADE_HISTORY
                    (TRADE_DATE, TICKER, ORDER_TYPE, QTY, PRICE, STATUS)
                VALUES (@date, @ticker, @type, @qty, @price, @status)", conn))
            {
                cmd.Parameters.AddWithValue("@date", dto.TradeDate.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@ticker", dto.Ticker);
                cmd.Parameters.AddWithValue("@type", dto.OrderType);
                cmd.Parameters.AddWithValue("@qty", dto.Qty);
                cmd.Parameters.AddWithValue("@price", dto.Price);
                cmd.Parameters.AddWithValue("@status", dto.Status);
                cmd.ExecuteNonQuery();
            }
        }

        public static List<TradeHistoryDto> GetRecent(int count = 50)
        {
            var list = new List<TradeHistoryDto>();
            using (var conn = DBManager.Instance.GetConnection())
            using (var cmd = new NpgsqlCommand(
                "SELECT TRADE_ID, TRADE_DATE, TICKER, ORDER_TYPE, QTY, PRICE, STATUS " +
                "FROM TB_TRADE_HISTORY ORDER BY TRADE_DATE DESC LIMIT @count", conn))
            {
                cmd.Parameters.AddWithValue("@count", count);
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        list.Add(new TradeHistoryDto
                        {
                            TradeId = rdr.GetInt32(0),
                            TradeDate = DateTime.Parse(rdr.GetString(1)),
                            Ticker = rdr.GetString(2),
                            OrderType = rdr.GetString(3),
                            Qty = rdr.GetInt32(4),
                            Price = rdr.GetDecimal(5),
                            Status = rdr.GetString(6)
                        });
            }
            return list;
        }
    }
}