using System.Collections.Generic;

namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// 적립 사이클 실행 결과 집계
    /// </summary>
    public class DcaCycleResult
    {
        /// <summary>
        /// 주문 접수에 성공한 매수 내역
        /// </summary>
        public List<TradeHistoryDto> Accepted { get; set; } = new List<TradeHistoryDto>();

        /// <summary>매수 실패 종목별 사유</summary>
        public List<DcaBuyFailure> Failures { get; set; } = new List<DcaBuyFailure>();

        /// <summary>예산 초과 경고 문구(초과 시에만 사용)</summary>
        public string BudgetWarning { get; set; } = string.Empty;

        /// <summary>
        /// 이번 사이클 매수 계획 전체 금액 합계(원화)
        /// </summary>
        public decimal TotalCostKrw { get; set; }

        /// <summary>
        /// 계획 산출에 사용한 환율
        /// </summary>
        public decimal ExchangeRate { get; set; }
    }
}
