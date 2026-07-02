using AutoInvest.Data;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using System;
using System.Globalization;

namespace AutoInvest.Core
{
    /// <summary>
    /// 해외 ETF(미국 상장 직접투자) 매도 시 예상 양도소득세·수수료를 계산하는 세금 추정기.
    ///
    /// ⚠️ 이것은 매수/매도 "타이밍 판단"이 아니라 **세금 산수 + 정보 제공**입니다.
    /// 백테스트로 무가치가 확인된 판단 레이어(신호/AI 타이밍) 재도입과 무관합니다(recommended_rules.md).
    ///
    /// 계산은 외부 I/O 없는 순수 함수(<see cref="Estimate"/>)로 분리되어 단위 검증이 가능합니다
    /// (DcaAccumulationEngine.PlanPurchases 패턴과 동일). 설정값 로딩(<see cref="TaxSettings.Load"/>)만
    /// I/O를 담당합니다.
    /// </summary>
    public static class TaxEstimator
    {
        /// <summary>
        /// 매도 예정 정보로 예상 양도차익·세금·수수료·비과세 최대수량을 계산합니다 (순수 함수 — 외부 I/O 없음).
        /// </summary>
        /// <param name="ticker">종목 코드</param>
        /// <param name="avgPriceUsd">평균 매입단가(취득가, USD). 0 이하이면 취득가 불명으로 처리.</param>
        /// <param name="sellPriceUsd">매도 단가(USD)</param>
        /// <param name="qty">매도 수량(주)</param>
        /// <param name="exchangeRate">USD→KRW 환율</param>
        /// <param name="ytdRealizedGainKrw">올해 이미 실현한 양도차익 합계(원). 남은 공제 계산에 사용.</param>
        /// <param name="settings">공제액·세율·수수료율 설정</param>
        public static SellTaxEstimateDto Estimate(
            string ticker,
            decimal avgPriceUsd,
            decimal sellPriceUsd,
            int qty,
            decimal exchangeRate,
            decimal ytdRealizedGainKrw,
            TaxSettings settings)
        {
            settings ??= new TaxSettings();

            var dto = new SellTaxEstimateDto
            {
                Ticker = ticker ?? string.Empty,
                Qty = qty,
                SellPriceUsd = sellPriceUsd,
                AvgPriceUsd = avgPriceUsd,
                ExchangeRate = exchangeRate,
            };

            decimal sellAmountKrw = sellPriceUsd * qty * exchangeRate;
            dto.SellAmountKrw = Round(sellAmountKrw);
            dto.EstimatedFeeKrw = Round(sellAmountKrw * settings.EstimatedSellFeeRate);

            // 취득가 불명(0 이하) → 차익을 신뢰성 있게 계산할 수 없어 추정 스킵(가드도 건너뛰게 함)
            if (avgPriceUsd <= 0m)
            {
                dto.CostBasisUnknown = true;
                dto.MaxTaxFreeQty = -1;
                dto.IsTaxable = false;
                return dto;
            }

            decimal gainPerShareUsd = sellPriceUsd - avgPriceUsd;
            decimal gainUsd = gainPerShareUsd * qty;
            decimal gainKrw = gainUsd * exchangeRate;

            decimal remainingDeduction = Math.Max(0m, settings.AnnualDeductionKrw - ytdRealizedGainKrw);
            decimal taxableBase = Math.Max(0m, gainKrw - remainingDeduction);
            decimal tax = taxableBase * settings.Rate;

            dto.GainUsd = Round(gainUsd);
            dto.GainKrw = Round(gainKrw);
            dto.RemainingDeductionKrw = Round(remainingDeduction);
            dto.TaxableBaseKrw = Round(taxableBase);
            dto.EstimatedTaxKrw = Round(tax);
            dto.IsTaxable = dto.EstimatedTaxKrw > 0m;

            // 비과세 최대수량: 손실/본전(주당 차익 ≤ 0)이면 무제한(-1),
            // 그 외에는 남은 공제를 넘지 않는 최대 정수 수량.
            decimal gainPerShareKrw = gainPerShareUsd * exchangeRate;
            if (gainPerShareKrw <= 0m)
            {
                dto.MaxTaxFreeQty = -1;
            }
            else
            {
                dto.MaxTaxFreeQty = (int)Math.Floor(remainingDeduction / gainPerShareKrw);
            }

            return dto;
        }

        /// <summary>원 단위로 반올림(소수점 이하 버림 대신 반올림 — 표시·비교용).</summary>
        private static decimal Round(decimal v) => Math.Round(v, 0, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// 세금 계산 설정값. appsettings.json(또는 DB)의 <c>Tax</c> 섹션에서 읽고, 없으면 안전한 기본값을 씁니다.
    /// (매직넘버 금지 원칙 — code-style-guide.md)
    /// </summary>
    public class TaxSettings
    {
        /// <summary>연간 기본공제 (원). 현행 해외주식 양도세 기준 250만원.</summary>
        public decimal AnnualDeductionKrw { get; set; } = 2_500_000m;

        /// <summary>세율 (양도세 20% + 지방소득세 2% = 22%).</summary>
        public decimal Rate { get; set; } = 0.22m;

        /// <summary>매도 수수료율 추정치 (매도대금 대비). 실제 증권사 수수료 확인 후 조정.</summary>
        public decimal EstimatedSellFeeRate { get; set; } = 0.0025m;

        /// <summary>
        /// 설정을 로드합니다 (DB → appsettings <c>Tax</c> 섹션 → 기본값 폴백). 값 파싱 실패 시 기본값 유지.
        /// </summary>
        public static TaxSettings Load()
        {
            var s = new TaxSettings();
            try
            {
                var map = AppConfigManager.GetMap("Tax");
                if (map.TryGetValue("AnnualDeductionKrw", out var d) &&
                    decimal.TryParse(d, NumberStyles.Any, CultureInfo.InvariantCulture, out var dv) && dv > 0)
                    s.AnnualDeductionKrw = dv;
                if (map.TryGetValue("Rate", out var r) &&
                    decimal.TryParse(r, NumberStyles.Any, CultureInfo.InvariantCulture, out var rv) && rv >= 0)
                    s.Rate = rv;
                if (map.TryGetValue("EstimatedSellFeeRate", out var f) &&
                    decimal.TryParse(f, NumberStyles.Any, CultureInfo.InvariantCulture, out var fv) && fv >= 0)
                    s.EstimatedSellFeeRate = fv;
            }
            catch (Exception ex)
            {
                Logger.Warn($"[TaxSettings] Tax 설정 로드 실패 — 기본값 사용: {ex.Message}");
            }
            return s;
        }
    }
}
