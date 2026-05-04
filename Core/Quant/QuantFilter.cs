using AutoInvest.Data.DTO;
using System.Collections.Generic;

namespace AutoInvest.Core.Quant
{
    /// <summary>
    /// 퀀트 필터 결과.
    /// 다중 조건의 통과 여부와 판단 근거를 담습니다.
    /// </summary>
    public class FilterResult
    {
        /// <summary>모든 조건을 만족했는가</summary>
        public bool Passed { get; set; }

        /// <summary>충족된 조건 목록 (예: "RSI=28.5 ≤ 30 ✓")</summary>
        public List<string> MetConditions { get; set; } = new();

        /// <summary>미충족 조건 목록 (예: "MACD Histogram > 0 ✗")</summary>
        public List<string> UnmetConditions { get; set; } = new();

        /// <summary>판단 요약 문자열</summary>
        public string Summary
        {
            get
            {
                if (Passed)
                    return $"조건 {MetConditions.Count}개 모두 충족 → 통과";
                return $"조건 미충족 {UnmetConditions.Count}개 → 거부 ({string.Join(", ", UnmetConditions)})";
            }
        }
    }

    /// <summary>
    /// 퀀트 다중 조건 AND 필터.
    /// 전략 유형(STRATEGY_TYPE)에 따라 서로 다른 지표 조건 조합을 적용합니다.
    ///
    /// ▶ MEAN_REVERSION (평균회귀)
    ///   - 가격이 저점에 있고, RSI가 과매도이고, 볼린저밴드 하단 이하일 때 매수
    ///   - "싸게 사서 제자리로 돌아오면 판다"
    ///
    /// ▶ MOMENTUM (모멘텀)
    ///   - RSI가 상승 추세이고, MACD 골든크로스(상향 돌파)일 때 매수
    ///   - "오르는 놈이 더 간다"
    ///
    /// ▶ MIXED (혼합)
    ///   - Position 조건만 적용하되, RSI 극단값 필터 추가
    ///   - 평균회귀와 모멘텀의 중간 성격
    /// </summary>
    public static class QuantFilter
    {
        /// <summary>
        /// 매수 조건 필터링.
        /// 전략 유형에 따라 다른 조건 조합으로 매수 가능 여부를 판단합니다.
        /// </summary>
        /// <param name="indicators">계산된 지표값</param>
        /// <param name="strategyType">전략 유형 (MEAN_REVERSION / MOMENTUM / MIXED)</param>
        /// <param name="buyThreshold">Position 매수 임계값 (기본 0.10)</param>
        /// <returns>필터 결과</returns>
        public static FilterResult CheckBuyCondition(
            IndicatorDto indicators,
            string strategyType,
            decimal buyThreshold = 0.10m)
        {
            return strategyType switch
            {
                "MOMENTUM" => CheckMomentumBuy(indicators),
                "MIXED" => CheckMixedBuy(indicators, buyThreshold),
                _ => CheckMeanReversionBuy(indicators, buyThreshold) // MEAN_REVERSION (기본)
            };
        }

        /// <summary>
        /// 매도 조건 필터링.
        /// </summary>
        public static FilterResult CheckSellCondition(
            IndicatorDto indicators,
            string strategyType,
            decimal sellThreshold = 0.90m)
        {
            return strategyType switch
            {
                "MOMENTUM" => CheckMomentumSell(indicators),
                "MIXED" => CheckMixedSell(indicators, sellThreshold),
                _ => CheckMeanReversionSell(indicators, sellThreshold)
            };
        }

        // ═══════════════════════════════════════════════════════
        // 평균회귀 (Mean Reversion) 전략
        // ═══════════════════════════════════════════════════════

        private static FilterResult CheckMeanReversionBuy(IndicatorDto ind, decimal threshold)
        {
            var result = new FilterResult();

            // 조건 1: Position이 매수 임계값 이하 (하위 10%)
            if (ind.Position <= threshold)
                result.MetConditions.Add($"Position={ind.Position:F4} ≤ {threshold} ✓");
            else
                result.UnmetConditions.Add($"Position={ind.Position:F4} > {threshold}");

            // 조건 2: RSI 14일이 30 이하 (과매도)
            if (ind.Rsi14 <= 30m)
                result.MetConditions.Add($"RSI={ind.Rsi14:F1} ≤ 30 ✓");
            else
                result.UnmetConditions.Add($"RSI={ind.Rsi14:F1} > 30");

            // 조건 3: 현재가가 볼린저밴드 하단 이하
            if (ind.BbLower > 0 && ind.Position <= threshold)
                result.MetConditions.Add($"BB 하단 근접 ✓");
            else if (ind.BbLower > 0)
                result.UnmetConditions.Add($"BB 하단 미도달");

            result.Passed = result.UnmetConditions.Count == 0;
            return result;
        }

        private static FilterResult CheckMeanReversionSell(IndicatorDto ind, decimal threshold)
        {
            var result = new FilterResult();

            if (ind.Position >= threshold)
                result.MetConditions.Add($"Position={ind.Position:F4} ≥ {threshold} ✓");
            else
                result.UnmetConditions.Add($"Position={ind.Position:F4} < {threshold}");

            if (ind.Rsi14 >= 70m)
                result.MetConditions.Add($"RSI={ind.Rsi14:F1} ≥ 70 ✓");
            else
                result.UnmetConditions.Add($"RSI={ind.Rsi14:F1} < 70");

            result.Passed = result.UnmetConditions.Count == 0;
            return result;
        }

        // ═══════════════════════════════════════════════════════
        // 모멘텀 (Momentum) 전략
        // ═══════════════════════════════════════════════════════

        private static FilterResult CheckMomentumBuy(IndicatorDto ind)
        {
            var result = new FilterResult();

            // 조건 1: RSI 50 이상 (상승 추세)
            if (ind.Rsi14 >= 50m)
                result.MetConditions.Add($"RSI={ind.Rsi14:F1} ≥ 50 (상승추세) ✓");
            else
                result.UnmetConditions.Add($"RSI={ind.Rsi14:F1} < 50 (하락추세)");

            // 조건 2: MACD Histogram 양수 (골든크로스)
            if (ind.MacdHistogram > 0)
                result.MetConditions.Add($"MACD Histogram={ind.MacdHistogram:F4} > 0 ✓");
            else
                result.UnmetConditions.Add($"MACD Histogram={ind.MacdHistogram:F4} ≤ 0");

            // 조건 3: MACD Line 양수 (강한 상승)
            if (ind.MacdLine > 0)
                result.MetConditions.Add($"MACD Line={ind.MacdLine:F4} > 0 ✓");
            else
                result.UnmetConditions.Add($"MACD Line={ind.MacdLine:F4} ≤ 0");

            result.Passed = result.UnmetConditions.Count == 0;
            return result;
        }

        private static FilterResult CheckMomentumSell(IndicatorDto ind)
        {
            var result = new FilterResult();

            // 모멘텀 소실: RSI 하락 + MACD 데드크로스
            if (ind.Rsi14 < 50m)
                result.MetConditions.Add($"RSI={ind.Rsi14:F1} < 50 (모멘텀 소실) ✓");
            else
                result.UnmetConditions.Add($"RSI={ind.Rsi14:F1} ≥ 50 (모멘텀 유지중)");

            if (ind.MacdHistogram < 0)
                result.MetConditions.Add($"MACD Histogram={ind.MacdHistogram:F4} < 0 (데드크로스) ✓");
            else
                result.UnmetConditions.Add($"MACD Histogram={ind.MacdHistogram:F4} ≥ 0");

            result.Passed = result.UnmetConditions.Count == 0;
            return result;
        }

        // ═══════════════════════════════════════════════════════
        // 혼합 (Mixed) 전략
        // ═══════════════════════════════════════════════════════

        private static FilterResult CheckMixedBuy(IndicatorDto ind, decimal threshold)
        {
            var result = new FilterResult();

            // 조건 1: Position이 매수 임계값 이하
            if (ind.Position <= threshold)
                result.MetConditions.Add($"Position={ind.Position:F4} ≤ {threshold} ✓");
            else
                result.UnmetConditions.Add($"Position={ind.Position:F4} > {threshold}");

            // 조건 2: RSI 극단적 과매수가 아닐 것 (70 미만)
            if (ind.Rsi14 < 70m)
                result.MetConditions.Add($"RSI={ind.Rsi14:F1} < 70 (과매수 아님) ✓");
            else
                result.UnmetConditions.Add($"RSI={ind.Rsi14:F1} ≥ 70 (과매수 위험)");

            result.Passed = result.UnmetConditions.Count == 0;
            return result;
        }

        private static FilterResult CheckMixedSell(IndicatorDto ind, decimal threshold)
        {
            var result = new FilterResult();

            if (ind.Position >= threshold)
                result.MetConditions.Add($"Position={ind.Position:F4} ≥ {threshold} ✓");
            else
                result.UnmetConditions.Add($"Position={ind.Position:F4} < {threshold}");

            if (ind.Rsi14 > 30m) // 극단적 과매도가 아닐 것
                result.MetConditions.Add($"RSI={ind.Rsi14:F1} > 30 (과매도 아님) ✓");
            else
                result.UnmetConditions.Add($"RSI={ind.Rsi14:F1} ≤ 30 (과매도 → 매도 보류)");

            result.Passed = result.UnmetConditions.Count == 0;
            return result;
        }
    }
}
