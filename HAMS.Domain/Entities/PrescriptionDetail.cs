using System;
using HAMS.Domain.Common;

namespace HAMS.Domain.Entities
{
    public class PrescriptionDetail : BaseEntity
    {
        public int MedicalRecordId { get; set; }
        public int MedicineId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string DosageInstruction { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Paid, Dispensed
        
        // Audit log for Dispensation
        public int? DispensedByPharmacistId { get; set; }
        public DateTime? DispensedAt { get; set; }

        // Navigation properties
        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
        public virtual Medicine Medicine { get; set; } = null!;
        public virtual User? DispensedByPharmacist { get; set; }

        public decimal CalculateLineTotal()
        {
            return Quantity * UnitPrice;
        }

        public void MarkAsDispensed(int pharmacistId)
        {
            Status = "Dispensed";
            DispensedByPharmacistId = pharmacistId;
            DispensedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
