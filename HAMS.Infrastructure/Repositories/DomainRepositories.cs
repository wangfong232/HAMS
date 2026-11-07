using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HAMS.Application.Interfaces;
using HAMS.Domain.Entities;
using HAMS.Infrastructure.Data;

namespace HAMS.Infrastructure.Repositories
{
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly HamsDbContext _context;

        public DepartmentRepository(HamsDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Department>> GetAllAsync()
        {
            return await _context.Departments.ToListAsync();
        }
    }

    public class DoctorRepository : IDoctorRepository
    {
        private readonly HamsDbContext _context;

        public DoctorRepository(HamsDbContext context)
        {
            _context = context;
        }

        public async Task<Doctor?> GetByIdAsync(int id)
        {
            return await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Department)
                .Include(d => d.Schedules).ThenInclude(s => s.Room)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<List<Department>> GetAllDepartmentAsync()
        {
            return await _context.Departments.ToListAsync();
        }

        public async Task<List<Doctor>> GetDoctorsAsync(int? departmentId, string? term)
        {
            var query = _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Department)
                .Include(d => d.Schedules).ThenInclude(s => s.Room)
                .AsQueryable();

            if (departmentId.HasValue && departmentId.Value > 0)
            {
                query = query.Where(d => d.DepartmentId == departmentId.Value);
            }

            if (!string.IsNullOrWhiteSpace(term))
            {
                var lower = term.Trim().ToLower();
                query = query.Where(d => d.User.FullName.ToLower().Contains(lower) || d.Specialization.ToLower().Contains(lower));
            }

            return await query.ToListAsync();
        }

        public async Task<Doctor?> GetDoctorWithDetailsAsync(int doctorId)
        {
            return await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Department)
                .Include(d => d.Schedules).ThenInclude(s => s.Room)
                .FirstOrDefaultAsync(d => d.Id == doctorId);
        }
    }

    public class TimeSlotRepository : ITimeSlotRepository
    {
        private readonly HamsDbContext _context;

        public TimeSlotRepository(HamsDbContext context)
        {
            _context = context;
        }

        public async Task<List<TimeSlot>> GetSlotsByDoctorAndDateAsync(int doctorId, DateTime targetDate)
        {
            var dateOnly = targetDate.Date;
            var nextDay = dateOnly.AddDays(1);

            return await _context.TimeSlots
                .Include(ts => ts.Schedule).ThenInclude(s => s.Room)
                .Include(ts => ts.Doctor).ThenInclude(d => d.User)
                .Include(ts => ts.Doctor).ThenInclude(d => d.Department)
                .Where(ts => ts.DoctorId == doctorId && ts.SlotTime >= dateOnly && ts.SlotTime < nextDay)
                .OrderBy(ts => ts.SlotTime)
                .ToListAsync();
        }

        public async Task<(bool success, string? newRowVersion)> HoldSlotAsync(int slotId, byte[] expectedRowVersion, int? userId = null, int holdMinutes = 10)
        {
            var slot = await _context.TimeSlots.FirstOrDefaultAsync(ts => ts.Id == slotId);
            if (slot == null || slot.Status != "Open")
            {
                return (false, null);
            }

            _context.Entry(slot).Property(s => s.RowVersion).OriginalValue = expectedRowVersion;
            slot.HoldSlot(userId ?? 1, holdMinutes);

            try
            {
                await _context.SaveChangesAsync();
                return (true, Convert.ToBase64String(slot.RowVersion));
            }
            catch (DbUpdateConcurrencyException)
            {
                return (false, null);
            }
        }

        public async Task<string?> ReleaseSlotAsync(int slotId)
        {
            var slot = await _context.TimeSlots.FirstOrDefaultAsync(ts => ts.Id == slotId);
            if (slot == null) return null;

            if (slot.Status == "Hold" || slot.Status == "Held")
            {
                slot.ReleaseHold();
                await _context.SaveChangesAsync();
                return Convert.ToBase64String(slot.RowVersion);
            }
            return null;
        }

        public async Task<TimeSlot?> GetSlotByIdAsync(int slotId)
        {
            return await _context.TimeSlots
                .Include(ts => ts.Schedule).ThenInclude(s => s.Room)
                .Include(ts => ts.Doctor).ThenInclude(d => d.User)
                .Include(ts => ts.Doctor).ThenInclude(d => d.Department)
                .FirstOrDefaultAsync(ts => ts.Id == slotId);
        }

        public async Task<bool> UpdateSlotStatus(int slotId, string status)
        {
            var slot = await _context.TimeSlots.FindAsync(slotId);
            if (slot == null) return false;

            slot.Status = status;
            slot.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }

    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly HamsDbContext _context;

        public AppointmentRepository(HamsDbContext context)
        {
            _context = context;
        }

        public async Task<Appointment?> FindAppointmentById(int appointmentId)
        {
            return await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Doctor).ThenInclude(d => d.Department)
                .Include(a => a.TimeSlot).ThenInclude(ts => ts.Schedule).ThenInclude(s => s.Room)
                .Include(a => a.VitalSigns)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
        }

        public async Task<bool> UpdateAppointmentStatus(int appointmentId, string status, bool isRefunded = false)
        {
            var app = await _context.Appointments.FindAsync(appointmentId);
            if (app == null) return false;

            app.Status = status;
            app.IsRefunded = isRefunded;
            app.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Patient?> GetPatientByUserIdAsync(int userId)
        {
            return await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId || p.Id == userId);
        }

        public async Task<Appointment> CreateAppointmentAsync(int patientId, int slotId, string? reason)
        {
            var slot = await _context.TimeSlots
                .Include(ts => ts.Doctor)
                .FirstOrDefaultAsync(ts => ts.Id == slotId);
            if (slot == null) throw new InvalidOperationException("Slot does not exist.");

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == patientId || p.UserId == patientId)
                          ?? await _context.Patients.FirstOrDefaultAsync();

            int validPatientId = patient != null ? patient.Id : 1;

            var appointment = new Appointment
            {
                PatientId = validPatientId,
                DoctorId = slot.DoctorId,
                TimeSlotId = slotId,
                AppointmentDate = slot.SlotTime,
                DepositFee = slot.Doctor?.PricePerSlot ?? 150000m,
                Status = "Confirmed",
                Reason = reason
            };

            await _context.Appointments.AddAsync(appointment);
            await _context.SaveChangesAsync();
            return appointment;
        }

        public async Task<Appointment?> GetAppointmentDetailsAsync(int appointmentId)
        {
            return await FindAppointmentById(appointmentId);
        }

        public async Task<List<Appointment>> GetAppointmentsInNext24h()
        {
            var tomorrow = DateTime.Today.AddDays(1);
            return await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Where(a => a.AppointmentDate.Date == tomorrow && a.Status == "Confirmed" && !a.IsReminded)
                .ToListAsync();
        }

        public async Task UpdateAsync(Appointment appointment)
        {
            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();
        }
    }

    public class NotificationRepository : INotificationRepository
    {
        private readonly HamsDbContext _context;

        public NotificationRepository(HamsDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Notification notification)
        {
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
        }
    }

}
