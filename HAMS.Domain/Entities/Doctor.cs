using System.Collections.Generic;
using HAMS.Domain.Common;

namespace HAMS.Domain.Entities
{
    public class Doctor : BaseEntity
    {
        public int UserId { get; set; }
        public int DepartmentId { get; set; }
        public string AcademicTitle { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public decimal PricePerSlot { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Department Department { get; set; } = null!;
        public virtual ICollection<DoctorSchedule> Schedules { get; set; } = new List<DoctorSchedule>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    }
}
