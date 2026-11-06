using System;
using System.Collections.Generic;
using HAMS.Domain.Common;

namespace HAMS.Domain.Entities
{
    public class Department : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal BaseConsultationFee { get; set; }
        public string? Description { get; set; }

        // Navigation properties
        public virtual ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
        public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
        public virtual ICollection<MedicalService> Services { get; set; } = new List<MedicalService>();

        public void UpdateConsultationFee(decimal newFee)
        {
            BaseConsultationFee = newFee;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateDetails(string name, string description)
        {
            Name = name;
            Description = description;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
