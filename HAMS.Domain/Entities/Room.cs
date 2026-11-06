using System;
using System.Collections.Generic;
using HAMS.Domain.Common;

namespace HAMS.Domain.Entities
{
    public class Room : BaseEntity
    {
        public string RoomNumber { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string RoomType { get; set; } = "Consultation"; // Consultation, Laboratory, Radiology
        public bool IsOperational { get; set; } = true;

        // Navigation properties
        public virtual Department Department { get; set; } = null!;
        public virtual ICollection<DoctorSchedule> Schedules { get; set; } = new List<DoctorSchedule>();

        public void ToggleOperationalStatus(bool isOperational)
        {
            IsOperational = isOperational;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateRoomDetails(string roomNumber, string roomType)
        {
            RoomNumber = roomNumber;
            RoomType = roomType;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
