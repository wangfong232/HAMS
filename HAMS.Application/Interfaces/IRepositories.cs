using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HAMS.Domain.Entities;

namespace HAMS.Application.Interfaces
{
    public interface IAppointmentRepository
    {
        Task<Appointment?> FindAppointmentById(int appointmentId);
        Task<bool> UpdateAppointmentStatus(int appointmentId, string status, bool isRefunded = false);
        Task<Patient?> GetPatientByUserIdAsync(int userId);
        Task<Appointment> CreateAppointmentAsync(int patientId, int slotId, string? reason);
        Task<Appointment?> GetAppointmentDetailsAsync(int appointmentId);
        Task<List<Appointment>> GetAppointmentsInNext24h();
        Task UpdateAsync(Appointment appointment);
    }

    public interface ITimeSlotRepository
    {
        Task<List<TimeSlot>> GetSlotsByDoctorAndDateAsync(int doctorId, DateTime targetDate);
        Task<(bool success, string? newRowVersion)> HoldSlotAsync(int slotId, byte[] expectedRowVersion, int? userId = null, int holdMinutes = 10);
        Task<string?> ReleaseSlotAsync(int slotId);
        Task<TimeSlot?> GetSlotByIdAsync(int slotId);
        Task<bool> UpdateSlotStatus(int slotId, string status);
    }

    public interface IDoctorRepository
    {
        Task<Doctor?> GetByIdAsync(int id);
        Task<List<Doctor>> GetDoctorsAsync(int? departmentId, string? term);
        Task<Doctor?> GetDoctorWithDetailsAsync(int doctorId);
        Task<List<Department>> GetAllDepartmentAsync();
    }

    public interface IDepartmentRepository
    {
        Task<IEnumerable<Department>> GetAllAsync();
    }

    public interface IEmrRepository
    {
        Task AddMedicalRecordAsync(MedicalRecord medicalRecord);
        Task AddPrescriptionDetailAsync(PrescriptionDetail detail);
    }

    public interface IMedicineRepository
    {
        Task<Medicine?> GetByIdAsync(int medicineId);
        Task<IEnumerable<Medicine>> GetAvailableMedicinesAsync(string keyword);
    }

    public interface INotificationService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendBookingConfirmationAsync(string toEmail, int appointmentId, DateTime appointmentTime, string qrCodeUrl);
    }

    public interface INotificationRepository
    {
        Task AddAsync(Notification notification);
    }
}
