using System;
using System.Threading.Tasks;
using AutoInvest.Core;
using AutoInvest.Utils;
using Microsoft.AspNetCore.Mvc;

namespace AutoInvest.Controllers
{
    /// <summary>
    /// 시장 국면 브리핑 API — 거시 지표(물가·유가·금리·고용)와 환율을 모아
    /// '지금이 어떤 국면인지'를 사람이 읽을 해설로 반환합니다.
    ///
    /// ⚠️ 정보/보고 전용입니다. 이 결과는 적립 매수 로직에 사용되지 않습니다
    /// ("사라/팔아라"가 아니라 "상황이 이렇다"만 제공 — 판단 레이어 재도입 금지).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MacroController : ControllerBase
    {
        /// <summary>
        /// 최신 시장 국면 브리핑(지표 묶음 + 국면 해설)을 반환합니다.
        /// 지표는 1시간 캐싱되며, 개별 지표 실패는 각 항목의 error 필드로 표현됩니다.
        /// </summary>
        [HttpGet("briefing")]
        public async Task<IActionResult> GetBriefing()
        {
            try
            {
                var briefing = await MacroBriefingService.GetBriefingAsync();
                return Ok(briefing);
            }
            catch (Exception ex)
            {
                Logger.Error($"[Macro] 국면 브리핑 조회 실패: {ex.Message}");
                return StatusCode(500, new { error = "시장 국면 브리핑을 생성하지 못했습니다." });
            }
        }
    }
}
