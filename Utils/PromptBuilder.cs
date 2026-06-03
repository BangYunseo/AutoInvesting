using AutoInvest.Data.DTO;
using System.Collections.Generic;
using System.Text;

namespace AutoInvest.Utils
{
    /// <summary>
    /// QuantIndicator 결과와 OHLCV 데이터를 Gemini API가 이해할 수 있는
    /// 텍스트 프롬프트로 변환합니다.
    /// </summary>
    public static class PromptBuilder
    {
        /// <summary>
        /// Gemini에게 전달할 System Prompt (역할 정의 + JSON 응답 강제).
        /// </summary>
        public static string BuildSystemPrompt()
        {
            return @"You are a quantitative investment analyst specializing in overseas ETF trading.
You will be given technical indicators for a single ETF ticker. Based on these indicators, decide whether to BUY, SELL, or HOLD.

You MUST respond with ONLY a valid JSON object in this exact format (no markdown, no extra text):
{""signal"": ""BUY"", ""confidence"": 0.82, ""reason"": ""Brief explanation in Korean within 2 sentences.""}

Rules:
- signal: must be exactly ""BUY"", ""SELL"", or ""HOLD""
- confidence: a decimal from 0.0 to 1.0 indicating your certainty
- reason: 1~2 sentences explaining your decision in Korean
- Do NOT include any text outside the JSON object";
        }

        /// <summary>
        /// 종목 지표를 Gemini 사용자 프롬프트로 변환합니다.
        /// </summary>
        /// <param name="ticker">종목 코드</param>
        /// <param name="indicators">퀀트 지표 (RSI, MACD, BB, Position)</param>
        /// <param name="ohlcv">최근 OHLCV 데이터 (최대 20개 사용)</param>
        public static string BuildUserPrompt(string ticker, IndicatorDto indicators, List<OhlcvDto>? ohlcv = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Ticker: {ticker}");
            sb.AppendLine($"Current Price Position (20-day range, 0=lowest, 1=highest): {indicators.Position:F3}");
            sb.AppendLine($"RSI(14): {indicators.Rsi14:F2}");
            sb.AppendLine($"MACD Line: {indicators.MacdLine:F4}");
            sb.AppendLine($"MACD Signal: {indicators.MacdSignal:F4}");
            sb.AppendLine($"MACD Histogram: {indicators.MacdHistogram:F4}");
            sb.AppendLine($"Bollinger Band Upper: {indicators.BbUpper:F2}");
            sb.AppendLine($"Bollinger Band Middle: {indicators.BbMiddle:F2}");
            sb.AppendLine($"Bollinger Band Lower: {indicators.BbLower:F2}");

            // ── 최근 N일 OHLCV 요약 (최대 20일) ──
            if (ohlcv != null && ohlcv.Count > 0)
            {
                int take = System.Math.Min(ohlcv.Count, 20);
                sb.AppendLine($"\nRecent {take}-day OHLCV (Date, Close, Volume):");
                for (int i = ohlcv.Count - take; i < ohlcv.Count; i++)
                {
                    var bar = ohlcv[i];
                    sb.AppendLine($"  {bar.Date:yyyy-MM-dd}  C={bar.Close:F2}  V={bar.Volume}");
                }
            }

            sb.AppendLine("\nBased on the above data, provide your BUY/SELL/HOLD decision:");
            return sb.ToString();
        }
    }
}
