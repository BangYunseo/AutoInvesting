using AutoInvest.Data.DTO;

namespace AutoInvest.Core
{
    /// <summary>
    /// 증권사 API 인터페이스
    /// </summary>
    public interface IBrokerClient
    {
        // 로그인 토큰
        Task<bool> LoginAsync();

        // 현재 로그인 상태
        bool IsLoggedIn { get; }

        // 현재가 조회(USD) 
        Task<decimal> GetCurrentPriceAsync(string ticker);

        // 환율 조회 
        Task<decimal> GetExchangeRateAsync();

        // 보유 종목 목록 조회
        Task<List<HoldingDto>> GetHoldingsAsync();

        // 예수금 조회(USD)
        Task<decimal> GetCashBalanceAsync();

        // 매수 주문. 성공 시 주문번호 반환
        Task<string> PlaceBuyOrderAsync(string ticker, int qty, decimal price);

        // 매도 주문. 성공 시 주문번호 반환
        Task<string> PlaceSellOrderAsync(string ticker, int qty, decimal price);
    }
}
