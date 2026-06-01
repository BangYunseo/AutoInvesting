using System;

namespace AutoInvest.Data.DTO
{
    public class SellPlanDto
    {
        public int PlanId { get; set; }
        public string Ticker { get; set; } = string.Empty;
        
        /// <summary>
        /// PRICE, TIME, CHART
        /// </summary>
        public string StrategyType { get; set; } = string.Empty;
        
        public int TargetQty { get; set; }
        public int SoldQty { get; set; }
        
        /// <summary>
        /// ACTIVE, COMPLETED, CANCELLED
        /// </summary>
        public string Status { get; set; } = "ACTIVE";
        
        /// <summary>
        /// JSON parameters for the specific strategy
        /// (e.g., {"TargetPrices": [250, 260], "Days": 5})
        /// </summary>
        public string Params { get; set; } = "{}";
        
        public DateTime CreatedAt { get; set; }
    }
}
