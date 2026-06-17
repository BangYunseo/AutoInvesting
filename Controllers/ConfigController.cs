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
                    { "GEMINI_MODEL", AppConfigManager.Get("GEMINI_MODEL", "gemini-2.0-flash") }
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
