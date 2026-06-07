using AutoInvest.Data.DTO;
using System.Collections.Generic;
using Npgsql;

namespace AutoInvest.Data.DAO
{
    /// <summary>
    /// 자산 마스터 DAO.
    /// TB_ASSET_MASTER 테이블에서 활성 상태인 투자 대상 ETF 목록을 조회합니다.
    /// </summary>
    public static class AssetDAO
    {
        /// <summary>
        /// 활성 상태(IS_ACTIVE=1)인 전체 자산 목록을 조회합니다.
        /// </summary>
        /// <returns>활성 자산 DTO 리스트</returns>
        public static List<AssetDto> GetActiveAssets()
        {
            var list = new List<AssetDto>();

            // DB 연결 → SELECT → DTO 변환
            using (var conn = DBManager.Instance.GetConnection())
            using (var cmd = new NpgsqlCommand(
                "SELECT TICKER, NAME, CURRENCY FROM TB_ASSET_MASTER WHERE IS_ACTIVE=1", conn))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                    list.Add(new AssetDto
                    {
                        Ticker = rdr.GetString(0),   // 종목 코드
                        Name = rdr.GetString(1),      // 종목명
                        Currency = rdr.GetString(2)   // 거래 통화
                    });
            }

            return list;
        }
    }
}