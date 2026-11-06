using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HAMS.Application.DTOs;
using HAMS.Application.Interfaces;
using HAMS.Domain.Entities;

namespace HAMS.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly ITimeSlotRepository _timeSlotRepo;
        private readonly IAppointmentRepository _appointmentRepo;
        private readonly ICapacityEngine _capacityEngine;
        private readonly INotificationService _notificationService;
        private readonly IDoctorRepository? _doctorRepo;
        private readonly IDepartmentRepository? _departmentRepo;

        public BookingService(
            ITimeSlotRepository timeSlotRepo,
            IAppointmentRepository appointmentRepo,
            ICapacityEngine capacityEngine,
            INotificationService notificationService,
            IDoctorRepository? doctorRepo = null,
            IDepartmentRepository? departmentRepo = null)
        {
            _timeSlotRepo = timeSlotRepo;
            _appointmentRepo = appointmentRepo;
            _capacityEngine = capacityEngine;
            _notificationService = notificationService;
            _doctorRepo = doctorRepo;
            _departmentRepo = departmentRepo;
        }

        public async Task<List<Department>> GetSpecialtiesAsync()
        {
            if (_doctorRepo != null)
            {
                return await _doctorRepo.GetAllDepartmentAsync();
            }
            if (_departmentRepo != null)
            {
                return (await _departmentRepo.GetAllAsync()).ToList();
            }
            return new List<Department>();
        }

        public async Task<List<Doctor>> SearchDoctorAsync(int? departmentId, string? term)
        {
            if (_doctorRepo != null)
            {
                return await _doctorRepo.GetDoctorsAsync(departmentId, term);
            }
            return new List<Doctor>();
        }

        public async Task<Doctor?> GetDoctorWithDaySlotsAsync(int doctorId, DateTime targetDate)
        {
            if (_doctorRepo != null)
            {
                return await _doctorRepo.GetDoctorWithDetailsAsync(doctorId);
            }
            return null;
        }

        public async Task<HoldSlotResult> HoldTimeSlotAsync(int slotId, string rowVersion, int? patientId)
        {
            byte[] rowVersionBytes;
            try
            {
                rowVersionBytes = Convert.FromBase64String(rowVersion);
            }
            catch
            {
                return new HoldSlotResult
                {
                    IsSuccess = false,
                    Message = "Mã xác thực token của khung giờ không hợp lệ."
                };
            }

            var (success, newRowVersion) = await _timeSlotRepo.HoldSlotAsync(slotId, rowVersionBytes, patientId);

            if (success)
            {
                return new HoldSlotResult
                {
                    IsSuccess = true,
                    SlotId = slotId,
                    NewStatus = "Hold",
                    ExpiresInSeconds = 600,
                    NewRowVersion = newRowVersion ?? string.Empty,
                    Message = "Giữ chỗ thành công! Bạn có 10 phút để hoàn tất xác nhận thông tin khám."
                };
            }

            // Conflict 409: Find alternative slots
            var currentSlot = await _timeSlotRepo.GetSlotByIdAsync(slotId);
            var alternatives = new List<AlternativeSlotDto>();

            if (currentSlot != null)
            {
                var daySlots = await _timeSlotRepo.GetSlotsByDoctorAndDateAsync(currentSlot.DoctorId, currentSlot.SlotTime.Date);
                var suggested = _capacityEngine.GetAlternativeSlots(daySlots, slotId, 3);
                alternatives = suggested.Select(s => new AlternativeSlotDto
                {
                    Id = s.Id,
                    TimeRange = $"{s.SlotTime:HH:mm} - {s.EndTime:HH:mm}",
                    RowVersion = Convert.ToBase64String(s.RowVersion)
                }).ToList();
            }

            return new HoldSlotResult
            {
                IsSuccess = false,
                SlotId = slotId,
                Message = "Rất tiếc! Khung giờ này vừa có bệnh nhân khác giữ trước (Xung đột Concurrency OCC). Vui lòng chọn khung giờ khác.",
                AlternativeSlots = alternatives
            };
        }

        public async Task<string?> ReleaseSlotAsync(int slotId)
        {
            return await _timeSlotRepo.ReleaseSlotAsync(slotId);
        }

        public async Task<Appointment> ConfirmBookingAsync(int userId, int slotId, string? reason, string? email = null)
        {
            var slot = await _timeSlotRepo.GetSlotByIdAsync(slotId);
            if (slot == null)
            {
                throw new InvalidOperationException("Khung giờ không tồn tại.");
            }

            await _timeSlotRepo.UpdateSlotStatus(slotId, "Booked");

            var patient = await _appointmentRepo.GetPatientByUserIdAsync(userId);
            int patientId = patient?.Id ?? 1;

            var appointment = await _appointmentRepo.CreateAppointmentAsync(patientId, slotId, reason);

            // Send confirmation email
            try
            {
                var fullAppt = await _appointmentRepo.GetAppointmentDetailsAsync(appointment.Id);
                string targetEmail = !string.IsNullOrWhiteSpace(email) 
                    ? email 
                    : (fullAppt?.Patient?.User?.Email ?? "patient.an@gmail.com");

                if (_notificationService != null)
                {
                    await _notificationService.SendBookingConfirmationAsync(
                        targetEmail,
                        appointment.Id,
                        slot.SlotTime,
                        fullAppt?.QrCodeToken ?? $"APP-{appointment.Id}"
                    );
                }
            }
            catch
            {
                // Notification failure should not break appointment booking
            }

            return appointment;
        }

        public async Task<Appointment?> GetAppointmentDetailsAsync(int appointmentId)
        {
            return await _appointmentRepo.GetAppointmentDetailsAsync(appointmentId);
        }
    }
}
