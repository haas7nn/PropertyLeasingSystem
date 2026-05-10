using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.Models
{
    /// <summary>
    /// Represents a physical building managed by the property company.
    /// Buildings are the top-level organisational unit: each building contains
    /// one or more Units that can be leased to Tenants.
    ///
    /// Managed exclusively by the Property Manager (Role C) via the MVC Buildings module.
    /// Tenants and Maintenance Staff can view building information (name, address)
    /// as part of lease and maintenance request context, but cannot create or edit buildings.
    /// </summary>
    public class Building
    {
        [Key]
        public int BuildingId { get; set; }

        /// <summary>
        /// Display name of the building, e.g., "Sunset Tower" or "Marina Heights".
        /// Shown on lease applications, maintenance requests, and reports.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Full civic address. Used for display and informational purposes.
        /// Not validated against a map API — entered manually by the Property Manager.
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Optional district or area label (e.g., "Seef", "Juffair", "Adliya").
        /// Used by the Reporting Application to group occupancy data by area.
        /// </summary>
        [MaxLength(100)]
        public string? Location { get; set; }

        /// <summary>
        /// Total number of units in this building (informational counter).
        /// Maintained manually by the Property Manager — does not auto-sync
        /// with the actual Units collection count. Kept separate to allow
        /// buildings with "future" units to be recorded.
        /// </summary>
        public int TotalUnits { get; set; }

        // ── Navigation Properties ────────────────────────────────────────────────

        /// <summary>
        /// All units belonging to this building.
        /// Delete is restricted — a building cannot be removed if it has any units.
        /// This is enforced in the MVC DeleteConfirmed action.
        /// </summary>
        public ICollection<Unit> Units { get; set; } = new List<Unit>();
    }
}
