using System.Collections.Generic;
using System.Threading.Tasks;
using AutoInvest.Controllers;
using AutoInvest.Core;
using AutoInvest.Data.DTO;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AutoInvest.Tests
{
    /// <summary>
    /// OrderController의 매도 배선(안전가드 + 절세 가드)을 실계좌·네트워크·DB 없이 검증합니다.
    /// SessionManager에 <see cref="FakeBrokerClient"/>를 주입해, "취득가 → 세금계산 → 409 차단"이
    /// 주문·기록(DB 호출) 이전에 이뤄지는지를 자동으로 확인합니다.
    /// (Documents/260702_절세기능-테스트-실행계획서.md B계층)
    ///
    /// ⚠️ 매도 성공(200) 경로는 TradeHistoryDAO.Insert(정적 DB 호출)를 타므로 DB 없이 검증 불가라
    /// 이 스위트에서 제외합니다. 해당 경로는 수동 UI(계획서 C계층) 또는 후속(DAO 추상화)에서 다룹니다.
    /// </summary>
    public class OrderControllerTaxGuardTests
    {
        // 절세 가드가 과세로 판정하는 대표 시나리오: 취득 $100 → 현재 $200, 25주, 환율 1300
        //   차익 = (200-100) × 25 × 1300 = 3,250,000원
        //   과세표준 = 3,250,000 − 250만 공제 = 750,000원
        //   예상세금 = 750,000 × 22% = 165,000원 (과세)
        private const decimal AvgUsd = 100m;
        private const decimal PriceUsd = 200m;
        private const decimal Fx = 1300m;
        private const int Qty = 25;

        /// <summary>scopeFactory는 dca-run 전용이라 manual/sell-preview 경로에서는 사용되지 않으므로 null을 넘긴다.</summary>
        private static OrderController BuildController(FakeBrokerClient broker)
            => new OrderController(new SessionManager(broker), null!);

        private static List<HoldingDto> Holding(string ticker, int qty, decimal avg)
            => new List<HoldingDto> { new HoldingDto { Ticker = ticker, Qty = qty, AvgPrice = avg } };

        /// <summary>익명 응답 객체에서 이름으로 프로퍼티를 꺼낸다(409 Conflict의 taxEstimate 검증용).</summary>
        private static T? GetProp<T>(object? value, string name) where T : class
            => value?.GetType().GetProperty(name)?.GetValue(value) as T;

        // ─────────────────────────── 절세 가드 (핵심) ───────────────────────────

        /// <summary>과세가 예상되는 매도인데 사용자가 확인(acknowledgeTax)하지 않으면 409로 차단하고,
        /// 주문을 실행하지 않아야 한다(가드가 주문·DB 기록 전에 반환).</summary>
        [Fact]
        public async Task ManualSell_과세인데_미확인이면_409차단_주문미실행()
        {
            var broker = new FakeBrokerClient(Holding("QQQM", Qty, AvgUsd), PriceUsd, Fx);
            var ctrl = BuildController(broker);

            var result = await ctrl.PlaceManualOrder(new ManualOrderRequest
            {
                Ticker = "QQQM",
                Qty = Qty,
                OrderType = "SELL",
                Price = PriceUsd,
                AcknowledgeTax = false,
                YtdRealizedGainKrw = 0m,
            });

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);

            // 가드가 주문을 실행하기 전에 반환했어야 한다.
            Assert.Equal(0, broker.SellOrderCallCount);

            // 응답에 taxEstimate가 실려 있어야 하고, 과세로 판정돼 있어야 한다.
            var est = GetProp<SellTaxEstimateDto>(conflict.Value, "taxEstimate");
            Assert.NotNull(est);
            Assert.True(est!.IsTaxable);
            Assert.Equal(165_000m, est.EstimatedTaxKrw);
        }

        /// <summary>보유하지 않은 종목은 매도할 수 없어야 한다(매도 안전가드 → 400).</summary>
        [Fact]
        public async Task ManualSell_미보유종목이면_400()
        {
            var broker = new FakeBrokerClient(Holding("QQQM", Qty, AvgUsd), PriceUsd, Fx);
            var ctrl = BuildController(broker);

            var result = await ctrl.PlaceManualOrder(new ManualOrderRequest
            {
                Ticker = "SPLG", // 보유 목록에 없음
                Qty = 1,
                OrderType = "SELL",
                Price = PriceUsd,
            });

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(0, broker.SellOrderCallCount);
        }

        /// <summary>보유 수량을 초과하는 매도는 거부돼야 한다(매도 안전가드 → 400).</summary>
        [Fact]
        public async Task ManualSell_보유수량초과면_400()
        {
            var broker = new FakeBrokerClient(Holding("QQQM", 10, AvgUsd), PriceUsd, Fx);
            var ctrl = BuildController(broker);

            var result = await ctrl.PlaceManualOrder(new ManualOrderRequest
            {
                Ticker = "QQQM",
                Qty = 25, // 보유 10주 초과
                OrderType = "SELL",
                Price = PriceUsd,
            });

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(0, broker.SellOrderCallCount);
        }

        // ─────────────────────────── sell-preview (DB 무관) ───────────────────────────

        /// <summary>과세 매도의 예상 차익·과세표준·세금·비과세최대수량·수수료가 정확히 계산돼야 한다.</summary>
        [Fact]
        public async Task SellPreview_과세매도_계산값검증()
        {
            var broker = new FakeBrokerClient(Holding("QQQM", Qty, AvgUsd), PriceUsd, Fx);
            var ctrl = BuildController(broker);

            var result = await ctrl.PreviewSell("QQQM", Qty, price: null, ytd: 0m);

            var ok = Assert.IsType<OkObjectResult>(result);
            var est = Assert.IsType<SellTaxEstimateDto>(ok.Value);
            Assert.Equal(3_250_000m, est.GainKrw);
            Assert.Equal(750_000m, est.TaxableBaseKrw);
            Assert.Equal(165_000m, est.EstimatedTaxKrw);
            Assert.True(est.IsTaxable);
            Assert.Equal(19, est.MaxTaxFreeQty);           // floor(2,500,000 / 130,000)
            Assert.Equal(6_500_000m, est.SellAmountKrw);   // 200 × 25 × 1300
            Assert.Equal(16_250m, est.EstimatedFeeKrw);    // 매도대금 × 0.25%
        }

        /// <summary>취득가 불명(평균단가 0)인 보유종목은 추정을 스킵하고 과세 판정을 하지 않아야 한다(가드 건너뜀).</summary>
        [Fact]
        public async Task SellPreview_취득가불명이면_추정스킵()
        {
            var broker = new FakeBrokerClient(Holding("QQQM", Qty, avg: 0m), PriceUsd, Fx);
            var ctrl = BuildController(broker);

            var result = await ctrl.PreviewSell("QQQM", Qty, price: null, ytd: 0m);

            var ok = Assert.IsType<OkObjectResult>(result);
            var est = Assert.IsType<SellTaxEstimateDto>(ok.Value);
            Assert.True(est.CostBasisUnknown);
            Assert.False(est.IsTaxable);
        }

        /// <summary>손실 매도(현재가 &lt; 취득가)는 세금 0·비과세이고 비과세최대수량은 무제한(-1)이어야 한다.</summary>
        [Fact]
        public async Task SellPreview_손실매도면_비과세_무제한()
        {
            // 취득 $200 → 현재 $180 (손실)
            var broker = new FakeBrokerClient(Holding("QQQM", Qty, avg: 200m), currentPrice: 180m, exchangeRate: Fx);
            var ctrl = BuildController(broker);

            var result = await ctrl.PreviewSell("QQQM", Qty, price: null, ytd: 0m);

            var ok = Assert.IsType<OkObjectResult>(result);
            var est = Assert.IsType<SellTaxEstimateDto>(ok.Value);
            Assert.False(est.IsTaxable);
            Assert.Equal(0m, est.EstimatedTaxKrw);
            Assert.Equal(-1, est.MaxTaxFreeQty);
        }

        /// <summary>보유하지 않은 종목은 매도 세금 프리뷰를 계산할 수 없어야 한다(400).</summary>
        [Fact]
        public async Task SellPreview_미보유종목이면_400()
        {
            var broker = new FakeBrokerClient(Holding("QQQM", Qty, AvgUsd), PriceUsd, Fx);
            var ctrl = BuildController(broker);

            var result = await ctrl.PreviewSell("SPLG", 1, price: null, ytd: 0m);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
