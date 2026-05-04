namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// 투자 대상 자산 마스터 DTO.
    /// TB_ASSET_MASTER 테이블과 매핑됩니다.
    /// 기본 마스터: SCHD, QQQM, GLD, JEPI, SPLG
    /// </summary>
    public class AssetDto
    {
        // 종목 코드 (예: "QQQM", "SCHD")
        public string Ticker { get; set; } = string.Empty;

        // 종목명 (예: "Invesco NASDAQ 100 ETF")
        public string Name { get; set; } = string.Empty;

        // 거래 통화 (기본 "USD")
        public string Currency { get; set; } = string.Empty;

        // 활성 상태 (true=투자 대상, false=비활성)
        public bool IsActive { get; set; }
    }
}
