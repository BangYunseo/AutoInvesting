using AutoInvest.Data.DAO;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoInvest.Core
{
    // ─── 스마트 주문 신호 ────────────────────────────────
    public enum SmartOrderSignal { BUY, SELL, HOLD }

    public class SmartOrderResult
    {
        public string Ticker { get; set; }
        public SmartOrderSignal Signal { get; set; }
        public PriceRangeDto PriceRange { get; set; }
        public string Reason { get; set; }
    }

    /// <summary>
    /// 스마트 주문 엔진.
    /// 종목별 N일 최고가/최저가를 조회하고, 현재가 위치에 따라
    /// 매수(하위 10%) / 매도(상위 10%) / 보류를 판단합니다.
    ///
    /// TODO [Phase 4] AI 시장분석 엔진 연동
    ///   현재: position = (현재가 - 최저가) / (최고가 - 최저가)
    ///         → 단순 가격 범위 기반 판단
    ///
    ///   미래: AI 모델이 아래 데이터를 학습하여 진입 기준 제공
    ///     1. 차트 데이터 (일봉, 주봉, 기술적 지표)
    ///     2. 뉴스 감성 분석 (국내외 금융 뉴스, 중앙은행 발표 등)
    ///     3. 커뮤니티 감성 분석 (Reddit, X(Twitter), StockTwits 등)
    ///     4. 매크로 지표 (금리, 환율, VIX 등)
    ///
    ///   확장 방향:
    ///     - IMarketAnalyzer 인터페이스 도입
    ///     - AnalyzeAsync(ticker) → confidence score + recommended action
    ///     - SmartOrderEngine이 IMarketAnalyzer 결과를 position과 종합하여 판단
    ///     - 학습 데이터 저장용 테이블 (TB_MARKET_FEATURES) 추가
    /// </summary>
    public class SmartOrderEngine
    {
        private readonly IBrokerClient _broker;
        private readonly int _rangeDays;
        private readonly decimal _buyThreshold;
        private readonly decimal _sellThreshold;

        /// <param name="broker">증권사 클라이언트</param>
        /// <param name="rangeDays">가격 범위 조회 기간 (기본 20일)</param>
        /// <param name="buyThreshold">매수 진입 기준 (기본 0.10 = 하위 10%)</param>
        /// <param name="sellThreshold">매도 진입 기준 (기본 0.90 = 상위 10%)</param>
        public SmartOrderEngine(
            IBrokerClient broker,
            int rangeDays = 20,
            decimal buyThreshold = 0.10m,
            decimal sellThreshold = 0.90m)
        {
            _broker = broker;
            _rangeDays = rangeDays;
            _buyThreshold = buyThreshold;
            _sellThreshold = sellThreshold;
        }

        /// <summary>
        /// 단일 종목 분석 → 매수/매도/보류 신호 반환
        /// </summary>
        public async Task<SmartOrderResult> AnalyzeAsync(string ticker)
        {
            var current = await _broker.GetCurrentPriceAsync(ticker);
            var (high, low) = await _broker.GetPriceRangeAsync(ticker, _rangeDays);

            // 최고가 = 최저가인 경우(변동 없음) → HOLD
            decimal position = (high == low) ? 0.5m : (current - low) / (high - low);
            position = Math.Max(0m, Math.Min(1m, position));

            var priceRange = new PriceRangeDto
            {
                Ticker = ticker,
                High = high,
                Low = low,
                Current = current,
                Days = _rangeDays,
                Position = Math.Round(position, 4)
            };

            SmartOrderSignal signal;
            string reason;

            if (position <= _buyThreshold)
            {
                signal = SmartOrderSignal.BUY;
                reason = $"{_rangeDays}일 최저가(${low}) 대비 하위 {position:P1} — 매수 추천";
            }
            else if (position >= _sellThreshold)
            {
                signal = SmartOrderSignal.SELL;
                reason = $"{_rangeDays}일 최고가(${high}) 대비 상위 {(1 - position):P1} — 매도 추천";
            }
            else
            {
                signal = SmartOrderSignal.HOLD;
                reason = $"현재가 ${current}는 범위 내 {position:P1} 위치 — 보류";
            }

            // TODO [Phase 4] AI 분석 결과와 종합하여 최종 signal 결정
            //   var aiResult = await _marketAnalyzer.AnalyzeAsync(ticker);
            //   signal = CombineSignals(signal, aiResult);

            Logger.Info($"[SmartOrder] {ticker}: {signal} — {reason}");

            return new SmartOrderResult
            {
                Ticker = ticker,
                Signal = signal,
                PriceRange = priceRange,
                Reason = reason
            };
        }

        /// <summary>
        /// 전략 내 전체 종목을 분석하고, 신호에 따라 주문을 실행합니다.
        /// </summary>
        /// <param name="strategies">전략 목록 (종목별 비중)</param>
        /// <param name="investAmountKrw">투자금액 (원)</param>
        public async Task<List<SmartOrderResult>> ExecuteSmartOrdersAsync(
            List<StrategyDto> strategies,
            decimal investAmountKrw)
        {
            var results = new List<SmartOrderResult>();
            var exchangeRate = await _broker.GetExchangeRateAsync();

            Logger.Info($"[SmartOrder] === 스마트 주문 분석 시작 (종목 {strategies.Count}개) ===");

            foreach (var strategy in strategies)
            {
                try
                {
                    var result = await AnalyzeAsync(strategy.Ticker);
                    results.Add(result);

                    switch (result.Signal)
                    {
                        case SmartOrderSignal.BUY:
                            await ExecuteBuyAsync(strategy, investAmountKrw, exchangeRate, result);
                            break;

                        case SmartOrderSignal.SELL:
                            await ExecuteSellAsync(strategy.Ticker, result);
                            break;

                        case SmartOrderSignal.HOLD:
                            Logger.Info($"[SmartOrder] {strategy.Ticker}: 보류 — 주문 없음");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"[SmartOrder] {strategy.Ticker} 처리 실패: {ex.Message}");
                }
            }

            Logger.Info($"[SmartOrder] === 스마트 주문 분석 완료 ===");
            return results;
        }

        private async Task ExecuteBuyAsync(
            StrategyDto strategy, decimal investAmountKrw,
            decimal exchangeRate, SmartOrderResult result)
        {
            decimal allocKrw = investAmountKrw * (decimal)strategy.Weight;
            decimal priceKrw = result.PriceRange.Current * exchangeRate;
            int qty = (int)Math.Floor(allocKrw / priceKrw);

            if (qty <= 0)
            {
                Logger.Warn($"[SmartOrder] {strategy.Ticker}: 매수 수량 0 — 투자금 부족");
                return;
            }

            var orderNo = await _broker.PlaceBuyOrderAsync(
                strategy.Ticker, qty, result.PriceRange.Current);

            TradeHistoryDAO.Insert(new TradeHistoryDto
            {
                TradeDate = DateTime.Now,
                Ticker = strategy.Ticker,
                OrderType = "BUY",
                Qty = qty,
                Price = result.PriceRange.Current,
                Status = "FILLED",
                OrderNo = orderNo
            });

            Logger.Info($"[SmartOrder] 매수 완료: {strategy.Ticker} {qty}주 @ ${result.PriceRange.Current}");
        }

        private async Task ExecuteSellAsync(string ticker, SmartOrderResult result)
        {
            var holdings = await _broker.GetHoldingsAsync();
            var holding = holdings.Find(h => h.Ticker == ticker);

            if (holding == null || holding.Qty <= 0)
            {
                Logger.Info($"[SmartOrder] {ticker}: 매도 신호이나 보유 수량 없음 — 스킵");
                return;
            }

            var orderNo = await _broker.PlaceSellOrderAsync(
                ticker, holding.Qty, result.PriceRange.Current);

            TradeHistoryDAO.Insert(new TradeHistoryDto
            {
                TradeDate = DateTime.Now,
                Ticker = ticker,
                OrderType = "SELL",
                Qty = holding.Qty,
                Price = result.PriceRange.Current,
                Status = "FILLED",
                OrderNo = orderNo
            });

            Logger.Info($"[SmartOrder] 매도 완료: {ticker} {holding.Qty}주 @ ${result.PriceRange.Current}");
        }
    }
}
