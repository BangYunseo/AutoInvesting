namespace AutoInvest.Data.DTO
{
    public class StrategySummaryDto
    {
        public string StrategyName { get; set; } = string.Empty;
        public string StrategyType { get; set; } = string.Empty;
        public int TickerCount { get; set; }
    }
}
