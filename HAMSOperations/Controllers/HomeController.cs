using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HAMS.Application.Interfaces;
using HAMS.Domain.Entities;
using HAMS.Infrastructure.Data;

namespace HAMSOperations.Controllers
{
    public class ReceptionController : Controller
    {
        private readonly HamsDbContext _context;
        private readonly IAppointmentRepository _appointmentRepo;

        public ReceptionController(HamsDbContext context, IAppointmentRepository appointmentRepo)
        {
            _context = context;
            _appointmentRepo = appointmentRepo;
        }

        // GET: /Reception/CheckIn
        public async Task<IActionResult> CheckIn(int? appointmentId, string? message, string? messageType)
        {
            Appointment? appointment = null;
            if (appointmentId.HasValue)
            {
                appointment = await _appointmentRepo.FindAppointmentById(appointmentId.Value);
            }

            // List appointments for today ordered by AppointmentDate (Giờ khám) ASC
            var todayAppointments = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.TimeSlot)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.Id)
                .Take(20)
                .ToListAsync();

            ViewBag.Message = message;
            ViewBag.MessageType = messageType;
            ViewBag.TodayAppointments = todayAppointments;

            return View(appointment);
        }

        // GET: /Reception/ResetDemo
        [HttpGet]
        public IActionResult ResetDemo()
        {
            DbInitializer.ResetDemoData(_context);
            return RedirectToAction(nameof(CheckIn), new { message = "Hệ thống đã được làm mới thành công về trạng thái ban đầu để demo lại!", messageType = "success" });
        }

        // POST: /Reception/Search
        [HttpPost]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction("CheckIn", new { message = "Vui lòng nhập mã QR vé hoặc số điện thoại.", messageType = "warning" });
            }

            query = query.Trim();

            // Check if query is HAMS-APT-X or APP-X
            int aptId = 0;
            if (query.StartsWith("HAMS-APT-", StringComparison.OrdinalIgnoreCase))
            {
                int.TryParse(query.Replace("HAMS-APT-", "", StringComparison.OrdinalIgnoreCase), out aptId);
            }
            else if (query.StartsWith("APP-", StringComparison.OrdinalIgnoreCase))
            {
                int.TryParse(query.Replace("APP-", "", StringComparison.OrdinalIgnoreCase), out aptId);
            }
            else
            {
                int.TryParse(query, out aptId);
            }

            Appointment? appointment = null;
            if (aptId > 0)
            {
                appointment = await _appointmentRepo.FindAppointmentById(aptId);
            }

            if (appointment == null)
            {
                // Try phone number search
                appointment = await _context.Appointments
                    .Include(a => a.Patient).ThenInclude(p => p.User)
                    .Include(a => a.Doctor).ThenInclude(d => d.User)
                    .Include(a => a.TimeSlot)
                    .FirstOrDefaultAsync(a => a.Patient.User.PhoneNumber == query);
            }

            if (appointment == null)
            {
                return RedirectToAction("CheckIn", new { message = $"Không tìm thấy lịch hẹn cho mã/SĐT: {query}", messageType = "danger" });
            }

            return RedirectToAction("CheckIn", new { appointmentId = appointment.Id });
        }

        // POST: /Reception/ConfirmCheckIn
        [HttpPost]
        public async Task<IActionResult> ConfirmCheckIn(
            int appointmentId,
            string bloodPressure,
            int pulseRate,
            decimal temperature)
        {
            var appointment = await _context.Appointments
                .Include(a => a.VitalSigns)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                return RedirectToAction("CheckIn", new { message = "Lịch hẹn không tồn tại.", messageType = "danger" });
            }

            // Mark Appointment as Arrived
            appointment.MarkAsArrived(2, 0);

            // Re-sequence queue numbers based on AppointmentDate (Giờ khám) for all arrived/in-consultation appointments today
            var arrivedToday = await _context.Appointments
                .Where(a => a.DoctorId == appointment.DoctorId
                    && a.AppointmentDate.Date == appointment.AppointmentDate.Date
                    && (a.Status == "Arrived" || a.Status == "InConsultation" || a.Status == "Completed"))
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.Id)
                .ToListAsync();

            int q = 1;
            int assignedQueue = 1;
            foreach (var app in arrivedToday)
            {
                app.QueueNumber = q;
                if (app.Id == appointment.Id)
                {
                    assignedQueue = q;
                }
                q++;
            }

            // Record Vital Signs
            if (appointment.VitalSigns == null)
            {
                appointment.VitalSigns = new VitalSigns
                {
                    AppointmentId = appointment.Id,
                    BloodPressure = bloodPressure,
                    Pulse = pulseRate,
                    Temperature = temperature,
                    Weight = 65m
                };
                _context.VitalSigns.Add(appointment.VitalSigns);
            }
            else
            {
                appointment.VitalSigns.BloodPressure = bloodPressure;
                appointment.VitalSigns.Pulse = pulseRate;
                appointment.VitalSigns.Temperature = temperature;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("CheckIn", new
            {
                appointmentId = appointment.Id,
                message = $"Check-in thành công! Đã chuyển trạng thái sang [ARRIVED] và cấp Số thứ tự khám: #{assignedQueue} (theo giờ hẹn {appointment.AppointmentDate:HH:mm})",
                messageType = "success"
            });
        }

        // GET: /Reception/GetTodayAppointments (Realtime sync)
        [HttpGet]
        public async Task<IActionResult> GetTodayAppointments()
        {
            var todayAppointments = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.TimeSlot)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.Id)
                .Take(20)
                .Select(a => new
                {
                    id = a.Id,
                    patientName = a.Patient != null && a.Patient.User != null ? a.Patient.User.FullName : "N/A",
                    phoneNumber = a.Patient != null && a.Patient.User != null ? a.Patient.User.PhoneNumber : "N/A",
                    doctorName = a.Doctor != null && a.Doctor.User != null ? a.Doctor.User.FullName : "N/A",
                    appointmentTime = a.AppointmentDate.ToString("HH:mm"),
                    status = a.Status,
                    queueNumber = a.QueueNumber
                })
                .ToListAsync();

            return Json(todayAppointments);
        }
    }
}