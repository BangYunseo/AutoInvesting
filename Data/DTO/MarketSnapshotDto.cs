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

        // ── Phase 4-e: 확률 기반 합의 점수 ──

        /// <summary>매수 확률 (0.0~1.0, 가중 합산 결과)</summary>
        public decimal BuyProbability { get; set; }

        /// <summary>매도 확률 (0.0~1.0, 가중 합산 결과)</summary>
        public decimal SellProbability { get; set; }

        /// <summary>차트 AI 에이전트 확신도 (0.0~1.0)</summary>
        public decimal ChartAiScore { get; set; }

        /// <summary>펀더멘털 AI 에이전트 확신도 (0.0~1.0)</summary>
        public decimal FundAiScore { get; set; }

        // ── Phase 5-d: 에이전트별 방향 신호 (적중률 분석 / 가중치 A/B 검증용) ──

        /// <summary>퀀트 필터 신호 ("BUY" / "SELL" / "HOLD"). Phase 5-d 이전 데이터는 빈 문자열.</summary>
        public string QuantSignal { get; set; } = string.Empty;

        /// <summary>차트 AI 에이전트 신호 ("BUY" / "SELL" / "HOLD"). Phase 5-d 이전 데이터는 빈 문자열.</summary>
        public string ChartAiSignal { get; set; } = string.Empty;

        /// <summary>펀더멘털 AI 에이전트 신호 ("BUY" / "SELL" / "HOLD"). Phase 5-d 이전 데이터는 빈 문자열.</summary>
        public string FundAiSignal { get; set; } = string.Empty;
    }
}
