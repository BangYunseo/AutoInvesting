namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// 실패한 종목(1건)
    /// </summary>
    public class DcaBuyFailure
    {
        public string Ticker { get; set; } = string.Empty;      // 종목 코드

        public int Qty { get; set; }                            // 매수 수량

        public string Error { get; set; } = string.Empty;       // 실패 사유
    }
}
