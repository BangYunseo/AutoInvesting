using System;

namespace AutoInvest.Data.DTO
{
    public class TokenUsageDto
    {
        public int UsageId { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public string AgentType { get; set; } = string.Empty;
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
