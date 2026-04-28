namespace AutoInvest.Data.DTO
{
    public class PriceRangeDto
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal High { get; set; }       // N일 최고가 (USD)
        public decimal Low { get; set; }        // N일 최저가 (USD)
        public decimal Current { get; set; }    // 현재가 (USD)
        public int Days { get; set; }           // 조회 기간 (일)

        /// <summary>
        /// 현재가의 위치 — 0.0(최저가) ~ 1.0(최고가)
        /// </summary>
        public decimal Position { get; set; }
    }
}
