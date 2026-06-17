using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AutoInvest.Utils
{
    /// <summary>
    /// 관리자 알림 메일 발송 서비스.
    /// Render.com이 아웃바운드 SMTP 포트(25/465/587)를 차단하므로, SMTP(MailKit) 대신
    /// Brevo의 HTTP(REST) 트랜잭션 이메일 API(443 포트)를 사용한다.
    /// </summary>
    public static class NotificationService
    {
        private const string BrevoEndpoint = "https://api.brevo.com/v3/smtp/email";

        // 무한 대기 방지 — HTTP 호출 타임아웃 (SMTP 시절 2분 hang 재발 방지)
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = RequestTimeout };

        private static string _apiKey = "";
        private static string _senderEmail = "";
        private static string _senderName = "AutoInvesting System";
        private static string _adminEmail = "";

        public static void Initialize(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            var brevoSection = configuration.GetSection("Brevo");
            var smtpSection = configuration.GetSection("Smtp"); // 발신/수신 주소는 기존 설정과 호환 유지

            // API 키는 환경변수 우선 (시크릿)
            _apiKey = Environment.GetEnvironmentVariable("BREVO_API_KEY")
                      ?? brevoSection["ApiKey"]
                      ?? string.Empty;

            // 수신자(관리자) — 기존 Smtp:AdminEmail 재사용, Brevo 섹션이 있으면 우선
            _adminEmail = brevoSection["AdminEmail"] ?? smtpSection["AdminEmail"] ?? string.Empty;

            // 발신자 이메일 — Brevo에 인증된 발신 주소. 미지정 시 관리자 이메일을 발신자로 사용
            _senderEmail = brevoSection["SenderEmail"] ?? _adminEmail;

            _senderName = brevoSection["SenderName"] ?? smtpSection["SenderName"] ?? _senderName;
        }

        /// <summary>
        /// 관리자에게 알림 메일을 발송합니다. (운영 경로용 — 절대 예외를 전파하지 않음)
        /// 일일 사이클 등 메일 실패가 본 흐름을 죽이면 안 되는 곳에서 사용합니다.
        /// 발송 성공 여부를 응답으로 확인해야 하면 <see cref="SendEmailOrThrowAsync"/>를 사용하세요.
        /// </summary>
        /// <param name="subject">메일 제목</param>
        /// <param name="messageBody">HTML 본문</param>
        public static async Task SendEmailAsync(string subject, string messageBody)
        {
            try
            {
                await SendEmailOrThrowAsync(subject, messageBody);
            }
            catch (InvalidOperationException ex)
            {
                // 설정 누락 — 발송 시도조차 못 함
                Logger.Warn($"[Notification] {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[Notification] 알림 메일 발송 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 관리자에게 알림 메일을 Brevo HTTP API로 발송합니다. (진단용 — 실패 시 예외를 그대로 전파)
        /// 설정 누락 시 <see cref="InvalidOperationException"/>, API 호출 실패 시 응답 본문을 담은 예외를 던집니다.
        /// 테스트/헬스체크 엔드포인트처럼 "실제 실패 원인"을 응답으로 확인해야 하는 곳에서 사용합니다.
        /// </summary>
        /// <param name="subject">메일 제목</param>
        /// <param name="messageBody">HTML 본문</param>
        public static async Task SendEmailOrThrowAsync(string subject, string messageBody)
        {
            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_senderEmail) || string.IsNullOrEmpty(_adminEmail))
            {
                throw new InvalidOperationException(
                    "이메일 설정(Brevo ApiKey / SenderEmail / AdminEmail)이 비어 있어 알림 메일을 발송할 수 없습니다. " +
                    "Render 환경변수 BREVO_API_KEY 및 appsettings의 Smtp:AdminEmail(또는 Brevo:SenderEmail)을 확인하세요.");
            }

            // ── Brevo 요청 본문 구성 ──
            var payload = new
            {
                sender = new { name = _senderName, email = _senderEmail },
                to = new[] { new { email = _adminEmail, name = "Admin" } },
                subject = $"[AutoInvesting] {subject}",
                htmlContent = messageBody
            };
            string json = JsonSerializer.Serialize(payload);

            using var request = new HttpRequestMessage(HttpMethod.Post, BrevoEndpoint);
            request.Headers.Add("accept", "application/json");
            request.Headers.Add("api-key", _apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var cts = new CancellationTokenSource(RequestTimeout);
            using var response = await _httpClient.SendAsync(request, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Brevo 발송 실패 (HTTP {(int)response.StatusCode}): {Truncate(body, 500)}");
            }

            Logger.Info($"[Notification] 관리자에게 알림 메일을 발송했습니다: {subject}");
        }

        /// <summary>
        /// 현재 이메일 발송 설정 상태를 점검합니다. (헬스체크용 — API 키 값은 노출하지 않고 채워짐 여부만 반환)
        /// </summary>
        public static EmailConfigStatus GetConfigStatus()
        {
            return new EmailConfigStatus
            {
                Provider = "Brevo (HTTP API)",
                ApiKeySet = !string.IsNullOrEmpty(_apiKey),
                SenderEmail = _senderEmail,
                SenderName = _senderName,
                AdminEmailSet = !string.IsNullOrEmpty(_adminEmail),
            };
        }

        private static string Truncate(string value, int max)
            => string.IsNullOrEmpty(value) || value.Length <= max ? value : value.Substring(0, max) + "...";
    }

    /// <summary>
    /// 이메일 발송 설정 점검 결과 (시크릿 값은 포함하지 않음 — 채워짐 여부만)
    /// </summary>
    public class EmailConfigStatus
    {
        public string Provider { get; set; } = string.Empty;
        public bool ApiKeySet { get; set; }
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public bool AdminEmailSet { get; set; }

        /// <summary>발송에 필요한 모든 항목(API 키/발신자/수신자)이 채워졌는지 여부</summary>
        public bool IsReady => ApiKeySet && !string.IsNullOrEmpty(SenderEmail) && AdminEmailSet;
    }
}
