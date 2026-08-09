namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// 적립식 사이클에서 매수에 실패한 종목 1건(종목·수량·사유).
    /// 종목별 개별 실패 메일을 난발하는 대신, 이 항목들을 사이클 종료 시 한 통의 보고서에 모아 발송합니다.
    /// </summary>
    public class DcaBuyFailure
    {
        /// <summary>실패한 종목 코드 (예: "QQQ").</summary>
        public string Ticker { get; set; } = string.Empty;

        /// <summary>매수 시도 수량 (주).</summary>
        public int Qty { get; set; }

        /// <summary>실패 사유 (예외 메시지).</summary>
        public string Error { get; set; } = string.Empty;
    }
}
