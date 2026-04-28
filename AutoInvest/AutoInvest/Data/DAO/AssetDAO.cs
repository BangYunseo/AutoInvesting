using AutoInvest.Data.DTO;
using System.Collections.Generic;
using System.Data.SQLite;

namespace AutoInvest.Data.DAO
{
    public static class AssetDAO
    {
        public static List<AssetDto> GetActiveAssets()
        {
            var list = new List<AssetDto>();
            using (var conn = DBManager.Instance.GetConnection())
            using (var cmd = new SQLiteCommand(
                "SELECT TICKER, NAME, CURRENCY FROM TB_ASSET_MASTER WHERE IS_ACTIVE=1", conn))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                    list.Add(new AssetDto
                    {
                        Ticker = rdr.GetString(0),
                        Name = rdr.GetString(1),
                        Currency = rdr.GetString(2)
                    });
            }
            return list;
        }
    }
}