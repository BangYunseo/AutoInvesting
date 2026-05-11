namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// 투자 전략 DTO.
    /// TB_INVEST_STRATEGY 테이블과 매핑됩니다.
    /// 전략별로 종목과 매수 수량을 정의합니다.
    /// </summary>
    public class StrategyDto
    {
        // 전략 고유 ID (DB 자동 증가)
        public int StrategyId { get; set; }

        // 전략명 (예: "사용자정의")
        public string StrategyName { get; set; } = string.Empty;

        // 종목 코드 (예: "QQQM", "SCHD")
        public string Ticker { get; set; } = string.Empty;

        // 매수 수량 (주 단위, 정수)
        public int Qty { get; set; }

        /// <summary>
        /// 전략 유형 — 퀀트 필터에서 매수/매도 조건 조합을 결정하는 기준:
        ///   MEAN_REVERSION — 평균회귀 (가격 하위 + RSI 과매도 + BB 하단)
        ///   MOMENTUM       — 모멘텀 (RSI 상승 + MACD 골든크로스)
        ///   MIXED          — 혼합 (Position + RSI 필터)
        /// </summary>
        public string StrategyType { get; set; } = "MEAN_REVERSION";
    }
}