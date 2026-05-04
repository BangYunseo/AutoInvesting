using AutoInvest.Data.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoInvest.Core
{
    /// <summary>
    /// 증권사 API 추상화 인터페이스.
    /// SimBrokerClient(시뮬레이션) 또는 LsBrokerClient(LS증권 실거래)를 구현체로 사용.
    ///
    /// TODO [Phase 3] LS증권 실제 구현체 (LsBrokerClient) 추가
    ///   - REST: https://openapi.ls-sec.co.kr/
    ///   - OAuth: APP KEY + APP SECRET → Access Token (익일 07시 만료)
    ///   - 해외주식 API 그룹: 시세, 주문, 계좌, 차트, 실시간 시세(WebSocket)
    ///
    /// TODO [Phase 4] AI 시장분석 엔진 통합
    ///   - AnalyzeMarketSentimentAsync(ticker) 메서드 추가 검토
    ///   - 차트 데이터 + 뉴스(해외 포함) + 커뮤니티 감성 분석 결과를
    ///     주문 판단 시 IBrokerClient와 함께 사용하도록 확장
    /// </summary>
    public interface IBrokerClient
    {
        // ─── 인증 ───────────────────────────────────────
        /// <summary>로그인 (토큰 발급)</summary>
        Task<bool> LoginAsync();

        /// <summary>현재 로그인 상태</summary>
        bool IsLoggedIn { get; }

        // ─── 시세 조회 ── LS증권 [해외주식] 시세 / 차트 ──
        /// <summary>현재가 조회 (USD)</summary>
        Task<decimal> GetCurrentPriceAsync(string ticker);

        /// <summary>N일 최고가/최저가 조회</summary>
        Task<(decimal High, decimal Low)> GetPriceRangeAsync(string ticker, int days);

        // ─── 잔고 ── LS증권 [해외주식] 계좌 ─────────────
        /// <summary>환율 조회 (USD → KRW)</summary>
        Task<decimal> GetExchangeRateAsync();

        /// <summary>보유 종목 목록 조회</summary>
        Task<List<HoldingDto>> GetHoldingsAsync();

        // ─── 차트 데이터 ── 퀀트 지표 계산용 ────────────
        /// <summary>N일치 OHLCV 일봉 데이터 조회 (퀀트 지표 계산용)</summary>
        Task<List<OhlcvDto>> GetOhlcvAsync(string ticker, int days);

        // ─── 주문 ── LS증권 [해외주식] 주문 ─────────────
        /// <summary>매수 주문. 성공 시 주문번호 반환</summary>
        Task<string> PlaceBuyOrderAsync(string ticker, int qty, decimal price);

        /// <summary>매도 주문. 성공 시 주문번호 반환</summary>
        Task<string> PlaceSellOrderAsync(string ticker, int qty, decimal price);
    }
}
