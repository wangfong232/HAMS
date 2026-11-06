using System;
using System.Collections.Generic;
using HAMS.Domain.Common;

namespace HAMS.Domain.Entities
{
    public class MedicalService : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
        public int DepartmentId { get; set; }

        // Navigation properties
        public virtual Department Department { get; set; } = null!;
        public virtual ICollection<ServiceOrder> ServiceOrders { get; set; } = new List<ServiceOrder>();

        public void UpdateBasePrice(decimal newPrice)
        {
            BasePrice = newPrice;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
