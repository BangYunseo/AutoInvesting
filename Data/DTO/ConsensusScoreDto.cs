namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// 확률 기반 합의 스코어링 결과 DTO (Phase 4-e).
    /// 퀀트 + 차트AI + 펀더멘털AI 세 에이전트의 가중치 × 확신도 합산 결과를 보관합니다.
    ///
    /// 계산 공식:
    ///   BuyProbability  = QUANT_WEIGHT(BUY 시 고정) + CHART_AI_WEIGHT × 차트확신도 + FUND_AI_WEIGHT × 펀더멘털확신도
    ///   SellProbability = (동일 구조, SELL 신호 기준)
    ///
    /// 활용:
    ///   - SmartOrderEngine에서 매매 판정의 수치적 근거로 사용
    ///   - TB_MARKET_SNAPSHOT에 저장되어 Phase 5 적응형 임계값 산출의 재료
    /// </summary>
    public class ConsensusScoreDto
    {
        /// <summary>최종 매수 확률 (0.0 ~ 1.0)</summary>
        public decimal BuyProbability { get; set; }

        /// <summary>최종 매도 확률 (0.0 ~ 1.0)</summary>
        public decimal SellProbability { get; set; }

        /// <summary>퀀트 기여도 (BUY/SELL 충족 시 QUANT_WEIGHT, 미충족 시 0)</summary>
        public decimal QuantContribution { get; set; }

        /// <summary>차트 AI 에이전트 기여도 (CHART_AI_WEIGHT × 확신도)</summary>
        public decimal ChartAiContribution { get; set; }

        /// <summary>펀더멘털 AI 에이전트 기여도 (FUND_AI_WEIGHT × 확신도)</summary>
        public decimal FundamentalAiContribution { get; set; }

        /// <summary>적용된 임계값 (BUY_THRESHOLD 또는 SELL_THRESHOLD)</summary>
        public decimal Threshold { get; set; }

        /// <summary>임계값 달성 여부 (매수 또는 매도 확률이 임계값 이상)</summary>
        public bool ThresholdMet => BuyProbability >= Threshold || SellProbability >= Threshold;

        /// <summary>매수 임계값 미달 시 부족분 (양수 = 부족, 음수 = 초과)</summary>
        public decimal BuyGap => Threshold - BuyProbability;

        /// <summary>매도 임계값 미달 시 부족분 (양수 = 부족, 음수 = 초과)</summary>
        public decimal SellGap => Threshold - SellProbability;
    }
}
