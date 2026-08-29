using System.Net;
using System.Net.Mail;

namespace AssignmentTest1.Helpers
{
    public class EmailHelper
    {
        private readonly IConfiguration _config;

        public EmailHelper(IConfiguration config)
        {
            _config = config;
        }

        public void SendEmail(MailMessage mail)
        {
            // ✅ 使用正确的配置键名（匹配 appsettings.json）
            var user = _config["EmailSettings:SenderEmail"] ?? "";
            var pass = _config["EmailSettings:SenderPassword"] ?? "";
            var name = _config["EmailSettings:SenderName"] ?? "FitBook System";
            var host = _config["EmailSettings:SmtpServer"] ?? "";
            var port = _config.GetValue<int>("EmailSettings:SmtpPort", 587);

            // ✅ 调试日志
            Console.WriteLine($"=== Email Config ===");
            Console.WriteLine($"SenderEmail: '{user}'");
            Console.WriteLine($"SenderName: '{name}'");
            Console.WriteLine($"SmtpServer: '{host}'");
            Console.WriteLine($"SmtpPort: {port}");

            // ✅ 验证配置
            if (string.IsNullOrEmpty(user))
                throw new InvalidOperationException("SenderEmail is empty. Check appsettings.json");
            if (string.IsNullOrEmpty(host))
                throw new InvalidOperationException("SmtpServer is empty. Check appsettings.json");
            if (string.IsNullOrEmpty(pass))
                throw new InvalidOperationException("SenderPassword is empty. Check appsettings.json");

            // ✅ 验证收件人
            if (mail.To == null || mail.To.Count == 0)
                throw new InvalidOperationException("No recipient specified.");

            // ✅ 设置发件人
            mail.From = new MailAddress(user, name);

            Console.WriteLine($"Sending email from {user} to {mail.To[0].Address} via {host}:{port}");

            // ✅ 发送邮件
            using var smtp = new SmtpClient()
            {
                Host = host,
                Port = port,
                EnableSsl = true,
                Credentials = new NetworkCredential(user, pass),
                Timeout = 10000
            };
            smtp.Send(mail);
            Console.WriteLine("✅ Email sent successfully!");
        }
    }
}