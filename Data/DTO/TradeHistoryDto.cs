using System;
namespace AutoInvest.Data.DTO
{
    public class TradeHistoryDto
    {
        public int TradeId { get; set; }
        public DateTime TradeDate { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public string OrderType { get; set; } = string.Empty; // BUY / SELL
        public int Qty { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = string.Empty; // PENDING / FILLED / FAILED
        public string OrderNo { get; set; } = string.Empty;
    }
}