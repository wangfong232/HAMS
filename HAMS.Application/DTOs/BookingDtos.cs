using System;
using System.Collections.Generic;

namespace HAMS.Application.DTOs
{
    public class ShiftCapacityMetrics
    {
        public int TotalSlots { get; set; }
        public int BookedSlots { get; set; }
        public int AvailableSlots { get; set; }
        public double LoadPercentage { get; set; }
        public string CapacityStatus { get; set; } = "Green";
        public string BadgeClass { get; set; } = "bg-success";
        public string StatusText { get; set; } = "Bình thường / Vắng";
    }

    public class HoldSlotResult
    {
        public bool IsSuccess { get; set; }
        public int SlotId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public int ExpiresInSeconds { get; set; } = 600;
        public string NewRowVersion { get; set; } = string.Empty;
        public List<AlternativeSlotDto> AlternativeSlots { get; set; } = new();
    }

    public class AlternativeSlotDto
    {
        public int Id { get; set; }
        public string TimeRange { get; set; } = string.Empty;
        public string RowVersion { get; set; } = string.Empty;
    }
}
