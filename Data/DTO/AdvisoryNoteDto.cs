using System.Collections.Generic;

namespace AutoInvest.Data.DTO
{
    /// <summary>
    /// 부가 조언(어드바이저리) 심각도.
    /// </summary>
    public enum AdvisorySeverity
    {
        /// <summary>참고 정보 (의사결정에 영향 없음)</summary>
        INFO,
        /// <summary>주의 (불리한 조건이지만 매매 가능)</summary>
        CAUTION,
        /// <summary>경고 (강한 불리 조건 — 진입 재고 권장)</summary>
        WARNING
    }

    /// <summary>
    /// 매매 신호와 별개로, 상황 컨텍스트(환율·변동성 등)에 따라 사용자에게 제공되는 부가 조언 (Phase 5-e).
    /// 매매 판정 자체에는 개입하지 않으며, 판정 결과에 첨부되어 표시 목적으로만 사용됩니다.
    /// <see cref="AutoInvest.Core.Advisors.IContextAdvisor"/> 구현체가 생성합니다.
    /// </summary>
    public class AdvisoryNoteDto
    {
        /// <summary>조언 출처 (예: "환율", "변동성")</summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>조언 심각도</summary>
        public AdvisorySeverity Severity { get; set; } = AdvisorySeverity.INFO;

        /// <summary>조언 제목 (한 줄 요약)</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>조언 본문 (상세 설명)</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>대체 종목·전략 제안 목록 (예: 환헤지 ETF). 없으면 빈 목록.</summary>
        public List<string> SuggestedAlternatives { get; set; } = new();
    }
}
