using Microsoft.AspNetCore.Mvc;
using AutoInvest.Core;
using AutoInvest.Utils;

namespace AutoInvest.Controllers
{
    /// <summary>
    /// 테스트용 API 도화지(모의계좌 전용)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly SessionManager _session;
        public TestController(SessionManager session)
        {
            _session = session;
        }

        [HttpGet("order-fills")]
        public async Task<IActionResult> TEST()
        {
            var client = _session.GetClient();
            var fills = await client.GetOrderFillsAsync("20260101", "20260911");
            return Ok(fills);
        }
    }
}
