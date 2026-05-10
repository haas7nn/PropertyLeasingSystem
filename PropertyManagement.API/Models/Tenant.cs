using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.Models
{
    /// <summary>
    /// Role A: Tenant
    /// Represents a person who rents a residential or commercial unit from the property company.
    /// Tenants can:
    ///   - Submit and track maintenance requests
    ///   - View their own lease details and payment history
    ///   - Receive in-system notifications about lease and maintenance events
    ///   - Use the public lookup page (no login required) to track maintenance by ticket number
    ///
    /// Inherits from IdentityUser so ASP.NET Core Identity handles password hashing,
    /// login, and role assignment automatically. The Tenant role is seeded at startup.
    /// </summary>
    public class Tenant : IdentityUser
    {
        /// <summary>
        /// Bahrain CPR (Central Population Register) number.
        /// Used as a secondary identifier on the public maintenance lookup page
        /// so unauthenticated users can verify their identity without a full login.
        /// </summary>
        [MaxLength(20)]
        public string? CPR { get; set; }

        /// <summary>
        /// Emergency contact name/phone stored as free text.
        /// Collected during tenant registration for safety purposes.
        /// </summary>
        [MaxLength(200)]
        public string? EmergencyContact { get; set; }

        /// <summary>
        /// Tenant's current occupation — used during lease screening
        /// to help the Property Manager assess financial stability.
        /// </summary>
        [MaxLength(200)]
        public string? Occupation { get; set; }

        /// <summary>
        /// The date the tenant account was created in the system.
        /// Set automatically at registration; used for reporting and audit purposes.
        /// </summary>
        public DateTime RegistrationDate { get; set; }

        // ── Navigation Properties ────────────────────────────────────────────────

        /// <summary>
        /// All lease applications and active/historical leases belonging to this tenant.
        /// A tenant may have multiple leases over time (e.g., moved units), but only
        /// one can be Active for a given unit at any moment — enforced at the application layer.
        /// </summary>
        public ICollection<Lease> Leases { get; set; } = new List<Lease>();

        /// <summary>
        /// All maintenance requests submitted by this tenant across all their units.
        /// Tenants can only view their own requests; managers and staff see all.
        /// </summary>
        public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
    }
}
