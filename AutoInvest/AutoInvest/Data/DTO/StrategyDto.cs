namespace AutoInvest.Data.DTO
{
    public class StrategyDto
    {
        public int StrategyId { get; set; }
        public string StrategyName { get; set; }
        public string Ticker { get; set; }
        public double Weight { get; set; }
    }
}