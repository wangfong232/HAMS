using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using HAMS.Application.DTOs;
using HAMS.Application.Interfaces;
using HAMS.Infrastructure.Data;
using HAMSPatient.Hubs;
using HAMSPatient.Models;

namespace HAMSPatient.Controllers
{
    public class BookingController : Controller
    {
        private readonly IBookingService bookingService;
        private readonly ICapacityEngine capacityEngine;
        private readonly ITimeSlotRepository timeSlotRepository;
        private readonly IHubContext<BookingHub> hubContext;
        private readonly HamsDbContext dbContext;

        public BookingController(
            IBookingService bookingService,
            ICapacityEngine capacityEngine,
            ITimeSlotRepository timeSlotRepository,
            IHubContext<BookingHub> hubContext,
            HamsDbContext dbContext)
        {
            this.bookingService = bookingService;
            this.capacityEngine = capacityEngine;
            this.timeSlotRepository = timeSlotRepository;
            this.hubContext = hubContext;
            this.dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult ResetDemo()
        {
            DbInitializer.ResetDemoData(dbContext);
            TempData["SuccessMessage"] = "Hệ thống đã được làm mới thành công về trạng thái ban đầu để demo lại!";
            return RedirectToAction(nameof(Search));
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Search));
        }

        public async Task<IActionResult> Search(int? departmentId, string? term, DateTime? date)
        {
            var targetDate = date?.Date ?? DateTime.Today;
            if (targetDate < DateTime.Today)
            {
                targetDate = DateTime.Today;
            }

            var departments = await bookingService.GetSpecialtiesAsync();
            var doctors = await bookingService.SearchDoctorAsync(departmentId, term);

            var doctorCards = new List<DoctorCardItem>();

            foreach (var doc in doctors)
            {
                var slots = await timeSlotRepository.GetSlotsByDoctorAndDateAsync(doc.Id, targetDate);

                var morningSlots = slots.Where(s => s.SlotTime.TimeOfDay < new TimeSpan(12, 0, 0));
                var afternoonSlots = slots.Where(s => s.SlotTime.TimeOfDay >= new TimeSpan(12, 0, 0));

                doctorCards.Add(new DoctorCardItem
                {
                    DoctorId = doc.Id,
                    FullName = doc.User?.FullName ?? "Bác sĩ",
                    AcademicTitle = doc.AcademicTitle,
                    DepartmentName = doc.Department?.Name ?? "Chuyên khoa",
                    RoomNumber = doc.Schedules.FirstOrDefault()?.Room?.RoomNumber ?? "P201",
                    PricePerSlot = doc.PricePerSlot,
                    MorningCapacity = capacityEngine.CalculateShiftMetrics(morningSlots),
                    AfternoonCapacity = capacityEngine.CalculateShiftMetrics(afternoonSlots)
                });
            }

            var model = new DoctorSearchViewModel
            {
                SelectedDepartmentId = departmentId,
                SearchTerm = term,
                TargetDate = targetDate,
                Departments = departments,
                DoctorCards = doctorCards
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> SelectSlot(int doctorId, DateTime? date)
        {
            // Auto initialize patient demo session if empty
            if (!HttpContext.Session.GetInt32("UserId").HasValue)
            {
                HttpContext.Session.SetInt32("UserId", 1);
                HttpContext.Session.SetString("FullName", "Nguyễn Văn An");
                HttpContext.Session.SetString("PhoneNumber", "0988776655");
            }

            var targetDate = date?.Date ?? DateTime.Today;
            var doctor = await bookingService.GetDoctorWithDaySlotsAsync(doctorId, targetDate);

            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin bác sĩ.";
                return RedirectToAction(nameof(Search));
            }

            var slots = await timeSlotRepository.GetSlotsByDoctorAndDateAsync(doctorId, targetDate);

            int? heldSlotId = HttpContext.Session.GetInt32("HeldSlotId");
            string? heldExpiresAtStr = HttpContext.Session.GetString("HeldExpiresAt");
            int remainingSeconds = 0;

            if (heldSlotId.HasValue && !string.IsNullOrEmpty(heldExpiresAtStr))
            {
                if (DateTime.TryParse(heldExpiresAtStr, out var heldExpiresAt))
                {
                    var diff = (heldExpiresAt - DateTime.UtcNow).TotalSeconds;
                    if (diff > 0)
                    {
                        remainingSeconds = (int)diff;
                    }
                    else
                    {
                        await bookingService.ReleaseSlotAsync(heldSlotId.Value);
                        HttpContext.Session.Remove("HeldSlotId");
                        HttpContext.Session.Remove("HeldExpiresAt");
                        heldSlotId = null;
                    }
                }
            }

            var slotGridItems = slots.Select(s => new SlotGridItem
            {
                SlotId = s.Id,
                StartTime = s.SlotTime.TimeOfDay,
                EndTime = s.EndTime.TimeOfDay,
                Status = s.Status,
                RowVersion = Convert.ToBase64String(s.RowVersion),
                IsHeldByCurrentUser = (heldSlotId.HasValue && s.Id == heldSlotId.Value)
            }).ToList();

            var currentHeldSlot = slotGridItems.FirstOrDefault(s => s.IsHeldByCurrentUser);

            var model = new SelectSlotViewModel
            {
                DoctorId = doctor.Id,
                DoctorName = doctor.User?.FullName ?? "Bác sĩ",
                AcademicTitle = doctor.AcademicTitle,
                DepartmentName = doctor.Department?.Name ?? "Chuyên khoa",
                PricePerSlot = doctor.PricePerSlot,
                SelectedDate = targetDate,
                MorningSlots = slotGridItems.Where(s => s.StartTime < new TimeSpan(12, 0, 0)).ToList(),
                AfternoonSlots = slotGridItems.Where(s => s.StartTime >= new TimeSpan(12, 0, 0)).ToList(),
                DayCapacity = capacityEngine.CalculateShiftMetrics(slots),
                CurrentHeldSlot = currentHeldSlot,
                RemainingSeconds = remainingSeconds
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> HoldSlot([FromBody] HoldSlotPostModel request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ." });
            }

            int? currentPatientId = HttpContext.Session.GetInt32("UserId") ?? 1;
            int? previousHeldSlotId = HttpContext.Session.GetInt32("HeldSlotId");
            string? releasedRowVersion = null;

            if (previousHeldSlotId.HasValue && previousHeldSlotId.Value != request.SlotId)
            {
                releasedRowVersion = await bookingService.ReleaseSlotAsync(previousHeldSlotId.Value);
            }

            var result = await bookingService.HoldTimeSlotAsync(request.SlotId, request.RowVersion, currentPatientId);

            if (result.IsSuccess)
            {
                HttpContext.Session.SetInt32("HeldSlotId", result.SlotId);
                HttpContext.Session.SetString("HeldExpiresAt", DateTime.UtcNow.AddSeconds(result.ExpiresInSeconds).ToString("o"));

                // Realtime SignalR broadcast to notify other clients
                try
                {
                    await hubContext.Clients.All.SendAsync("SlotStatusChanged", new
                    {
                        slotId = result.SlotId,
                        status = "Hold",
                        newRowVersion = result.NewRowVersion
                    });

                    if (previousHeldSlotId.HasValue && !string.IsNullOrEmpty(releasedRowVersion))
                    {
                        await hubContext.Clients.All.SendAsync("SlotStatusChanged", new
                        {
                            slotId = previousHeldSlotId.Value,
                            status = "Open",
                            newRowVersion = releasedRowVersion
                        });
                    }
                }
                catch { }

                return Ok(new
                {
                    success = true,
                    slotId = result.SlotId,
                    newRowVersion = result.NewRowVersion,
                    releasedSlotId = previousHeldSlotId,
                    releasedNewRowVersion = releasedRowVersion,
                    expiresIn = result.ExpiresInSeconds,
                    message = result.Message
                });
            }

            // Conflict 409
            return StatusCode(409, new
            {
                success = false,
                slotId = result.SlotId,
                message = result.Message,
                alternatives = result.AlternativeSlots
            });
        }

        [HttpGet]
        public async Task<IActionResult> Confirm()
        {
            int? heldSlotId = HttpContext.Session.GetInt32("HeldSlotId");
            if (!heldSlotId.HasValue)
            {
                TempData["ErrorMessage"] = "Bạn chưa chọn khung giờ khám hoặc phiên giữ chỗ đã hết hạn.";
                return RedirectToAction(nameof(Search));
            }

            var slot = await timeSlotRepository.GetSlotByIdAsync(heldSlotId.Value);
            if (slot == null || (slot.Status != "Hold" && slot.Status != "Held"))
            {
                TempData["ErrorMessage"] = "Khung giờ không còn hợp lệ. Vui lòng chọn lại.";
                return RedirectToAction(nameof(Search));
            }

            var model = new BookingConfirmViewModel
            {
                SlotId = slot.Id,
                DoctorId = slot.DoctorId,
                DoctorName = slot.Doctor?.User?.FullName ?? "Bác sĩ",
                AcademicTitle = slot.Doctor?.AcademicTitle ?? "Bác sĩ",
                DepartmentName = slot.Doctor?.Department?.Name ?? "Chuyên khoa",
                RoomNumber = slot.Schedule?.Room?.RoomNumber ?? "P201",
                AppointmentDate = slot.SlotTime.Date,
                StartTime = slot.SlotTime.TimeOfDay,
                EndTime = slot.EndTime.TimeOfDay,
                ConsultationFee = slot.Doctor?.PricePerSlot ?? 150000m,
                PatientName = HttpContext.Session.GetString("FullName") ?? "Nguyễn Văn An",
                PatientPhone = HttpContext.Session.GetString("PhoneNumber") ?? "0988776655",
                PatientEmail = HttpContext.Session.GetString("Email") ?? "patient.an@gmail.com"
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmBooking(ConfirmBookingPostModel model)
        {
            int? userId = HttpContext.Session.GetInt32("UserId") ?? 1;

            int? heldSlotId = HttpContext.Session.GetInt32("HeldSlotId");
            if (!heldSlotId.HasValue || heldSlotId.Value != model.SlotId)
            {
                TempData["ErrorMessage"] = "Phiên giữ chỗ của bạn đã hết hạn hoặc không hợp lệ. Vui lòng chọn lại.";
                return RedirectToAction(nameof(Search));
            }

            string targetEmail = !string.IsNullOrWhiteSpace(model.Email)
                ? model.Email.Trim()
                : (HttpContext.Session.GetString("Email") ?? "patient.an@gmail.com");

            HttpContext.Session.SetString("Email", targetEmail);

            try
            {
                // Cập nhật email người dùng trong CSDL nếu người dùng sửa email nhận vé
                var user = await dbContext.Users.FindAsync(userId.Value);
                if (user != null && !string.IsNullOrWhiteSpace(targetEmail) && user.Email != targetEmail)
                {
                    user.Email = targetEmail;
                    user.UpdatedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync();
                }

                var appointment = await bookingService.ConfirmBookingAsync(userId.Value, model.SlotId, model.Reason, targetEmail);
                HttpContext.Session.Remove("HeldSlotId");
                HttpContext.Session.Remove("HeldExpiresAt");

                // Realtime SignalR broadcast that slot is now Booked
                try
                {
                    await hubContext.Clients.All.SendAsync("SlotStatusChanged", new
                    {
                        slotId = model.SlotId,
                        status = "Booked"
                    });
                }
                catch { }

                TempData["SuccessMessage"] = $"Đặt lịch khám thành công! Email thông báo kèm mã vé đã được gửi đến {targetEmail}";
                return RedirectToAction(nameof(Success), new { appointmentId = appointment.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi xác nhận đặt lịch: " + ex.Message;
                return RedirectToAction(nameof(Confirm));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Success(int appointmentId)
        {
            var appt = await bookingService.GetAppointmentDetailsAsync(appointmentId);
            if (appt == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin lịch hẹn.";
                return RedirectToAction(nameof(Search));
            }

            string? sessionEmail = HttpContext.Session.GetString("Email");
            var model = new BookingSuccessViewModel
            {
                AppointmentId = appt.Id,
                QrCodeToken = appt.QrCodeToken ?? $"APP-{appt.Id}",
                DoctorName = appt.Doctor?.User?.FullName ?? "Bác sĩ",
                AcademicTitle = appt.Doctor?.AcademicTitle ?? "Bác sĩ",
                DepartmentName = appt.Doctor?.Department?.Name ?? "Chuyên khoa",
                RoomNumber = appt.TimeSlot?.Schedule?.Room?.RoomNumber ?? "P201",
                AppointmentDate = appt.AppointmentDate,
                StartTime = appt.TimeSlot?.SlotTime.TimeOfDay ?? new TimeSpan(8, 0, 0),
                EndTime = appt.TimeSlot?.EndTime.TimeOfDay ?? new TimeSpan(8, 15, 0),
                ConsultationFee = appt.Doctor?.PricePerSlot ?? 150000m,
                PatientName = appt.Patient?.User?.FullName ?? "Nguyễn Văn An",
                PatientPhone = appt.Patient?.User?.PhoneNumber ?? "0988776655",
                PatientEmail = !string.IsNullOrWhiteSpace(sessionEmail)
                    ? sessionEmail
                    : (appt.Patient?.User?.Email ?? "patient.an@gmail.com"),
                Reason = appt.Reason,
                Status = appt.Status
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetSlotStatuses(int doctorId, DateTime? date)
        {
            var targetDate = date?.Date ?? DateTime.Today;
            var slots = await timeSlotRepository.GetSlotsByDoctorAndDateAsync(doctorId, targetDate);
            int? heldSlotId = HttpContext.Session.GetInt32("HeldSlotId");

            var data = slots.Select(s => new
            {
                slotId = s.Id,
                status = (heldSlotId.HasValue && s.Id == heldSlotId.Value) ? "MyHold" : s.Status,
                rowVersion = Convert.ToBase64String(s.RowVersion)
            });

            return Json(data);
        }
    }
}
