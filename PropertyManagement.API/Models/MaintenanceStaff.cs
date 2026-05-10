using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.Models
{
    /// <summary>
    /// Role B: Maintenance Staff
    /// Represents a maintenance technician employed by the property company.
    /// Maintenance Staff can:
    ///   - View maintenance requests assigned to them
    ///   - Update status of their assigned requests (InProgress → Resolved)
    ///   - View the real-time live maintenance board (SignalR-powered)
    ///   - Receive in-system notifications when a new request is assigned to them
    ///
    /// The Property Manager (Role C) is responsible for:
    ///   - Creating/editing staff profiles
    ///   - Assigning maintenance requests to staff based on skills and availability
    ///
    /// Inherits from IdentityUser — password hashing and authentication are handled
    /// by ASP.NET Core Identity. The MaintenanceStaff role is seeded at startup.
    /// </summary>
    public class MaintenanceStaff : IdentityUser
    {
        /// <summary>
        /// A JSON-serialized list of skill categories this staff member can handle,
        /// e.g. ["Plumbing", "Electrical", "HVAC"].
        /// Stored as a JSON string for simplicity — the Property Manager can view
        /// skills when deciding which staff member to assign to a request.
        /// Default is an empty JSON array so deserialisation never throws.
        /// </summary>
        [MaxLength(500)]
        public string Skills { get; set; } = "[]";

        /// <summary>
        /// Current availability of this staff member: "Available" or "Unavailable".
        /// The Property Manager can only assign requests to staff whose status is "Available".
        /// Defaults to "Available" at registration.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string AvailabilityStatus { get; set; } = "Available";

        // ── Navigation Properties ────────────────────────────────────────────────

        /// <summary>
        /// All maintenance requests currently or previously assigned to this staff member.
        /// Used for workload reporting and to enforce business rules
        /// (e.g., cannot be deleted if active requests are assigned).
        /// </summary>
        public ICollection<MaintenanceRequest> AssignedRequests { get; set; } = new List<MaintenanceRequest>();
    }
}
