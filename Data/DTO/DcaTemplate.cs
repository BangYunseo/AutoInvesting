using System.Collections.Generic;

namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// 적립 매수 템플릿 — 명명된 매수 구성(예산 + 종목별 고정 수량).
    /// 여러 템플릿을 만들어 두고 월별로 배정하면, 그 달의 적립 사이클은 해당 템플릿대로 매수합니다.
    /// 비중(%)은 저장하지 않습니다(수량×현재가로 환산되는 화면 표시용 값).
    /// </summary>
    public class DcaTemplate
    {
        /// <summary>템플릿 식별자 (프론트에서 생성, 월배정 참조 키).</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>템플릿 이름 (예: "공격형 70:30").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>월 예산 (원, 초과 경고용 상한).</summary>
        public decimal BudgetKrw { get; set; }

        /// <summary>종목별 고정 매수 수량 (예: QQQ=2, SPLG=3).</summary>
        public Dictionary<string, int> Quantities { get; set; } = new();
    }
}
