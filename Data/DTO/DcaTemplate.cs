namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// 적립 매수 템플릿
    /// </summary>
    public class DcaTemplate
    {
        // 템플릿 식별자
        public string Id { get; set; } = string.Empty;

        // 템플릿 이름
        public string Name { get; set; } = string.Empty;

        // 월 예산
        public decimal BudgetKrw { get; set; }

        // 매수 수량
        public Dictionary<string, int> Quantities { get; set; } = new();
    }
}
