using AutoInvest.Core.Quant;
using AutoInvest.Data.DAO;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoInvest.Core
{
    // ─── 스마트 주문 신호 ────────────────────────────────
    public enum SmartOrderSignal { BUY, SELL, HOLD }

    public class SmartOrderResult
    {
        public string Ticker { get; set; } = string.Empty;
        public SmartOrderSignal Signal { get; set; }
        public PriceRangeDto PriceRange { get; set; } = null!;
        public string Reason { get; set; } = string.Empty;

        /// <summary>퀀트 지표 계산 결과 (Phase 2.5)</summary>
        public IndicatorDto? Indicators { get; set; }

        /// <summary>충족된 퀀트 조건 목록</summary>
        public List<string> QuantConditions { get; set; } = new();

        /// <summary>상세 판단 근거 (로그용)</summary>
        public string DecisionReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 스마트 주문 엔진 (Phase 2.5 — 퀀트 통합).
    /// 종목별 N일 최고가/최저가 + 퀀트 지표(RSI, MACD, BB)를 조회하고,
    /// 전략 유형에 따른 다중 조건 AND 필터를 통과해야만 매수/매도를 실행합니다.
    ///
    /// 전략 유형별 동작:
    ///   MEAN_REVERSION — Position ≤ 0.10 AND RSI ≤ 30 AND BB 하단 근접
    ///   MOMENTUM       — RSI ≥ 50 AND MACD 골든크로스 AND MACD Line 양수
    ///   MIXED          — Position ≤ 0.10 AND RSI &lt; 70
    ///
    /// TODO [Phase 4] AI 시장분석 엔진 연동
    ///   현재: position + 퀀트 지표(RSI, MACD, BB) 기반 판단
    ///   미래: AI 모델이 차트/뉴스/커뮤니티/매크로 데이터를 학습하여 진입 기준 제공
    /// </summary>
    public class SmartOrderEngine
    {
        private readonly IBrokerClient _broker;
        private readonly IMarketAnalyzer _analyzer;
        private readonly int _rangeDays;
        private readonly decimal _buyThreshold;
        private readonly decimal _sellThreshold;

        /// <param name="broker">증권사 클라이언트</param>
        /// <param name="analyzer">AI 시장 분석 엔진</param>
        /// <param name="rangeDays">가격 범위 조회 기간 (기본 20일)</param>
        /// <param name="buyThreshold">매수 진입 기준 (기본 0.10 = 하위 10%)</param>
        /// <param name="sellThreshold">매도 진입 기준 (기본 0.90 = 상위 10%)</param>
        public SmartOrderEngine(
            IBrokerClient broker,
            IMarketAnalyzer analyzer,
            int rangeDays = 20,
            decimal buyThreshold = 0.10m,
            decimal sellThreshold = 0.90m)
        {
            _broker = broker;
            _analyzer = analyzer;
            _rangeDays = rangeDays;
            _buyThreshold = buyThreshold;
            _sellThreshold = sellThreshold;
        }

        /// <summary>
        /// 단일 종목 분석 → 퀀트 조건 판단 → AI 판단 합산 → 매수/매도/보류 신호 반환
        /// </summary>
        /// <param name="ticker">종목 코드</param>
        /// <param name="strategyType">전략 유형 (MEAN_REVERSION / MOMENTUM / MIXED)</param>
        public async Task<SmartOrderResult> AnalyzeAsync(string ticker, string strategyType = "MEAN_REVERSION")
        {
            var current = await _broker.GetCurrentPriceAsync(ticker);
            var (high, low) = await _broker.GetPriceRangeAsync(ticker, _rangeDays);

            // ── OHLCV 데이터 + 퀀트 지표 계산 (Phase 2.5) ──
            var ohlcv = await _broker.GetOhlcvAsync(ticker, Math.Max(_rangeDays, 60));
            var indicators = QuantIndicator.CalculateAll(ticker, ohlcv, current, high, low);

            var priceRange = new PriceRangeDto
            {
                Ticker = ticker,
                High = high,
                Low = low,
                Current = current,
                Days = _rangeDays,
                Position = indicators.Position
            };

            // ── 퀀트 필터 적용 ──
            SmartOrderSignal quantSignal;
            string quantReason;
            var quantConditions = new List<string>();

            // 매수 조건 필터
            var buyFilter = QuantFilter.CheckBuyCondition(indicators, strategyType, _buyThreshold);
            // 매도 조건 필터
            var sellFilter = QuantFilter.CheckSellCondition(indicators, strategyType, _sellThreshold);

            if (buyFilter.Passed)
            {
                quantSignal = SmartOrderSignal.BUY;
                quantConditions = buyFilter.MetConditions;
                quantReason = $"[{strategyType}] {buyFilter.Summary}";
            }
            else if (sellFilter.Passed)
            {
                quantSignal = SmartOrderSignal.SELL;
                quantConditions = sellFilter.MetConditions;
                quantReason = $"[{strategyType}] {sellFilter.Summary}";
            }
            else
            {
                quantSignal = SmartOrderSignal.HOLD;
                var unmet = buyFilter.UnmetConditions.Concat(sellFilter.UnmetConditions).ToList();
                quantReason = $"[{strategyType}] 매수/매도 조건 미충족 — {string.Join(", ", unmet.Take(3))}";
            }

            // ── Phase 4: AI 분석 결과 종합 (CombineSignals) ──
            var aiResult = await _analyzer.AnalyzeAsync(ticker, indicators);
            var (finalSignal, finalReason) = CombineSignals(quantSignal, quantReason, aiResult);

            // ── 상세 판단 근거 로그 ──
            string decisionDetail = $"[{strategyType}] {ticker}: " +
                $"Pos={indicators.Position:F4}, RSI={indicators.Rsi14:F1}, " +
                $"MACD={indicators.MacdLine:F4}/{indicators.MacdSignal:F4}, " +
                $"BB={indicators.BbLower:F2}~{indicators.BbUpper:F2} " +
                $"| Quant: {quantSignal} | AI: {aiResult.Signal}({aiResult.ConfidenceScore:F2}) → Final: {finalSignal}";

            Logger.LogQuant(ticker, quantConditions, finalSignal, strategyType);
            Logger.Info($"[SmartOrder] {decisionDetail}");

            return new SmartOrderResult
            {
                Ticker = ticker,
                Signal = finalSignal,
                PriceRange = priceRange,
                Reason = finalReason,
                Indicators = indicators,
                QuantConditions = quantConditions,
                DecisionReason = decisionDetail
            };
        }

        private (SmartOrderSignal, string) CombineSignals(SmartOrderSignal quantSignal, string quantReason, AiAnalysisResult aiResult)
        {
            // AI Confidence Score 임계값 설정
            decimal CONFIDENCE_THRESHOLD = 0.7m;

            if (aiResult.ConfidenceScore < CONFIDENCE_THRESHOLD)
            {
                // 확신도가 낮을 경우 기존 퀀트 신호 우선
                return (quantSignal, $"{quantReason} (AI 확신도 부족으로 퀀트 신호 유지)");
            }

            // 강한 퀀트 신호(BUY/SELL)가 있고 AI 신호도 같은 방향일 때
            if (quantSignal == aiResult.Signal && quantSignal != SmartOrderSignal.HOLD)
            {
                return (quantSignal, $"{quantReason} + AI 강력 동의: {aiResult.Reason}");
            }

            // 퀀트는 HOLD인데 AI가 강하게 매수/매도를 주장할 때 (공격적 반영)
            // 현재 설계상 보수적 매매를 위해 HOLD를 유지하거나, AI를 우선할 수 있습니다.
            // 여기서는 보수적 접근: 둘 다 동의할 때만 실행
            if (quantSignal == SmartOrderSignal.HOLD && aiResult.Signal != SmartOrderSignal.HOLD)
            {
                return (SmartOrderSignal.HOLD, $"{quantReason} (AI는 {aiResult.Signal}을 제시했으나, 퀀트 미달로 보류)");
            }

            // 퀀트는 매수/매도인데 AI가 반대하거나 HOLD일 때 -> 방어적 HOLD 전환
            if (quantSignal != SmartOrderSignal.HOLD && quantSignal != aiResult.Signal)
            {
                return (SmartOrderSignal.HOLD, $"퀀트 신호({quantSignal})가 AI 신호({aiResult.Signal})와 상충하여 보류: {aiResult.Reason}");
            }

            return (SmartOrderSignal.HOLD, "종합 판단: HOLD");
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

            // 전략 유형은 첫 번째 종목의 설정을 사용 (동일 전략 내 모두 같은 유형)
            string strategyType = strategies.FirstOrDefault()?.StrategyType ?? "MEAN_REVERSION";

            Logger.Info($"[SmartOrder] === 스마트 주문 분석 시작 (종목 {strategies.Count}개, 전략={strategyType}) ===");

            foreach (var strategy in strategies)
            {
                try
                {
                    var result = await AnalyzeAsync(strategy.Ticker, strategyType);
                    results.Add(result);

                    // ── 시장 스냅샷 저장 (AI 학습 데이터) ──
                    if (result.Indicators != null)
                    {
                        SaveMarketSnapshot(result);
                    }

                    // ── 분할매도(Split-Sell) 플랜 무조건 확인 ──
                    var sellPlanManager = new SellStrategyManager(_broker);
                    await sellPlanManager.ProcessActivePlansAsync(strategy.Ticker, result.PriceRange.Current, result.Indicators!);

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
                    _ = NotificationService.SendEmailAsync($"주문 처리 실패: {strategy.Ticker}", $"오류 내용: {ex.Message}");
                }
            }

            Logger.Info($"[SmartOrder] === 스마트 주문 분석 완료 ===");
            return results;
        }

        private async Task ExecuteBuyAsync(
            StrategyDto strategy, decimal investAmountKrw,
            decimal exchangeRate, SmartOrderResult result)
        {
            // 전략에 설정된 수량 사용
            int qty = strategy.Qty;

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

            Logger.Info($"[SmartOrder] 매수 완료: {strategy.Ticker} {qty}주 @ ${result.PriceRange.Current} " +
                $"(근거: {string.Join(" + ", result.QuantConditions)})");
            _ = NotificationService.SendEmailAsync($"매수 체결: {strategy.Ticker}", 
                $"수량: {qty}주<br/>단가: ${result.PriceRange.Current}<br/>주문번호: {orderNo}<br/>근거: {string.Join(", ", result.QuantConditions)}");
        }

        private async Task ExecuteSellAsync(string ticker, SmartOrderResult result)
        {
            var holdings = await _broker.GetHoldingsAsync();
            var holding = holdings.Find(h => h.Ticker == ticker);

            if (holding == null || holding.Qty <= 0)
            {
                return;
            }

            // 분할매도 플랜이 없는 경우에만 전량 매도
            var activePlans = Data.DAO.SellPlanDAO.GetPlansByTicker(ticker).FindAll(p => p.Status == "ACTIVE");
            if (activePlans.Count > 0)
            {
                Logger.Info($"[SmartOrder] {ticker}: 활성화된 분할매도 플랜이 존재하여 전량 일괄 매도를 생략합니다.");
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

            Logger.Info($"[SmartOrder] 일괄 매도 완료: {ticker} {holding.Qty}주 @ ${result.PriceRange.Current} " +
                $"(근거: {string.Join(" + ", result.QuantConditions)})");
            _ = NotificationService.SendEmailAsync($"일괄 매도 체결: {ticker}", 
                $"수량: {holding.Qty}주<br/>단가: ${result.PriceRange.Current}<br/>주문번호: {orderNo}<br/>근거: {string.Join(", ", result.QuantConditions)}");
        }

        /// <summary>
        /// 매매 시점의 시장 지표 스냅샷을 DB에 저장합니다 (Phase 4 AI 학습 데이터).
        /// </summary>
        private void SaveMarketSnapshot(SmartOrderResult result)
        {
            try
            {
                var ind = result.Indicators!;
                MarketSnapshotDAO.Insert(new MarketSnapshotDto
                {
                    SnapDate = DateTime.Now,
                    Ticker = result.Ticker,
                    Price = result.PriceRange.Current,
                    Position20d = ind.Position,
                    Rsi14 = ind.Rsi14,
                    MacdValue = ind.MacdLine,
                    MacdSignal = ind.MacdSignal,
                    BbUpper = ind.BbUpper,
                    BbLower = ind.BbLower,
                    Signal = result.Signal.ToString()
                });
            }
            catch (Exception ex)
            {
                Logger.Warn($"[SmartOrder] 시장 스냅샷 저장 실패: {ex.Message}");
            }
        }
    }
}
