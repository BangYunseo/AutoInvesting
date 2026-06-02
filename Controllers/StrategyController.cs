using AutoInvest.Data.DAO;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace AutoInvest.Controllers
{
    /// <summary>
    /// 투자 전략 CRUD API.
    /// 종목별 수량, 전략 유형을 관리합니다.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class StrategyController : ControllerBase
    {
        /// <summary>
        /// 전체 전략 요약 목록을 조회합니다.
        /// </summary>
        [HttpGet("summary")]
        public IActionResult GetStrategySummaries()
        {
            try
            {
                var summaries = StrategyDAO.GetStrategySummaries();
                return Ok(summaries);
            }
            catch (Exception ex)
            {
                Logger.Error($"[Strategy] 요약 조회 실패: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 특정 전략의 종목 목록을 조회합니다.
        /// </summary>
        /// <param name="name">전략명 (기본: 사용자정의)</param>
        [HttpGet("{name}")]
        public IActionResult GetStrategy(string name = "사용자정의")
        {
            try
            {
                var strategies = StrategyDAO.GetStrategy(name);
                Logger.Info($"[Strategy] 전략 '{name}' 조회: {strategies.Count}건");
                return Ok(strategies);
            }
            catch (Exception ex)
            {
                Logger.Error($"[Strategy] 전략 조회 실패: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 전략 전체를 저장(덮어쓰기)합니다.
        /// </summary>
        /// <param name="name">전략명</param>
        /// <param name="strategies">저장할 전략 종목 리스트</param>
        [HttpPost("{name}")]
        public IActionResult SaveStrategy(string name, [FromBody] List<StrategyDto> strategies)
        {
            try
            {
                if (strategies == null)
                {
                    return BadRequest(new { error = "잘못된 요청입니다." });
                }

                StrategyDAO.SaveStrategy(name, strategies);
                Logger.Info($"[Strategy] 전략 저장 완료: {name} ({strategies.Count}건)");
                return Ok(new { message = "전략이 성공적으로 저장되었습니다." });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Strategy] 전략 저장 실패: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 전략을 삭제합니다.
        /// </summary>
        /// <param name="name">전략명</param>
        [HttpDelete("{name}")]
        public IActionResult DeleteStrategy(string name)
        {
            try
            {
                StrategyDAO.DeleteStrategy(name);
                Logger.Info($"[Strategy] 전략 삭제: {name}");
                return Ok(new { message = "전략이 삭제되었습니다." });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Strategy] 전략 삭제 실패: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
