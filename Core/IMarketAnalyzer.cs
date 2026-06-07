using AutoInvest.Data.DTO;
using System.Threading.Tasks;

namespace AutoInvest.Core
{
    /// <summary>
    /// AI 분석 엔진의 단일 에이전트 분석 결과를 나타냅니다.
    /// </summary>
    public class AiAnalysisResult
    {
        /// <summary>BUY / SELL / HOLD 신호</summary>
        public SmartOrderSignal Signal { get; set; } = SmartOrderSignal.HOLD;

        /// <summary>AI 판단의 확신도 (0.0 ~ 1.0)</summary>
        public decimal ConfidenceScore { get; set; } = 0m;

        /// <summary>판단 근거 (한국어 1~2문장)</summary>
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 다중 에이전트(투자 위원회) 분석 결과를 나타냅니다 (Phase 4-d).
    /// 차트 기술 에이전트와 거시경제·펀더멘털 에이전트의 의견을 분리 보관합니다.
    /// SmartOrderEngine.CombineSignals()에서 퀀트 신호와 함께 3자 만장일치 합의에 사용됩니다.
    /// </summary>
    public class MultiAgentAnalysisResult
    {
        /// <summary>차트 기술 애널리스트 에이전트 의견</summary>
        public AiAnalysisResult ChartAgent { get; set; } = new AiAnalysisResult();

        /// <summary>거시경제·펀더멘털 애널리스트 에이전트 의견</summary>
        public AiAnalysisResult FundamentalAgent { get; set; } = new AiAnalysisResult();

        /// <summary>두 에이전트가 모두 정상 응답했는지 여부 (false = 하나 이상 fallback)</summary>
        public bool IsFullConsensus { get; set; } = false;
    }

    /// <summary>
    /// AI 시장 분석 엔진 인터페이스 (Phase 4).
    /// Phase 4-d에서 반환 타입이 MultiAgentAnalysisResult로 확장되었습니다.
    /// </summary>
    public interface IMarketAnalyzer
    {
        /// <summary>
        /// 특정 종목에 대해 다중 에이전트 AI 분석을 수행합니다.
        /// </summary>
        /// <param name="ticker">종목 코드</param>
        /// <param name="indicators">현재 시장 지표 (RSI, MACD 등)</param>
        /// <param name="ohlcv">최근 OHLCV 데이터 (선택적, 차트 에이전트용)</param>
        /// <returns>차트 에이전트 + 펀더멘털 에이전트의 분리된 분석 결과</returns>
        Task<MultiAgentAnalysisResult> AnalyzeAsync(
            string ticker,
            IndicatorDto indicators,
            System.Collections.Generic.List<OhlcvDto>? ohlcv = null);
    }
}
