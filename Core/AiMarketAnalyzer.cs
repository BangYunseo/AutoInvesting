using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using System;
using System.Threading.Tasks;

namespace AutoInvest.Core
{
    /// <summary>
    /// AI 시장 분석 엔진의 임시(Mock) 구현체입니다. (Phase 4 초기 단계)
    /// 향후 LLM 외부 API 연동이나 로컬 ML 모델 호출 로직으로 교체될 예정입니다.
    /// </summary>
    public class AiMarketAnalyzer : IMarketAnalyzer
    {
        private static readonly Random _random = new Random();

        public Task<AiAnalysisResult> AnalyzeAsync(string ticker, IndicatorDto indicators)
        {
            // TODO [Phase 4] 실제 AI 모델 연동 시, TB_MARKET_SNAPSHOT에 쌓인 데이터를
            // 기반으로 추론하거나, 최신 뉴스 데이터/거시 경제 지표 등을 프롬프트로 
            // 묶어 OpenAI, Anthropic 등의 API로 전송하고 결과를 파싱해야 합니다.
            
            Logger.Info($"[AiAnalyzer] {ticker}에 대한 AI 시장 분석을 시도합니다. (현재 Mock 모드)");

            // 가상의 딜레이 (네트워크 지연 시뮬레이션)
            Task.Delay(500).Wait();

            var result = new AiAnalysisResult();

            // 단순히 RSI와 Position을 기반으로 가짜 확신도 생성 (Fallback 테스트용)
            // 실제 환경에서는 AI가 직접 차트나 다른 데이터를 보고 스코어를 부여해야 합니다.
            if (indicators.Rsi14 < 30 && indicators.Position < 0.2m)
            {
                result.Signal = SmartOrderSignal.BUY;
                // 0.6 ~ 0.9 사이의 임의 확신도
                result.ConfidenceScore = 0.6m + (decimal)_random.NextDouble() * 0.3m;
                result.Reason = "과매도 구간 및 하단 지지선 근접으로 인한 기술적 반등 예상 (Mock AI)";
            }
            else if (indicators.Rsi14 > 70 && indicators.Position > 0.8m)
            {
                result.Signal = SmartOrderSignal.SELL;
                result.ConfidenceScore = 0.6m + (decimal)_random.NextDouble() * 0.3m;
                result.Reason = "과매수 구간 진입 및 저항선 도달로 인한 조정 예상 (Mock AI)";
            }
            else
            {
                result.Signal = SmartOrderSignal.HOLD;
                result.ConfidenceScore = 0.3m + (decimal)_random.NextDouble() * 0.2m;
                result.Reason = "특별한 추세가 보이지 않는 횡보장세로 관망 권장 (Mock AI)";
            }

            Logger.Info($"[AiAnalyzer] 분석 완료: {ticker} -> {result.Signal} (확신도: {result.ConfidenceScore:F2})");
            return Task.FromResult(result);
        }
    }
}
