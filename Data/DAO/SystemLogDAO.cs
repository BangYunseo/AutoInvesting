using System;
using System.Collections.Generic;
using Npgsql;

namespace AutoInvest.Data.DAO
{
    /// <summary>
    /// 시스템 로그를 PostgreSQL(TB_SYSTEM_LOG)에 영구 저장/조회하는 DAO.
    /// Render의 휘발성 파일시스템과 달리 재시작·재배포에도 로그가 보존됩니다.
    /// </summary>
    public static class SystemLogDAO
    {
        /// <summary>
        /// 로그 한 줄을 적재합니다. (Logger.DbSink로 연결되어 호출됨)
        /// </summary>
        /// <param name="when">로그 발생 시각</param>
        /// <param name="level">로그 레벨 (INFO/WARN/ERROR/FATAL)</param>
        /// <param name="message">로그 메시지</param>
        public static void Insert(DateTime when, string level, string message)
        {
            using (var conn = DBManager.Instance.GetConnection())
            using (var cmd = new NpgsqlCommand(@"
                INSERT INTO TB_SYSTEM_LOG (LOG_DATE, LOG_TIME, LEVEL, MESSAGE)
                VALUES (@date, @time, @level, @msg)", conn))
            {
                cmd.Parameters.AddWithValue("@date", when.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@time", when.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@level", level);
                cmd.Parameters.AddWithValue("@msg", message);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 특정 날짜의 로그를 최대 lines줄(최신 우선 선별 후 시간순 정렬)로 반환합니다.
        /// 각 줄은 "[시각] [LEVEL] 메시지" 형식의 문자열입니다.
        /// </summary>
        /// <param name="date">조회 날짜 (yyyy-MM-dd)</param>
        /// <param name="lines">최대 줄 수</param>
        public static List<string> GetByDate(string date, int lines)
        {
            var list = new List<string>();
            using (var conn = DBManager.Instance.GetConnection())
            using (var cmd = new NpgsqlCommand(@"
                SELECT LOG_TIME, LEVEL, MESSAGE FROM (
                    SELECT LOG_ID, LOG_TIME, LEVEL, MESSAGE FROM TB_SYSTEM_LOG
                    WHERE LOG_DATE = @date ORDER BY LOG_ID DESC LIMIT @lines
                ) sub ORDER BY LOG_ID ASC", conn))
            {
                cmd.Parameters.AddWithValue("@date", date);
                cmd.Parameters.AddWithValue("@lines", lines);
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        list.Add($"[{rdr.GetString(0)}] [{rdr.GetString(1)}] {rdr.GetString(2)}");
            }
            return list;
        }

        /// <summary>
        /// 로그가 존재하는 날짜 목록을 최신순으로 반환합니다. (yyyy-MM-dd)
        /// </summary>
        public static List<string> GetAvailableDates()
        {
            var list = new List<string>();
            using (var conn = DBManager.Instance.GetConnection())
            using (var cmd = new NpgsqlCommand(
                "SELECT DISTINCT LOG_DATE FROM TB_SYSTEM_LOG ORDER BY LOG_DATE DESC", conn))
            using (var rdr = cmd.ExecuteReader())
                while (rdr.Read())
                    list.Add(rdr.GetString(0));
            return list;
        }

        /// <summary>
        /// 지정 일수보다 오래된 로그를 삭제합니다. (무한 증가 방지 — 시작 시 1회 호출)
        /// </summary>
        /// <param name="days">보관 일수</param>
        public static void PruneOlderThan(int days)
        {
            using (var conn = DBManager.Instance.GetConnection())
            using (var cmd = new NpgsqlCommand(
                "DELETE FROM TB_SYSTEM_LOG WHERE CREATED_AT < NOW() - make_interval(days => @days)", conn))
            {
                cmd.Parameters.AddWithValue("@days", days);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
