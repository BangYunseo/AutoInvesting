using System.Collections.Generic;
using AutoInvest.Core;
using Xunit;

namespace AutoInvest.Tests
{
    /// <summary>
    /// DcaAccumulationEngine.PlanPurchases(순수 함수 — 외부 I/O 없음)의 단위 검증.
    /// 실계좌로 돈이 나가기 전에 "무엇을 몇 주 살지" 계산이 정확한지 자동으로 확인합니다.
    /// (규칙: recommended_rules.md의 배분 로직 단위 검증 시나리오)
    /// </summary>
    public class DcaAccumulationEngineTests
    {
        private const decimal Rate = 1_300m; // USD→KRW 환율(테스트 고정값)

        /// <summary>지정한 수량을 그대로 매수 계획에 담아야 한다.</summary>
        [Fact]
        public void PlanPurchases_지정수량을_그대로_담는다()
        {
            var quantities = new Dictionary<string, int> { ["SPLG"] = 3, ["QQQ"] = 2 };
            var prices = new Dictionary<string, decimal> { ["SPLG"] = 60m, ["QQQ"] = 200m };

            var plan = DcaAccumulationEngine.PlanPurchases(quantities, Rate, prices, out _);

            Assert.Equal(2, plan.Count);
            Assert.Equal(3, plan["SPLG"]);
            Assert.Equal(2, plan["QQQ"]);
        }

        /// <summary>현재가가 없는 종목은 계획에서 제외해야 한다.</summary>
        [Fact]
        public void PlanPurchases_현재가없는종목은_제외한다()
        {
            var quantities = new Dictionary<string, int> { ["SPLG"] = 3, ["QQQ"] = 2 };
            var prices = new Dictionary<string, decimal> { ["SPLG"] = 60m }; // QQQ 가격 없음

            var plan = DcaAccumulationEngine.PlanPurchases(quantities, Rate, prices, out _);

            Assert.True(plan.ContainsKey("SPLG"));
            Assert.False(plan.ContainsKey("QQQ"));
        }

        /// <summary>현재가가 0 이하인 종목은 계획에서 제외해야 한다.</summary>
        [Fact]
        public void PlanPurchases_현재가0이하종목은_제외한다()
        {
            var quantities = new Dictionary<string, int> { ["SPLG"] = 3, ["QQQ"] = 2 };
            var prices = new Dictionary<string, decimal> { ["SPLG"] = 60m, ["QQQ"] = 0m };

            var plan = DcaAccumulationEngine.PlanPurchases(quantities, Rate, prices, out _);

            Assert.True(plan.ContainsKey("SPLG"));
            Assert.False(plan.ContainsKey("QQQ"));
        }

        /// <summary>수량이 0 이하인 종목은 계획에서 제외해야 한다.</summary>
        [Fact]
        public void PlanPurchases_수량0이하종목은_제외한다()
        {
            var quantities = new Dictionary<string, int> { ["SPLG"] = 0, ["QQQ"] = -1, ["GLD"] = 1 };
            var prices = new Dictionary<string, decimal> { ["SPLG"] = 60m, ["QQQ"] = 200m, ["GLD"] = 190m };

            var plan = DcaAccumulationEngine.PlanPurchases(quantities, Rate, prices, out _);

            Assert.Single(plan);
            Assert.True(plan.ContainsKey("GLD"));
        }

        /// <summary>총 매수금액(원)은 수량×현재가×환율의 합과 정확히 일치해야 한다.</summary>
        [Fact]
        public void PlanPurchases_총매수금액을_정확히_합산한다()
        {
            var quantities = new Dictionary<string, int> { ["SPLG"] = 3, ["QQQ"] = 2 };
            var prices = new Dictionary<string, decimal> { ["SPLG"] = 60m, ["QQQ"] = 200m };

            DcaAccumulationEngine.PlanPurchases(quantities, Rate, prices, out decimal total);

            // (3 × 60 + 2 × 200) × 1300 = (180 + 400) × 1300 = 754,000
            Assert.Equal(754_000m, total);
        }

        /// <summary>제외된 종목은 총 매수금액에 포함되지 않아야 한다.</summary>
        [Fact]
        public void PlanPurchases_제외종목은_총액에_포함되지_않는다()
        {
            var quantities = new Dictionary<string, int> { ["SPLG"] = 3, ["QQQ"] = 2 };
            var prices = new Dictionary<string, decimal> { ["SPLG"] = 60m }; // QQQ 제외

            DcaAccumulationEngine.PlanPurchases(quantities, Rate, prices, out decimal total);

            // 3 × 60 × 1300 = 234,000 (QQQ 미포함)
            Assert.Equal(234_000m, total);
        }

        /// <summary>매수할 종목이 하나도 없으면 빈 계획과 총액 0을 반환해야 한다.</summary>
        [Fact]
        public void PlanPurchases_매수대상없으면_빈계획과_0원()
        {
            var quantities = new Dictionary<string, int> { ["SPLG"] = 3 };
            var prices = new Dictionary<string, decimal>(); // 가격 전무

            var plan = DcaAccumulationEngine.PlanPurchases(quantities, Rate, prices, out decimal total);

            Assert.Empty(plan);
            Assert.Equal(0m, total);
        }
    }
}
