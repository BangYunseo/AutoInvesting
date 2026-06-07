using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoInvest.Core
{
    /// <summary>
    /// AI 시장 분석 엔진의 임시(Mock) 구현체입니다 (Phase 4 초기 단계).
    /// Phase 4-d: MultiAgentAnalysisResult를 반환하도록 업데이트.
    ///
    /// 실제 GeminiMarketAnalyzer와 동일한 인터페이스를 갖추되,
    /// 내부 로직은 간단한 규칙 기반으로 두 에이전트 의견을 시뮬레이션합니다.
    /// GEMINI_API_KEY가 없거나 AI_PROVIDER=mock일 때 SessionManager가 이 클래스를 사용합니다.
    /// </summary>
    public class AiMarketAnalyzer : IMarketAnalyzer
    {
        private static readonly Random _random = new Random();

        /// <summary>
        /// Mock 차트 에이전트 + Mock 펀더멘털 에이전트 결과를 시뮬레이션합니다.
        /// </summary>
        public Task<MultiAgentAnalysisResult> AnalyzeAsync(
            string ticker,
            IndicatorDto indicators,
            List<OhlcvDto>? ohlcv = null)
        {
            Logger.Info($"[AiAnalyzer-Mock] {ticker} — Mock 다중 에이전트 분석 시작");

            // ── Mock 차트 에이전트 ──
            var chartResult = SimulateChartAgent(ticker, indicators);

            // ── Mock 펀더멘털 에이전트 ──
            // Mock에서는 차트 에이전트와 독립적으로 약간 다른 확신도와 의견을 생성합니다.
            var fundamentalResult = SimulateFundamentalAgent(ticker, indicators);

            bool isFull = chartResult.ConfidenceScore > 0m && fundamentalResult.ConfidenceScore > 0m;

            Logger.Info($"[AiAnalyzer-Mock] {ticker} — 차트: {chartResult.Signal}({chartResult.ConfidenceScore:F2}), " +
                $"펀더멘털: {fundamentalResult.Signal}({fundamentalResult.ConfidenceScore:F2})");

            return Task.FromResult(new MultiAgentAnalysisResult
            {
                ChartAgent       = chartResult,
                FundamentalAgent = fundamentalResult,
                IsFullConsensus  = isFull
            });
        }

        // ── 내부 시뮬레이션 메서드 ─────────────────────────────────────────

        /// <summary>RSI와 Position 기반으로 차트 에이전트의 Mock 의견을 생성합니다.</summary>
        private static AiAnalysisResult SimulateChartAgent(string ticker, IndicatorDto indicators)
        {
            if (indicators.Rsi14 < 30 && indicators.Position < 0.2m)
            {
                return new AiAnalysisResult
                {
                    Signal          = SmartOrderSignal.BUY,
                    ConfidenceScore = 0.6m + (decimal)_random.NextDouble() * 0.3m,
                    Reason          = "과매도 구간 및 하단 지지선 근접으로 기술적 반등 예상 (Mock 차트)"
                };
            }
            if (indicators.Rsi14 > 70 && indicators.Position > 0.8m)
            {
                return new AiAnalysisResult
                {
                    Signal          = SmartOrderSignal.SELL,
                    ConfidenceScore = 0.6m + (decimal)_random.NextDouble() * 0.3m,
                    Reason          = "과매수 구간 진입 및 저항선 도달로 조정 예상 (Mock 차트)"
                };
            }
            return new AiAnalysisResult
            {
                Signal          = SmartOrderSignal.HOLD,
                ConfidenceScore = 0.3m + (decimal)_random.NextDouble() * 0.2m,
                Reason          = "특별한 기술적 추세가 보이지 않아 관망 권장 (Mock 차트)"
            };
        }

        /// <summary>MACD와 Position 기반으로 펀더멘털 에이전트의 Mock 의견을 생성합니다.</summary>
        private static AiAnalysisResult SimulateFundamentalAgent(string ticker, IndicatorDto indicators)
        {
            // 펀더멘털 에이전트는 MACD Histogram과 Position으로 거시적 모멘텀을 시뮬레이션
            if (indicators.MacdHistogram > 0 && indicators.Position < 0.3m)
            {
                return new AiAnalysisResult
                {
                    Signal          = SmartOrderSignal.BUY,
                    ConfidenceScore = 0.55m + (decimal)_random.NextDouble() * 0.25m,
                    Reason          = "모멘텀 회복 신호와 저평가 구간이 겹쳐 중기 진입 기회로 판단 (Mock 펀더멘털)"
                };
            }
            if (indicators.MacdHistogram < 0 && indicators.Position > 0.75m)
            {
                return new AiAnalysisResult
                {
                    Signal          = SmartOrderSignal.SELL,
                    ConfidenceScore = 0.55m + (decimal)_random.NextDouble() * 0.25m,
                    Reason          = "상단 과열 구간에서 모멘텀 약화, 거시적 리스크 회피 권장 (Mock 펀더멘털)"
                };
            }
            return new AiAnalysisResult
            {
                Signal          = SmartOrderSignal.HOLD,
                ConfidenceScore = 0.3m + (decimal)_random.NextDouble() * 0.2m,
                Reason          = "거시적 관점에서 방향성이 불분명하여 관망 권장 (Mock 펀더멘털)"
            };
        }
    }
}
