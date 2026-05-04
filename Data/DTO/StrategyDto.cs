namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// 투자 전략 DTO.
    /// TB_INVEST_STRATEGY 테이블과 매핑됩니다.
    /// 전략별로 종목과 비중(또는 수량)을 정의합니다.
    ///
    /// 기본 전략 예시:
    ///   안정형: SCHD 40%, QQQM 30%, GLD 20%, JEPI 10%
    ///   공격형: QQQM 60%, SPLG 25%, GLD 15%
    ///   사용자정의: Weight에 수량을 저장하여 직접 관리
    /// </summary>
    public class StrategyDto
    {
        // 전략 고유 ID (DB 자동 증가)
        public int StrategyId { get; set; }

        // 전략명 (예: "안정형", "공격형", "사용자정의")
        public string StrategyName { get; set; } = string.Empty;

        // 종목 코드 (예: "QQQM", "SCHD")
        public string Ticker { get; set; } = string.Empty;

        // 비중 (0.0~1.0) 또는 수량 (사용자정의 전략에서는 Weight에 수량을 저장)
        public double Weight { get; set; }

        /// <summary>
        /// 전략 유형 — 퀀트 필터에서 매수/매도 조건 조합을 결정하는 기준:
        ///   MEAN_REVERSION — 평균회귀 (가격 하위 + RSI 과매도 + BB 하단)
        ///   MOMENTUM       — 모멘텀 (RSI 상승 + MACD 골든크로스)
        ///   MIXED          — 혼합 (Position + RSI 필터)
        /// </summary>
        public string StrategyType { get; set; } = "MEAN_REVERSION";
    }
}