using AutoInvest.Data.DTO;
using System.Threading.Tasks;

namespace AutoInvest.Core
{
    /// <summary>
    /// AI 분석 엔진의 분석 결과를 나타내는 클래스입니다.
    /// </summary>
    public class AiAnalysisResult
    {
        public SmartOrderSignal Signal { get; set; } = SmartOrderSignal.HOLD;
        
        /// <summary>
        /// AI 판단의 확신도 (0.0 ~ 1.0)
        /// </summary>
        public decimal ConfidenceScore { get; set; } = 0m;
        
        /// <summary>
        /// 판단 근거
        /// </summary>
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// AI 시장 분석 엔진 인터페이스 (Phase 4)
    /// </summary>
    public interface IMarketAnalyzer
    {
        /// <summary>
        /// 특정 종목에 대해 AI 분석을 수행합니다.
        /// </summary>
        /// <param name="ticker">종목 코드</param>
        /// <param name="indicators">현재 시장 지표 (RSI, MACD 등)</param>
        /// <returns>분석 결과</returns>
        Task<AiAnalysisResult> AnalyzeAsync(string ticker, IndicatorDto indicators);
    }
}
