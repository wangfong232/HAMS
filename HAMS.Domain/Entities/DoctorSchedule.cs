using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using HAMS.Domain.Common;

namespace HAMS.Domain.Entities
{
    public class DoctorSchedule : BaseEntity
    {
        public int DoctorId { get; set; }
        public int RoomId { get; set; }
        public DateTime ShiftDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int MaxPatients { get; set; } = 16; // 4 hours / 15 mins
        public string Status { get; set; } = "Active"; // Active, Cancelled

        [NotMapped]
        public DateTime WorkDate => ShiftDate;

        // Navigation properties
        public virtual Doctor Doctor { get; set; } = null!;
        public virtual Room Room { get; set; } = null!;
        public virtual ICollection<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();
    }
}
