using System;
using HAMS.Domain.Common;

namespace HAMS.Domain.Entities
{
    public class Feedback : BaseEntity
    {
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public int Rating { get; set; } // 1 - 5 stars
        public string? Comments { get; set; }

        // Navigation properties
        public virtual Appointment Appointment { get; set; } = null!;
        public virtual Patient Patient { get; set; } = null!;
    }
}
