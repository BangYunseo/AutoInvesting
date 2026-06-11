namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// Phase 5-d: 합의 가중치 조합(Scheme) A/B 백테스트 결과 DTO.
    /// 누적 스냅샷(에이전트별 신호+확신도)에 가상의 가중치 조합을 적용해
    /// 재계산한 매수 확률이 임계값을 넘었을 때의 가상 성과를 나타냅니다.
    /// ⚠️ 검증용 리포트 전용 — 실제 매매 가중치에 자동 반영되지 않습니다.
    /// </summary>
    public class WeightSchemeResultDto
    {
        /// <summary>가중치 조합 이름 (예: "기본(40/30/30)")</summary>
        public string SchemeName { get; set; } = string.Empty;

        /// <summary>퀀트 가중치</summary>
        public decimal QuantWeight { get; set; }

        /// <summary>차트 AI 가중치</summary>
        public decimal ChartWeight { get; set; }

        /// <summary>펀더멘털 AI 가중치</summary>
        public decimal FundWeight { get; set; }

        /// <summary>이 조합에서 매수 신호가 발생했을 표본 수 (재계산 확률 ≥ 임계값)</summary>
        public int TriggerCount { get; set; }

        /// <summary>매수 발생 표본 중 실제로 가격이 상승한 건수</summary>
        public int HitCount { get; set; }

        /// <summary>가상 승률 (HitCount / TriggerCount). 발생 0건이면 0.</summary>
        public decimal WinRate { get; set; }

        /// <summary>매수 발생 표본의 평균 미래 수익률(%)</summary>
        public decimal AvgForwardReturnPct { get; set; }
    }
}
