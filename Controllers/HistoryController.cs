using AutoInvest.Data.DAO;
using AutoInvest.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AutoInvest.Controllers
{
    /// <summary>
    /// 매매 이력과 시스템 로그를 조회하는 API.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HistoryController : ControllerBase
    {
        /// <summary>
        /// 매매 내역을 조회합니다.
        /// </summary>
        /// <param name="limit">최대 조회 건수 (기본 50)</param>
        [HttpGet("trades")]
        public IActionResult GetTradeHistory([FromQuery] int limit = 50)
        {
            try
            {
                var trades = TradeHistoryDAO.GetRecent(limit);
                Logger.Info($"[History] 매매 내역 {trades.Count}건 조회");
                return Ok(trades);
            }
            catch (Exception ex)
            {
                Logger.Error($"[History] 매매 내역 조회 실패: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 시스템 로그를 조회합니다.
        /// </summary>
        /// <param name="date">조회 날짜 (yyyy-MM-dd, 기본 오늘)</param>
        /// <param name="lines">최대 줄 수 (기본 200)</param>
        [HttpGet("logs")]
        public IActionResult GetSystemLogs([FromQuery] string? date = null, [FromQuery] int lines = 200)
        {
            try
            {
                string targetDate = date ?? DateTime.Now.ToString("yyyy-MM-dd");
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                string logFile = Path.Combine(logDir, $"{targetDate}.log");

                if (!System.IO.File.Exists(logFile))
                {
                    // 사용 가능한 로그 파일 목록 반환
                    var available = new List<string>();
                    if (Directory.Exists(logDir))
                    {
                        available = Directory.GetFiles(logDir, "*.log")
                            .Select(f => Path.GetFileNameWithoutExtension(f))
                            .OrderByDescending(f => f)
                            .ToList();
                    }

                    return Ok(new
                    {
                        message = $"{targetDate} 날짜의 로그 파일이 없습니다.",
                        availableDates = available
                    });
                }

                var logLines = System.IO.File.ReadAllLines(logFile)
                    .Reverse()
                    .Take(lines)
                    .Reverse()
                    .ToArray();

                return Ok(new
                {
                    date = targetDate,
                    totalLines = logLines.Length,
                    logs = logLines
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"[History] 로그 조회 실패: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
