namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// 매도 시 예상 양도소득세·수수료 추정 결과 (순수 계산 결과 — 판단/타이밍 아님).
    ///
    /// 미국 상장 ETF 직접투자(해외주식 양도소득세) 기준의 "추정치"입니다.
    /// 실제 신고는 결제일 기준환율·증권사 취득가 산정방식에 따라 달라질 수 있습니다(세무 조언 아님).
    /// </summary>
    public class SellTaxEstimateDto
    {
        /// <summary>종목 코드 (예: QQQM)</summary>
        public string Ticker { get; set; } = string.Empty;

        /// <summary>매도 수량 (주)</summary>
        public int Qty { get; set; }

        /// <summary>매도 단가 (USD)</summary>
        public decimal SellPriceUsd { get; set; }

        /// <summary>평균 매입 단가 (USD, 취득가)</summary>
        public decimal AvgPriceUsd { get; set; }

        /// <summary>적용 환율 (USD→KRW)</summary>
        public decimal ExchangeRate { get; set; }

        /// <summary>매도 대금 (원) = 매도단가 × 수량 × 환율</summary>
        public decimal SellAmountKrw { get; set; }

        /// <summary>예상 양도차익 (USD) = (매도단가 − 평균매입단가) × 수량. 음수면 손실.</summary>
        public decimal GainUsd { get; set; }

        /// <summary>예상 양도차익 (원)</summary>
        public decimal GainKrw { get; set; }

        /// <summary>남은 기본공제 (원) = max(0, 연 공제 − 올해 이미 실현한 차익)</summary>
        public decimal RemainingDeductionKrw { get; set; }

        /// <summary>과세표준 (원) = max(0, 양도차익 − 남은공제)</summary>
        public decimal TaxableBaseKrw { get; set; }

        /// <summary>예상 양도소득세 (원) = 과세표준 × 세율. 추정치.</summary>
        public decimal EstimatedTaxKrw { get; set; }

        /// <summary>예상 매도 수수료 (원) = 매도대금 × 수수료율. 추정치.</summary>
        public decimal EstimatedFeeKrw { get; set; }

        /// <summary>세금 없이 팔 수 있는 최대 수량 (주). -1이면 무제한(손실/본전이라 과세 없음).</summary>
        public int MaxTaxFreeQty { get; set; }

        /// <summary>과세 발생 여부 (예상 세금 &gt; 0)</summary>
        public bool IsTaxable { get; set; }

        /// <summary>취득가(평균매입단가)를 확인할 수 없어 추정이 불가능한 경우 true (이때 가드는 건너뜀)</summary>
        public bool CostBasisUnknown { get; set; }
    }
}
