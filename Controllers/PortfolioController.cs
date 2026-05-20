using AutoInvest.Core;
using AutoInvest.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AutoInvest.Controllers
{
    /// <summary>
    /// 투자 자산 배분 및 잔고를 조회하는 API.
    /// 기존 WinForms의 AllocationPanel 역할을 대체합니다.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PortfolioController : ControllerBase
    {
        private readonly SessionManager _session;

        public PortfolioController(SessionManager session)
        {
            _session = session;
        }

        [HttpGet("holdings")]
        public async Task<IActionResult> GetHoldings()
        {
            try
            {
                var client = _session.GetClient();
                if (!client.IsLoggedIn)
                {
                    await client.LoginAsync();
                }

                var holdings = await client.GetHoldingsAsync();
                return Ok(holdings);
            }
            catch (Exception ex)
            {
                Logger.Error($"잔고 조회 실패: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }
    }
}
