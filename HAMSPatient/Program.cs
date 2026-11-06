using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HAMS.Application.Interfaces;
using HAMS.Application.Services;
using HAMS.Infrastructure.BackgroundWorkers;
using HAMS.Infrastructure.Data;
using HAMS.Infrastructure.ExternalServices;
using HAMS.Infrastructure.Repositories;
using HAMSPatient.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=(localdb)\\mssqllocaldb;Database=HamsCoreDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

builder.Services.AddDbContext<HamsDbContext>(options =>
    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("HAMS.Infrastructure")));

// Repositories
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<ITimeSlotRepository, TimeSlotRepository>();
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();

// External Services
builder.Services.AddScoped<INotificationService, SmtpEmailService>();

// Application Services
builder.Services.AddScoped<ICapacityEngine, CapacityEngine>();
builder.Services.AddScoped<IBookingService, BookingService>();

// Background Worker for Auto-releasing Expired Held Slots (every 30s)
builder.Services.AddHostedService<ExpiredSlotCleanupWorker>();

var app = builder.Build();

// Automatically ensure DB created & seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<HamsDbContext>();
    DbInitializer.Initialize(context);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapHub<BookingHub>("/bookingHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Booking}/{action=Search}/{id?}");

app.Run();
