using AutoInvest.Core;
using AutoInvest.Data;
using AutoInvest.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoInvest.Controllers
{
    /// <summary>
    /// 시스템 설정 값 (API 키, 전략 등)을 조회하고 변경하는 API.
    /// ConfigPanel 역할을 대체합니다.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly SessionManager _session;

        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        /// <summary>
        /// 복호화 값을 단건 조회(보기)할 수 있는 시크릿 키 화이트리스트.
        /// 이 목록에 없는 키는 값 노출을 거부합니다.
        /// </summary>
        private static readonly HashSet<string> RevealableSecretKeys = new(StringComparer.Ordinal)
        {
            "KIS_APP_KEY", "KIS_APP_SECRET", "KIS_ACCOUNT_NO", "GEMINI_API_KEY"
        };

        public ConfigController(SessionManager session)
        {
            _session = session;
        }

        [HttpGet]
        public IActionResult GetAllConfigs()
        {
            try
            {
                // AppConfigManager를 통해 현재 설정 반환
                // (GEMINI_API_KEY 등 시크릿 값은 노출하지 않고, 운영에 필요한 설정만 반환)
                var configs = new Dictionary<string, string>
                {
                    { "IS_PAPER_TRADING", AppConfigManager.Get("IS_PAPER_TRADING", "1") },
                    { "ACTIVE_STRATEGY", AppConfigManager.Get("ACTIVE_STRATEGY", "안정형") },
                    { "INVEST_AMOUNT_KRW", AppConfigManager.Get("INVEST_AMOUNT_KRW", "1000000") },
                    { "ORDER_SCHEDULE", AppConfigManager.Get("ORDER_SCHEDULE", "22:30") },
                    { "REBALANCE_THRESHOLD", AppConfigManager.Get("REBALANCE_THRESHOLD", "0.05") },
                    { "AI_PROVIDER", AppConfigManager.Get("AI_PROVIDER", "mock") },
                    { "GEMINI_MODEL", AppConfigManager.Get("GEMINI_MODEL", "gemini-2.0-flash") },
                    { "KIS_SERVER", AppConfigManager.Get("KIS_SERVER", "vps") },
                    // ── 시크릿은 값 대신 설정 여부(boolean)만 노출 ──
                    { "KIS_APP_KEY_SET", string.IsNullOrWhiteSpace(AppConfigManager.Get("KIS_APP_KEY", "")) ? "0" : "1" },
                    { "KIS_APP_SECRET_SET", string.IsNullOrWhiteSpace(AppConfigManager.Get("KIS_APP_SECRET", "")) ? "0" : "1" },
                    { "KIS_ACCOUNT_NO_SET", string.IsNullOrWhiteSpace(AppConfigManager.Get("KIS_ACCOUNT_NO", "")) ? "0" : "1" },
                    { "GEMINI_API_KEY_SET", string.IsNullOrWhiteSpace(AppConfigManager.Get("GEMINI_API_KEY", "")) ? "0" : "1" }
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
                // 시크릿 키는 빈 값으로 덮어쓰지 않음 (UI에서 미입력 시 기존 값 유지)
                var secretKeys = new HashSet<string>
                {
                    "KIS_APP_KEY", "KIS_APP_SECRET", "KIS_ACCOUNT_NO", "GEMINI_API_KEY", "RESEND_API_KEY", "API_ACCESS_KEY"
                };

                foreach (var kvp in newConfigs)
                {
                    if (secretKeys.Contains(kvp.Key) && string.IsNullOrWhiteSpace(kvp.Value))
                        continue; // 빈 입력은 미변경 처리

                    AppConfigManager.Set(kvp.Key, kvp.Value);
                }

                // ── 브로커/AI 설정이 바뀌었을 수 있으므로 세션을 초기화 ──
                //    (다음 호출 시 새 설정으로 클라이언트·분석기를 재생성한다)
                _session.Reset();

                return Ok(new { message = "설정이 성공적으로 저장되었습니다." });
            }
            catch (Exception ex)
            {
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

        /// <summary>
        /// 현재 Gemini API 키로 사용 가능한 모델 목록을 조회합니다 (generateContent 지원 gemini 계열만).
        /// 설정 화면의 AI 모델 드롭다운을 채우는 데 사용합니다.
        /// </summary>
        [HttpGet("gemini-models")]
        public async Task<IActionResult> GetGeminiModels()
        {
            try
            {
                string apiKey = AppConfigManager.Get("GEMINI_API_KEY", "");
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return Ok(new { models = Array.Empty<string>(), error = "GEMINI_API_KEY가 설정되지 않았습니다." });
                }

                string url = $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}";
                var resp = await _httpClient.GetAsync(url);
                string body = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    Logger.Warn($"[Config] Gemini 모델 목록 조회 실패 HTTP {(int)resp.StatusCode}");
                    return Ok(new { models = Array.Empty<string>(), error = $"모델 목록 조회 실패 ({(int)resp.StatusCode})" });
                }

                var list = new List<string>();
                using (var doc = JsonDocument.Parse(body))
                {
                    if (doc.RootElement.TryGetProperty("models", out var models))
                    {
                        foreach (var m in models.EnumerateArray())
                        {
                            string name = m.TryGetProperty("name", out var n) ? (n.GetString() ?? "") : "";

                            bool supportsGenerate = false;
                            if (m.TryGetProperty("supportedGenerationMethods", out var methods))
                            {
                                foreach (var meth in methods.EnumerateArray())
                                {
                                    if (meth.GetString() == "generateContent")
                                    {
                                        supportsGenerate = true;
                                        break;
                                    }
                                }
                            }

                            // generateContent를 지원하는 gemini 계열 모델만 노출
                            if (supportsGenerate && name.StartsWith("models/gemini"))
                            {
                                list.Add(name.Replace("models/", ""));
                            }
                        }
                    }
                }

                return Ok(new { models = list });
            }
            catch (Exception ex)
            {
                Logger.Error($"[Config] Gemini 모델 목록 조회 오류: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
