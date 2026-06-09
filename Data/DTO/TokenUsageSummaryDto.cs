using System;

namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// 에이전트 유형별 토큰 사용량 집계 결과 (모니터링용).
    /// </summary>
    public class AgentTokenSummaryDto
    {
        public string AgentType { get; set; } = string.Empty;
        public int CallCount { get; set; }
        public long PromptTokens { get; set; }
        public long CompletionTokens { get; set; }
        public long TotalTokens { get; set; }
    }

    /// <summary>
    /// 일자별 토큰 사용량 집계 결과 (모니터링 추이 차트용).
    /// </summary>
    public class DailyTokenUsageDto
    {
        public string Date { get; set; } = string.Empty;
        public int CallCount { get; set; }
        public long PromptTokens { get; set; }
        public long CompletionTokens { get; set; }
        public long TotalTokens { get; set; }
    }
}
