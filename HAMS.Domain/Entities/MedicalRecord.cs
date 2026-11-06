using System;
using System.Collections.Generic;
using HAMS.Domain.Common;

namespace HAMS.Domain.Entities
{
    public class MedicalRecord : BaseEntity
    {
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public string Icd10Code { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public string? InternalDoctorNotes { get; set; }

        // Navigation properties
        public virtual Appointment Appointment { get; set; } = null!;
        public virtual Patient Patient { get; set; } = null!;
        public virtual Doctor Doctor { get; set; } = null!;
        public virtual ICollection<PrescriptionDetail> PrescriptionDetails { get; set; } = new List<PrescriptionDetail>();
    }
}
