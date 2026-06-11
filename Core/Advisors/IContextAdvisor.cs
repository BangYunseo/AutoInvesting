using System.Threading.Tasks;
using AutoInvest.Data.DTO;

namespace AutoInvest.Core.Advisors
{
    /// <summary>
    /// 상황 기반 부가 조언 제공자 (Phase 5-e).
    /// 매매 판정과 독립적으로, 환율·변동성·실적시즌 등 외부 컨텍스트를 평가하여
    /// 사용자에게 도움이 될 조언(<see cref="AdvisoryNoteDto"/>)을 생성합니다.
    /// 새 조언 유형 추가 시 이 인터페이스를 구현하고 <see cref="ContextAdvisorService"/>에 등록합니다.
    /// </summary>
    public interface IContextAdvisor
    {
        /// <summary>조언 출처명 (예: "환율")</summary>
        string Name { get; }

        /// <summary>
        /// 주어진 매매 컨텍스트를 평가하여 조언을 생성합니다.
        /// 해당 상황에 조언할 내용이 없으면 <c>null</c>을 반환합니다.
        /// </summary>
        /// <param name="context">매매 판정 컨텍스트</param>
        Task<AdvisoryNoteDto?> EvaluateAsync(AdvisoryContext context);
    }
}
