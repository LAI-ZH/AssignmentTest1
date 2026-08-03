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
            var user = _config["Smtp:User"] ?? "";
            var pass = _config["Smtp:Pass"] ?? "";
            var name = _config["Smtp:Name"] ?? "";
            var host = _config["Smtp:Host"] ?? "";
            var port = _config.GetValue<int>("Smtp:Port");

            mail.From = new MailAddress(user, name);

            using var smtp = new SmtpClient()
            {
                Host = host,
                Port = port,
                EnableSsl = true,
                Credentials = new NetworkCredential(user, pass)
            };
            smtp.Send(mail);
        }
    }
}