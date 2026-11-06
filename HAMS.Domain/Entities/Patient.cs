using System;
using System.Collections.Generic;
using HAMS.Domain.Common;

namespace HAMS.Domain.Entities
{
    public class Patient : BaseEntity
    {
        public int UserId { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
        public string? IdentificationNumber { get; set; }
        public string? MedicalHistory { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
        public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    }
}
