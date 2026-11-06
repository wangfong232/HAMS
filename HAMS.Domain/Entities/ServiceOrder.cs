using System;
using HAMS.Domain.Common;

namespace HAMS.Domain.Entities
{
    public class ServiceOrder : BaseEntity
    {
        public int AppointmentId { get; set; }
        public int ServiceId { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public string Status { get; set; } = "Ordered"; // Ordered, InProgress, Completed, Cancelled
        public string? ResultText { get; set; }
        public string? DiagnosticImageUrl { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        // Audit log for Laboratory / Radiology Technician
        public int? TechnicianId { get; set; }

        // Navigation properties
        public virtual Appointment Appointment { get; set; } = null!;
        public virtual MedicalService Service { get; set; } = null!;
        public virtual User? Technician { get; set; }

        public void MarkAsInProgress()
        {
            Status = "InProgress";
            UpdatedAt = DateTime.UtcNow;
        }

        public void SubmitResult(string resultText, string imageUrl, int? technicianId = null)
        {
            ResultText = resultText;
            DiagnosticImageUrl = imageUrl;
            Status = "Completed";
            if (technicianId.HasValue) TechnicianId = technicianId.Value;
            CompletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
