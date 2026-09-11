namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// 주문 체결 내역(1건)
    /// </summary>
    public class OrderFillDto
    {
        public string OrderNo { get; set; } = string.Empty;             // 주문번호

        public string Ticker { get; set; } = string.Empty;              // 종목코드

        public int OrderQty { get; set; }                               // 주문 수량

        public int FilledQty { get; set; }                              // 체결 수량

        public int UnfilledQty { get; set; }                            // 미체결 수량

        public string StatusName { get; set; } = string.Empty;          // 처리 상태

        public string RejectReason { get; set; } = string.Empty;        // 거부 사유
    }
}
