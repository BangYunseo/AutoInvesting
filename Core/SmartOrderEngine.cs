using AutoInvest.Core.Quant;
using AutoInvest.Data;
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

        /// <summary>다중 에이전트 분석 결과 (Phase 4-d)</summary>
        public MultiAgentAnalysisResult? MultiAgentResult { get; set; }

        /// <summary>확률 기반 합의 점수 (Phase 4-e)</summary>
        public ConsensusScoreDto? ConsensusScore { get; set; }
    }

    /// <summary>
    /// 스마트 주문 엔진 (Phase 4-e — 퀀트 + 다중 AI 에이전트 확률 기반 합의).
    /// 종목별 N일 최고가/최저가 + 퀀트 지표(RSI, MACD, BB)를 조회하고,
    /// 전략 유형에 따른 다중 조건 AND 필터를 통과한 뒤,
    /// 퀀트 + 차트AI + 펀더멘털AI의 가중치 × 확신도 합산 확률이
    /// 임계값(BUY_THRESHOLD / SELL_THRESHOLD)을 초과할 때만 매수/매도를 실행합니다.
    ///
    /// 전략 유형별 동작:
    ///   MEAN_REVERSION — Position ≤ 0.10 AND RSI ≤ 30 AND BB 하단 근접
    ///   MOMENTUM       — RSI ≥ 50 AND MACD 골든크로스 AND MACD Line 양수
    ///   MIXED          — Position ≤ 0.10 AND RSI &lt; 70
    ///
    /// 확률 기반 합의 (CalculateConsensusScore):
    ///   BuyProbability = QUANT_WEIGHT(BUY 시) + CHART_AI_WEIGHT × 차트확신도 + FUND_AI_WEIGHT × 펀더멘털확신도
    ///   퀀트 HOLD → 최대 60% → 임계값(65%) 자동 미달로 수식만으로 1차 관문 유지
    /// </summary>
    public class SmartOrderEngine
    {
        private readonly IBrokerClient _broker;
        private readonly IMarketAnalyzer _analyzer;
        private readonly int _rangeDays;
        private readonly decimal _buyThreshold;
        private readonly decimal _sellThreshold;

        // ── Phase 4-e: 확률 기반 합의 가중치 ──
        private readonly decimal _quantWeight;
        private readonly decimal _chartAiWeight;
        private readonly decimal _fundAiWeight;
        private readonly decimal _consensusBuyThreshold;
        private readonly decimal _consensusSellThreshold;

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

            // ── appsettings.json에서 합의 가중치/임계값 로드 ──
            _quantWeight           = decimal.Parse(AppConfigManager.Get("QUANT_WEIGHT", "0.40"));
            _chartAiWeight         = decimal.Parse(AppConfigManager.Get("CHART_AI_WEIGHT", "0.30"));
            _fundAiWeight          = decimal.Parse(AppConfigManager.Get("FUND_AI_WEIGHT", "0.30"));
            _consensusBuyThreshold = decimal.Parse(AppConfigManager.Get("BUY_THRESHOLD", "0.65"));
            _consensusSellThreshold= decimal.Parse(AppConfigManager.Get("SELL_THRESHOLD", "0.65"));

            Logger.Info($"[SmartOrder] 합의 가중치 로드 — 퀀트:{_quantWeight} 차트AI:{_chartAiWeight} 펀더멘털AI:{_fundAiWeight} | 임계값 BUY:{_consensusBuyThreshold} SELL:{_consensusSellThreshold}");
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
                quantReason = buyFilter.Summary;
            }
            else if (sellFilter.Passed)
            {
                quantSignal = SmartOrderSignal.SELL;
                quantConditions = sellFilter.MetConditions;
                quantReason = sellFilter.Summary;
            }
            else
            {
                quantSignal = SmartOrderSignal.HOLD;
                // 매수/매도 조건이 모두 충족되지 않았을 때는, 왜 매수를 안하는지에 대한 요약(buyFilter.Summary)을 제공합니다.
                quantReason = buyFilter.Summary;
            }

            // ── Phase 4-e: 다중 AI 에이전트 분석 (차트 + 펀더멘털 병렬 실행) ──
            var multiAgentResult = await _analyzer.AnalyzeAsync(ticker, indicators, ohlcv);

            // ── Phase 4-e: 확률 기반 합의 스코어링 ──
            var consensusScore = CalculateConsensusScore(
                quantSignal, multiAgentResult);

            SmartOrderSignal finalSignal;
            string finalReason;

            if (consensusScore.BuyProbability >= _consensusBuyThreshold && quantSignal == SmartOrderSignal.BUY)
            {
                finalSignal = SmartOrderSignal.BUY;
                finalReason = $"매수 확률 {consensusScore.BuyProbability:P1} ≥ 임계값 {_consensusBuyThreshold:P1} — 매수 실행";
            }
            else if (consensusScore.SellProbability >= _consensusSellThreshold && quantSignal == SmartOrderSignal.SELL)
            {
                finalSignal = SmartOrderSignal.SELL;
                finalReason = $"매도 확률 {consensusScore.SellProbability:P1} ≥ 임계값 {_consensusSellThreshold:P1} — 매도 실행";
            }
            else
            {
                finalSignal = SmartOrderSignal.HOLD;
                decimal gap = quantSignal == SmartOrderSignal.BUY
                    ? consensusScore.BuyGap
                    : (quantSignal == SmartOrderSignal.SELL ? consensusScore.SellGap : 0m);

                finalReason = quantSignal == SmartOrderSignal.HOLD
                    ? $"퀀트 조건 미충족 (최대 도달 가능 확률: {(_chartAiWeight + _fundAiWeight):P1})"
                    : $"합의 확률 미달 (부족분: {gap:P1}) — {quantReason}";
            }

            // ── 확률 분해 상세 로그 (Phase 4-e) ──
            string quantIcon = quantSignal == SmartOrderSignal.BUY ? "BUY" : (quantSignal == SmartOrderSignal.SELL ? "SELL" : "HOLD");
            string resultIcon = finalSignal == SmartOrderSignal.HOLD ? "⚠️" : "✅";
            decimal buyProb = consensusScore.BuyProbability;
            decimal sellProb = consensusScore.SellProbability;

            string decisionDetail =
                $"[{strategyType}] {ticker} 최종 판정: {finalSignal} {resultIcon}\n" +
                $"  ├── 퀀트       : {quantIcon}  → +{consensusScore.QuantContribution:P1}\n" +
                $"  ├── 차트AI     : {multiAgentResult.ChartAgent.Signal} (확신도:{multiAgentResult.ChartAgent.ConfidenceScore:F2}) → +{consensusScore.ChartAiContribution:P1}\n" +
                $"  └── 펀더멘털AI : {multiAgentResult.FundamentalAgent.Signal} (확신도:{multiAgentResult.FundamentalAgent.ConfidenceScore:F2}) → +{consensusScore.FundamentalAiContribution:P1}\n" +
                $"  ─────────────────────────────────────\n" +
                $"  매수 확률: {buyProb:P1} {(buyProb >= _consensusBuyThreshold ? "≥" : "<")} {_consensusBuyThreshold:P1} (임계값) → {finalReason}";

            Logger.LogQuant(ticker, quantConditions, finalSignal, strategyType);
            Logger.Info($"[SmartOrder] {decisionDetail}");

            return new SmartOrderResult
            {
                Ticker           = ticker,
                Signal           = finalSignal,
                PriceRange       = priceRange,
                Reason           = finalReason,
                Indicators       = indicators,
                QuantConditions  = quantConditions,
                DecisionReason   = decisionDetail,
                MultiAgentResult = multiAgentResult,
                ConsensusScore   = consensusScore
            };
        }

        /// <summary>
        /// 퀀트 + 차트AI + 펀더멘털AI의 가중치 × 확신도 합산을 수행합니다 (Phase 4-e).
        ///
        /// 계산 공식:
        ///   BuyProbability  = QUANT_WEIGHT(BUY 시) + CHART_AI_WEIGHT × 차트확신도(BUY 시) + FUND_AI_WEIGHT × 펀더멘털확신도(BUY 시)
        ///   SellProbability = QUANT_WEIGHT(SELL 시) + CHART_AI_WEIGHT × 차트확신도(SELL 시) + FUND_AI_WEIGHT × 펀더멘털확신도(SELL 시)
        ///
        /// 퀀트 1차 관문 수식 자동 보장:
        ///   퀀트 HOLD → QUANT_WEIGHT=0 → 최대 확률 = CHART_AI_WEIGHT + FUND_AI_WEIGHT (기본 60%)
        ///   임계값(기본 65%) 자동 미달로 별도 if 분기 없이 관문 유지
        /// </summary>
        private ConsensusScoreDto CalculateConsensusScore(
            SmartOrderSignal quantSignal,
            MultiAgentAnalysisResult multiAgent)
        {
            var chart       = multiAgent.ChartAgent;
            var fundamental = multiAgent.FundamentalAgent;

            // ── 매수 확률 계산 ──
            decimal quantBuyContrib = (quantSignal == SmartOrderSignal.BUY) ? _quantWeight : 0m;
            decimal chartBuyContrib = (chart.Signal == SmartOrderSignal.BUY)
                ? _chartAiWeight * chart.ConfidenceScore : 0m;
            decimal fundBuyContrib = (fundamental.Signal == SmartOrderSignal.BUY)
                ? _fundAiWeight * fundamental.ConfidenceScore : 0m;
            decimal buyProbability = quantBuyContrib + chartBuyContrib + fundBuyContrib;

            // ── 매도 확률 계산 ──
            decimal quantSellContrib = (quantSignal == SmartOrderSignal.SELL) ? _quantWeight : 0m;
            decimal chartSellContrib = (chart.Signal == SmartOrderSignal.SELL)
                ? _chartAiWeight * chart.ConfidenceScore : 0m;
            decimal fundSellContrib = (fundamental.Signal == SmartOrderSignal.SELL)
                ? _fundAiWeight * fundamental.ConfidenceScore : 0m;
            decimal sellProbability = quantSellContrib + chartSellContrib + fundSellContrib;

            // ── 결과 DTO 조립 ──
            decimal activeThreshold = (quantSignal == SmartOrderSignal.SELL)
                ? _consensusSellThreshold : _consensusBuyThreshold;

            return new ConsensusScoreDto
            {
                BuyProbability             = buyProbability,
                SellProbability            = sellProbability,
                QuantContribution          = (quantSignal == SmartOrderSignal.SELL) ? quantSellContrib : quantBuyContrib,
                ChartAiContribution        = (quantSignal == SmartOrderSignal.SELL) ? chartSellContrib : chartBuyContrib,
                FundamentalAiContribution  = (quantSignal == SmartOrderSignal.SELL) ? fundSellContrib : fundBuyContrib,
                Threshold                  = activeThreshold
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
        /// 매매 시점의 시장 지표 스냅샷을 DB에 저장합니다 (Phase 4-e AI 학습 데이터 + 합의 점수).
        /// </summary>
        private void SaveMarketSnapshot(SmartOrderResult result)
        {
            try
            {
                var ind = result.Indicators!;
                var score = result.ConsensusScore;
                var multi = result.MultiAgentResult;

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
                    Signal = result.Signal.ToString(),
                    BuyProbability = score?.BuyProbability ?? 0m,
                    SellProbability = score?.SellProbability ?? 0m,
                    ChartAiScore = multi?.ChartAgent.ConfidenceScore ?? 0m,
                    FundAiScore = multi?.FundamentalAgent.ConfidenceScore ?? 0m
                });
            }
            catch (Exception ex)
            {
                Logger.Warn($"[SmartOrder] 시장 스냅샷 저장 실패: {ex.Message}");
            }
        }
    }
}
