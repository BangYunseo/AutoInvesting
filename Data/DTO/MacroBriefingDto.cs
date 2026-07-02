using System;
using System.Collections.Generic;

namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// 시장 국면 브리핑 결과 — 거시 지표 묶음 + 사람이 읽을 국면 해설(정보 전용).
    ///
    /// ⚠️ 이 객체는 오직 '표시/보고'를 위한 것이며, 적립 엔진(DcaAccumulationEngine)이
    /// 참조하지 않는다. 매수 수량·타이밍에 어떤 값도 흘러가지 않는다(판단 레이어 재도입 금지).
    /// </summary>
    public class MacroBriefingDto
    {
        /// <summary>표시할 거시 지표 목록.</summary>
        public List<MacroIndicatorDto> Indicators { get; set; } = new();

        /// <summary>국면 해설 본문 (4단 구조: 현재 상황 / 뉴스 / 시나리오 / 결론).</summary>
        public string Narrative { get; set; } = string.Empty;

        /// <summary>해설 생성 주체: "AI" 또는 "규칙 기반".</summary>
        public string Source { get; set; } = "규칙 기반";

        /// <summary>폴백 등 부가 설명 (예: "AI 키가 없어 규칙 기반으로 대체").</summary>
        public string Note { get; set; } = string.Empty;

        /// <summary>브리핑 생성 시각 (UTC).</summary>
        public DateTime GeneratedAt { get; set; }
    }
}
