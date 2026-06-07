using System;

namespace AutoInvest.Data.DTO
{
    public class AiPerformanceDto
    {
        public int PerfId { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public string Signal { get; set; } = string.Empty;
        public decimal PriceAtSignal { get; set; }
        public decimal? PriceLater { get; set; }
        public decimal? WinRate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? EvaluatedAt { get; set; }
    }
}
