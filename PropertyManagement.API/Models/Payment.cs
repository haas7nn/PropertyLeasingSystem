using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.API.Models
{
    /// <summary>
    /// Represents a single rent payment installment linked to an active Lease.
    ///
    /// Payment workflow (managed by Property Manager, Role C):
    ///   1. Manager creates a Payment record with Status = "Pending" and a DueDate.
    ///   2. When the Tenant pays, the Manager records AmountPaid, PaymentMethod,
    ///      and a ReceiptNumber via the Record Payment action.
    ///   3. Status is automatically set to:
    ///        "Paid"    — AmountPaid >= AmountDue
    ///        "Partial" — AmountPaid < AmountDue
    ///   4. Payments not recorded by DueDate are automatically flagged "Overdue"
    ///      when the Payments Index page is loaded (real-time flag in MVC controller).
    ///
    /// Tenants can view their own payment history but cannot create or modify records.
    /// Overdue count is surfaced on the Home dashboard and in the Reporting Application.
    /// </summary>
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        /// <summary>
        /// The lease this payment belongs to. Must be an Active lease —
        /// enforced in the Create action to prevent payments against
        /// rejected or terminated leases.
        /// Cascade delete: removing a lease removes all its payment history
        /// (guarded by a business rule that prevents deletion of leases with payments).
        /// </summary>
        public int LeaseId { get; set; }

        [ForeignKey("LeaseId")]
        public Lease Lease { get; set; } = null!;

        /// <summary>
        /// Date by which payment is expected. Used to auto-flag overdue payments.
        /// The dashboard and Reports app both surface the overdue count to managers.
        /// </summary>
        public DateTime DueDate { get; set; }

        /// <summary>
        /// The rent amount expected for this installment, in Bahraini Dinar (BD).
        /// Defaulted from Lease.MonthlyRent when the record is created,
        /// but can be adjusted for pro-rated or special billing periods.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountDue { get; set; }

        /// <summary>
        /// Amount actually received, in BD. Set to 0 on creation.
        /// Updated when the Property Manager records a receipt.
        /// Compared with AmountDue to determine Paid vs Partial status.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }

        /// <summary>
        /// Date/time the payment was physically received and recorded.
        /// Null when Status is Pending or Overdue.
        /// </summary>
        public DateTime? PaymentDate { get; set; }

        /// <summary>
        /// Current status: "Pending" | "Paid" | "Partial" | "Overdue".
        /// Auto-progresses from Pending to Overdue when DueDate passes
        /// (detected and persisted in PaymentsController.Index).
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// How the tenant paid: "Bank Transfer", "Cash", "Check", etc.
        /// Free text — recorded by the Property Manager at time of receipt.
        /// </summary>
        [MaxLength(100)]
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// Bank transaction or cash receipt reference number.
        /// Entered by the Property Manager for audit trail purposes.
        /// </summary>
        [MaxLength(100)]
        public string? ReceiptNumber { get; set; }
    }
}
