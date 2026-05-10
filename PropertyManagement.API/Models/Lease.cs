using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.API.Models
{
    /// <summary>
    /// Represents a lease agreement between a Tenant (Role A) and the property company.
    ///
    /// Lifecycle enforced by the Property Manager (Role C):
    ///   Application → Screening → Active (Approved) | Rejected → Terminated
    ///
    ///   • Application : Tenant applies for a specific unit (unit must be Available).
    ///   • Screening   : Manager is reviewing CPR, employment, and references.
    ///   • Active      : Approved — unit status set to Occupied, CurrentLeaseId linked.
    ///   • Rejected    : Application denied; RejectionReason is recorded and sent
    ///                   as a notification to the tenant.
    ///   • Terminated  : Active lease ended early; unit returns to Available.
    ///
    /// Business rule: A unit cannot have more than one Active lease simultaneously.
    /// This is enforced by checking unit.AvailabilityStatus == "Available" before
    /// any Approval action. The application layer also validates this on Create.
    /// </summary>
    public class Lease
    {
        [Key]
        public int LeaseId { get; set; }

        /// <summary>
        /// The unit being leased. Must have AvailabilityStatus == "Available"
        /// when the lease application is created or approved.
        /// </summary>
        public int UnitId { get; set; }

        [ForeignKey("UnitId")]
        public Unit Unit { get; set; } = null!;

        /// <summary>
        /// The applying or active Tenant (Role A).
        /// Foreign key references the ASP.NET Core Identity Users table.
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        [ForeignKey("TenantId")]
        public Tenant Tenant { get; set; } = null!;

        /// <summary>
        /// Date/time the tenant submitted this application.
        /// Automatically set to DateTime.Now on creation — tenants cannot set this.
        /// </summary>
        public DateTime ApplicationDate { get; set; }

        /// <summary>
        /// Intended start of the rental period. Nullable because it may not be
        /// confirmed until after the screening/approval stage.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Intended end of the rental period. Nullable for the same reason as StartDate.
        /// Expiry checking (for renewal reminders) would compare EndDate to DateTime.Now.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Agreed monthly rent amount in Bahraini Dinar (BD).
        /// May differ from Unit.MonthlyRent if a negotiated rate was applied.
        /// Used as the default AmountDue when the Property Manager creates a payment record.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyRent { get; set; }

        /// <summary>
        /// Security deposit collected upfront, typically 1–2 months' rent.
        /// Recorded for reference; refund logic is handled outside the system.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal SecurityDeposit { get; set; }

        /// <summary>
        /// Current lifecycle status. Valid values:
        ///   "Application" | "Screening" | "Active" | "Rejected" | "Terminated"
        /// Transitions are enforced in LeasesController — attempts to skip
        /// or reverse steps return HTTP 400 Bad Request.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Application";

        /// <summary>
        /// Reason provided by the Property Manager when rejecting or terminating.
        /// Sent to the tenant as a notification so they know why their application failed.
        /// Also reused to store termination reason (field name kept general for reuse).
        /// </summary>
        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        /// <summary>
        /// Notes added during the Screening stage, e.g., CPR check result,
        /// employer verification outcome, or reference check summary.
        /// Visible to the Property Manager only — not exposed to the tenant.
        /// </summary>
        [MaxLength(1000)]
        public string? ScreeningNotes { get; set; }

        // ── Navigation Properties ────────────────────────────────────────────────

        /// <summary>
        /// Payment records (installments) associated with this lease.
        /// Each month the Property Manager creates a Payment record with Status = Pending,
        /// then records receipt when the tenant pays. Cascade delete is configured so
        /// removing a lease also removes its payment history (protected by business rule
        /// in DeleteConfirmed — leases with payments cannot be deleted).
        /// </summary>
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
