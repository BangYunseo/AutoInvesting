namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// 자산 마스터(TB_ASSET_MASTER) 한 종목 정보.
    /// 전략에 편입 가능한 "허용 종목" 목록을 나타냅니다. (TB_INVEST_STRATEGY.TICKER가 FK로 참조)
    /// </summary>
    public class AssetMasterDto
    {
        public string Ticker { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
        public bool IsActive { get; set; } = true;
    }
}
