namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// Phase 5-d: 에이전트(퀀트/차트AI/펀더멘털AI)별 실측 적중률 집계 결과 DTO.
    /// TB_MARKET_SNAPSHOT에 저장된 에이전트별 방향 신호를, 일정 기간(Horizon) 경과 후
    /// 실제 가격 변동과 대조하여 "얼마나 맞췄는지"를 나타냅니다.
    /// </summary>
    public class AgentAccuracyDto
    {
        /// <summary>에이전트 이름 ("퀀트" / "차트AI" / "펀더멘털AI")</summary>
        public string AgentName { get; set; } = string.Empty;

        /// <summary>평가 가능한(미래 가격이 존재하는) BUY 신호 수</summary>
        public int BuySignals { get; set; }

        /// <summary>평가 가능한(미래 가격이 존재하는) SELL 신호 수</summary>
        public int SellSignals { get; set; }

        /// <summary>적중률 산출에 사용된 전체 표본 수 (BUY+SELL)</summary>
        public int SampleCount { get; set; }

        /// <summary>적중 건수 (BUY 후 상승 또는 SELL 후 하락)</summary>
        public int HitCount { get; set; }

        /// <summary>적중률 (0.0~1.0). 표본이 없으면 0.</summary>
        public decimal WinRate { get; set; }
    }
}
