using System;
using HAMS.Domain.Common;

namespace HAMS.Domain.Entities
{
    public class Notification : BaseEntity
    {
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "Email"; // Email, SMS, Push
        public bool IsRead { get; set; } = false;

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }
}
