using System;

namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// OHLCV 일봉 데이터 DTO.
    /// 하루 단위의 시가/고가/저가/종가/거래량 데이터를 표현합니다.
    /// IBrokerClient.GetOhlcvAsync()의 반환값으로 사용되며,
    /// QuantIndicator에서 RSI, MACD, 볼린저밴드를 계산하는 원본 데이터입니다.
    /// </summary>
    public class OhlcvDto
    {
        // 일봉 날짜
        public DateTime Date { get; set; }

        // 시가 (Open) — 장 시작 시 가격 (USD)
        public decimal Open { get; set; }

        // 고가 (High) — 하루 중 최고가 (USD)
        public decimal High { get; set; }

        // 저가 (Low) — 하루 중 최저가 (USD)
        public decimal Low { get; set; }

        // 종가 (Close) — 장 마감 시 가격 (USD)
        public decimal Close { get; set; }

        // 거래량 (Volume) — 하루 동안의 거래 주식 수
        public long Volume { get; set; }
    }
}
