using AutoInvest.Data.DAO;
using AutoInvest.Data.DTO;
using AutoInvest.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace AutoInvest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SellPlanController : ControllerBase
    {
        [HttpGet]
        public ActionResult<List<SellPlanDto>> GetActivePlans()
        {
            try
            {
                var plans = SellPlanDAO.GetAllActivePlans();
                return Ok(plans);
            }
            catch (Exception ex)
            {
                Logger.Error($"[API] SellPlan 조회 실패: {ex.Message}");
                return StatusCode(500, "서버 내부 오류가 발생했습니다.");
            }
        }

        [HttpPost]
        public ActionResult<SellPlanDto> CreatePlan([FromBody] SellPlanDto dto)
        {
            try
            {
                dto.Status = "ACTIVE";
                dto.SoldQty = 0;
                int id = SellPlanDAO.Insert(dto);
                if (id > 0)
                {
                    dto.PlanId = id;
                    Logger.Info($"[API] 분할매도 플랜 생성 완료 (ID: {id}, Ticker: {dto.Ticker}, Type: {dto.StrategyType})");
                    return Ok(dto);
                }
                return StatusCode(500, "플랜 생성에 실패했습니다.");
            }
            catch (Exception ex)
            {
                Logger.Error($"[API] SellPlan 생성 실패: {ex.Message}");
                return StatusCode(500, "서버 내부 오류가 발생했습니다.");
            }
        }

        [HttpDelete("{id}")]
        public ActionResult CancelPlan(int id)
        {
            try
            {
                var plans = SellPlanDAO.GetAllActivePlans();
                var plan = plans.Find(p => p.PlanId == id);
                if (plan == null)
                {
                    return NotFound("활성화된 플랜을 찾을 수 없습니다.");
                }

                plan.Status = "CANCELLED";
                SellPlanDAO.Update(plan);
                Logger.Info($"[API] 분할매도 플랜 취소 완료 (ID: {id})");
                return Ok(new { Message = "취소되었습니다." });
            }
            catch (Exception ex)
            {
                Logger.Error($"[API] SellPlan 취소 실패: {ex.Message}");
                return StatusCode(500, "서버 내부 오류가 발생했습니다.");
            }
        }
    }
}
