using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HAMS.Infrastructure.Data;
using HAMS.Application.Interfaces; // Import Interface chứa hàm SendEmail

namespace HAMS.Infrastructure.BackgroundWorkers
{
    public class ReminderWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public ReminderWorker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine(">>> [HỆ THỐNG] ReminderWorker đã khởi chạy ngầm...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 1. Tạo Scope ảo để Resolve DbContext và EmailService
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<HamsDbContext>();
                    var emailService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    // 2. Quét các ca khám ngày mai (Status = Confirmed vì DB Seed của bạn gán Confirmed)
                    var tomorrow = DateTime.Today.AddDays(1);

                    var appointmentsToRemind = await db.Appointments
                        .Include(a => a.Patient).ThenInclude(p => p.User) // Join bảng User để lấy Email
                        .Where(a => a.AppointmentDate.Date == tomorrow && a.Status == "Confirmed" && !a.IsReminded)
                        .ToListAsync(stoppingToken);

                    // 3. Xử lý gửi thư
                    if (appointmentsToRemind.Any())
                    {
                        foreach (var appt in appointmentsToRemind)
                        {
                            var patientEmail = appt.Patient?.User?.Email;

                            if (!string.IsNullOrEmpty(patientEmail))
                            {
                                // Tái sử dụng hàm Template HTML xịn xò của bạn
                                await emailService.SendBookingConfirmationAsync(
                                    patientEmail,
                                    appt.Id,
                                    appt.AppointmentDate,
                                    $"HAMS-QR-APP-{appt.Id}" // Data QR giả lập
                                );
                            }

                            // Đánh dấu để không spam
                            appt.MarkAsReminded();
                        }

                        await db.SaveChangesAsync(stoppingToken);
                        Console.WriteLine($">>> [HỆ THỐNG] Đã quét và gửi {appointmentsToRemind.Count} email nhắc lịch.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ReminderWorker ERROR]: {ex.Message}");
                }

                // Chạy mỗi 15 giây để test lúc bảo vệ
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
    }
}