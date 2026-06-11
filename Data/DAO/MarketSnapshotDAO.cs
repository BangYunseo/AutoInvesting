using AutoInvest.Data.DTO;
using System;
using System.Collections.Generic;
using Npgsql;

namespace AutoInvest.Data.DAO
{
    /// <summary>
    /// 시장 스냅샷 DAO.
    /// 매매 시점의 퀀트 지표값을 TB_MARKET_SNAPSHOT에 저장/조회합니다.
    /// Phase 4 AI 학습 데이터의 원본이 됩니다.
    /// </summary>
    public static class MarketSnapshotDAO
    {
        /// <summary>
        /// 시장 스냅샷을 저장합니다.
        /// </summary>
        public static void Insert(MarketSnapshotDto dto)
        {
            using (var conn = DBManager.Instance.GetConnection())
            using (var cmd = new NpgsqlCommand(@"
                INSERT INTO TB_MARKET_SNAPSHOT
                    (SNAP_DATE, TICKER, PRICE, POSITION_20D, RSI_14,
                     MACD_VALUE, MACD_SIGNAL, BB_UPPER, BB_LOWER, SIGNAL,
                     BUY_PROBABILITY, SELL_PROBABILITY, CHART_AI_SCORE, FUND_AI_SCORE,
                     QUANT_SIGNAL, CHART_AI_SIGNAL, FUND_AI_SIGNAL)
                VALUES
                    (@snapDate, @ticker, @price, @position, @rsi,
                     @macdValue, @macdSignal, @bbUpper, @bbLower, @signal,
                     @buyProb, @sellProb, @chartAi, @fundAi,
                     @quantSignal, @chartAiSignal, @fundAiSignal)", conn))
            {
                cmd.Parameters.AddWithValue("@snapDate", dto.SnapDate.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@ticker", dto.Ticker);
                cmd.Parameters.AddWithValue("@price", (double)dto.Price);
                cmd.Parameters.AddWithValue("@position", (double)dto.Position20d);
                cmd.Parameters.AddWithValue("@rsi", (double)dto.Rsi14);
                cmd.Parameters.AddWithValue("@macdValue", (double)dto.MacdValue);
                cmd.Parameters.AddWithValue("@macdSignal", (double)dto.MacdSignal);
                cmd.Parameters.AddWithValue("@bbUpper", (double)dto.BbUpper);
                cmd.Parameters.AddWithValue("@bbLower", (double)dto.BbLower);
                cmd.Parameters.AddWithValue("@signal", dto.Signal);
                cmd.Parameters.AddWithValue("@buyProb", (double)dto.BuyProbability);
                cmd.Parameters.AddWithValue("@sellProb", (double)dto.SellProbability);
                cmd.Parameters.AddWithValue("@chartAi", (double)dto.ChartAiScore);
                cmd.Parameters.AddWithValue("@fundAi", (double)dto.FundAiScore);
                cmd.Parameters.AddWithValue("@quantSignal", dto.QuantSignal ?? "");
                cmd.Parameters.AddWithValue("@chartAiSignal", dto.ChartAiSignal ?? "");
                cmd.Parameters.AddWithValue("@fundAiSignal", dto.FundAiSignal ?? "");
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 특정 종목의 최근 N일 스냅샷을 조회합니다.
        /// </summary>
        public static List<MarketSnapshotDto> GetByTicker(string ticker, int days = 30)
        {
            var list = new List<MarketSnapshotDto>();
            using (var conn = DBManager.Instance.GetConnection())
            using (var cmd = new NpgsqlCommand(@"
                SELECT SNAPSHOT_ID, SNAP_DATE, TICKER, PRICE, POSITION_20D,
                       RSI_14, MACD_VALUE, MACD_SIGNAL, BB_UPPER, BB_LOWER, SIGNAL,
                       BUY_PROBABILITY, SELL_PROBABILITY, CHART_AI_SCORE, FUND_AI_SCORE,
                       QUANT_SIGNAL, CHART_AI_SIGNAL, FUND_AI_SIGNAL
                FROM TB_MARKET_SNAPSHOT
                WHERE TICKER = @ticker
                ORDER BY SNAP_DATE DESC
                LIMIT @days", conn))
            {
                cmd.Parameters.AddWithValue("@ticker", ticker);
                cmd.Parameters.AddWithValue("@days", days);

                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        list.Add(MapSnapshot(rdr));
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Phase 5-d: 전체 종목의 최근 스냅샷을 종목·일자 오름차순으로 조회합니다.
        /// 적중률 분석 / 가중치 A/B 백테스트에서 미래 수익(forward return) 계산의 원천으로 사용됩니다.
        /// </summary>
        /// <param name="limit">최대 조회 건수 (기본 5000)</param>
        public static List<MarketSnapshotDto> GetRecentAll(int limit = 5000)
        {
            var list = new List<MarketSnapshotDto>();
            using (var conn = DBManager.Instance.GetConnection())
            using (var cmd = new NpgsqlCommand(@"
                SELECT SNAPSHOT_ID, SNAP_DATE, TICKER, PRICE, POSITION_20D,
                       RSI_14, MACD_VALUE, MACD_SIGNAL, BB_UPPER, BB_LOWER, SIGNAL,
                       BUY_PROBABILITY, SELL_PROBABILITY, CHART_AI_SCORE, FUND_AI_SCORE,
                       QUANT_SIGNAL, CHART_AI_SIGNAL, FUND_AI_SIGNAL
                FROM TB_MARKET_SNAPSHOT
                ORDER BY TICKER ASC, SNAP_DATE ASC
                LIMIT @limit", conn))
            {
                cmd.Parameters.AddWithValue("@limit", limit);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        list.Add(MapSnapshot(rdr));
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// 특정 종목의 과거 SellProbability 배열을 최근 순으로 조회합니다 (매도 적응형 임계값용).
        /// </summary>
        public static List<decimal> GetHistoricalSellProbabilities(string ticker, int limit = 100)
        {
            var list = new List<decimal>();
            using (var conn = DBManager.Instance.GetConnection())
            using (var cmd = new NpgsqlCommand(@"
                SELECT SELL_PROBABILITY
                FROM TB_MARKET_SNAPSHOT
                WHERE TICKER = @ticker AND SELL_PROBABILITY IS NOT NULL
                ORDER BY SNAP_DATE DESC
                LIMIT @limit", conn))
            {
                cmd.Parameters.AddWithValue("@ticker", ticker);
                cmd.Parameters.AddWithValue("@limit", limit);

                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        decimal prob = rdr.IsDBNull(0) ? 0m : rdr.GetDecimal(0);
                        if (prob > 0)
                        {
                            list.Add(prob);
                        }
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// NpgsqlDataReader 한 행을 MarketSnapshotDto로 매핑합니다.
        /// SELECT 컬럼 순서: SNAPSHOT_ID, SNAP_DATE, TICKER, PRICE, POSITION_20D, RSI_14,
        ///   MACD_VALUE, MACD_SIGNAL, BB_UPPER, BB_LOWER, SIGNAL, BUY_PROBABILITY,
        ///   SELL_PROBABILITY, CHART_AI_SCORE, FUND_AI_SCORE, QUANT_SIGNAL, CHART_AI_SIGNAL, FUND_AI_SIGNAL
        /// </summary>
        private static MarketSnapshotDto MapSnapshot(NpgsqlDataReader rdr)
        {
            return new MarketSnapshotDto
            {
                SnapshotId = rdr.GetInt32(0),
                SnapDate = DateTime.Parse(rdr.GetString(1)),
                Ticker = rdr.GetString(2),
                Price = rdr.GetDecimal(3),
                Position20d = rdr.GetDecimal(4),
                Rsi14 = rdr.GetDecimal(5),
                MacdValue = rdr.GetDecimal(6),
                MacdSignal = rdr.GetDecimal(7),
                BbUpper = rdr.GetDecimal(8),
                BbLower = rdr.GetDecimal(9),
                Signal = rdr.GetString(10),
                BuyProbability = rdr.IsDBNull(11) ? 0m : rdr.GetDecimal(11),
                SellProbability = rdr.IsDBNull(12) ? 0m : rdr.GetDecimal(12),
                ChartAiScore = rdr.IsDBNull(13) ? 0m : rdr.GetDecimal(13),
                FundAiScore = rdr.IsDBNull(14) ? 0m : rdr.GetDecimal(14),
                QuantSignal = rdr.IsDBNull(15) ? "" : rdr.GetString(15),
                ChartAiSignal = rdr.IsDBNull(16) ? "" : rdr.GetString(16),
                FundAiSignal = rdr.IsDBNull(17) ? "" : rdr.GetString(17)
            };
        }

        /// <summary>
        /// 특정 종목의 과거 BuyProbability 배열을 최근 순으로 조회합니다.
        /// 적응형 임계값 계산을 위한 원천 데이터로 사용됩니다.
        /// </summary>
        public static List<decimal> GetHistoricalProbabilities(string ticker, int limit = 100)
        {
            var list = new List<decimal>();
            using (var conn = DBManager.Instance.GetConnection())
            using (var cmd = new NpgsqlCommand(@"
                SELECT BUY_PROBABILITY
                FROM TB_MARKET_SNAPSHOT
                WHERE TICKER = @ticker AND BUY_PROBABILITY IS NOT NULL
                ORDER BY SNAP_DATE DESC
                LIMIT @limit", conn))
            {
                cmd.Parameters.AddWithValue("@ticker", ticker);
                cmd.Parameters.AddWithValue("@limit", limit);

                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        decimal prob = rdr.IsDBNull(0) ? 0m : rdr.GetDecimal(0);
                        if (prob > 0)
                        {
                            list.Add(prob);
                        }
                    }
                }
            }
            return list;
        }
    }
}
