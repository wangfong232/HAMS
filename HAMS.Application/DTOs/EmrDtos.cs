using System;
using System.Collections.Generic;

namespace HAMS.Application.DTOs
{
    public class CreateEmrDto
    {
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public string Icd10Code { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public string? InternalDoctorNotes { get; set; }
        public List<PrescriptionLineDto> Prescriptions { get; set; } = new List<PrescriptionLineDto>();
    }

    public class PrescriptionLineDto
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string DosageInstruction { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
    }
}
