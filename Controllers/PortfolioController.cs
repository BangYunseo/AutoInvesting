using AutoInvest.Core;
using AutoInvest.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AutoInvest.Controllers
{
    /// <summary>
    /// 보유 잔고·예수금·대시보드 요약을 조회하는 API.
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

        /// <summary>
        /// 현재 보유 종목 목록을 조회합니다.
        /// </summary>
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
                return Ok(new { holdings });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Portfolio] 잔고 조회 실패: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// 대시보드 요약 정보를 한 번에 조회합니다.
        /// 보유 종목, 예수금(현금 잔고), 환율, 계좌 모드(SIM/PAPER/LIVE)와 마스킹 계좌번호를 포함합니다.
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            try
            {
                var client = _session.GetClient();
                if (!client.IsLoggedIn)
                {
                    await client.LoginAsync();
                }

                var holdings = await client.GetHoldingsAsync();
                var cashBalance = await client.GetCashBalanceAsync();
                var exchangeRate = await client.GetExchangeRateAsync();
                var (accountMode, accountMasked) = _session.GetAccountInfo();

                return Ok(new
                {
                    holdings,
                    cashBalance,
                    exchangeRate,
                    accountMode,
                    accountMasked
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Portfolio] 포트폴리오 요약 조회 실패: {ex.Message}");
                return StatusCode(500, "포트폴리오 요약 조회 중 오류가 발생했습니다.");
            }
        }
    }
}
