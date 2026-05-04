using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoInvest.Core.Quant
{
    /// <summary>
    /// 백테스팅 엔진.
    /// 과거 OHLCV 데이터를 기반으로 퀀트 전략의 수익성을 검증합니다.
    ///
    /// 흐름:
    ///   1. 과거 N일치 OHLCV 데이터 로드
    ///   2. 매일 퀀트 지표 계산 (RSI, MACD, BB, Position)
    ///   3. 전략 유형별 조건 필터링
    ///   4. 조건 충족 시 가상 매수/매도 실행
    ///   5. 수익률, MDD, 승률 등 결과 산출
    /// </summary>
    public class BacktestEngine
    {
        private readonly IBrokerClient _broker;
        private readonly decimal _buyThreshold;
        private readonly decimal _sellThreshold;
        private readonly decimal _initialAmount;

        public BacktestEngine(
            IBrokerClient broker,
            decimal initialAmount = 10_000_000m,
            decimal buyThreshold = 0.10m,
            decimal sellThreshold = 0.90m)
        {
            _broker = broker;
            _initialAmount = initialAmount;
            _buyThreshold = buyThreshold;
            _sellThreshold = sellThreshold;
        }

        /// <summary>
        /// 백테스트 실행
        /// </summary>
        /// <param name="ticker">종목 코드</param>
        /// <param name="strategyName">전략명</param>
        /// <param name="strategyType">전략 유형</param>
        /// <param name="days">백테스트 기간 (일)</param>
        /// <returns>백테스트 결과</returns>
        public async Task<BacktestResultDto> RunAsync(
            string ticker,
            string strategyName,
            string strategyType = "MEAN_REVERSION",
            int days = 365)
        {
            Logger.Info($"[Backtest] === 백테스트 시작: {ticker} ({strategyType}, {days}일) ===");

            // OHLCV 데이터 로드 (지표 계산에 최소 60일 추가)
            var ohlcv = await _broker.GetOhlcvAsync(ticker, days + 60);
            if (ohlcv.Count < 60)
            {
                Logger.Warn($"[Backtest] 데이터 부족 ({ohlcv.Count}일) — 최소 60일 필요");
                return new BacktestResultDto
                {
                    StrategyName = strategyName,
                    StrategyType = strategyType,
                    InitialAmount = _initialAmount,
                    FinalAmount = _initialAmount,
                    ReturnRate = 0
                };
            }

            var trades = new List<BacktestTradeDto>();
            decimal cash = _initialAmount;
            int holdingQty = 0;
            decimal holdingAvgPrice = 0;
            decimal peakValue = _initialAmount;
            decimal maxDrawdown = 0;
            int winTrades = 0;

            // 시뮬레이션: 60일째부터 시작 (지표 계산에 필요한 데이터 확보 후)
            for (int i = 60; i < ohlcv.Count; i++)
            {
                var today = ohlcv[i];
                var historicalSlice = ohlcv.Take(i + 1).ToList();
                var closes = historicalSlice.Select(o => o.Close).ToList();

                // 20일 최고/최저 계산
                var recent20 = historicalSlice.Skip(Math.Max(0, historicalSlice.Count - 20)).ToList();
                decimal high20 = recent20.Max(o => o.High);
                decimal low20 = recent20.Min(o => o.Low);

                // 퀀트 지표 계산
                var indicators = QuantIndicator.CalculateAll(ticker, historicalSlice, today.Close, high20, low20);

                // 매수 조건 체크
                var buyFilter = QuantFilter.CheckBuyCondition(indicators, strategyType, _buyThreshold);
                var sellFilter = QuantFilter.CheckSellCondition(indicators, strategyType, _sellThreshold);

                // 매수
                if (buyFilter.Passed && holdingQty == 0 && cash > 0)
                {
                    int qty = (int)Math.Floor(cash / (today.Close * 1350m)); // KRW 환산
                    if (qty > 0)
                    {
                        holdingQty = qty;
                        holdingAvgPrice = today.Close;
                        cash -= qty * today.Close * 1350m;

                        trades.Add(new BacktestTradeDto
                        {
                            Date = today.Date,
                            Ticker = ticker,
                            Action = "BUY",
                            Price = today.Close,
                            Qty = qty,
                            ProfitLoss = 0,
                            Reason = buyFilter.Summary
                        });
                    }
                }
                // 매도
                else if (sellFilter.Passed && holdingQty > 0)
                {
                    decimal profitLoss = (today.Close - holdingAvgPrice) * holdingQty * 1350m;
                    cash += holdingQty * today.Close * 1350m;

                    if (profitLoss > 0) winTrades++;

                    trades.Add(new BacktestTradeDto
                    {
                        Date = today.Date,
                        Ticker = ticker,
                        Action = "SELL",
                        Price = today.Close,
                        Qty = holdingQty,
                        ProfitLoss = Math.Round(profitLoss, 0),
                        Reason = sellFilter.Summary
                    });

                    holdingQty = 0;
                    holdingAvgPrice = 0;
                }

                // MDD 계산
                decimal portfolioValue = cash + (holdingQty * ohlcv[i].Close * 1350m);
                if (portfolioValue > peakValue)
                    peakValue = portfolioValue;

                decimal drawdown = (peakValue - portfolioValue) / peakValue;
                if (drawdown > maxDrawdown)
                    maxDrawdown = drawdown;
            }

            // 최종 평가금액
            decimal finalValue = cash + (holdingQty * ohlcv.Last().Close * 1350m);
            decimal returnRate = (_initialAmount > 0) ? (finalValue - _initialAmount) / _initialAmount * 100m : 0;
            int totalTrades = trades.Count(t => t.Action == "SELL");
            decimal winRate = totalTrades > 0 ? (decimal)winTrades / totalTrades * 100 : 0;

            var result = new BacktestResultDto
            {
                StrategyName = strategyName,
                StrategyType = strategyType,
                StartDate = ohlcv[60].Date,
                EndDate = ohlcv.Last().Date,
                InitialAmount = _initialAmount,
                FinalAmount = Math.Round(finalValue, 0),
                ReturnRate = Math.Round(returnRate, 2),
                MaxDrawdown = Math.Round(maxDrawdown * 100, 2),
                TotalTrades = trades.Count,
                WinTrades = winTrades,
                WinRate = Math.Round(winRate, 1),
                Trades = trades
            };

            Logger.Info($"[Backtest] === 백테스트 완료 ===");
            Logger.Info($"[Backtest] 수익률: {result.ReturnRate:F2}%, MDD: {result.MaxDrawdown:F2}%, " +
                $"거래: {result.TotalTrades}회, 승률: {result.WinRate:F1}%");

            return result;
        }
    }
}
