using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoInvest.Core;
using AutoInvest.Core.Quant;
using AutoInvest.Data;
using AutoInvest.Data.DAO;
using AutoInvest.Data.DTO;
using Microsoft.AspNetCore.Mvc;

using AutoInvest.Utils; 

namespace AutoInvest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly SmartOrderEngine _engine;
        private readonly SessionManager _sessionManager;

        public TestController(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
            var broker = sessionManager.GetClient();
            var analyzer = sessionManager.GetAnalyzer();
            _engine = new SmartOrderEngine(broker, analyzer);
        }

        [HttpPost("inject-mock")]
        public IActionResult InjectMockData()
        {
            using (var conn = DBManager.Instance.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM TB_MARKET_SNAPSHOT WHERE TICKER = 'QQQ'";
                cmd.ExecuteNonQuery();
            }

            for (int i = 1; i <= 30; i++)
            {
                decimal prob = 0.50m + (i * 0.01m); // 0.51 to 0.80
                var dto = new MarketSnapshotDto
                {
                    SnapDate = DateTime.Now.AddDays(-i),
                    Ticker = "QQQ",
                    Price = 200m,
                    Position20d = 0.1m,
                    Rsi14 = 30m,
                    MacdValue = 0m,
                    MacdSignal = 0m,
                    BbUpper = 210m,
                    BbLower = 190m,
                    Signal = "BUY",
                    BuyProbability = prob,
                    SellProbability = 0m,
                    ChartAiScore = 0.6m,
                    FundAiScore = 0.6m
                };
                MarketSnapshotDAO.Insert(dto);
            }
            return Ok("Mock data injected. Range: 0.51 ~ 0.80. Expected 70th Percentile ~ 0.71");
        }

        [HttpGet("test-adaptive")]
        public async Task<IActionResult> TestAdaptive(string ticker = "QQQ")
        {
            var (threshold, reason) = AdaptiveThresholdEngine.GetBuyThreshold(ticker);
            var result = await _engine.AnalyzeAsync(ticker, "MEAN_REVERSION");

            return Ok(new
            {
                AdaptiveThreshold = threshold,
                ThresholdReason = reason,
                AnalysisResult = result
            });
        }

        [HttpPost("buy")]
        public async Task<IActionResult> Buy(string ticker = "QQQM", int qty = 1)
        {
            try
            {
                var broker = _sessionManager.GetClient();
                if (!broker.IsLoggedIn)
                {
                    await broker.LoginAsync();
                }

                // 현재가 조회 후 시장가(또는 현재가)로 매수
                decimal price = await broker.GetCurrentPriceAsync(ticker);
                if (price <= 0) return BadRequest($"현재가를 조회할 수 없습니다: {ticker}");

                string orderNo = await broker.PlaceBuyOrderAsync(ticker, qty, price);
                return Ok(new { message = "매수 주문 성공", orderNo, ticker, qty, price });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("send-report")]
        public async Task<IActionResult> SendDailyReport()
        {
            try
            {
                // 1. 토큰 사용량
                int totalTokens = TokenUsageDAO.GetTodayTotalTokens();
                
                // 2. AI 성과
                var (perfCount, avgWinRate) = AiPerformanceDAO.GetOverallPerformance();

                string htmlBody = $@"
                    <h2>AutoInvesting 일일 운용 보고서 (테스트 발송)</h2>
                    <hr/>
                    <h3>1. 금일 매매 내역</h3>
                    <p>테스트 발송이므로 매매 내역은 생략됩니다.</p>
                    <br/>
                    <h3>2. AI 성과 요약</h3>
                    <ul>
                        <li>현재까지 평가 완료된 신호 건수: {perfCount}건</li>
                        <li><strong>AI 누적 적중률(Win Rate): {avgWinRate:P1}</strong></li>
                    </ul>
                    <br/>
                    <h3>3. AI API 토큰 소모량</h3>
                    <ul>
                        <li>금일 사용 토큰 합계: <strong>{totalTokens:N0} tokens</strong></li>
                    </ul>
                    <hr/>
                    <p style='color: gray; font-size: 12px;'>본 메일은 TestController에 의해 발송되었습니다.</p>";

                await AutoInvest.Utils.NotificationService.SendEmailAsync("일일 운용 보고서 (테스트)", htmlBody);
                
                return Ok(new { message = "테스트 일일 보고서 메일 발송 성공" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// SMTP 설정으로 실제 테스트 이메일을 발송하고, 실패 시 그 원인을 HTTP 응답에 그대로 반환합니다.
        /// (운영 경로와 달리 예외를 삼키지 않으므로 "메일이 안 오는 진짜 이유"를 즉시 확인할 수 있습니다.)
        /// </summary>
        [HttpGet("send-test-email")]
        public async Task<IActionResult> SendTestEmail()
        {
            string subject = "AutoInvesting 테스트 이메일";
            string body = "<p>이것은 <b>AutoInvesting 시스템</b>에서 보낸 <b>테스트 이메일</b>입니다.<br/>이 메일이 성공적으로 도착했다면 SMTP 설정이 올바르게 작동하는 것입니다.</p>";
            try
            {
                // 진단용: 실패 원인을 응답으로 확인하기 위해 예외 전파 버전을 사용
                await NotificationService.SendEmailOrThrowAsync(subject, body);
                return Ok(new { ok = true, message = "테스트 이메일 발송 성공. 수신함(스팸함 포함)을 확인하세요." });
            }
            catch (InvalidOperationException ex)
            {
                // 설정 누락 — 발송 시도조차 못 한 경우
                Logger.Warn($"[TestController] 테스트 이메일 설정 누락: {ex.Message}");
                return StatusCode(503, new { ok = false, reason = "CONFIG_MISSING", message = ex.Message });
            }
            catch (Exception ex)
            {
                // 이메일 API 호출 실패 — 실제 원인을 그대로 노출
                Logger.Error($"[TestController] 테스트 이메일 발송 중 오류 발생: {ex.Message}");
                return StatusCode(500, new { ok = false, reason = "SEND_ERROR", message = ex.Message });
            }
        }

        /// <summary>
        /// 시스템 핵심 의존성(이메일 설정 / DB 연결 / 브로커 로그인)과 운영 모드(실/목업)를 한 번에 점검합니다.
        /// 비밀번호·키·계좌 등 시크릿 값은 노출하지 않고 "설정됨/연결됨" 여부와 활성 타입만 반환합니다.
        /// "지금 실데이터로 도는지, 무엇이 동작하지 않는지"를 응답 한 번으로 확인하기 위한 헬스체크입니다.
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> Health()
        {
            // ── 이메일 설정 점검 (시크릿 미노출) ──
            var email = NotificationService.GetConfigStatus();

            // ── DB 연결 점검 ──
            bool dbOk = false;
            string? dbError = null;
            try
            {
                using var conn = DBManager.Instance.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1";
                cmd.ExecuteScalar();
                dbOk = true;
            }
            catch (Exception ex)
            {
                dbError = ex.Message;
                Logger.Error($"[Health] DB 연결 점검 실패: {ex.Message}");
            }

            // ── 브로커(KIS/Sim) 로그인 점검 + 활성 타입 식별 ──
            bool brokerOk = false;
            string? brokerError = null;
            string brokerType = "(unknown)";
            try
            {
                var broker = _sessionManager.GetClient();
                brokerType = broker.GetType().Name; // SimBrokerClient / KisBrokerClient
                if (!broker.IsLoggedIn)
                {
                    await broker.LoginAsync();
                }
                brokerOk = broker.IsLoggedIn;
            }
            catch (Exception ex)
            {
                brokerError = ex.Message;
                Logger.Error($"[Health] 브로커 로그인 점검 실패: {ex.Message}");
            }

            // ── 활성 AI 분석기 타입 식별 ──
            string analyzerType = "(unknown)";
            try
            {
                analyzerType = _sessionManager.GetAnalyzer().GetType().Name; // AiMarketAnalyzer(Mock) / GeminiMarketAnalyzer
            }
            catch (Exception ex)
            {
                Logger.Error($"[Health] AI 분석기 식별 실패: {ex.Message}");
            }

            // ── 운영 모드 요약 (시크릿 값은 노출하지 않고 설정 여부만) ──
            bool isLiveBroker = brokerType == "KisBrokerClient";
            bool isLiveAi = analyzerType == "GeminiMarketAnalyzer";
            string kisServer = AppConfigManager.Get("KIS_SERVER", "vps");
            var mode = new
            {
                // 실데이터 운영 여부 — 둘 다 true여야 "진짜" 분석/주문
                liveBroker = isLiveBroker,
                liveAi = isLiveAi,
                brokerType,
                kisServer,                 // vps=모의투자 / prod=실전
                kisAppKeySet = !string.IsNullOrEmpty(AppConfigManager.Get("KIS_APP_KEY", "")),
                kisAccountSet = !string.IsNullOrEmpty(AppConfigManager.Get("KIS_ACCOUNT_NO", "")),
                analyzerType,
                aiProvider = AppConfigManager.Get("AI_PROVIDER", "mock"),
                geminiKeySet = !string.IsNullOrEmpty(AppConfigManager.Get("GEMINI_API_KEY", "")),
                activeStrategy = AppConfigManager.Get("ACTIVE_STRATEGY", "")
            };

            bool allOk = email.IsReady && dbOk && brokerOk;
            var payload = new
            {
                ok = allOk,
                mode,
                email = new
                {
                    ready = email.IsReady,
                    provider = email.Provider,
                    apiKeySet = email.ApiKeySet,
                    senderEmail = email.SenderEmail,
                    senderName = email.SenderName,
                    adminEmailSet = email.AdminEmailSet
                },
                db = new { ok = dbOk, error = dbError },
                broker = new { ok = brokerOk, error = brokerError, type = brokerType }
            };

            return allOk ? Ok(payload) : StatusCode(503, payload);
        }
    }
}
