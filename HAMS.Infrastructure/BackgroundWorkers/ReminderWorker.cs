using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HAMS.Application.Interfaces;
using HAMS.Domain.Entities;

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
                    using var scope = _serviceProvider.CreateScope();

                    var appointmentRepo = scope.ServiceProvider.GetRequiredService<IAppointmentRepository>();
                    var notificationRepo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
                    var emailService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    var appointmentsToRemind = await appointmentRepo.GetAppointmentsInNext24h();

                    if (appointmentsToRemind.Any())
                    {
                        foreach (var appt in appointmentsToRemind)
                        {
                            var patientEmail = appt.Patient?.User?.Email;
                            var patientUserId = appt.Patient?.UserId ?? 1;

                            if (!string.IsNullOrEmpty(patientEmail))
                            {
                                await emailService.SendBookingConfirmationAsync(
                                    patientEmail,
                                    appt.Id,
                                    appt.AppointmentDate,
                                    $"HAMS-QR-APP-{appt.Id}"
                                );

                                var notif = new Notification
                                {
                                    UserId = patientUserId,
                                    Title = "Nhắc lịch khám",
                                    Message = $"Bạn có lịch khám (Mã {appt.Id}) vào lúc {appt.AppointmentDate:dd/MM/yyyy HH:mm}.",
                                    Type = "EmailReminder",
                                    IsRead = false
                                };
                                await notificationRepo.AddAsync(notif);
                            }

                            appt.MarkAsReminded();
                            await appointmentRepo.UpdateAsync(appt);
                        }

                        Console.WriteLine($">>> [HỆ THỐNG] Đã quét và gửi {appointmentsToRemind.Count} email nhắc lịch.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ReminderWorker ERROR]: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
    }
}