using AutoInvest.Data.DAO;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoInvest.Core
{
    /// <summary>
    /// 적립식(DCA) 자동 매수 엔진.
    ///
    /// 퀀트/AI 판단을 일절 하지 않습니다. 백테스트 결과 "타이밍 판단은 잘해야 본전,
    /// 실제로는 손해"로 검증되었기에, 이 엔진은 오직 다음만 수행합니다:
    ///   정해진 예산을 종목별 목표비중을 향해 정수 단위로 매수 + 남는 돈은 이월(미체결).
    ///
    /// 배분 방식(목표비중 바스켓):
    ///   "현재 목표비중보다 가장 부족한 종목"을 1주씩 정수로 매수하고, 더 이상 살 수
    ///   없을 때까지 반복합니다. 소수점 매수를 하지 않으므로 1주 단가가 비싼 종목은
    ///   잔돈이 모일 때까지 자연스럽게 건너뜁니다.
    ///
    /// 배분 결정(PlanPurchases)은 순수 함수(외부 I/O 없음)로 분리되어 단위 검증이 가능합니다.
    /// </summary>
    public class DcaAccumulationEngine
    {
        private readonly IBrokerClient _broker;

        /// <summary>매수 가능 수량 산정 시 수수료/환전 여유분(과매수 방지 버퍼).</summary>
        public const decimal CostBuffer = 0.01m;

        /// <summary>무한 루프 방지용 매수 반복 상한.</summary>
        private const int MaxIterations = 10000;

        /// <param name="broker">증권사 클라이언트 (Sim 또는 KIS)</param>
        public DcaAccumulationEngine(IBrokerClient broker)
        {
            _broker = broker;
        }

        /// <summary>
        /// 목표비중을 향한 정수 매수 계획을 계산합니다 (순수 함수 — 외부 I/O 없음, 검증 대상).
        /// </summary>
        /// <param name="targets">종목별 목표비중 (예: SPLG=0.4 ...)</param>
        /// <param name="budgetKrw">투입 예산 (원)</param>
        /// <param name="exchangeRate">USD→KRW 환율</param>
        /// <param name="priceUsd">종목별 현재가 (USD)</param>
        /// <param name="ownedQty">종목별 기존 보유 수량</param>
        /// <param name="leftoverKrw">매수 후 남는 잔돈 (이월 대상)</param>
        /// <returns>종목별 매수 수량 (1주 이상인 종목만)</returns>
        public static Dictionary<string, int> PlanPurchases(
            Dictionary<string, decimal> targets,
            decimal budgetKrw,
            decimal exchangeRate,
            IReadOnlyDictionary<string, decimal> priceUsd,
            IReadOnlyDictionary<string, int> ownedQty,
            out decimal leftoverKrw)
        {
            var bought = priceUsd.Keys.ToDictionary(t => t, _ => 0);
            decimal cash = budgetKrw;

            for (int iter = 0; iter < MaxIterations; iter++)
            {
                // 현재 포트폴리오 가치(보유분 + 이번에 산 분, 원화)
                decimal totalValue = 0m;
                var valueKrw = new Dictionary<string, decimal>();
                foreach (var ticker in priceUsd.Keys)
                {
                    decimal qty = ownedQty[ticker] + bought[ticker];
                    decimal v = qty * priceUsd[ticker] * exchangeRate;
                    valueKrw[ticker] = v;
                    totalValue += v;
                }

                // 살 수 있는 종목 중 "목표 대비 가장 부족한" 종목 선택
                string? pick = null;
                decimal worstGap = decimal.MinValue;
                foreach (var ticker in priceUsd.Keys)
                {
                    decimal unitCost = priceUsd[ticker] * exchangeRate * (1 + CostBuffer);
                    if (unitCost > cash) continue; // 1주도 못 사면 후보 제외

                    decimal currentWeight = totalValue > 0 ? valueKrw[ticker] / totalValue : 0m;
                    decimal gap = targets[ticker] - currentWeight;
                    if (gap > worstGap)
                    {
                        worstGap = gap;
                        pick = ticker;
                    }
                }

                if (pick == null) break; // 남은 예산으로 1주도 살 수 없음 → 종료

                bought[pick] += 1;
                cash -= priceUsd[pick] * exchangeRate * (1 + CostBuffer);
            }

            leftoverKrw = cash;
            return bought.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        /// <summary>
        /// 주어진 예산(원)을 목표비중을 향해 정수 단위로 매수합니다.
        /// 주문 체결분은 TB_TRADE_HISTORY에 기록되며, 체결 내역 목록을 반환합니다.
        /// </summary>
        /// <param name="targets">종목별 목표비중 맵 (예: SPLG=0.4, QQQM=0.3 ... 합계 1.0 권장)</param>
        /// <param name="budgetKrw">이번 사이클에 투입할 예산 (원)</param>
        /// <returns>체결된 매수 내역 목록 (없으면 빈 목록)</returns>
        public async Task<List<TradeHistoryDto>> AccumulateAsync(
            Dictionary<string, decimal> targets,
            decimal budgetKrw)
        {
            var filled = new List<TradeHistoryDto>();

            if (targets == null || targets.Count == 0)
            {
                Logger.Warn("[DCA] 목표비중(targets)이 비어 있어 매수를 건너뜁니다.");
                return filled;
            }
            if (budgetKrw <= 0)
            {
                Logger.Warn($"[DCA] 예산이 0 이하({budgetKrw})라 매수를 건너뜁니다.");
                return filled;
            }

            decimal exchangeRate = await _broker.GetExchangeRateAsync();
            if (exchangeRate <= 0)
            {
                Logger.Error("[DCA] 환율 조회 실패(0 이하) — 매수 중단");
                return filled;
            }

            // ── 현재가 + 보유수량 수집 ──
            var holdings = await _broker.GetHoldingsAsync();
            var priceUsd = new Dictionary<string, decimal>();
            var ownedQty = new Dictionary<string, int>();

            foreach (var ticker in targets.Keys)
            {
                decimal px = await _broker.GetCurrentPriceAsync(ticker);
                if (px <= 0)
                {
                    Logger.Warn($"[DCA] {ticker} 현재가 조회 실패(0 이하) — 이 종목은 제외");
                    continue;
                }
                priceUsd[ticker] = px;
                ownedQty[ticker] = holdings.Find(h => h.Ticker == ticker)?.Qty ?? 0;
            }

            if (priceUsd.Count == 0)
            {
                Logger.Error("[DCA] 유효한 현재가가 있는 종목이 없어 매수 중단");
                return filled;
            }

            // 제외된 종목이 있으면 그 종목만 빼고 비중 적용 (나머지 종목 기준으로 매수)
            var effectiveTargets = targets.Where(t => priceUsd.ContainsKey(t.Key))
                .ToDictionary(t => t.Key, t => t.Value);

            Logger.Info($"[DCA] === 적립식 매수 시작 (예산 {budgetKrw:N0}원, 환율 {exchangeRate:N0}, 종목 {priceUsd.Count}개) ===");

            // ── 순수 배분 계획 산출 ──
            var plan = PlanPurchases(effectiveTargets, budgetKrw, exchangeRate, priceUsd, ownedQty, out decimal leftover);

            // ── 계획대로 주문 실행 + 기록 ──
            foreach (var (ticker, qty) in plan)
            {
                decimal price = priceUsd[ticker];
                try
                {
                    var orderNo = await _broker.PlaceBuyOrderAsync(ticker, qty, price);

                    var trade = new TradeHistoryDto
                    {
                        TradeDate = DateTime.Now,
                        Ticker = ticker,
                        OrderType = "BUY",
                        Qty = qty,
                        Price = price,
                        Status = "FILLED",
                        OrderNo = orderNo
                    };
                    TradeHistoryDAO.Insert(trade);
                    filled.Add(trade);

                    Logger.Info($"[DCA] 매수 완료: {ticker} {qty}주 @ ${price} (주문번호: {orderNo})");
                }
                catch (Exception ex)
                {
                    Logger.Error($"[DCA] {ticker} 매수 실패: {ex.Message}");
                    _ = NotificationService.SendEmailAsync($"DCA 매수 실패: {ticker}", $"수량: {qty}주\n오류: {ex.Message}");
                }
            }

            decimal spent = budgetKrw - leftover;
            var perTicker = plan.Select(kv => $"{kv.Key} {kv.Value}주");
            Logger.Info($"[DCA] === 매수 완료: 총 {plan.Values.Sum()}주 계획 ({string.Join(", ", perTicker)}), " +
                $"투입 {spent:N0}원, 잔돈 이월 {leftover:N0}원 ===");

            return filled;
        }
    }
}
