using System;
using System.Collections.Generic;

namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// 백테스팅 결과 DTO.
    /// BacktestEngine.RunAsync()의 반환값으로, 전략의 과거 수익성 검증 결과를 담습니다.
    /// 수익률, MDD, 승률 등 전략 검증 지표를 포함합니다.
    /// </summary>
    public class BacktestResultDto
    {
        // 전략명 (예: "안정형", "백테스트")
        public string StrategyName { get; set; } = string.Empty;

        // 전략 유형 (MEAN_REVERSION / MOMENTUM / MIXED)
        public string StrategyType { get; set; } = string.Empty;

        // 백테스트 시작일
        public DateTime StartDate { get; set; }

        // 백테스트 종료일
        public DateTime EndDate { get; set; }

        // 초기 투자금 (KRW)
        public decimal InitialAmount { get; set; }

        // 최종 평가금액 (KRW) — 현금 + 보유주식 시가
        public decimal FinalAmount { get; set; }

        /// <summary>
        /// 총 수익률 (%)
        /// 공식: (FinalAmount - InitialAmount) / InitialAmount × 100
        /// 양수 → 수익, 음수 → 손실
        /// </summary>
        public decimal ReturnRate { get; set; }

        /// <summary>
        /// 최대 낙폭 MDD (%) — Maximum Drawdown
        /// 최고점 대비 최대 하락 비율. 작을수록 안정적인 전략.
        /// 예: 15.3 → 최고점 대비 15.3% 하락한 구간이 있었음
        /// </summary>
        public decimal MaxDrawdown { get; set; }

        // 총 거래 횟수 (매수 + 매도)
        public int TotalTrades { get; set; }

        // 수익 거래 횟수 (매도 시 이익 실현)
        public int WinTrades { get; set; }

        /// <summary>
        /// 승률 (%) — 수익 거래 / 총 매도 거래 × 100
        /// 예: 60.0 → 매도 10번 중 6번은 수익
        /// </summary>
        public decimal WinRate { get; set; }

        // 개별 거래 내역 리스트 (시간순)
        public List<BacktestTradeDto> Trades { get; set; } = new();
    }

    /// <summary>
    /// 백테스팅 개별 거래 기록.
    /// 각 매수/매도 시점의 상세 정보를 담습니다.
    /// </summary>
    public class BacktestTradeDto
    {
        // 거래 일자
        public DateTime Date { get; set; }

        // 종목 코드
        public string Ticker { get; set; } = string.Empty;

        // 거래 유형 ("BUY" = 매수, "SELL" = 매도)
        public string Action { get; set; } = string.Empty;

        // 체결 가격 (USD)
        public decimal Price { get; set; }

        // 거래 수량 (주)
        public int Qty { get; set; }

        // 손익 (KRW) — 매도 시에만 계산, 매수 시 0
        public decimal ProfitLoss { get; set; }

        // 판단 근거 (퀀트 필터 Summary)
        public string Reason { get; set; } = string.Empty;
    }
}
