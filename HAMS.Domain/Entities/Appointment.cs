using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HAMS.Domain.Common;

namespace HAMS.Domain.Entities
{
    public class Appointment : BaseEntity
    {
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int TimeSlotId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Arrived, InConsultation, WaitingForResults, Completed, Cancelled
        public string? Reason { get; set; }
        public int RescheduledCount { get; set; } = 0;
        public decimal DepositFee { get; set; }
        public bool IsRefunded { get; set; } = false;
        public bool IsReminded { get; set; } = false;

        // Audit Log & Reception Queue
        public int? CheckedInByReceptionistId { get; set; }
        public DateTime? CheckedInAt { get; set; }
        public int? QueueNumber { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string QrCodeToken => $"APP-{Id}";

        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
        public virtual Doctor Doctor { get; set; } = null!;
        public virtual TimeSlot TimeSlot { get; set; } = null!;
        public virtual User? CheckedInByReceptionist { get; set; }
        public virtual MedicalRecord? MedicalRecord { get; set; }
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<ServiceOrder> ServiceOrders { get; set; } = new List<ServiceOrder>();
        public virtual VitalSigns? VitalSigns { get; set; }
        public virtual Feedback? Feedback { get; set; }

        public bool CanBeRescheduled()
        {
            return Status == "Confirmed" && RescheduledCount < 1;
        }

        public bool CanBeRefunded()
        {
            return (AppointmentDate - DateTime.UtcNow).TotalHours >= 24;
        }

        public void MarkAsConfirmed()
        {
            Status = "Confirmed";
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkAsReminded()
        {
            IsReminded = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkAsArrived(int receptionistId, int queueNumber)
        {
            Status = "Arrived";
            CheckedInByReceptionistId = receptionistId;
            CheckedInAt = DateTime.UtcNow;
            QueueNumber = queueNumber;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkAsInConsultation()
        {
            Status = "InConsultation";
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkAsCompleted()
        {
            Status = "Completed";
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkAsCancelled()
        {
            Status = "Cancelled";
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
