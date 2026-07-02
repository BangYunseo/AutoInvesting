using System.Collections.Generic;
using AutoInvest.Core;
using AutoInvest.Data.DTO;
using Xunit;

namespace AutoInvest.Tests
{
    /// <summary>
    /// MacroBriefingService.BuildRuleBasedNarrative(순수 함수 — 외부 I/O 없음)의 단위 검증.
    /// 국면 해설은 '정보/보고 전용'이며 매수 판단에 쓰이지 않으므로, 여기서는
    /// 지표 입력에 따라 규칙 기반 해설이 결정적으로 생성되는지만 확인합니다.
    /// </summary>
    public class MacroBriefingServiceTests
    {
        private static MacroIndicatorDto Index(string key, string label, decimal yoy, decimal yoyDelta)
            => new() { Key = key, Label = label, YoyPercent = yoy, YoyDelta = yoyDelta, Unit = "%" };

        private static MacroIndicatorDto Level(string key, string label, decimal value, string unit, string dir)
            => new() { Key = key, Label = label, LatestValue = value, Unit = unit, Direction = dir };

        /// <summary>물가가 3% 이상이면 '신중 시나리오'와 '주시 필요' 결론이 나와야 한다.</summary>
        [Fact]
        public void 물가높으면_신중국면_결론이_나온다()
        {
            var indicators = new Dictionary<string, MacroIndicatorDto>
            {
                ["CPI"] = Index("CPI", "소비자물가지수(CPI)", 3.4m, 0.1m),
                ["WTI"] = Level("WTI", "WTI 국제유가", 70m, "$", "down"),
            };

            string text = MacroBriefingService.BuildRuleBasedNarrative(indicators);

            Assert.Contains("신중 시나리오의 무게가 더 큽니다", text);
            Assert.Contains("계속 주시할 필요가 있습니다", text);
            Assert.Contains("유가는 최근 하락 흐름입니다", text);
        }

        /// <summary>물가가 목표 이하로 낮으면 '낙관 여지' 국면으로 서술해야 한다.</summary>
        [Fact]
        public void 물가낮으면_낙관여지_국면으로_서술한다()
        {
            var indicators = new Dictionary<string, MacroIndicatorDto>
            {
                ["CorePCE"] = Index("CorePCE", "근원 PCE 물가지수", 1.8m, -0.2m),
            };

            string text = MacroBriefingService.BuildRuleBasedNarrative(indicators);

            Assert.Contains("낙관 시나리오의 여지가 있습니다", text);
            Assert.Contains("물가 압력이 비교적 낮은 국면입니다", text);
        }

        /// <summary>지표가 없으면 단정하지 않고 '확인 어려움'으로 안전하게 결론내야 한다.</summary>
        [Fact]
        public void 지표없으면_단정하지_않는다()
        {
            var text = MacroBriefingService.BuildRuleBasedNarrative(new Dictionary<string, MacroIndicatorDto>());

            Assert.Contains("(수치 없음)", text);
            Assert.Contains("단정하기 어렵습니다", text);
        }

        /// <summary>해설 끝에는 항상 '매수·매도 권유가 아님' 고지가 붙어야 한다(판단 레이어 금지).</summary>
        [Fact]
        public void 항상_투자권유아님_고지가_붙는다()
        {
            var indicators = new Dictionary<string, MacroIndicatorDto>
            {
                ["CPI"] = Index("CPI", "소비자물가지수(CPI)", 2.5m, 0m),
            };

            string text = MacroBriefingService.BuildRuleBasedNarrative(indicators);

            Assert.Contains("매수·매도 권유가 아닙니다", text);
        }

        /// <summary>조회 실패(Error) 지표는 상황 서술에서 제외되어야 한다.</summary>
        [Fact]
        public void 조회실패지표는_서술에서_제외된다()
        {
            var indicators = new Dictionary<string, MacroIndicatorDto>
            {
                ["CPI"] = new() { Key = "CPI", Label = "소비자물가지수(CPI)", Error = "데이터 없음" },
                ["RATE10Y"] = Level("RATE10Y", "미국 10년 국채금리", 4.2m, "%", "up"),
            };

            string text = MacroBriefingService.BuildRuleBasedNarrative(indicators);

            Assert.DoesNotContain("소비자물가지수(CPI):", text);
            Assert.Contains("미국 10년 국채금리: 4.20%로 직전 대비 상승 흐름", text);
        }
    }
}
