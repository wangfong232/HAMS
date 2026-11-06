using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HAMS.Infrastructure.Data;

namespace HAMS.Infrastructure.BackgroundWorkers
{
    public class ExpiredSlotCleanupWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public ExpiredSlotCleanupWorker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<HamsDbContext>();

                    var now = DateTime.UtcNow;
                    var expiredSlots = await db.TimeSlots
                        .Where(s => (s.Status == "Held" || s.Status == "Hold") && s.HoldExpiresAt != null && s.HoldExpiresAt <= now)
                        .ToListAsync(stoppingToken);

                    if (expiredSlots.Any())
                    {
                        foreach (var slot in expiredSlots)
                        {
                            slot.ReleaseHold();
                        }
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception)
                {

                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
