using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HAMS.Application.DTOs;
using HAMS.Domain.Entities;

namespace HAMS.Application.Interfaces
{
    public interface IBookingService
    {
        Task<List<Department>> GetSpecialtiesAsync();
        Task<List<Doctor>> SearchDoctorAsync(int? departmentId, string? term);
        Task<Doctor?> GetDoctorWithDaySlotsAsync(int doctorId, DateTime targetDate);
        Task<HoldSlotResult> HoldTimeSlotAsync(int slotId, string rowVersion, int? patientId);
        Task<string?> ReleaseSlotAsync(int slotId);
        Task<Appointment> ConfirmBookingAsync(int userId, int slotId, string? reason, string? email = null);
        Task<Appointment?> GetAppointmentDetailsAsync(int appointmentId);
    }

    public interface ICapacityEngine
    {
        ShiftCapacityMetrics CalculateShiftMetrics(IEnumerable<TimeSlot> slots);
        List<TimeSlot> GetAlternativeSlots(List<TimeSlot> allSlots, int conflictSlotId, int limit = 3);
    }

    public interface IEmrService
    {
        Task<bool> SaveEmrAndPrescriptionAsync(CreateEmrDto request);
    }
}
