using AutoInvest.Core.Quant;
using AutoInvest.Data;
using AutoInvest.Data.DAO;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

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
        /// 자산 마스터(전략에 편입 가능한 허용 종목) 전체 목록을 조회합니다.
        /// </summary>
        [HttpGet("assets")]
        public IActionResult GetAssetMaster()
        {
            try
            {
                var assets = StrategyDAO.GetAssetMaster();
                Logger.Info($"[Strategy] 자산 마스터 조회: {assets.Count}종목");
                return Ok(assets);
            }
            catch (Exception ex)
            {
                Logger.Error($"[Strategy] 자산 마스터 조회 실패: {ex.Message}");
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
        /// 활성 전략(또는 지정 전략) 종목들의 적응형 임계값 작동 현황을 진단합니다.
        /// 종목별 누적 스냅샷 표본 수와 현재 적용 임계값(기본값/적응값)을 한 번에 반환하여,
        /// 적응형 임계값이 데이터 기반으로 실제 작동 중인지 점검하는 데 사용합니다.
        /// </summary>
        /// <param name="name">전략명 (미지정 시 활성 전략 ACTIVE_STRATEGY 사용)</param>
        [HttpGet("adaptive-status")]
        public IActionResult GetAdaptiveStatus([FromQuery] string? name = null)
        {
            try
            {
                string strategyName = string.IsNullOrWhiteSpace(name)
                    ? AppConfigManager.Get("ACTIVE_STRATEGY", "사용자정의")
                    : name;

                var strategies = StrategyDAO.GetStrategy(strategyName);
                var items = strategies
                    .Select(s => AdaptiveThresholdEngine.GetStatus(s.Ticker))
                    .ToList();

                Logger.Info($"[Strategy] 적응형 임계값 진단: 전략 '{strategyName}' 종목 {items.Count}개");
                return Ok(new { strategy = strategyName, items });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Strategy] 적응형 임계값 진단 실패: {ex.Message}");
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
