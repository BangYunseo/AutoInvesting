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

        public static async Task SendEmailAsync(string subject, string messageBody)
        {
            if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password) || string.IsNullOrEmpty(_adminEmail))
            {
                Logger.Warn("[Notification] SMTP 설정(ID/PW/AdminEmail)이 비어 있어 알림 메일을 발송할 수 없습니다.");
                return;
            }

            try
            {
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
            catch (Exception ex)
            {
                Logger.Error($"[Notification] 알림 메일 발송 중 오류 발생: {ex.Message}");
            }
        }
    }
}
