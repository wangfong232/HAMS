using System;
using HAMS.Domain.Common;

namespace HAMS.Domain.Entities
{
    public class Payment : BaseEntity
    {
        public int AppointmentId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "VNPAY"; // VNPAY, MoMo, Cash, POS
        public string PaymentStatus { get; set; } = "Pending"; // Pending, Success, Failed, Refunded
        public string? TransactionId { get; set; }
        public string PaymentType { get; set; } = "Deposit"; // Deposit, FinalSettlement, Refund
        
        // Audit log for Cashier/Processor
        public int? CashierId { get; set; }

        // Navigation properties
        public virtual Appointment Appointment { get; set; } = null!;
        public virtual User? Cashier { get; set; }

        public void MarkAsSuccess(string transactionRef, int? cashierId = null)
        {
            PaymentStatus = "Success";
            TransactionId = transactionRef;
            if (cashierId.HasValue) CashierId = cashierId.Value;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkAsRefunded()
        {
            PaymentStatus = "Refunded";
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
