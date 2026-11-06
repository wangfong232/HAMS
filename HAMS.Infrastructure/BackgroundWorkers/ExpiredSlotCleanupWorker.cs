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
     
    }
}
