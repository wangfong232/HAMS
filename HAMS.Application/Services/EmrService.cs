using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HAMS.Application.DTOs;
using HAMS.Application.Interfaces;
using HAMS.Domain.Entities;

namespace HAMS.Application.Services
{
    public class EmrService : IEmrService
    {
        private readonly IEmrRepository _emrRepo;
        private readonly IAppointmentRepository _appointmentRepo;

        public EmrService(
            IEmrRepository emrRepo,
            IAppointmentRepository appointmentRepo)
        {
            _emrRepo = emrRepo;
            _appointmentRepo = appointmentRepo;
        }

        public async Task<bool> SaveEmrAndPrescriptionAsync(CreateEmrDto request)
        {
            var app = await _appointmentRepo.FindAppointmentById(request.AppointmentId);
            int patientId = request.PatientId > 0 ? request.PatientId : (app?.PatientId ?? 1);
            int doctorId = request.DoctorId > 0 ? request.DoctorId : (app?.DoctorId ?? 1);

            var record = new MedicalRecord
            {
                AppointmentId = request.AppointmentId,
                PatientId = patientId,
                DoctorId = doctorId,
                Diagnosis = request.Diagnosis,
                Icd10Code = request.Icd10Code,
                Note = request.Note,
                FollowUpDate = request.FollowUpDate
            };

            if (request.Prescriptions != null && request.Prescriptions.Any())
            {
                foreach (var item in request.Prescriptions)
                {
                    var detail = new PrescriptionDetail
                    {
                        MedicineId = item.MedicineId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        DosageInstruction = item.DosageInstruction,
                        Status = "Pending"
                    };
                    record.PrescriptionDetails.Add(detail);
                }
            }

            await _emrRepo.AddMedicalRecordAsync(record);
            return true;
        }
    }
}
