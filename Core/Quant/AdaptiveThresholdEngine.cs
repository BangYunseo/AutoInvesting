using System;
using System.Collections.Generic;
using System.Linq;
using AutoInvest.Data;
using AutoInvest.Data.DAO;
using AutoInvest.Utils;

namespace AutoInvest.Core.Quant
{
    /// <summary>
    /// Phase 5-a: 종목별 적응형 임계값 산출 엔진.
    /// TB_MARKET_SNAPSHOT에 축적된 종목별 과거 BuyProbability를 바탕으로
    /// 동적 임계값(Percentile 기반)을 계산합니다.
    /// </summary>
    public static class AdaptiveThresholdEngine
    {
        // 통계를 내기 위한 최소 데이터 포인트 개수 (이보다 적으면 기본값 사용)
        private const int MIN_DATA_POINTS = 20;

        // 상위 N%를 임계값으로 잡을지 결정 (기본 상위 30% = 70th Percentile)
        private const double TARGET_PERCENTILE = 0.70;

        /// <summary>
        /// 주어진 종목에 대해 적응형 매수 임계값을 반환합니다.
        /// 데이터가 부족하면 appsettings.json의 기본값을 반환합니다.
        /// </summary>
        /// <param name="ticker">종목 코드</param>
        /// <returns>적응형 임계값 (0.0 ~ 1.0), 데이터 부족 시 기본값 반환</returns>
        public static (decimal Threshold, string Reason) GetBuyThreshold(string ticker)
        {
            decimal defaultThreshold = ParseConfigThreshold("BUY_THRESHOLD");
            List<decimal> history;
            try
            {
                history = MarketSnapshotDAO.GetHistoricalProbabilities(ticker, 100);
            }
            catch (Exception ex)
            {
                Logger.Error($"[AdaptiveThreshold] 매수 임계값 산출 실패 ({ticker}): {ex.Message}");
                return (defaultThreshold, "산출 중 오류 발생 → 기본값 사용");
            }
            return ComputeThreshold(history, defaultThreshold);
        }

        /// <summary>
        /// 주어진 종목에 대해 적응형 매도 임계값을 반환합니다 (Phase 5-d, 매수와 대칭).
        /// 과거 SellProbability 분포의 상위 백분위를 임계값으로 사용합니다.
        /// 데이터가 부족하면 appsettings.json의 기본값을 반환합니다.
        /// </summary>
        /// <param name="ticker">종목 코드</param>
        /// <returns>적응형 임계값 (0.0 ~ 1.0), 데이터 부족 시 기본값 반환</returns>
        public static (decimal Threshold, string Reason) GetSellThreshold(string ticker)
        {
            decimal defaultThreshold = ParseConfigThreshold("SELL_THRESHOLD");
            List<decimal> history;
            try
            {
                history = MarketSnapshotDAO.GetHistoricalSellProbabilities(ticker, 100);
            }
            catch (Exception ex)
            {
                Logger.Error($"[AdaptiveThreshold] 매도 임계값 산출 실패 ({ticker}): {ex.Message}");
                return (defaultThreshold, "산출 중 오류 발생 → 기본값 사용");
            }
            return ComputeThreshold(history, defaultThreshold);
        }

        /// <summary>appsettings.json에서 임계값 기본값을 읽습니다 (실패 시 0.65).</summary>
        private static decimal ParseConfigThreshold(string key)
        {
            return decimal.TryParse(AppConfigManager.Get(key, "0.65"), out decimal val) ? val : 0.65m;
        }

        /// <summary>
        /// 확률 이력에 대해 목표 백분위(상위 30%) 기반 임계값을 선형 보간으로 산출합니다.
        /// 데이터가 부족하면(MIN_DATA_POINTS 미만) 기본값을 그대로 반환합니다.
        /// </summary>
        private static (decimal Threshold, string Reason) ComputeThreshold(List<decimal> history, decimal defaultThreshold)
        {
            if (history.Count < MIN_DATA_POINTS)
            {
                return (defaultThreshold, $"데이터 부족 (현재 {history.Count}건, 최소 {MIN_DATA_POINTS}건 필요) → 기본값 사용");
            }

            // 오름차순 정렬
            history.Sort();

            // 퍼센타일 계산
            int n = history.Count;
            double index = (n - 1) * TARGET_PERCENTILE;
            int lowerIndex = (int)Math.Floor(index);
            int upperIndex = (int)Math.Ceiling(index);
            double fraction = index - lowerIndex;

            decimal lowerValue = history[lowerIndex];
            decimal upperValue = history[upperIndex];

            // 선형 보간 (Linear Interpolation)
            decimal threshold = lowerValue + (decimal)fraction * (upperValue - lowerValue);

            // 안전장치: 너무 낮거나 높은 임계값 방지
            if (threshold < 0.50m) threshold = 0.50m;
            if (threshold > 0.85m) threshold = 0.85m;

            return (Math.Round(threshold, 3), $"과거 {n}건 중 상위 {(1.0 - TARGET_PERCENTILE) * 100}% 기준");
        }
    }
}
