using System;
using System.Collections.Generic;
using HAMS.Domain.Common;

namespace HAMS.Domain.Entities
{
    public class Medicine : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = "Tablet"; // Tablet, Bottle, Box, Tube
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public bool IsAvailable { get; set; } = true;

        // Navigation properties
        public virtual ICollection<PrescriptionDetail> PrescriptionDetails { get; set; } = new List<PrescriptionDetail>();

        public bool CheckAvailability()
        {
            return IsAvailable;
        }

        public void SetAvailabilityStatus(bool isAvailable)
        {
            IsAvailable = isAvailable;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
