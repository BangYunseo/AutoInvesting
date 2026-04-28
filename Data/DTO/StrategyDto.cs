namespace AutoInvest.Data.DTO
{
    public class StrategyDto
    {
        public int StrategyId { get; set; }
        public string StrategyName { get; set; } = string.Empty;
        public string Ticker { get; set; } = string.Empty;
        public double Weight { get; set; }
    }
}