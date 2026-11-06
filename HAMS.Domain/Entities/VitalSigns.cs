using System;
using HAMS.Domain.Common;

namespace HAMS.Domain.Entities
{
    public class VitalSigns : BaseEntity
    {
        public int AppointmentId { get; set; }
        public int Pulse { get; set; } // bpm
        public string BloodPressure { get; set; } = string.Empty; // e.g. 120/80
        public decimal Temperature { get; set; } // Celsius
        public int SpO2 { get; set; } // %
        public decimal? Weight { get; set; } // kg
        public decimal? Height { get; set; } // cm
        public string? TriageNotes { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Appointment Appointment { get; set; } = null!;
    }
}
