using AutoInvest.Data.DTO;
using System;
using System.Collections.Generic;
using Npgsql;

namespace AutoInvest.Data.DAO
{
    /// <summary>
    /// TB_TRADE_HISTORY 기록·조회.
    ///
    /// <c>ORDER_NO</c>(증권사 주문번호, KIS의 <c>ODNO</c>)는 우리 기록과 증권사 계좌의 주문을 잇는
    /// 유일한 키다. 같은 날 같은 종목을 두 번 매수하면(적립 사이클 + 수동 주문) 이 값 없이는 구분이
    /// 안 되고, 미체결·분쟁 조회의 실마리도 이 값뿐이다.
    /// </summary>
    public static class TradeHistoryDAO
    {
        /// <summary>
        /// 거래 1건을 기록합니다. 주문번호가 빈 값이면 NULL로 저장해 "주문번호 없음"과 빈 문자열을 구분합니다.
        /// </summary>
        public static void Insert(TradeHistoryDto dto)
        {
            using (var conn = DBManager.Instance.GetConnection())
            using (var cmd = new NpgsqlCommand(@"
                INSERT INTO TB_TRADE_HISTORY
                    (TRADE_DATE, TICKER, ORDER_TYPE, QTY, PRICE, STATUS, ORDER_NO)
                VALUES (@date, @ticker, @type, @qty, @price, @status, @orderNo)", conn))
            {
                cmd.Parameters.AddWithValue("@date", dto.TradeDate.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@ticker", dto.Ticker);
                cmd.Parameters.AddWithValue("@type", dto.OrderType);
                cmd.Parameters.AddWithValue("@qty", dto.Qty);
                cmd.Parameters.AddWithValue("@price", dto.Price);
                cmd.Parameters.AddWithValue("@status", dto.Status);
                cmd.Parameters.AddWithValue("@orderNo",
                    string.IsNullOrWhiteSpace(dto.OrderNo) ? (object)DBNull.Value : dto.OrderNo);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 주문번호로 거래 1건의 상태를 갱신합니다 (체결 대사용).
        ///
        /// 주문 시점에는 접수만 확인되므로 <c>PENDING</c>으로 적재되고, 장 마감 후 대사에서
        /// 실제 체결 여부가 확인되면 이 메서드로 <c>FILLED</c>/<c>PARTIAL</c>/<c>FAILED</c>로 바꾼다.
        /// 주문번호가 비어 있으면(ODNO 미수신) 매칭할 키가 없으므로 아무것도 하지 않는다.
        /// </summary>
        /// <param name="orderNo">증권사 주문번호(ODNO)</param>
        /// <param name="status">갱신할 상태</param>
        /// <returns>갱신된 행 수</returns>
        public static int UpdateStatusByOrderNo(string orderNo, string status)
        {
            if (string.IsNullOrWhiteSpace(orderNo)) return 0;

            using (var conn = DBManager.Instance.GetConnection())
            using (var cmd = new NpgsqlCommand(
                "UPDATE TB_TRADE_HISTORY SET STATUS=@s WHERE ORDER_NO=@o", conn))
            {
                cmd.Parameters.AddWithValue("@s", status);
                cmd.Parameters.AddWithValue("@o", orderNo);
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 최근 거래 내역을 조회합니다.
        /// 2026-07-30 배선 이전에 적재된 행은 <c>ORDER_NO</c>가 NULL이므로 빈 문자열로 읽습니다.
        /// </summary>
        /// <param name="count">최대 조회 건수</param>
        public static List<TradeHistoryDto> GetRecent(int count = 50)
        {
            var list = new List<TradeHistoryDto>();
            using (var conn = DBManager.Instance.GetConnection())
            using (var cmd = new NpgsqlCommand(
                "SELECT TRADE_ID, TRADE_DATE, TICKER, ORDER_TYPE, QTY, PRICE, STATUS, ORDER_NO " +
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
                            Status = rdr.GetString(6),
                            OrderNo = rdr.IsDBNull(7) ? string.Empty : rdr.GetString(7)
                        });
            }
            return list;
        }
    }
}