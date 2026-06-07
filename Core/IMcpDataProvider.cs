using System.Threading.Tasks;

namespace AutoInvest.Core
{
    /// <summary>
    /// MCP(Model Context Protocol) 기반 외부 데이터 공급자 인터페이스 (Phase 4-d 골격).
    ///
    /// 현재 Phase에서는 인터페이스 정의만 존재하며, 실제 구현체(MCP 서버 연결)는
    /// 유료 데이터 라이선스(FactSet, Bloomberg 등) 확보 후 구현합니다.
    ///
    /// 향후 구현체가 추가되면 GeminiMarketAnalyzer의 BuildFundamentalUserPrompt에
    /// 이 인터페이스를 통해 실제 거시경제 데이터를 주입할 수 있습니다.
    /// </summary>
    public interface IMcpDataProvider
    {
        /// <summary>
        /// 특정 종목의 최신 뉴스 센티먼트 요약을 가져옵니다.
        /// </summary>
        /// <param name="ticker">종목 코드</param>
        /// <returns>뉴스 센티먼트 요약 텍스트 (예: "긍정 65%, 부정 20%, 중립 15%")</returns>
        Task<string> GetNewsSentimentAsync(string ticker);

        /// <summary>
        /// 현재 거시경제 지표 요약을 가져옵니다.
        /// 예: FRED 기준금리, CPI, 달러 인덱스(DXY), VIX 등
        /// </summary>
        /// <returns>거시경제 컨텍스트 텍스트</returns>
        Task<string> GetMacroContextAsync();
    }
}
