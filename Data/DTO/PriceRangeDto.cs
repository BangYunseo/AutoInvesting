namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// N일 가격 범위 DTO.
    /// SmartOrderEngine이 매매 판단 시 사용하는 가격 범위 정보입니다.
    /// Position 값으로 현재가가 최근 N일 범위에서 어디에 위치하는지 나타냅니다.
    /// </summary>
    public class PriceRangeDto
    {
        // 종목 코드 (예: "QQQM")
        public string Ticker { get; set; } = string.Empty;

        // N일 최고가 (USD)
        public decimal High { get; set; }

        // N일 최저가 (USD)
        public decimal Low { get; set; }

        // 현재가 (USD)
        public decimal Current { get; set; }

        // 조회 기간 (일) — 기본 20일
        public int Days { get; set; }

        /// <summary>
        /// 현재가의 위치 — 0.0(최저가) ~ 1.0(최고가)
        /// 공식: (Current - Low) / (High - Low)
        /// 0.0에 가까울수록 저점, 1.0에 가까울수록 고점
        /// </summary>
        public decimal Position { get; set; }
    }
}
