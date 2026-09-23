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

            // API 키는 환경변수 우선 (시크릿)
            _apiKey = Coalesce(Environment.GetEnvironmentVariable("RESEND_API_KEY"), resendSection["ApiKey"]);

            // 수신자(관리자) — 환경변수 ADMIN_EMAIL 우선(개인정보를 소스에 두지 않음).
            _adminEmail = Coalesce(
                Environment.GetEnvironmentVariable("ADMIN_EMAIL"),
                resendSection["AdminEmail"]);

            // 발신자 이메일 — 자체 도메인을 Resend에 인증했다면 그 주소, 아니면 기본 테스트 도메인 사용
            _senderEmail = Coalesce(resendSection["SenderEmail"], DefaultSender);

            _senderName = Coalesce(resendSection["SenderName"], _senderName);
        }

        /// <summary>
        /// 관리자에게 알림 메일을 발송합니다. (운영 경로용 — 절대 예외를 전파하지 않음)
        /// 메일 실패가 적립 사이클 본 흐름을 죽이면 안 되므로 예외를 여기서 삼킵니다.
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
        /// Resend HTTP API로 실제 발송을 수행합니다.
        /// 설정 누락 시 <see cref="InvalidOperationException"/>, API 호출 실패 시 응답 본문을 담은 예외를 던지며,
        /// 호출부(<see cref="SendEmailAsync"/>)가 사유별로 로그 수준을 나눠 기록합니다.
        /// </summary>
        /// <param name="subject">메일 제목</param>
        /// <param name="messageBody">HTML 본문</param>
        private static async Task SendEmailOrThrowAsync(string subject, string messageBody)
        {
            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_senderEmail) || string.IsNullOrEmpty(_adminEmail))
            {
                throw new InvalidOperationException(
                    "이메일 설정(Resend ApiKey / SenderEmail / AdminEmail)이 비어 있어 알림 메일을 발송할 수 없습니다. " +
                    "Render 환경변수 RESEND_API_KEY·ADMIN_EMAIL을 확인하세요.");
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

}
