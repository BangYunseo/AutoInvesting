using AutoInvest.Data.DTO;

namespace AutoInvest.Core.Advisors
{
    /// <summary>
    /// 부가 조언 생성에 필요한 매매 컨텍스트 (Phase 5-e).
    /// SmartOrderEngine이 판정 직후 구성하여 각 <see cref="IContextAdvisor"/>에 전달합니다.
    /// </summary>
    public class AdvisoryContext
    {
        /// <summary>종목 코드</summary>
        public string Ticker { get; set; } = string.Empty;

        /// <summary>전략 유형 (MEAN_REVERSION / MOMENTUM / MIXED)</summary>
        public string StrategyType { get; set; } = string.Empty;

        /// <summary>최종 합의 신호 (BUY/SELL/HOLD)</summary>
        public SmartOrderSignal FinalSignal { get; set; }

        /// <summary>퀀트 단독 신호 (AI 합산 이전)</summary>
        public SmartOrderSignal QuantSignal { get; set; }

        /// <summary>현재가 (USD)</summary>
        public decimal CurrentPriceUsd { get; set; }

        /// <summary>퀀트 지표 (없을 수 있음 — 향후 변동성 어드바이저 등에서 활용)</summary>
        public IndicatorDto? Indicators { get; set; }

        /// <summary>매수 진입 의향 여부 (퀀트 또는 최종 신호가 BUY).</summary>
        public bool HasBuyIntent =>
            QuantSignal == SmartOrderSignal.BUY || FinalSignal == SmartOrderSignal.BUY;

        /// <summary>매도 의향 여부 (퀀트 또는 최종 신호가 SELL).</summary>
        public bool HasSellIntent =>
            QuantSignal == SmartOrderSignal.SELL || FinalSignal == SmartOrderSignal.SELL;
    }
}
