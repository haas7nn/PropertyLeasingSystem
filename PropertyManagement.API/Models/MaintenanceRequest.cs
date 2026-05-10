using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.API.Models
{
    /// <summary>
    /// Represents a maintenance request submitted by a Tenant (Role A).
    ///
    /// Lifecycle enforced by the application layer:
    ///   Submitted → Assigned → InProgress → Resolved → Closed
    ///
    ///   • Submitted  : Tenant submits request via MVC form or API.
    ///                  A unique TicketNumber is generated (format: MNT-YYMMDD-NN).
    ///   • Assigned   : Property Manager assigns a MaintenanceStaff member.
    ///                  Both the tenant and the assigned staff receive a notification.
    ///   • InProgress : Maintenance Staff updates status when work begins.
    ///   • Resolved   : Staff marks the request resolved and adds ResolutionNotes.
    ///                  Tenant receives a notification to confirm or reopen.
    ///   • Closed     : Property Manager closes the ticket; ClosedDate is stamped.
    ///
    /// Real-time updates: Each status change and new submission is broadcast to the
    /// SignalR "StaffBoard" group so the live maintenance board updates instantly.
    ///
    /// Public Lookup: Unauthenticated tenants can look up a request by TicketNumber
    /// and registered phone number via the MVC public lookup page (HttpClient → API).
    /// </summary>
    public class MaintenanceRequest
    {
        [Key]
        public int RequestId { get; set; }

        /// <summary>
        /// Human-readable unique identifier for this request.
        /// Format: MNT-YYMMDD-NN (e.g., MNT-260514-03).
        /// Generated at submission time — date prefix + sequential daily counter.
        /// Has a unique database index to prevent duplicate ticket numbers.
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string TicketNumber { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key to the Tenant (Role A) who submitted this request.
        /// Tenants can only view requests where TenantId == their own user ID.
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        [ForeignKey("TenantId")]
        public Tenant Tenant { get; set; } = null!;

        /// <summary>
        /// The unit where the maintenance issue was reported.
        /// Linked to a specific unit (and transitively a building) for location context.
        /// </summary>
        public int UnitId { get; set; }

        [ForeignKey("UnitId")]
        public Unit Unit { get; set; } = null!;

        /// <summary>
        /// Category of the maintenance issue: Plumbing, Electrical, HVAC, General, etc.
        /// Used by the Property Manager when matching staff skills to the request.
        /// Also used in the Reporting Application for backlog analysis by category.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Urgency level: Low, Medium, High, or Urgent.
        /// High/Urgent requests are highlighted on the live maintenance board.
        /// The Reporting Application uses priority distribution for operational metrics.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Priority { get; set; } = string.Empty;

        /// <summary>
        /// Tenant's description of the problem. Stored as free text.
        /// Staff use this when planning the repair approach.
        /// </summary>
        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// UTC timestamp when the tenant submitted the request.
        /// Used to calculate average resolution time in reports.
        /// </summary>
        public DateTime SubmittedDate { get; set; }

        /// <summary>
        /// Current lifecycle status. Valid values:
        ///   "Submitted" | "Assigned" | "InProgress" | "Resolved" | "Closed"
        /// Status transitions are enforced in the API controller — invalid transitions
        /// return a 400 Bad Request with an explanatory message.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Submitted";

        /// <summary>
        /// Foreign key to the assigned MaintenanceStaff (Role B) member.
        /// Nullable — null means the request is still unassigned (Submitted status).
        /// Only Available staff can be assigned (enforced in AssignStaff endpoint).
        /// </summary>
        public string? AssignedStaffId { get; set; }

        [ForeignKey("AssignedStaffId")]
        public MaintenanceStaff? AssignedStaff { get; set; }

        /// <summary>
        /// Notes added by the assigned staff when resolving the request.
        /// Required before a request can be moved to Resolved status.
        /// Visible to the tenant via the public lookup page and MVC detail view.
        /// </summary>
        [MaxLength(2000)]
        public string? ResolutionNotes { get; set; }

        /// <summary>
        /// Timestamp set by the Property Manager when the ticket is formally Closed.
        /// Used to compute resolution time: ClosedDate - SubmittedDate.
        /// Null if the request is still open.
        /// </summary>
        public DateTime? ClosedDate { get; set; }
    }
}
