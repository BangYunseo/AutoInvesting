using System;

namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// 거래 내역 DTO.
    /// TB_TRADE_HISTORY 테이블과 매핑됩니다.
    /// 매수/매도 주문의 실행 결과를 기록합니다.
    /// </summary>
    public class TradeHistoryDto
    {
        // 거래 고유 ID (DB 자동 증가)
        public int TradeId { get; set; }

        // 거래 일시
        public DateTime TradeDate { get; set; }

        // 종목 코드 (예: "QQQ")
        public string Ticker { get; set; } = string.Empty;

        // 주문 유형 ("BUY" = 매수, "SELL" = 매도)
        public string OrderType { get; set; } = string.Empty;

        // 주문 수량 (주)
        public int Qty { get; set; }

        // 주문 지정가 (USD) — 접수 시점에 기록된다. 체결가가 아니다.
        // 실제 체결가는 증권사 체결 조회로 확인하며, 취득단가의 단일 진실 원천은 KIS 매입평단이다.
        public decimal Price { get; set; }

        // 주문 상태
        //   "PENDING" = 접수됨, 체결 미확인 (지정가 주문이라 미체결로 끝날 수 있다)
        //   "PARTIAL" = 일부 체결 / "FILLED" = 전량 체결 확인 / "FAILED" = 실패
        public string Status { get; set; } = string.Empty;

        // 증권사 주문번호 (LS증권 or 시뮬레이션 UUID)
        public string OrderNo { get; set; } = string.Empty;
    }
}