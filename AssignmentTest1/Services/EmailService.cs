using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace AssignmentTest1.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                // ✅ 从 EmailSettings 读取配置
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderPassword = _configuration["EmailSettings:SenderPassword"];
                var senderName = _configuration["EmailSettings:SenderName"] ?? "FitBook";
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPortStr = _configuration["EmailSettings:SmtpPort"];

                // ✅ 检查配置是否完整
                if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(smtpServer) ||
                    string.IsNullOrEmpty(senderPassword) || string.IsNullOrEmpty(smtpPortStr))
                {
                    Console.WriteLine("❌ Email configuration is missing. Check appsettings.json");
                    Console.WriteLine($"SenderEmail: {senderEmail}");
                    Console.WriteLine($"SmtpServer: {smtpServer}");
                    Console.WriteLine($"SmtpPort: {smtpPortStr}");
                    return false;
                }

                if (!int.TryParse(smtpPortStr, out int smtpPort))
                {
                    Console.WriteLine($"❌ Invalid SmtpPort: {smtpPortStr}");
                    return false;
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = body
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpServer, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(senderEmail, senderPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                Console.WriteLine($"✅ Email sent to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Email error: {ex.Message}");
                return false;
            }
        }

        public string GetBookingConfirmationEmail(string userName, string className, string date, string time, string venue, string trainer, string bookingId)
        {
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='background: #4F6EF7; padding: 20px; text-align: center; border-radius: 8px 8px 0 0;'>
                        <h1 style='color: white; margin: 0;'>🏋️ FitBook</h1>
                    </div>
                    <div style='border: 1px solid #E5E7EB; padding: 30px; border-radius: 0 0 8px 8px;'>
                        <h2 style='color: #1A1A2E;'>Booking Confirmation</h2>
                        <p style='color: #6B7280;'>Hello <strong>{userName}</strong>,</p>
                        <p style='color: #6B7280;'>Thank you for booking with FitBook! Here are your booking details:</p>
                        
                        <div style='background: #F9FAFB; padding: 15px; border-radius: 8px; margin: 15px 0;'>
                            <p style='margin: 5px 0;'><strong>Booking ID:</strong> #{bookingId}</p>
                            <p style='margin: 5px 0;'><strong>Class:</strong> {className}</p>
                            <p style='margin: 5px 0;'><strong>Date:</strong> {date}</p>
                            <p style='margin: 5px 0;'><strong>Time:</strong> {time}</p>
                            <p style='margin: 5px 0;'><strong>Venue:</strong> {venue}</p>
                            <p style='margin: 5px 0;'><strong>Trainer:</strong> {trainer}</p>
                            <p style='margin: 5px 0;'><strong>Status:</strong> <span style='color: #22C55E;'>✅ Confirmed</span></p>
                        </div>

                        <div style='background: #FFF3CD; padding: 15px; border-radius: 8px; margin: 15px 0;'>
                            <p style='margin: 5px 0;'>📍 Please arrive <strong>10 minutes</strong> before class starts</p>
                            <p style='margin: 5px 0;'>💪 Bring your gym attire and water bottle</p>
                            <p style='margin: 5px 0;'>🔔 You can cancel your booking up to <strong>24 hours</strong> in advance</p>
                        </div>

                        <div style='text-align: center; margin: 20px 0;'>
                            <a href='https://localhost:7066/Booking/MyBookings' 
                               style='background: #4F6EF7; color: white; padding: 10px 30px; text-decoration: none; border-radius: 6px; display: inline-block; margin: 5px;'>
                                View My Bookings
                            </a>
                        </div>

                        <hr style='border: 0.5px solid #F3F4F6; margin: 20px 0;' />
                        <p style='color: #9CA3AF; font-size: 12px; text-align: center;'>
                            © 2026 FitBook - Fitness Class Booking System
                        </p>
                    </div>
                </body>
                </html>
            ";
        }
    }
}