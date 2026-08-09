namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// 보유 종목(잔고) DTO.
    /// IBrokerClient.GetHoldingsAsync()의 반환값으로 사용됩니다.
    /// </summary>
    public class HoldingDto
    {
        // 종목 코드 (예: "QQQ")
        public string Ticker { get; set; } = string.Empty;

        // 보유 수량 (주)
        public int Qty { get; set; }

        // 평균 매입 단가 (USD)
        public decimal AvgPrice { get; set; }

        // 현재가 (USD)
        public decimal CurrentPrice { get; set; }

        // 수익률 (소수점, 예: 0.05 = 5% 수익)
        public decimal ProfitRate { get; set; }
    }
}
