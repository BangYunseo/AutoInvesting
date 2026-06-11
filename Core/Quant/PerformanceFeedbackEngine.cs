using System;
using System.Collections.Generic;
using AutoInvest.Data;
using AutoInvest.Data.DAO;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;

namespace AutoInvest.Core.Quant
{
    /// <summary>
    /// Phase 5-d: 성과 기반 피드백 엔진.
    /// TB_MARKET_SNAPSHOT에 축적된 에이전트별 방향 신호를, 일정 기간(Horizon) 경과 후
    /// 실제 가격 변동과 대조하여 (1) 에이전트별 실측 적중률, (2) 합의 가중치 조합별 가상 성과(A/B)를 산출합니다.
    ///
    /// ⚠️ 본 엔진은 읽기 전용 분석 전용입니다. TB_MARKET_SNAPSHOT을 수정·삭제하지 않으며,
    ///    A/B 결과를 실제 매매 가중치에 자동 반영하지 않습니다 (검증용 리포트만 제공).
    /// </summary>
    public static class PerformanceFeedbackEngine
    {
        /// <summary>미래 수익(forward return)이 매칭된 단일 스냅샷 결과.</summary>
        private class Outcome
        {
            public MarketSnapshotDto Snap { get; set; } = null!;
            public decimal PriceLater { get; set; }
            public decimal ForwardReturnPct { get; set; }
        }

        /// <summary>
        /// DB에서 전체 스냅샷을 읽어 미래 수익을 매칭합니다 (실제 운영 경로).
        /// </summary>
        private static List<Outcome> LoadOutcomes(int horizonDays, int maxRows)
        {
            // 종목 ASC, 일자 ASC 정렬로 반환됨
            return BuildOutcomes(MarketSnapshotDAO.GetRecentAll(maxRows), horizonDays);
        }

        /// <summary>
        /// 전체 스냅샷을 종목별로 묶어, 각 스냅샷에 Horizon일 이후의 가격을 매칭합니다 (순수 함수, DB 비의존).
        /// SNAP_DATE 기준 가장 가까운 미래(≥ 기준일+Horizon) 스냅샷의 가격을 사용합니다.
        /// 입력은 종목 ASC, 일자 ASC로 정렬되어 있어야 합니다.
        /// </summary>
        private static List<Outcome> BuildOutcomes(List<MarketSnapshotDto> snaps, int horizonDays)
        {
            var outcomes = new List<Outcome>();

            int i = 0;
            while (i < snaps.Count)
            {
                string ticker = snaps[i].Ticker;
                int start = i;
                while (i < snaps.Count && snaps[i].Ticker == ticker) i++;
                int end = i; // [start, end) 구간이 동일 종목

                for (int a = start; a < end; a++)
                {
                    var s = snaps[a];
                    if (s.Price <= 0) continue;
                    DateTime target = s.SnapDate.AddDays(horizonDays);

                    for (int b = a + 1; b < end; b++)
                    {
                        if (snaps[b].SnapDate >= target)
                        {
                            decimal later = snaps[b].Price;
                            outcomes.Add(new Outcome
                            {
                                Snap = s,
                                PriceLater = later,
                                ForwardReturnPct = (later - s.Price) / s.Price * 100m
                            });
                            break;
                        }
                    }
                }
            }
            return outcomes;
        }

        /// <summary>
        /// 에이전트(퀀트/차트AI/펀더멘털AI)별 실측 적중률을 산출합니다.
        /// </summary>
        /// <param name="horizonDays">신호 이후 성과를 측정할 경과 일수 (기본 7일)</param>
        /// <param name="maxRows">분석에 사용할 최대 스냅샷 수 (기본 5000)</param>
        public static List<AgentAccuracyDto> GetAgentAccuracy(int horizonDays = 7, int maxRows = 5000)
        {
            try
            {
                return ComputeAgentAccuracy(LoadOutcomes(horizonDays, maxRows));
            }
            catch (Exception ex)
            {
                Logger.Error($"[PerfFeedback] 에이전트 적중률 산출 실패: {ex.Message}");
                return new List<AgentAccuracyDto>();
            }
        }

        /// <summary>
        /// 주어진 스냅샷 목록으로 에이전트별 적중률을 산출합니다 (순수 함수, DB 비의존 — 검증/재사용용).
        /// </summary>
        /// <param name="snaps">종목 ASC, 일자 ASC로 정렬된 스냅샷 목록</param>
        /// <param name="horizonDays">신호 이후 성과를 측정할 경과 일수 (기본 7일)</param>
        public static List<AgentAccuracyDto> GetAgentAccuracy(List<MarketSnapshotDto> snaps, int horizonDays = 7)
        {
            return ComputeAgentAccuracy(BuildOutcomes(snaps, horizonDays));
        }

        private static List<AgentAccuracyDto> ComputeAgentAccuracy(List<Outcome> outcomes)
        {
            return new List<AgentAccuracyDto>
            {
                Evaluate("퀀트", outcomes, o => o.Snap.QuantSignal),
                Evaluate("차트AI", outcomes, o => o.Snap.ChartAiSignal),
                Evaluate("펀더멘털AI", outcomes, o => o.Snap.FundAiSignal)
            };
        }

        private static AgentAccuracyDto Evaluate(string name, List<Outcome> outcomes, Func<Outcome, string> signalSelector)
        {
            int buy = 0, sell = 0, hit = 0, sample = 0;
            foreach (var o in outcomes)
            {
                string sig = (signalSelector(o) ?? "").ToUpperInvariant();
                if (sig == "BUY")
                {
                    buy++; sample++;
                    if (o.PriceLater > o.Snap.Price) hit++;
                }
                else if (sig == "SELL")
                {
                    sell++; sample++;
                    if (o.PriceLater < o.Snap.Price) hit++;
                }
            }
            return new AgentAccuracyDto
            {
                AgentName = name,
                BuySignals = buy,
                SellSignals = sell,
                SampleCount = sample,
                HitCount = hit,
                WinRate = sample > 0 ? Math.Round((decimal)hit / sample, 4) : 0m
            };
        }

        /// <summary>
        /// 여러 합의 가중치 조합(Scheme)을 누적 데이터에 가상 적용해, 조합별 가상 매수 성과를 비교합니다.
        /// 매수 확률 재계산식은 SmartOrderEngine.CalculateConsensusScore의 매수식과 동일합니다 (원본 불변).
        /// </summary>
        /// <param name="horizonDays">매수 이후 성과를 측정할 경과 일수 (기본 7일)</param>
        /// <param name="maxRows">분석에 사용할 최대 스냅샷 수 (기본 5000)</param>
        public static List<WeightSchemeResultDto> RunWeightAbTest(int horizonDays = 7, int maxRows = 5000)
        {
            try
            {
                decimal buyThreshold = decimal.TryParse(AppConfigManager.Get("BUY_THRESHOLD", "0.65"), out decimal t) ? t : 0.65m;
                return RunWeightAbTest(LoadOutcomes(horizonDays, maxRows), buyThreshold);
            }
            catch (Exception ex)
            {
                Logger.Error($"[PerfFeedback] 가중치 A/B 백테스트 실패: {ex.Message}");
                return new List<WeightSchemeResultDto>();
            }
        }

        /// <summary>
        /// 주어진 미래 수익 매칭 결과로 가중치 조합별 A/B 성과를 산출합니다 (순수 함수, DB 비의존 — 검증/재사용용).
        /// </summary>
        public static List<WeightSchemeResultDto> RunWeightAbTest(List<MarketSnapshotDto> snaps, decimal buyThreshold, int horizonDays = 7)
        {
            return RunWeightAbTest(BuildOutcomes(snaps, horizonDays), buyThreshold);
        }

        private static List<WeightSchemeResultDto> RunWeightAbTest(List<Outcome> outcomes, decimal buyThreshold)
        {
            // 비교할 가중치 조합 후보 (퀀트 / 차트AI / 펀더멘털AI)
            var schemes = new (string Name, decimal Q, decimal C, decimal F)[]
            {
                ("기본(40/30/30)",     0.40m, 0.30m, 0.30m),
                ("퀀트 강화(60/20/20)", 0.60m, 0.20m, 0.20m),
                ("AI 강화(20/40/40)",  0.20m, 0.40m, 0.40m),
                ("균등(34/33/33)",     0.34m, 0.33m, 0.33m)
            };

            var results = new List<WeightSchemeResultDto>();
            foreach (var sc in schemes)
            {
                int trigger = 0, hit = 0;
                decimal sumReturn = 0m;
                foreach (var o in outcomes)
                {
                    decimal prob = RecomputeBuyProbability(o.Snap, sc.Q, sc.C, sc.F);
                    if (prob >= buyThreshold)
                    {
                        trigger++;
                        sumReturn += o.ForwardReturnPct;
                        if (o.PriceLater > o.Snap.Price) hit++;
                    }
                }
                results.Add(new WeightSchemeResultDto
                {
                    SchemeName = sc.Name,
                    QuantWeight = sc.Q,
                    ChartWeight = sc.C,
                    FundWeight = sc.F,
                    TriggerCount = trigger,
                    HitCount = hit,
                    WinRate = trigger > 0 ? Math.Round((decimal)hit / trigger, 4) : 0m,
                    AvgForwardReturnPct = trigger > 0 ? Math.Round(sumReturn / trigger, 3) : 0m
                });
            }
            return results;
        }

        /// <summary>
        /// 저장된 에이전트별 방향 신호 + 확신도로 매수 확률을 재계산합니다.
        /// (CalculateConsensusScore 매수식과 동일: 퀀트는 고정 가중치, AI는 가중치 × 확신도)
        /// </summary>
        private static decimal RecomputeBuyProbability(MarketSnapshotDto s, decimal qw, decimal cw, decimal fw)
        {
            decimal quant = ((s.QuantSignal ?? "").ToUpperInvariant() == "BUY") ? qw : 0m;
            decimal chart = ((s.ChartAiSignal ?? "").ToUpperInvariant() == "BUY") ? cw * s.ChartAiScore : 0m;
            decimal fund = ((s.FundAiSignal ?? "").ToUpperInvariant() == "BUY") ? fw * s.FundAiScore : 0m;
            return quant + chart + fund;
        }
    }
}
