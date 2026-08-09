using AutoInvest.Core;
using Xunit;

namespace AutoInvest.Tests
{
    /// <summary>
    /// TaxEstimator.Estimate(순수 함수 — 외부 I/O 없음)의 단위 검증.
    /// 실제 매도로 세금이 나가기 전에 "이 매도가 과세 구간인지 / 세금이 얼마인지" 계산이
    /// 정확한지 자동으로 확인합니다. (근거: Documents/analysis/[2026-07-02] 02_해외 ETF 절세 가이드.md)
    ///
    /// 대부분의 케이스는 환율=1로 두어 USD 값을 그대로 '원'처럼 다뤄 계산을 명확히 합니다.
    /// </summary>
    public class TaxEstimatorTests
    {
        private static TaxSettings Defaults() => new TaxSettings
        {
            AnnualDeductionKrw = 2_500_000m,
            Rate = 0.22m,
            EstimatedSellFeeRate = 0.0025m,
        };

        /// <summary>차익 240만원(공제 250만 이내)이면 세금 0·비과세여야 한다.</summary>
        [Fact]
        public void Estimate_차익_240만원이면_세금없음()
        {
            var r = TaxEstimator.Estimate("QQQ", avgPriceUsd: 7_000_000m, sellPriceUsd: 9_400_000m,
                qty: 1, exchangeRate: 1m, ytdRealizedGainKrw: 0m, settings: Defaults());

            Assert.Equal(2_400_000m, r.GainKrw);
            Assert.Equal(0m, r.EstimatedTaxKrw);
            Assert.False(r.IsTaxable);
        }

        /// <summary>차익 260만원이면 초과분 10만원에만 22% = 22,000원이어야 한다(전체 260만×22% 아님).</summary>
        [Fact]
        public void Estimate_차익_260만원이면_초과분만_과세()
        {
            var r = TaxEstimator.Estimate("QQQ", avgPriceUsd: 7_000_000m, sellPriceUsd: 9_600_000m,
                qty: 1, exchangeRate: 1m, ytdRealizedGainKrw: 0m, settings: Defaults());

            Assert.Equal(2_600_000m, r.GainKrw);
            Assert.Equal(100_000m, r.TaxableBaseKrw);
            Assert.Equal(22_000m, r.EstimatedTaxKrw);
            Assert.True(r.IsTaxable);
        }

        /// <summary>손실 매도(매도가 &lt; 취득가)는 세금 0이고 비과세 최대수량은 무제한(-1)이어야 한다.</summary>
        [Fact]
        public void Estimate_손실이면_세금없고_무제한()
        {
            var r = TaxEstimator.Estimate("SPLG", avgPriceUsd: 100m, sellPriceUsd: 80m,
                qty: 10, exchangeRate: 1_300m, ytdRealizedGainKrw: 0m, settings: Defaults());

            Assert.True(r.GainKrw < 0m);
            Assert.Equal(0m, r.EstimatedTaxKrw);
            Assert.False(r.IsTaxable);
            Assert.Equal(-1, r.MaxTaxFreeQty);
        }

        /// <summary>올해 공제를 이미 소진(YTD=250만)했으면 차익 전액이 과세돼야 한다.</summary>
        [Fact]
        public void Estimate_공제소진이면_전액과세()
        {
            var r = TaxEstimator.Estimate("QQQ", avgPriceUsd: 1_000_000m, sellPriceUsd: 2_000_000m,
                qty: 1, exchangeRate: 1m, ytdRealizedGainKrw: 2_500_000m, settings: Defaults());

            Assert.Equal(0m, r.RemainingDeductionKrw);
            Assert.Equal(1_000_000m, r.TaxableBaseKrw);
            Assert.Equal(220_000m, r.EstimatedTaxKrw); // 1,000,000 × 22%
        }

        /// <summary>비과세 최대수량 = 남은공제 / 주당차익의 정수 내림이어야 한다.</summary>
        [Fact]
        public void Estimate_비과세최대수량_경계계산()
        {
            // 주당 차익 500,000원, 남은공제 2,500,000원 → 최대 5주까지 비과세
            var r = TaxEstimator.Estimate("QQQ", avgPriceUsd: 1_000_000m, sellPriceUsd: 1_500_000m,
                qty: 10, exchangeRate: 1m, ytdRealizedGainKrw: 0m, settings: Defaults());

            Assert.Equal(5, r.MaxTaxFreeQty);
        }

        /// <summary>취득가가 0 이하(불명)이면 추정 불가로 표시하고 과세 판정을 하지 않아야 한다.</summary>
        [Fact]
        public void Estimate_취득가불명이면_추정스킵()
        {
            var r = TaxEstimator.Estimate("QQQ", avgPriceUsd: 0m, sellPriceUsd: 200m,
                qty: 5, exchangeRate: 1_300m, ytdRealizedGainKrw: 0m, settings: Defaults());

            Assert.True(r.CostBasisUnknown);
            Assert.False(r.IsTaxable);
            Assert.Equal(-1, r.MaxTaxFreeQty);
        }

        /// <summary>예상 수수료는 매도대금 × 수수료율로 계산돼야 한다.</summary>
        [Fact]
        public void Estimate_수수료는_매도대금_대비()
        {
            // 매도대금 = 100 × 10 × 1,300 = 1,300,000원, 수수료 0.25% = 3,250원
            var r = TaxEstimator.Estimate("SPLG", avgPriceUsd: 90m, sellPriceUsd: 100m,
                qty: 10, exchangeRate: 1_300m, ytdRealizedGainKrw: 0m, settings: Defaults());

            Assert.Equal(1_300_000m, r.SellAmountKrw);
            Assert.Equal(3_250m, r.EstimatedFeeKrw);
        }
    }
}
