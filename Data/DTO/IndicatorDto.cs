namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// 퀀트 지표 계산 결과 DTO.
    /// QuantIndicator.CalculateAll()의 반환값으로 사용되며,
    /// QuantFilter에서 매수/매도 조건 판단에 활용됩니다.
    /// </summary>
    public class IndicatorDto
    {
        // 종목 코드 (예: "QQQM")
        public string Ticker { get; set; } = string.Empty;

        // ── Position (20일 가격 위치) ──

        /// <summary>
        /// 20일 가격 범위 내 위치 — 0.0(최저) ~ 1.0(최고)
        /// 공식: (현재가 - 20일최저가) / (20일최고가 - 20일최저가)
        /// 0.10 이하 → 하위 10% (평균회귀 매수 신호)
        /// 0.90 이상 → 상위 10% (매도 신호)
        /// </summary>
        public decimal Position { get; set; }

        // ── RSI (상대강도지수) ──

        /// <summary>
        /// RSI 14일 — 0~100 범위
        /// 30 이하 → 과매도 (매수 기회)
        /// 70 이상 → 과매수 (매도 기회)
        /// </summary>
        public decimal Rsi14 { get; set; }

        // ── MACD (이동평균수렴확산) ──

        /// <summary>
        /// MACD Line = EMA(12) - EMA(26)
        /// 양수 → 단기 이평이 장기 이평 위 (상승 추세)
        /// 음수 → 단기 이평이 장기 이평 아래 (하락 추세)
        /// </summary>
        public decimal MacdLine { get; set; }

        /// <summary>
        /// MACD Signal Line = MACD Line의 EMA(9)
        /// MACD Line이 Signal을 상향 돌파 → 골든크로스 (매수)
        /// MACD Line이 Signal을 하향 돌파 → 데드크로스 (매도)
        /// </summary>
        public decimal MacdSignal { get; set; }

        /// <summary>
        /// MACD Histogram = MACD Line - Signal Line
        /// 양수 → 골든크로스 상태 (상승 모멘텀)
        /// 음수 → 데드크로스 상태 (하락 모멘텀)
        /// </summary>
        public decimal MacdHistogram { get; set; }

        // ── 볼린저밴드 (변동성 밴드) ──

        /// <summary>
        /// 볼린저밴드 상단 = SMA(20) + 2σ
        /// 현재가가 이 선 위에 있으면 과매수 위험
        /// </summary>
        public decimal BbUpper { get; set; }

        /// <summary>
        /// 볼린저밴드 중간 = SMA(20) — 20일 단순이동평균
        /// </summary>
        public decimal BbMiddle { get; set; }

        /// <summary>
        /// 볼린저밴드 하단 = SMA(20) - 2σ
        /// 현재가가 이 선 아래에 있으면 과매도 (평균회귀 매수 기회)
        /// </summary>
        public decimal BbLower { get; set; }
    }
}
