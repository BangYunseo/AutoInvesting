using AutoInvest.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace AutoInvest.Controllers
{
    /// <summary>
    /// 시스템 설정 값 (API 키, 전략 등)을 조회하고 변경하는 API.
    /// 기존 WinForms의 ConfigPanel 역할을 대체합니다.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAllConfigs()
        {
            try
            {
                // AppConfigManager를 통해 현재 설정 반환
                var configs = new Dictionary<string, string>
                {
                    { "IS_PAPER_TRADING", AppConfigManager.Get("IS_PAPER_TRADING", "1") },
                    { "ACTIVE_STRATEGY", AppConfigManager.Get("ACTIVE_STRATEGY", "안정형") },
                    { "INVEST_AMOUNT_KRW", AppConfigManager.Get("INVEST_AMOUNT_KRW", "1000000") },
                    { "ORDER_SCHEDULE", AppConfigManager.Get("ORDER_SCHEDULE", "22:30") },
                    { "REBALANCE_THRESHOLD", AppConfigManager.Get("REBALANCE_THRESHOLD", "0.05") }
                };
                return Ok(configs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public IActionResult UpdateConfig([FromBody] Dictionary<string, string> newConfigs)
        {
            try
            {
                foreach (var kvp in newConfigs)
                {
                    AppConfigManager.Set(kvp.Key, kvp.Value);
                }
                return Ok(new { message = "설정이 성공적으로 저장되었습니다." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
