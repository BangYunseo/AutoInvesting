using AutoInvest.Data.DTO;
using System.Collections.Generic;

namespace AutoInvest.Core.Quant
{
    /// <summary>
    /// 퀀트 필터 결과.
    /// 다중 조건의 통과 여부와 판단 근거를 담습니다.
    /// </summary>
    public class FilterResult
    {
        /// <summary>모든 조건을 만족했는가</summary>
        public bool Passed { get; set; }

        /// <summary>충족된 조건 목록</summary>
        public List<string> MetConditions { get; set; } = new();

        /// <summary>미충족 조건 목록</summary>
        public List<string> UnmetConditions { get; set; } = new();

        /// <summary>전문가 어투 판단 요약 문자열</summary>
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// 퀀트 다중 조건 AND 필터.
    /// </summary>
    public static class QuantFilter
    {
        public static FilterResult CheckBuyCondition(
            IndicatorDto indicators,
            string strategyType,
            decimal buyThreshold = 0.10m)
        {
            return strategyType switch
            {
                "MOMENTUM" => CheckMomentumBuy(indicators),
                "MIXED" => CheckMixedBuy(indicators, buyThreshold),
                _ => CheckMeanReversionBuy(indicators, buyThreshold)
            };
        }

        public static FilterResult CheckSellCondition(
            IndicatorDto indicators,
            string strategyType,
            decimal sellThreshold = 0.90m)
        {
            return strategyType switch
            {
                "MOMENTUM" => CheckMomentumSell(indicators),
                "MIXED" => CheckMixedSell(indicators, sellThreshold),
                _ => CheckMeanReversionSell(indicators, sellThreshold)
            };
        }

        // ═══════════════════════════════════════════════════════
        // 하락 매수 (Mean Reversion) 전략
        // ═══════════════════════════════════════════════════════
        private static FilterResult CheckMeanReversionBuy(IndicatorDto ind, decimal threshold)
        {
            var result = new FilterResult();
            bool posMet = ind.Position <= threshold;
            bool rsiMet = ind.Rsi14 <= 30m;

            if (posMet) result.MetConditions.Add("가격 바닥권 도달");
            else result.UnmetConditions.Add("추가 하락 가능성 존재");

            if (rsiMet) result.MetConditions.Add("과매도 구간 진입");
            else result.UnmetConditions.Add("과매도 신호 미약");

            result.Passed = posMet && rsiMet;

            if (result.Passed)
            {
                result.Summary = "주가가 단기적으로 과도하게 하락하여 통계적인 바닥권(과매도 구간)에 진입했습니다. 반등을 기대하고 분할 매수로 접근하기에 매우 매력적인 구간입니다.";
            }
            else if (posMet)
            {
                result.Summary = "가격은 많이 내려왔지만 아직 확고한 과매도 신호가 보이지 않습니다. 시장의 투매가 멈추는지 조금 더 지켜보는 것이 안전합니다.";
            }
            else if (rsiMet)
            {
                result.Summary = "RSI 지표상으로는 과매도를 가리키고 있으나, 전체적인 하락 밴드의 바닥에는 도달하지 않았습니다. 섣부른 진입은 자제하시길 권장합니다.";
            }
            else
            {
                result.Summary = "현재 주가는 하락 매수(저점 매수)를 시도하기엔 여전히 높은 가격대를 유지하고 있습니다. 조정이 올 때까지 충분히 기다려주세요.";
            }

            return result;
        }

        private static FilterResult CheckMeanReversionSell(IndicatorDto ind, decimal threshold)
        {
            var result = new FilterResult();
            bool posMet = ind.Position >= threshold;
            bool rsiMet = ind.Rsi14 >= 70m;

            result.Passed = posMet && rsiMet;

            if (result.Passed)
            {
                result.Summary = "주가가 단기적으로 과열(과매수 구간) 양상을 보이며 고점에 도달했습니다. 단기 차익 실현을 통해 수익을 확보하는 것을 추천합니다.";
            }
            else
            {
                result.Summary = "아직 매도 기준선(단기 과열)에 도달하지 않았습니다. 조금 더 수익을 극대화할 수 있는 여력이 남아있다고 판단됩니다.";
            }

            return result;
        }

        // ═══════════════════════════════════════════════════════
        // 상승 매수 (Momentum) 전략
        // ═══════════════════════════════════════════════════════
        private static FilterResult CheckMomentumBuy(IndicatorDto ind)
        {
            var result = new FilterResult();
            bool rsiMet = ind.Rsi14 >= 50m;
            bool macdHistMet = ind.MacdHistogram > 0;
            bool macdLineMet = ind.MacdLine > 0;

            result.Passed = rsiMet && macdHistMet && macdLineMet;

            if (result.Passed)
            {
                result.Summary = "주가가 견고한 상승 추세를 타고 있으며, MACD 지표도 강한 매수세(골든크로스)를 확증하고 있습니다. 달리는 말에 올라타기에 아주 적합한 타이밍입니다.";
            }
            else if (rsiMet && (!macdHistMet || !macdLineMet))
            {
                result.Summary = "주가가 상승 흐름을 타고 있으나, 추세의 강도가 약해지고 있어(MACD 데드크로스 우려) 단기 하락 전환의 위험이 있습니다. 지금 추격 매수하기에는 리스크가 크므로 관망하는 것을 권장합니다.";
            }
            else if (!rsiMet && (macdHistMet || macdLineMet))
            {
                result.Summary = "MACD 상으로는 상승 전환 시그널이 보이지만, 아직 시장의 전반적인 매수 심리(RSI)가 뒷받침되지 않고 있습니다. 확실한 상승 추세 전환이 확인된 후 진입하는 것이 좋습니다.";
            }
            else
            {
                result.Summary = "현재 주가는 명확한 하락 추세에 놓여 있어 상승 모멘텀을 찾아보기 어렵습니다. 매수를 보류하고 추세 반전을 기다려주세요.";
            }

            return result;
        }

        private static FilterResult CheckMomentumSell(IndicatorDto ind)
        {
            var result = new FilterResult();
            bool rsiMet = ind.Rsi14 < 50m;
            bool macdHistMet = ind.MacdHistogram < 0;

            result.Passed = rsiMet && macdHistMet;

            if (result.Passed)
            {
                result.Summary = "기존의 상승 모멘텀이 꺾이고 뚜렷한 하락 반전 시그널(데드크로스 및 투자 심리 악화)이 나타났습니다. 추가 하락을 피하기 위해 신속하게 매도하는 것이 좋습니다.";
            }
            else
            {
                result.Summary = "단기적인 출렁임은 있으나 아직 강력한 상승 추세가 완전히 무너지지 않았습니다. 당분간은 매도를 보류하고 추세를 지켜보시기 바랍니다.";
            }

            return result;
        }

        // ═══════════════════════════════════════════════════════
        // 혼합 매수 (Mixed) 전략
        // ═══════════════════════════════════════════════════════
        private static FilterResult CheckMixedBuy(IndicatorDto ind, decimal threshold)
        {
            var result = new FilterResult();
            bool posMet = ind.Position <= threshold;
            bool rsiMet = ind.Rsi14 < 70m;

            result.Passed = posMet && rsiMet;

            if (result.Passed)
            {
                result.Summary = "현재 가격대가 충분히 저렴하면서도 극단적인 과열 상태가 아닙니다. 안전하게 분할 매수를 시작하기에 훌륭한 타이밍입니다.";
            }
            else if (!posMet)
            {
                result.Summary = "가치 대비 현재 주가가 약간 높은 편입니다. 좀 더 조정이 이루어질 때까지 인내심을 갖고 기다리시는 것을 권합니다.";
            }
            else
            {
                result.Summary = "가격은 양호한 위치에 있으나, 최근 단기 급등으로 과열 징후가 보입니다. 소폭 조정을 받은 후 진입하는 편이 낫습니다.";
            }

            return result;
        }

        private static FilterResult CheckMixedSell(IndicatorDto ind, decimal threshold)
        {
            var result = new FilterResult();
            bool posMet = ind.Position >= threshold;
            bool rsiMet = ind.Rsi14 > 30m;

            result.Passed = posMet && rsiMet;

            if (result.Passed)
            {
                result.Summary = "주가가 충분한 수익 구간에 도달했으며, 아직 심각한 투매 조짐도 없습니다. 기분 좋게 이익을 확정 짓기에 좋은 시점입니다.";
            }
            else
            {
                result.Summary = "아직 매도 목표가에 도달하지 않았거나, 단기적으로 지나치게 낙폭이 큽니다. 반등을 좀 더 기다린 뒤 매도하는 것이 유리해 보입니다.";
            }

            return result;
        }
    }
}
