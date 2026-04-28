namespace AutoInvest.Data.DTO
{
    public class HoldingDto
    {
        public string Ticker { get; set; } = string.Empty;
        public int Qty { get; set; }
        public decimal AvgPrice { get; set; }     // 평균 매입가 (USD)
        public decimal CurrentPrice { get; set; } // 현재가 (USD)
        public decimal ProfitRate { get; set; }   // 수익률
    }
}
