using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;

namespace AutoInvest.Core.Advisors
{
    /// <summary>
    /// 등록된 모든 <see cref="IContextAdvisor"/>를 실행하여 부가 조언 목록을 수집합니다 (Phase 5-e).
    /// 개별 어드바이저의 예외는 격리되어 다른 조언 수집에 영향을 주지 않습니다.
    /// 새 조언 유형은 생성자 기본 목록 또는 주입을 통해 확장합니다.
    /// </summary>
    public class ContextAdvisorService
    {
        private readonly List<IContextAdvisor> _advisors;

        /// <param name="advisors">사용할 어드바이저 목록 (미지정 시 기본 세트: 환율)</param>
        public ContextAdvisorService(IEnumerable<IContextAdvisor>? advisors = null)
        {
            _advisors = advisors?.ToList() ?? new List<IContextAdvisor>
            {
                new FxRateAdvisor()
                // 향후: new VolatilityAdvisor(), new EarningsAdvisor() ...
            };
        }

        /// <summary>
        /// 모든 어드바이저를 평가하여 생성된 조언만 모아 반환합니다 (null·예외는 제외).
        /// </summary>
        public async Task<List<AdvisoryNoteDto>> GatherAsync(AdvisoryContext context)
        {
            var notes = new List<AdvisoryNoteDto>();

            foreach (var advisor in _advisors)
            {
                try
                {
                    var note = await advisor.EvaluateAsync(context);
                    if (note != null) notes.Add(note);
                }
                catch (Exception ex)
                {
                    Logger.Error($"[Advisor] {advisor.Name} 평가 실패: {ex.Message}");
                }
            }

            return notes;
        }
    }
}
