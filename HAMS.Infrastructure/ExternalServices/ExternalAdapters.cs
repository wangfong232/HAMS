using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using HAMS.Application.Interfaces;

namespace HAMS.Infrastructure.ExternalServices
{
    public class SmtpNotificationService : INotificationService
    {
        private readonly IConfiguration? _config;

        public SmtpNotificationService(IConfiguration? config = null)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            Console.WriteLine($"[EMAIL] To: {toEmail} | Subject: {subject}");

            var host = _config?["EmailSettings:Host"];
            var portStr = _config?["EmailSettings:Port"];
            var username = _config?["EmailSettings:Username"];
            var password = _config?["EmailSettings:Password"];
            var fromEmail = _config?["EmailSettings:FromEmail"] ?? username ?? "noreply@hams.clinic";

            if (!string.IsNullOrEmpty(host) && int.TryParse(portStr, out int port) && !string.IsNullOrEmpty(username))
            {
                try
                {
                    using var client = new System.Net.Mail.SmtpClient(host, port)
                    {
                        Credentials = new System.Net.NetworkCredential(username, password),
                        EnableSsl = true
                    };

                    using var mail = new System.Net.Mail.MailMessage
                    {
                        From = new System.Net.Mail.MailAddress(fromEmail, "HAMS Clinic"),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };
                    mail.To.Add(toEmail);

                    await client.SendMailAsync(mail);
                    Console.WriteLine($"[EMAIL SENT SUCCESSFULLY] Delivered to {toEmail}");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SMTP WARNING] Failed to deliver real email via SMTP: {ex.Message}");
                }
            }

            // Fallback: save local HTML email preview
            try
            {
                var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sent_emails");
                Directory.CreateDirectory(folder);
                var filePath = Path.Combine(folder, $"Email_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{toEmail.Replace("@", "_")}.html");
                await File.WriteAllTextAsync(filePath, body);
            }
            catch { }
        }

        public async Task SendBookingConfirmationAsync(string toEmail, int appointmentId, DateTime appointmentTime, string qrCodeUrl)
        {
            string subject = $"[HAMS] Xác nhận đặt lịch khám thành công - Mã lịch hẹn #{appointmentId}";
            string qrImg = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={qrCodeUrl}";

            string body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8' /></head>
<body style='font-family: Arial, sans-serif; background-color: #f4f6f9; margin: 0; padding: 20px;'>
    <div style='max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1); border: 1px solid #e0e0e0;'>
        <div style='background-color: #0d6efd; color: #ffffff; padding: 24px; text-align: center;'>
            <h2 style='margin: 0; font-size: 22px;'>BỆNH VIỆN ĐA KHOA HAMS</h2>
            <p style='margin: 8px 0 0; font-size: 14px; opacity: 0.9;'>Xác Nhận Đặt Lịch Khám Bệnh & Vé Điện Tử</p>
        </div>
        <div style='padding: 24px; color: #333333; line-height: 1.6;'>
            <p>Kính gửi <strong>Quý bệnh nhân</strong>,</p>
            <p>Hệ thống HAMS xin thông báo lịch hẹn khám của Quý khách đã được xác nhận thành công trên hệ thống:</p>
            
            <div style='background-color: #f8f9fa; border: 1px solid #dee2e6; border-radius: 6px; padding: 16px; margin: 20px 0;'>
                <p style='margin: 6px 0;'><strong>Mã lịch hẹn:</strong> <span style='color: #0d6efd; font-weight: bold;'>#{appointmentId}</span></p>
                <p style='margin: 6px 0;'><strong>Thời gian khám:</strong> {appointmentTime:dd/MM/yyyy HH:mm}</p>
                <p style='margin: 6px 0;'><strong>Địa điểm:</strong> Phòng P201, Khoa Nội tổng quát</p>
                <p style='margin: 6px 0;'><strong>Mã vé QR:</strong> <span style='font-family: monospace; font-weight: bold;'>{qrCodeUrl}</span></p>
            </div>

            <div style='text-align: center; margin: 25px 0;'>
                <p style='font-weight: bold; color: #495057; margin-bottom: 10px;'>Mã QR làm thủ tục Check-in tại quầy Lễ tân:</p>
                <img src='{qrImg}' alt='Mã QR Vé Khám' style='width: 180px; height: 180px; border: 1px solid #ced4da; padding: 8px; border-radius: 8px; background: #fff;' />
            </div>

            <p style='color: #6c757d; font-size: 13px;'>* Quý khách vui lòng có mặt trước giờ hẹn <strong>15 phút</strong> và đưa mã QR này cho nhân viên lễ tân để làm thủ tục vào phòng khám.</p>
        </div>
        <div style='background-color: #f1f3f5; padding: 16px; text-align: center; font-size: 12px; color: #6c757d; border-top: 1px solid #e9ecef;'>
            Hệ thống Quản lý Y tế Đa viện HAMS &copy; 2026 - Hotline Hỗ trợ: 1900 1234
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(toEmail, subject, body);
        }
    }

    public class SmtpEmailService : SmtpNotificationService
    {
        public SmtpEmailService(IConfiguration? config = null) : base(config)
        {
        }
    }
}
