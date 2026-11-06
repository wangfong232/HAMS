using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HAMS.Application.DTOs;
using HAMS.Application.Interfaces;
using HAMS.Domain.Entities;
using HAMS.Infrastructure.Data;

namespace HAMSClinical.Controllers
{
    public class DoctorController : Controller
    {
        private readonly HamsDbContext _context;
        private readonly IAppointmentRepository _appointmentRepo;
        private readonly IEmrService _emrService;

        public DoctorController(
            HamsDbContext context,
            IAppointmentRepository appointmentRepo,
            IEmrService emrService)
        {
            _context = context;
            _appointmentRepo = appointmentRepo;
            _emrService = emrService;
        }

        // GET: /Doctor/Consultation
        public async Task<IActionResult> Consultation(int? appointmentId, string? message, string? messageType)
        {
            // Get Queue: Patients that have checked in, ordered by AppointmentDate (Giờ hẹn khám) ASC
            var waitingQueue = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.TimeSlot)
                .Include(a => a.VitalSigns)
                .Where(a => a.Status == "Arrived" || a.Status == "InConsultation")
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.QueueNumber ?? 999)
                .ToListAsync();

            Appointment? selectedAppointment = null;
            if (appointmentId.HasValue)
            {
                selectedAppointment = await _context.Appointments
                    .Include(a => a.Patient).ThenInclude(p => p.User)
                    .Include(a => a.Doctor).ThenInclude(d => d.User)
                    .Include(a => a.Doctor).ThenInclude(d => d.Department)
                    .Include(a => a.Doctor).ThenInclude(d => d.Schedules).ThenInclude(s => s.Room)
                    .Include(a => a.TimeSlot)
                    .Include(a => a.VitalSigns)
                    .Include(a => a.MedicalRecord).ThenInclude(m => m!.PrescriptionDetails).ThenInclude(p => p.Medicine)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId.Value);
            }
            else if (waitingQueue.Any())
            {
                selectedAppointment = waitingQueue.First();
            }

            var medicines = await _context.Medicines.Where(m => m.IsAvailable).ToListAsync();

            var completedAppointments = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.TimeSlot)
                .Where(a => a.Status == "Completed" && a.AppointmentDate.Date == DateTime.Today)
                .OrderByDescending(a => a.UpdatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.WaitingQueue = waitingQueue;
            ViewBag.CompletedList = completedAppointments;
            ViewBag.Medicines = medicines;
            ViewBag.Message = message;
            ViewBag.MessageType = messageType;

            return View(selectedAppointment);
        }

        // GET: /Doctor/ResetDemo
        [HttpGet]
        public IActionResult ResetDemo()
        {
            DbInitializer.ResetDemoData(_context);
            return RedirectToAction(nameof(Consultation), new { message = "Hệ thống đã được làm mới thành công về trạng thái ban đầu để demo lại!", messageType = "success" });
        }

        // POST: /Doctor/StartConsultation
        [HttpPost]
        public async Task<IActionResult> StartConsultation(int appointmentId)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment != null && appointment.Status == "Arrived")
            {
                appointment.Status = "InConsultation";
                appointment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Consultation", new { appointmentId = appointmentId });
        }

        // POST: /Doctor/SaveEmr
        [HttpPost]
        public async Task<IActionResult> SaveEmr(
            int appointmentId,
            string icd10Code,
            string diagnosis,
            string? note,
            int? medicineId,
            int? quantity,
            string? dosageInstruction)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                return RedirectToAction("Consultation", new { message = "Lịch hẹn không tồn tại.", messageType = "danger" });
            }

            var prescriptions = new System.Collections.Generic.List<PrescriptionLineDto>();
            string prescriptionSummary = "Không kê đơn thuốc";

            if (medicineId.HasValue && medicineId.Value > 0)
            {
                var medicine = await _context.Medicines.FindAsync(medicineId.Value);
                if (medicine != null)
                {
                    prescriptions.Add(new PrescriptionLineDto
                    {
                        MedicineId = medicineId.Value,
                        MedicineName = medicine.Name,
                        Quantity = quantity ?? 10,
                        UnitPrice = medicine.Price,
                        DosageInstruction = !string.IsNullOrWhiteSpace(dosageInstruction) ? dosageInstruction : "Theo chỉ dẫn của bác sĩ",
                        Status = "Pending"
                    });
                    prescriptionSummary = $"Đã kê đơn: {medicine.Name} x{quantity ?? 10}";
                }
            }

            var createEmrDto = new CreateEmrDto
            {
                AppointmentId = appointmentId,
                PatientId = appointment.PatientId,
                DoctorId = appointment.DoctorId,
                Diagnosis = diagnosis,
                Icd10Code = icd10Code,
                Note = note,
                Prescriptions = prescriptions
            };

            await _emrService.SaveEmrAndPrescriptionAsync(createEmrDto);

            // Mark Appointment as Completed
            appointment.MarkAsCompleted();
            await _context.SaveChangesAsync();

            return RedirectToAction("Consultation", new
            {
                appointmentId = appointmentId,
                message = $"Khám bệnh thành công! Đã lưu EMR chuẩn ICD-10 ({icd10Code}) - {prescriptionSummary} và chuyển trạng thái lịch sang [COMPLETED].",
                messageType = "success"
            });
        }

        // GET: /Doctor/GetWaitingQueue (Realtime sync for waiting list)
        [HttpGet]
        public async Task<IActionResult> GetWaitingQueue()
        {
            var waitingQueue = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.TimeSlot)
                .Include(a => a.VitalSigns)
                .Where(a => a.Status == "Arrived" || a.Status == "InConsultation")
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.QueueNumber ?? 999)
                .Select(a => new
                {
                    id = a.Id,
                    patientName = a.Patient != null && a.Patient.User != null ? a.Patient.User.FullName : "N/A",
                    appointmentTime = a.AppointmentDate.ToString("HH:mm"),
                    status = a.Status,
                    queueNumber = a.QueueNumber ?? 0,
                    bloodPressure = a.VitalSigns != null ? a.VitalSigns.BloodPressure : "",
                    pulse = a.VitalSigns != null ? a.VitalSigns.Pulse : 0,
                    temperature = a.VitalSigns != null ? a.VitalSigns.Temperature : 0m
                })
                .ToListAsync();

            return Json(waitingQueue);
        }
    }
}
