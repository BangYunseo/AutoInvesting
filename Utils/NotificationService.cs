using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using System;
using System.Threading.Tasks;

namespace AutoInvest.Utils
{
    public static class NotificationService
    {
        private static string _host = "smtp.naver.com";
        private static int _port = 465;
        private static bool _useSsl = true;
        private static string _username = "";
        private static string _password = "";
        private static string _senderName = "AutoInvesting System";
        private static string _adminEmail = "";

        public static void Initialize(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            var smtpSection = configuration.GetSection("Smtp");
            if (smtpSection.Exists())
            {
                _host = smtpSection["Host"] ?? _host;
                _port = int.TryParse(smtpSection["Port"], out int port) ? port : _port;
                _useSsl = bool.TryParse(smtpSection["UseSsl"], out bool ssl) ? ssl : _useSsl;
                
                // For real usage, prefer Environment Variables over appsettings.json for passwords
                _username = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? smtpSection["Username"] ?? string.Empty;
                _password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? smtpSection["Password"] ?? string.Empty;
                
                _senderName = smtpSection["SenderName"] ?? _senderName;
                _adminEmail = smtpSection["AdminEmail"] ?? string.Empty;
            }
        }

        /// <summary>
        /// 관리자에게 알림 메일을 발송합니다. (운영 경로용 — 절대 예외를 전파하지 않음)
        /// 일일 사이클 등 메일 실패가 본 흐름을 죽이면 안 되는 곳에서 사용합니다.
        /// 실패해도 로그만 남으므로, 발송 성공 여부를 응답으로 확인해야 하면 <see cref="SendEmailOrThrowAsync"/>를 사용하세요.
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
        /// 관리자에게 알림 메일을 발송합니다. (진단용 — 실패 시 예외를 그대로 전파)
        /// 설정 누락 시 <see cref="InvalidOperationException"/>, SMTP 연결/인증/발송 실패 시 원래 예외를 던집니다.
        /// 테스트/헬스체크 엔드포인트처럼 "실제 실패 원인"을 응답으로 확인해야 하는 곳에서 사용합니다.
        /// </summary>
        /// <param name="subject">메일 제목</param>
        /// <param name="messageBody">HTML 본문</param>
        public static async Task SendEmailOrThrowAsync(string subject, string messageBody)
        {
            if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password) || string.IsNullOrEmpty(_adminEmail))
            {
                throw new InvalidOperationException(
                    "SMTP 설정(ID/PW/AdminEmail)이 비어 있어 알림 메일을 발송할 수 없습니다. " +
                    "Render 환경변수 SMTP_USERNAME/SMTP_PASSWORD 및 appsettings의 Smtp:AdminEmail을 확인하세요.");
            }

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_senderName, _username + "@naver.com"));
            email.To.Add(new MailboxAddress("Admin", _adminEmail));
            email.Subject = $"[AutoInvesting] {subject}";
            email.Body = new TextPart(TextFormat.Html) { Text = messageBody };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_host, _port, _useSsl);
            await smtp.AuthenticateAsync(_username, _password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            Logger.Info($"[Notification] 관리자에게 알림 메일을 발송했습니다: {subject}");
        }

        /// <summary>
        /// 현재 SMTP 설정 상태를 점검합니다. (헬스체크용 — 비밀번호·계정 값은 노출하지 않고 채워짐 여부만 반환)
        /// </summary>
        /// <returns>설정 완료 여부와 항목별 채워짐 상태</returns>
        public static SmtpConfigStatus GetConfigStatus()
        {
            return new SmtpConfigStatus
            {
                Host = _host,
                Port = _port,
                UseSsl = _useSsl,
                UsernameSet = !string.IsNullOrEmpty(_username),
                PasswordSet = !string.IsNullOrEmpty(_password),
                AdminEmailSet = !string.IsNullOrEmpty(_adminEmail),
            };
        }
    }

    /// <summary>
    /// SMTP 설정 점검 결과 (시크릿 값은 포함하지 않음 — 채워짐 여부만)
    /// </summary>
    public class SmtpConfigStatus
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool UseSsl { get; set; }
        public bool UsernameSet { get; set; }
        public bool PasswordSet { get; set; }
        public bool AdminEmailSet { get; set; }

        /// <summary>발송에 필요한 모든 항목(ID/PW/관리자 이메일)이 채워졌는지 여부</summary>
        public bool IsReady => UsernameSet && PasswordSet && AdminEmailSet;
    }
}
