using AutoInvest.Data.DTO;
using System.Collections.Generic;
using System.Text;

namespace AutoInvest.Utils
{
    /// <summary>
    /// QuantIndicator 결과와 OHLCV 데이터를 Gemini API가 이해할 수 있는
    /// 텍스트 프롬프트로 변환합니다.
    ///
    /// Phase 4-d: 차트 기술 에이전트(Chart)와 거시경제·펀더멘털 에이전트(Fundamental)의
    /// 프롬프트를 완전히 분리하여 투자 위원회(Investment Committee) 구조를 구현합니다.
    /// </summary>
    public static class PromptBuilder
    {
        // ── 차트 기술 에이전트 (기존 유지) ────────────────────────────────────

        /// <summary>
        /// [차트 에이전트] Gemini에게 전달할 System Prompt.
        /// 역할: 순수 기술적 차트 분석 전문가 (RSI·MACD·BB·OHLCV 기반 판단).
        /// </summary>
        /// <param name="investmentPhilosophy">사용자 설정 투자 철학 (선택적)</param>
        public static string BuildSystemPrompt(string investmentPhilosophy = "")
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are a quantitative chart analyst specializing in overseas ETF trading.");
            sb.AppendLine("Your ONLY job is to analyze TECHNICAL INDICATORS (RSI, MACD, Bollinger Bands, price position, and OHLCV trend) to decide BUY, SELL, or HOLD.");
            sb.AppendLine("Do NOT consider macroeconomic factors, interest rates, or news sentiment — that is another analyst's responsibility.");

            if (!string.IsNullOrWhiteSpace(investmentPhilosophy))
            {
                sb.AppendLine("\n[YOUR INVESTMENT PHILOSOPHY]");
                sb.AppendLine(investmentPhilosophy);
                sb.AppendLine("You MUST strictly reflect the above investment philosophy in your ultimate decision.");
            }

            sb.AppendLine("\n[CHART ANALYSIS GUIDELINES & FEW-SHOT EXAMPLES]");
            sb.AppendLine("- Avoid catching a falling knife: If RSI is extremely low (< 30) but MACD Histogram is sharply negative or BB is breaking lower severely, DO NOT BUY. Output HOLD.");
            sb.AppendLine("- Confirm trend reversal: A BUY is much safer when MACD Line crosses above MACD Signal alongside a low RSI (< 40).");
            sb.AppendLine("- Take profit logically: If RSI > 70 and Position > 0.85, confidently signal SELL.");
            sb.AppendLine("- When in doubt or indicators are conflicting, output HOLD.");

            sb.AppendLine("\nYou MUST respond with ONLY a valid JSON object in this exact format (no markdown, no extra text):");
            sb.AppendLine("{\"signal\": \"BUY\", \"confidence\": 0.82, \"reason\": \"Brief explanation in Korean within 2 sentences.\"}");

            sb.AppendLine("\nRules:");
            sb.AppendLine("- signal: must be exactly \"BUY\", \"SELL\", or \"HOLD\"");
            sb.AppendLine("- confidence: a decimal from 0.0 to 1.0 indicating your certainty");
            sb.AppendLine("- reason: 1~2 sentences explaining your decision in Korean");
            sb.AppendLine("- Do NOT include any text outside the JSON object");

            return sb.ToString();
        }

        /// <summary>
        /// [차트 에이전트] 종목 지표를 Gemini 사용자 프롬프트로 변환합니다.
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

            sb.AppendLine("\n[YOUR ROLE] You are the CHART ANALYST. Analyze the technical indicators above and provide your BUY/SELL/HOLD decision:");
            return sb.ToString();
        }

        // ── 거시경제·펀더멘털 에이전트 (Phase 4-d 신규) ─────────────────────

        /// <summary>
        /// [펀더멘털 에이전트] Gemini에게 전달할 System Prompt.
        /// 역할: 거시경제·섹터 흐름 중심의 투자 위원회 펀더멘털 애널리스트.
        /// 차트 수치보다 더 넓은 맥락(금리·달러·섹터 유불리)에서 판단합니다.
        /// </summary>
        /// <param name="investmentPhilosophy">사용자 설정 투자 철학 (선택적)</param>
        public static string BuildFundamentalSystemPrompt(string investmentPhilosophy = "")
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are a macro-economic and fundamental analyst on an overseas ETF Investment Committee.");
            sb.AppendLine("Your job is to evaluate whether the CURRENT MARKET ENVIRONMENT is favorable for investing in this ETF, based on:");
            sb.AppendLine("  1. The ETF's inferred sector (Tech/Bond/Commodity/Dividend/Broad Market etc.) from its ticker");
            sb.AppendLine("  2. The broader macro environment (interest rate cycle, USD strength, inflation trend)");
            sb.AppendLine("  3. The long-term investment horizon (3~6 months view), NOT short-term noise");
            sb.AppendLine("Do NOT make decisions based solely on chart signals — interpret the technical data from a fundamentals perspective.");

            if (!string.IsNullOrWhiteSpace(investmentPhilosophy))
            {
                sb.AppendLine("\n[INVESTMENT PHILOSOPHY]");
                sb.AppendLine(investmentPhilosophy);
                sb.AppendLine("You MUST strictly reflect the above investment philosophy in your ultimate decision.");
            }

            sb.AppendLine("\n[FUNDAMENTAL ANALYSIS GUIDELINES]");
            sb.AppendLine("- If the ticker suggests a Tech ETF (QQQ, XLK, QQQM, etc.) and RSI is low, consider whether current macro conditions (rate cuts expected vs. hikes) support a bounce.");
            sb.AppendLine("- If the ticker suggests a Bond ETF (TLT, IEF, AGG, etc.) and rates are rising, lean HOLD/SELL even if technical indicators look attractive.");
            sb.AppendLine("- If the ticker suggests a Commodity ETF (GLD, SLV, DBC, etc.) and inflation expectations are high, a low position may be a long-term entry opportunity.");
            sb.AppendLine("- A low Position (0.0~0.3) combined with low RSI can mean either: (a) a great long-term entry, or (b) a macro-driven downtrend — use sector context to decide.");
            sb.AppendLine("- When macro context is uncertain or the ticker is unrecognized, default to HOLD to avoid overconfident calls.");

            sb.AppendLine("\nYou MUST respond with ONLY a valid JSON object in this exact format (no markdown, no extra text):");
            sb.AppendLine("{\"signal\": \"BUY\", \"confidence\": 0.75, \"reason\": \"Brief explanation in Korean within 2 sentences.\"}");

            sb.AppendLine("\nRules:");
            sb.AppendLine("- signal: must be exactly \"BUY\", \"SELL\", or \"HOLD\"");
            sb.AppendLine("- confidence: a decimal from 0.0 to 1.0 indicating your certainty");
            sb.AppendLine("- reason: 1~2 sentences explaining your macro/fundamental rationale in Korean");
            sb.AppendLine("- Do NOT include any text outside the JSON object");

            return sb.ToString();
        }

        /// <summary>
        /// [펀더멘털 에이전트] 종목 지표를 거시경제 관점의 사용자 프롬프트로 변환합니다.
        /// 차트 수치를 제공하되, 섹터·거시 환경 관점에서 재해석하도록 유도합니다.
        /// </summary>
        /// <param name="ticker">종목 코드</param>
        /// <param name="indicators">퀀트 지표 (RSI, Position 등)</param>
        public static string BuildFundamentalUserPrompt(string ticker, IndicatorDto indicators)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"ETF Ticker: {ticker}");
            sb.AppendLine($"Price Position in 20-day range (0=lowest, 1=highest): {indicators.Position:F3}");
            sb.AppendLine($"RSI(14): {indicators.Rsi14:F2}");
            sb.AppendLine($"MACD Histogram (momentum): {indicators.MacdHistogram:F4}");
            sb.AppendLine($"Bollinger Band Width (Upper-Lower): {indicators.BbUpper - indicators.BbLower:F2}");

            sb.AppendLine("\n[CONTEXT FOR YOUR ANALYSIS]");
            sb.AppendLine("- The chart analyst has already assessed the technical signals separately.");
            sb.AppendLine("- Your job is to evaluate this from a MACRO & FUNDAMENTAL perspective.");
            sb.AppendLine("- Consider: What sector does this ETF likely belong to? Are macro conditions (rates, dollar, inflation) favorable for this sector right now?");
            sb.AppendLine("- A low RSI + low Position could be a great entry (mean reversion) OR a macro-driven trap — you decide from a fundamentals view.");

            sb.AppendLine($"\n[YOUR ROLE] You are the FUNDAMENTAL ANALYST on the Investment Committee. Analyze the macro/sector context for {ticker} and provide your BUY/SELL/HOLD decision:");
            return sb.ToString();
        }
    }
}
