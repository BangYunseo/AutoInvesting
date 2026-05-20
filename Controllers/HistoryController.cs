using AutoInvest.Data;
using Microsoft.AspNetCore.Mvc;
using System;

namespace AutoInvest.Controllers
{
    /// <summary>
    /// 매매 이력과 애플리케이션 로그를 조회하는 API.
    /// 기존 WinForms의 HistoryPanel, LogPanel 역할을 대체합니다.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HistoryController : ControllerBase
    {
        [HttpGet("trades")]
        public IActionResult GetTradeHistory()
        {
            try
            {
                // TODO: TradeHistoryDAO 등 DB에서 매매 내역 로드
                return Ok(new { message = "추후 TradeHistoryDAO 연동 예정" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("logs")]
        public IActionResult GetSystemLogs()
        {
            try
            {
                // 로그 파일(log_yyMMdd.txt)을 읽어오거나 최근 로그 반환
                return Ok(new { message = "최근 시스템 로그" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
