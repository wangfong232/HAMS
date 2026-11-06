using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HAMS.Domain.Common;

namespace HAMS.Domain.Entities
{
    public class TimeSlot : BaseEntity
    {
        public int ScheduleId { get; set; }
        public int DoctorId { get; set; }
        public DateTime SlotTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = "Open"; // Open, Hold, Held, Booked, Blocked
        public DateTime? HoldExpiresAt { get; set; }
        public int? HeldByUserId { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // Navigation properties
        public virtual DoctorSchedule Schedule { get; set; } = null!;
        public virtual Doctor Doctor { get; set; } = null!;
        public virtual Appointment? Appointment { get; set; }

        [NotMapped]
        public virtual DoctorSchedule DoctorSchedule => Schedule;

        [NotMapped]
        public TimeSpan StartTime => SlotTime.TimeOfDay;

        [NotMapped]
        public TimeSpan EndTimeSpan => EndTime.TimeOfDay;

        public void HoldSlot(int userId, int minutes = 10)
        {
            Status = "Hold";
            HeldByUserId = userId;
            HoldExpiresAt = DateTime.UtcNow.AddMinutes(minutes);
            UpdatedAt = DateTime.UtcNow;
        }

        public void ReleaseHold()
        {
            Status = "Open";
            HeldByUserId = null;
            HoldExpiresAt = null;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
