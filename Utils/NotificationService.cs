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
    /// Resend의 HTTP(REST) 이메일 API(443 포트)를 사용한다.
    /// </summary>
    public static class NotificationService
    {
        private const string ResendEndpoint = "https://api.resend.com/emails";

        // Resend 기본(테스트) 발신 도메인 — 도메인/발신자 인증 없이 사용 가능.
        // 단, 이 주소로는 "Resend 계정에 등록된 본인 이메일"로만 수신 가능.
        private const string DefaultSender = "onboarding@resend.dev";

        // 무한 대기 방지 — HTTP 호출 타임아웃 (SMTP 시절 2분 hang 재발 방지)
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = RequestTimeout };

        private static string _apiKey = "";
        private static string _senderEmail = DefaultSender;
        private static string _senderName = "AutoInvesting System";
        private static string _adminEmail = "";

        public static void Initialize(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            var resendSection = configuration.GetSection("Resend");
            var smtpSection = configuration.GetSection("Smtp"); // 수신 주소는 기존 설정과 호환 유지

            // API 키는 환경변수 우선 (시크릿)
            _apiKey = Coalesce(Environment.GetEnvironmentVariable("RESEND_API_KEY"), resendSection["ApiKey"]);

            // 수신자(관리자) — Resend:AdminEmail 우선, 없으면 기존 Smtp:AdminEmail 재사용
            _adminEmail = Coalesce(resendSection["AdminEmail"], smtpSection["AdminEmail"]);

            // 발신자 이메일 — 자체 도메인을 Resend에 인증했다면 그 주소, 아니면 기본 테스트 도메인 사용
            _senderEmail = Coalesce(resendSection["SenderEmail"], DefaultSender);

            _senderName = Coalesce(resendSection["SenderName"], smtpSection["SenderName"], _senderName);
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
        /// 관리자에게 알림 메일을 Resend HTTP API로 발송합니다. (진단용 — 실패 시 예외를 그대로 전파)
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
                    "이메일 설정(Resend ApiKey / SenderEmail / AdminEmail)이 비어 있어 알림 메일을 발송할 수 없습니다. " +
                    "Render 환경변수 RESEND_API_KEY 및 appsettings의 Smtp:AdminEmail(또는 Resend:AdminEmail)을 확인하세요.");
            }

            // ── Resend 요청 본문 구성 ──
            string fromHeader = string.IsNullOrEmpty(_senderName)
                ? _senderEmail
                : $"{_senderName} <{_senderEmail}>";

            var payload = new
            {
                from = fromHeader,
                to = new[] { _adminEmail },
                subject = $"[AutoInvesting] {subject}",
                html = messageBody
            };
            string json = JsonSerializer.Serialize(payload);

            using var request = new HttpRequestMessage(HttpMethod.Post, ResendEndpoint);
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var cts = new CancellationTokenSource(RequestTimeout);
            using var response = await _httpClient.SendAsync(request, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Resend 발송 실패 (HTTP {(int)response.StatusCode}): {Truncate(body, 500)}");
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
                Provider = "Resend (HTTP API)",
                ApiKeySet = !string.IsNullOrEmpty(_apiKey),
                SenderEmail = _senderEmail,
                SenderName = _senderName,
                AdminEmailSet = !string.IsNullOrEmpty(_adminEmail),
            };
        }

        private static string Truncate(string value, int max)
            => string.IsNullOrEmpty(value) || value.Length <= max ? value : value.Substring(0, max) + "...";

        /// <summary>null·빈 문자열·공백을 건너뛰고 첫 유효 값을 반환합니다. 모두 비면 빈 문자열.</summary>
        private static string Coalesce(params string?[] values)
        {
            foreach (var v in values)
            {
                if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
            }
            return string.Empty;
        }
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
