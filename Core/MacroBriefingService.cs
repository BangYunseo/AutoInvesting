using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;

namespace AutoInvest.Core
{
    /// <summary>
    /// 시장 국면 브리핑 서비스 — 거시 지표(FRED)와 환율을 모아 '지금이 어떤 국면인지'를
    /// 사람이 읽을 해설로 만들어 줍니다.
    ///
    /// ⚠️ 정보/보고 전용 레이어입니다. 이 서비스의 결과는 <c>DcaAccumulationEngine</c>이
    /// 절대 참조하지 않으며, 매수 수량·타이밍에 어떤 값도 흘러가지 않습니다
    /// (판단 레이어 재도입 금지 — recommended_rules.md). "사라/팔아라"가 아니라
    /// "상황이 이렇다"만 설명합니다.
    ///
    /// Phase 1: 규칙 기반 해설만 생성. AI(Gemini)·뉴스 연동은 후속 단계에서 이 지점에
    /// 폴백을 유지한 채 추가됩니다.
    /// </summary>
    public static class MacroBriefingService
    {
        private const string Disclaimer =
            "※ 본 내용은 공개 데이터에 기반한 정보 정리이며, 특정 종목의 매수·매도 권유가 아닙니다.";

        /// <summary>
        /// 최신 거시 지표 + 환율을 조회해 국면 브리핑을 생성합니다.
        /// 개별 지표 실패는 각 지표의 Error로 표현되며, 전체 흐름은 멈추지 않습니다.
        /// </summary>
        public static async Task<MacroBriefingDto> GetBriefingAsync()
        {
            var indicators = await FredClient.GetAllAsync();

            // ── 환율은 기존 ExchangeRateService(Frankfurter/ECB, 무키)를 그대로 재사용 ──
            var fx = await BuildFxIndicatorAsync();
            if (fx != null)
                indicators["FX"] = fx;

            // Phase 1: 규칙 기반 해설 (AI 없이도 동작 — 환각 없음)
            string narrative = BuildRuleBasedNarrative(indicators);

            return new MacroBriefingDto
            {
                Indicators = OrderForDisplay(indicators),
                Narrative = narrative,
                Source = "규칙 기반",
                GeneratedAt = DateTime.UtcNow,
            };
        }

        /// <summary>지표를 화면 표시 순서(거시 5종 → 환율)로 정렬해 리스트로 반환합니다.</summary>
        private static List<MacroIndicatorDto> OrderForDisplay(Dictionary<string, MacroIndicatorDto> indicators)
        {
            var ordered = new List<MacroIndicatorDto>();
            foreach (var key in FredClient.DisplayOrder)
                if (indicators.TryGetValue(key, out var dto))
                    ordered.Add(dto);
            if (indicators.TryGetValue("FX", out var fx))
                ordered.Add(fx);
            return ordered;
        }

        /// <summary>
        /// 현재 USD/KRW 환율을 지표 형태로 만듭니다 (표시용).
        /// ExchangeRateService는 현재값만 주므로 direction 없이 값만 담습니다.
        /// </summary>
        private static async Task<MacroIndicatorDto?> BuildFxIndicatorAsync()
        {
            try
            {
                decimal rate = await ExchangeRateService.GetUsdKrwAsync();
                return new MacroIndicatorDto
                {
                    Key = "FX",
                    Label = "원/달러 환율",
                    LatestValue = Math.Round(rate, 2),
                    LatestDate = DateTime.UtcNow.AddHours(9).ToString("yyyy-MM-dd"),
                    Unit = "원",
                };
            }
            catch (Exception ex)
            {
                Logger.Warn($"[Macro] 환율 조회 실패: {ex.Message}");
                return new MacroIndicatorDto { Key = "FX", Label = "원/달러 환율", Unit = "원", Error = ex.Message };
            }
        }

        // ================================================================
        // 규칙 기반 해설 — AI 없이 동작하는 4단 구조 분석 (환각 0, 비용 0)
        // (CheckUSA/analyzer.py의 규칙 로직을 C#로 포팅)
        // ================================================================

        /// <summary>
        /// 지표들을 근거로 4단 구조(현재 상황 / 시나리오 / 결론)의 국면 해설을 만듭니다.
        /// (뉴스 요약은 뉴스 연동 단계에서 2)번 항목으로 추가됩니다.)
        ///
        /// 외부 I/O 없는 순수 함수 — 입력 지표 딕셔너리만으로 결정적 결과를 내므로 단위 검증이 가능합니다.
        /// </summary>
        public static string BuildRuleBasedNarrative(Dictionary<string, MacroIndicatorDto> indicators)
        {
            var (situationPairs, inflationHigh, oilDir) = AnalyzeSituation(indicators);

            var sb = new StringBuilder();

            sb.AppendLine("1) 현재 상황 한눈에:");
            if (situationPairs.Count > 0)
                foreach (var (label, detail) in situationPairs)
                    sb.AppendLine($"- {label}: {detail}");
            else
                sb.AppendLine("- (수치 없음)");
            sb.AppendLine();

            sb.AppendLine("2) 미래 시나리오:");
            sb.AppendLine(BuildScenario(inflationHigh, oilDir));
            sb.AppendLine();

            sb.AppendLine($"3) 결론: {BuildConclusion(inflationHigh, oilDir)}");
            sb.AppendLine();

            sb.Append(Disclaimer);
            return sb.ToString();
        }

        /// <summary>
        /// 각 지표를 규칙으로 서술합니다.
        /// 반환: (지표별 (이름, 상세) 목록, 물가가 목표를 웃도는가, 유가 방향)
        /// </summary>
        private static (List<(string Label, string Detail)> Pairs, bool? InflationHigh, string? OilDir)
            AnalyzeSituation(Dictionary<string, MacroIndicatorDto> indicators)
        {
            var pairs = new List<(string, string)>();
            var inflationFlags = new List<bool>();

            // 물가(CPI·Core PCE): 전년 대비 상승률을 임계값으로 해석
            foreach (var key in new[] { "CPI", "CorePCE" })
            {
                if (!indicators.TryGetValue(key, out var ind) || ind.Error != null || ind.YoyPercent == null)
                    continue;
                decimal y = ind.YoyPercent.Value;
                string level = y >= 4.0m ? "매우 높은 편"
                    : y >= 3.0m ? "연준 목표(약 2%)를 웃도는 높은 편"
                    : y >= 2.0m ? "연준 목표(약 2%) 부근"
                    : "목표를 밑도는 낮은 편";
                pairs.Add((ind.Label, $"전년 대비 {y:+0.00;-0.00}%로 {level}"));
                inflationFlags.Add(y >= 3.0m);
            }

            // 유가(WTI): 값과 직전 대비 방향
            string? oilDir = null;
            if (indicators.TryGetValue("WTI", out var oil) && oil.Error == null && oil.LatestValue != null)
            {
                if (oil.Direction == "up" || oil.Direction == "down")
                {
                    oilDir = oil.Direction == "up" ? "상승" : "하락";
                    pairs.Add((oil.Label, $"${oil.LatestValue:0.00}로 직전 대비 {oilDir} 흐름"));
                }
                else
                {
                    pairs.Add((oil.Label, $"${oil.LatestValue:0.00}"));
                }
            }

            // 금리(10년)·실업률: 값과 직전 대비 방향
            foreach (var key in new[] { "RATE10Y", "UNEMP" })
            {
                if (!indicators.TryGetValue(key, out var ind) || ind.Error != null || ind.LatestValue == null)
                    continue;
                string word = ind.Direction switch { "up" => "상승", "down" => "하락", "flat" => "보합", _ => "" };
                string detail = string.IsNullOrEmpty(word)
                    ? $"{ind.LatestValue:0.00}%"
                    : $"{ind.LatestValue:0.00}%로 직전 대비 {word} 흐름";
                pairs.Add((ind.Label, detail));
            }

            bool? inflationHigh = inflationFlags.Count > 0 ? inflationFlags.Any(f => f) : (bool?)null;
            return (pairs, inflationHigh, oilDir);
        }

        /// <summary>물가·유가 방향으로 낙관/신중 시나리오를 구성합니다.</summary>
        private static string BuildScenario(bool? inflationHigh, string? oilDir)
        {
            var lines = new List<string>();
            if (inflationHigh == true)
                lines.Add("현재 물가가 목표를 웃돌고 있어, 단기적으로는 신중 시나리오의 무게가 더 큽니다.");
            else if (inflationHigh == false)
                lines.Add("현재 물가가 목표 부근/이하로, 낙관 시나리오의 여지가 있습니다.");

            lines.Add("- 낙관: 물가 상승률이 둔화되고 유가가 안정되면 금리 인하 기대가 커지며 "
                    + "자산시장에 우호적일 수 있습니다. (조건: 물가·유가의 하향 안정 지속)");
            lines.Add("- 신중: 물가가 높게 유지되거나 유가가 다시 오르면 긴축이 길어지며 "
                    + "변동성이 커질 수 있습니다. (조건: 물가 고착 또는 유가 재상승)");
            return string.Join("\n", lines);
        }

        /// <summary>한 줄 결론.</summary>
        private static string BuildConclusion(bool? inflationHigh, string? oilDir)
        {
            string baseLine = inflationHigh switch
            {
                true => "물가가 아직 높은 국면으로, 인플레이션 지표를 계속 주시할 필요가 있습니다.",
                false => "물가 압력이 비교적 낮은 국면입니다.",
                _ => "수치를 충분히 확인하지 못해 단정하기 어렵습니다."
            };
            if (!string.IsNullOrEmpty(oilDir))
                baseLine += $" 유가는 최근 {oilDir} 흐름입니다.";
            return baseLine;
        }
    }
}
