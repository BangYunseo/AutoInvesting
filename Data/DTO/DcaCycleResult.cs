using System.Collections.Generic;

namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// 적립식(DCA) 사이클 1회 실행 결과 집계.
    /// 매수 성공/실패/예산 경고를 한곳에 모아, 사이클 종료 시 <b>한 통</b>의 종합 보고서로
    /// 발송하기 위한 순수 데이터 홀더입니다. (종목별 개별 실패 메일 난발 방지)
    /// </summary>
    public class DcaCycleResult
    {
        /// <summary>체결에 성공한 매수 내역 (종목별 1건).</summary>
        public List<TradeHistoryDto> Filled { get; set; } = new List<TradeHistoryDto>();

        /// <summary>매수에 실패한 종목별 사유 목록.</summary>
        public List<DcaBuyFailure> Failures { get; set; } = new List<DcaBuyFailure>();

        /// <summary>예산 초과 경고 문구 (초과 시에만 채워짐, 없으면 빈 문자열).</summary>
        public string BudgetWarning { get; set; } = string.Empty;
    }
}
