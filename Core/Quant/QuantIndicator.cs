using AutoInvest.Data.DTO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoInvest.Core.Quant
{
    /// <summary>
    /// 퀀트 기술적 지표 계산기.
    /// OHLCV 데이터를 입력받아 RSI, MACD, 볼린저밴드를 계산합니다.
    ///
    /// ▶ RSI (Relative Strength Index)
    ///   - 14일간 상승/하락 평균으로 과매수(70↑)/과매도(30↓) 판단
    ///   - 공식: RSI = 100 - (100 / (1 + RS)), RS = 평균상승 / 평균하락
    ///
    /// ▶ MACD (Moving Average Convergence Divergence)
    ///   - 12일 EMA와 26일 EMA의 차이로 추세 전환 감지
    ///   - MACD Line = EMA(12) - EMA(26)
    ///   - Signal Line = EMA(MACD, 9)
    ///   - Histogram = MACD Line - Signal Line
    ///
    /// ▶ 볼린저밴드 (Bollinger Bands)
    ///   - 20일 이동평균 ±2σ(표준편차)로 가격 밴드 설정
    ///   - 하단 밴드 이하 = 과매도 가능성, 상단 밴드 이상 = 과매수 가능성
    /// </summary>
    public static class QuantIndicator
    {
        // ═══════════════════════════════════════════════════════
        // RSI 계산
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// RSI(Relative Strength Index) 계산.
        /// </summary>
        /// <param name="closes">종가 리스트 (오래된 순 → 최신 순)</param>
        /// <param name="period">RSI 기간 (기본 14일)</param>
        /// <returns>RSI 값 (0~100)</returns>
        public static decimal CalculateRsi(List<decimal> closes, int period = 14)
        {
            if (closes.Count < period + 1)
                return 50m; // 데이터 부족 시 중립값 반환

            var gains = new List<decimal>();
            var losses = new List<decimal>();

            // 변동폭 계산 (전일 대비 상승/하락)
            for (int i = 1; i < closes.Count; i++)
            {
                decimal change = closes[i] - closes[i - 1];
                gains.Add(change > 0 ? change : 0);
                losses.Add(change < 0 ? Math.Abs(change) : 0);
            }

            // 초기 평균 (단순 평균)
            decimal avgGain = gains.Take(period).Average();
            decimal avgLoss = losses.Take(period).Average();

            // Wilder's Smoothing (지수 이동 평균 방식)
            for (int i = period; i < gains.Count; i++)
            {
                avgGain = (avgGain * (period - 1) + gains[i]) / period;
                avgLoss = (avgLoss * (period - 1) + losses[i]) / period;
            }

            if (avgLoss == 0)
                return 100m; // 하락 없음 = RSI 100

            decimal rs = avgGain / avgLoss;
            decimal rsi = 100m - (100m / (1m + rs));

            return Math.Round(rsi, 2);
        }

        // ═══════════════════════════════════════════════════════
        // MACD 계산
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// MACD 계산 (12, 26, 9 파라미터).
        /// </summary>
        /// <param name="closes">종가 리스트 (오래된 순 → 최신 순)</param>
        /// <param name="fastPeriod">빠른 EMA 기간 (기본 12)</param>
        /// <param name="slowPeriod">느린 EMA 기간 (기본 26)</param>
        /// <param name="signalPeriod">시그널 EMA 기간 (기본 9)</param>
        /// <returns>(MACD Line, Signal Line, Histogram)</returns>
        public static (decimal MacdLine, decimal Signal, decimal Histogram) CalculateMacd(
            List<decimal> closes,
            int fastPeriod = 12,
            int slowPeriod = 26,
            int signalPeriod = 9)
        {
            if (closes.Count < slowPeriod + signalPeriod)
                return (0m, 0m, 0m); // 데이터 부족

            // EMA 계산
            var emaFast = CalculateEma(closes, fastPeriod);
            var emaSlow = CalculateEma(closes, slowPeriod);

            // MACD Line = 빠른 EMA - 느린 EMA
            int minLen = Math.Min(emaFast.Count, emaSlow.Count);
            var macdLine = new List<decimal>();
            for (int i = 0; i < minLen; i++)
            {
                int fastIdx = emaFast.Count - minLen + i;
                int slowIdx = emaSlow.Count - minLen + i;
                macdLine.Add(emaFast[fastIdx] - emaSlow[slowIdx]);
            }

            // Signal Line = MACD의 9일 EMA
            var signalLine = CalculateEma(macdLine, signalPeriod);

            if (macdLine.Count == 0 || signalLine.Count == 0)
                return (0m, 0m, 0m);

            decimal lastMacd = macdLine.Last();
            decimal lastSignal = signalLine.Last();
            decimal histogram = lastMacd - lastSignal;

            return (Math.Round(lastMacd, 4), Math.Round(lastSignal, 4), Math.Round(histogram, 4));
        }

        // ═══════════════════════════════════════════════════════
        // 볼린저밴드 계산
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// 볼린저밴드 계산 (20일, ±2σ).
        /// </summary>
        /// <param name="closes">종가 리스트 (오래된 순 → 최신 순)</param>
        /// <param name="period">이동평균 기간 (기본 20)</param>
        /// <param name="multiplier">표준편차 배수 (기본 2.0)</param>
        /// <returns>(중심선, 상단, 하단)</returns>
        public static (decimal Middle, decimal Upper, decimal Lower) CalculateBollingerBands(
            List<decimal> closes,
            int period = 20,
            decimal multiplier = 2.0m)
        {
            if (closes.Count < period)
                return (closes.LastOrDefault(), closes.LastOrDefault(), closes.LastOrDefault());

            // 최근 N일 종가
            var recent = closes.Skip(closes.Count - period).Take(period).ToList();
            decimal sma = recent.Average();

            // 표준편차 계산
            decimal sumSqDiff = recent.Sum(c => (c - sma) * (c - sma));
            decimal stdDev = (decimal)Math.Sqrt((double)(sumSqDiff / period));

            decimal upper = sma + multiplier * stdDev;
            decimal lower = sma - multiplier * stdDev;

            return (Math.Round(sma, 4), Math.Round(upper, 4), Math.Round(lower, 4));
        }

        // ═══════════════════════════════════════════════════════
        // 종합 지표 계산
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// OHLCV 데이터에서 전체 퀀트 지표를 한 번에 계산합니다.
        /// </summary>
        /// <param name="ticker">종목 코드</param>
        /// <param name="ohlcvList">OHLCV 일봉 데이터 (오래된 순)</param>
        /// <param name="currentPrice">현재가</param>
        /// <param name="high20d">20일 최고가</param>
        /// <param name="low20d">20일 최저가</param>
        /// <returns>종합 지표 DTO</returns>
        public static IndicatorDto CalculateAll(
            string ticker,
            List<OhlcvDto> ohlcvList,
            decimal currentPrice,
            decimal high20d,
            decimal low20d)
        {
            var closes = ohlcvList.Select(o => o.Close).ToList();

            // RSI
            decimal rsi = CalculateRsi(closes);

            // MACD
            var (macdLine, signal, histogram) = CalculateMacd(closes);

            // 볼린저밴드
            var (bbMiddle, bbUpper, bbLower) = CalculateBollingerBands(closes);

            // Position (기존 SmartOrderEngine 로직)
            decimal position = (high20d == low20d) ? 0.5m : (currentPrice - low20d) / (high20d - low20d);
            position = Math.Max(0m, Math.Min(1m, position));

            return new IndicatorDto
            {
                Ticker = ticker,
                Rsi14 = rsi,
                MacdLine = macdLine,
                MacdSignal = signal,
                MacdHistogram = histogram,
                BbMiddle = bbMiddle,
                BbUpper = bbUpper,
                BbLower = bbLower,
                Position = Math.Round(position, 4),
                CalculatedAt = DateTime.Now
            };
        }

        // ═══════════════════════════════════════════════════════
        // 내부 헬퍼: EMA (지수 이동 평균)
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// EMA (Exponential Moving Average) 계산.
        /// </summary>
        private static List<decimal> CalculateEma(List<decimal> data, int period)
        {
            if (data.Count < period)
                return new List<decimal>();

            var ema = new List<decimal>();
            decimal multiplier = 2.0m / (period + 1);

            // 첫 EMA = 단순 이동 평균 (SMA)
            decimal sma = data.Take(period).Average();
            ema.Add(sma);

            // 이후 EMA = (현재값 - 이전 EMA) × 승수 + 이전 EMA
            for (int i = period; i < data.Count; i++)
            {
                decimal newEma = (data[i] - ema.Last()) * multiplier + ema.Last();
                ema.Add(newEma);
            }

            return ema;
        }
    }
}
