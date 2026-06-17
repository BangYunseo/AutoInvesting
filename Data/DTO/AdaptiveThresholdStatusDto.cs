namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// 종목별 적응형 임계값 진단 상태.
    /// 적응형 임계값(AdaptiveThresholdEngine)이 누적 데이터 기반으로 작동 중인지,
    /// 아직 기본값을 사용하는지 한눈에 가시화하기 위한 점검용 객체입니다.
    /// </summary>
    public class AdaptiveThresholdStatusDto
    {
        /// <summary>종목 코드</summary>
        public string Ticker { get; set; } = string.Empty;

        /// <summary>매수 확률(BuyProbability) 누적 표본 수</summary>
        public int BuySampleCount { get; set; } = 0;

        /// <summary>매도 확률(SellProbability) 누적 표본 수</summary>
        public int SellSampleCount { get; set; } = 0;

        /// <summary>현재 적용 중인 매수 임계값 (0.0 ~ 1.0)</summary>
        public decimal BuyThreshold { get; set; } = 0m;

        /// <summary>매수 임계값 산출 사유 (기본값/적응값 구분 포함)</summary>
        public string BuyReason { get; set; } = string.Empty;

        /// <summary>현재 적용 중인 매도 임계값 (0.0 ~ 1.0)</summary>
        public decimal SellThreshold { get; set; } = 0m;

        /// <summary>매도 임계값 산출 사유 (기본값/적응값 구분 포함)</summary>
        public string SellReason { get; set; } = string.Empty;

        /// <summary>적응형 임계값이 데이터 기반으로 활성화되었는지 여부 (true면 자동 조정 중)</summary>
        public bool IsAdaptiveActive { get; set; } = false;

        /// <summary>적응형 활성화에 필요한 최소 표본 수</summary>
        public int MinDataPoints { get; set; } = 0;
    }
}
