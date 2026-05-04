namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// 매매 시점 시장 지표 스냅샷 DTO.
    /// SmartOrderEngine이 주문 실행 시 TB_MARKET_SNAPSHOT에 저장합니다.
    /// Phase 4 AI 모델의 학습 데이터(Feature)로 활용됩니다.
    ///
    /// 활용 시나리오:
    ///   SELECT * FROM TB_MARKET_SNAPSHOT WHERE SIGNAL = 'BUY'
    ///   → "이런 지표 조합일 때 매수했더니 수익이 났다" 패턴 학습
    /// </summary>
    public class MarketSnapshotDto
    {
        // 스냅샷 고유 ID (DB 자동 증가)
        public int SnapshotId { get; set; }

        // 스냅샷 저장 일시
        public DateTime SnapDate { get; set; }

        // 종목 코드 (예: "QQQM")
        public string Ticker { get; set; } = string.Empty;

        // 매매 시점 현재가 (USD)
        public decimal Price { get; set; }

        // 20일 가격 위치 (0.0~1.0)
        public decimal Position20d { get; set; }

        // RSI 14일 (0~100)
        public decimal Rsi14 { get; set; }

        // MACD Line 값 (EMA12 - EMA26)
        public decimal MacdValue { get; set; }

        // MACD Signal Line 값 (MACD의 EMA9)
        public decimal MacdSignal { get; set; }

        // 볼린저밴드 상단 (SMA20 + 2σ)
        public decimal BbUpper { get; set; }

        // 볼린저밴드 하단 (SMA20 - 2σ)
        public decimal BbLower { get; set; }

        // 매매 신호 ("BUY" / "SELL" / "HOLD")
        public string Signal { get; set; } = string.Empty;
    }
}
