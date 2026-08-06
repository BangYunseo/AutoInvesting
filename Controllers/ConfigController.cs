using AutoInvest.Core;
using AutoInvest.Data;
using AutoInvest.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace AutoInvest.Controllers
{
    /// <summary>
    /// 운영 설정 값 (거래 모드, KIS 인증 정보 등)을 조회하고 변경하는 API.
    /// 프론트엔드 "설정" 페이지와 연동됩니다.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly SessionManager _session;

        /// <summary>
        /// 복호화 값을 단건 조회(보기)할 수 있는 시크릿 키 화이트리스트.
        /// 이 목록에 없는 키는 값 노출을 거부합니다.
        /// </summary>
        private static readonly HashSet<string> RevealableSecretKeys = new(StringComparer.Ordinal)
        {
            "KIS_APP_KEY", "KIS_APP_SECRET", "KIS_ACCOUNT_NO"
        };

        public ConfigController(SessionManager session)
        {
            _session = session;
        }

        /// <summary>
        /// 운영 설정을 조회합니다. 시크릿(KIS 키/계좌)은 값 대신 설정 여부(_SET)만 반환합니다.
        /// </summary>
        [HttpGet]
        public IActionResult GetAllConfigs()
        {
            try
            {
                // AppConfigManager를 통해 현재 설정 반환
                // (시크릿 값은 노출하지 않고, 운영에 필요한 설정만 반환)
                // Phase 6에서 판단 레이어(전략/스마트주문/스케줄러/리밸런싱/AI)를 제거하여
                // 관련 설정(ACTIVE_STRATEGY/INVEST_AMOUNT_KRW/ORDER_SCHEDULE/REBALANCE_THRESHOLD/AI_*)은
                // 더 이상 노출하지 않는다. (DCA 예산·목표비중은 /api/dca/config에서 관리)
                var configs = new Dictionary<string, string>
                {
                    { "IS_PAPER_TRADING", AppConfigManager.Get("IS_PAPER_TRADING", "1") },
                    { "KIS_SERVER", AppConfigManager.Get("KIS_SERVER", "vps") },
                    // ── 시크릿은 값 대신 설정 여부(boolean)만 노출 ──
                    { "KIS_APP_KEY_SET", string.IsNullOrWhiteSpace(AppConfigManager.Get("KIS_APP_KEY", "")) ? "0" : "1" },
                    { "KIS_APP_SECRET_SET", string.IsNullOrWhiteSpace(AppConfigManager.Get("KIS_APP_SECRET", "")) ? "0" : "1" },
                    { "KIS_ACCOUNT_NO_SET", string.IsNullOrWhiteSpace(AppConfigManager.Get("KIS_ACCOUNT_NO", "")) ? "0" : "1" }
                };
                return Ok(configs);
            }
            catch (Exception ex)
            {
                Logger.Error($"[Config] 설정 조회 실패: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// 운영 설정을 저장합니다. 시크릿 키는 빈 값으로 들어오면 기존 값을 유지(미변경)하며,
        /// 저장 후 세션을 리셋해 다음 호출부터 새 설정으로 브로커를 재생성합니다.
        /// </summary>
        /// <param name="newConfigs">키-값 설정 맵</param>
        [HttpPost]
        public IActionResult UpdateConfig([FromBody] Dictionary<string, string> newConfigs)
        {
            try
            {
                // 시크릿 키는 빈 값으로 덮어쓰지 않음 (UI에서 미입력 시 기존 값 유지)
                var secretKeys = new HashSet<string>
                {
                    "KIS_APP_KEY", "KIS_APP_SECRET", "KIS_ACCOUNT_NO", "RESEND_API_KEY", "API_ACCESS_KEY"
                };

                // AppConfigManager.Set은 실패를 예외가 아니라 false로 돌려준다. 예전에는 이 값을
                // 버려서 DB 쓰기가 실패해도 "저장되었습니다"만 남았고, 거래 모드를 실전으로 바꿨다고
                // 믿는 채 모의로 도는 상태가 만들어졌다(Neon 콜드스타트·연결 끊김에서 실제로 발생).
                // 실패한 키를 모아 그대로 알린다.
                var failedKeys = new List<string>();

                foreach (var kvp in newConfigs)
                {
                    if (secretKeys.Contains(kvp.Key) && string.IsNullOrWhiteSpace(kvp.Value))
                        continue; // 빈 입력은 미변경 처리

                    if (!AppConfigManager.Set(kvp.Key, kvp.Value))
                        failedKeys.Add(kvp.Key);
                }

                // ── 브로커/거래 설정이 바뀌었을 수 있으므로 세션을 초기화 ──
                //    (다음 호출 시 새 설정으로 브로커 클라이언트를 재생성한다)
                //    일부만 저장된 경우에도 성공한 값은 반영해야 하므로 실패 여부와 무관하게 호출한다.
                _session.Reset();

                if (failedKeys.Count > 0)
                {
                    // 키 이름만 남긴다 — 값은 로그·응답 어디에도 넣지 않는다(시크릿 포함 가능).
                    Logger.Error($"[Config] 설정 저장 실패 — 반영되지 않은 키: {string.Join(", ", failedKeys)}");
                    return StatusCode(500, new
                    {
                        error = $"일부 설정을 저장하지 못했습니다: {string.Join(", ", failedKeys)}. "
                            + "해당 값은 바뀌지 않았습니다 — 잠시 후 다시 시도하고, 화면의 현재 값을 확인하세요.",
                        failedKeys
                    });
                }

                return Ok(new { message = "설정이 성공적으로 저장되었습니다." });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Config] 설정 저장 실패: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// 저장된 시크릿 값(복호화 평문)을 단건 조회합니다.
        /// 입력한 키가 올바른지 UI에서 눈 아이콘으로 확인하는 용도이며,
        /// 글로벌 인증 필터(관리자 Bearer 또는 크론 x-api-key)를 통과한 요청만 도달합니다.
        /// 화이트리스트에 없는 키는 거부하고, 값은 로그에 남기지 않습니다.
        /// </summary>
        /// <param name="key">조회할 시크릿 키 (예: KIS_APP_KEY)</param>
        [HttpGet("secret/{key}")]
        public IActionResult RevealSecret(string key)
        {
            try
            {
                // 사람(세션 토큰)만 허용한다. 전역 필터는 x-api-key도 통과시키는데, 그 키는 크론용으로
                // GitHub Actions Secret에 있어 관리자 비밀번호와 노출 표면이 다르다. 그 키 하나로
                // 앱키·시크릿·계좌번호 평문이 나가는 통로를 남기지 않는다.
                // 표식이 없으면(필터를 타지 않았으면) 사람이 아닌 것으로 보고 거부한다(fail-closed).
                if (!string.Equals(
                        HttpContext.Items[ApiKeyAuthAttribute.AuthKindItemKey] as string,
                        ApiKeyAuthAttribute.AuthKindSession,
                        StringComparison.Ordinal))
                {
                    Logger.Warn($"[Config] 시크릿 조회 거부 — 세션 토큰이 아닌 자격증명 [{key}]");
                    return StatusCode(403, new
                    {
                        error = "시크릿 값 조회는 관리자 로그인(세션 토큰)으로만 가능합니다."
                    });
                }

                if (!RevealableSecretKeys.Contains(key))
                    return BadRequest(new { error = "조회할 수 없는 키입니다." });

                string value = AppConfigManager.Get(key, "");
                Logger.Info($"[Config] 시크릿 값 조회: {key} (값 비노출)");
                return Ok(new { key, value, set = !string.IsNullOrWhiteSpace(value) });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Config] 시크릿 조회 실패 [{key}]: {ex.Message}");
                return StatusCode(500, new { error = "서버 내부 오류가 발생했습니다." });
            }
        }
    }
}
