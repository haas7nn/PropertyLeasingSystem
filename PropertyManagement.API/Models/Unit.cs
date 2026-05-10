using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.API.Models
{
    /// <summary>
    /// Represents a rentable unit (apartment, office, shop) within a Building.
    /// Units are managed by the Property Manager (Role C) and can be browsed
    /// by Tenants (Role A) when submitting lease applications.
    ///
    /// Availability lifecycle:
    ///   Available → Occupied (when lease is Approved)
    ///   Occupied  → Available (when lease is Terminated or Expired)
    ///   Available → UnderMaintenance (optional status for major works)
    ///
    /// Concurrency protection: the application checks AvailabilityStatus == "Available"
    /// before approving a lease to prevent double-booking.
    /// </summary>
    public class Unit
    {
        [Key]
        public int UnitId { get; set; }

        /// <summary>
        /// The building this unit belongs to. Cannot be null — every unit must
        /// belong to exactly one building (Building-Unit is a 1-to-many relationship).
        /// </summary>
        public int BuildingId { get; set; }

        [ForeignKey("BuildingId")]
        public Building Building { get; set; } = null!;

        /// <summary>
        /// Unit identifier within the building, e.g., "101", "2B", "G-Shop-3".
        /// Displayed on leases, maintenance requests, and the units index page.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string UnitNumber { get; set; } = string.Empty;

        /// <summary>
        /// Unit category: "Apartment", "Studio", "Office", "Shop", etc.
        /// Used to filter available units on the lease application create page.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        /// <summary>Bedroom count. Null for non-residential units (offices, shops).</summary>
        public int? Bedrooms { get; set; }

        /// <summary>Bathroom count. Null for non-residential units.</summary>
        public int? Bathrooms { get; set; }

        /// <summary>
        /// Floor area in square feet. Used for display on listings and the lease form.
        /// Stored as decimal to allow fractional values (e.g., 850.5 sq ft).
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal SizeInSqFt { get; set; }

        /// <summary>
        /// Standard monthly asking rent in Bahraini Dinar (BD).
        /// Auto-filled into the lease form as the suggested MonthlyRent —
        /// the Property Manager may adjust it for individual tenants.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyRent { get; set; }

        /// <summary>
        /// Free-text list of amenities, e.g., "Parking, Pool, Gym".
        /// Displayed on unit detail pages to help tenants choose a unit.
        /// </summary>
        [MaxLength(1000)]
        public string? Amenities { get; set; }

        /// <summary>
        /// Current rental availability: "Available", "Occupied", or "UnderMaintenance".
        /// Controls whether a lease application can be approved for this unit.
        /// Updated automatically when leases are approved or terminated.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string AvailabilityStatus { get; set; } = "Available";

        /// <summary>
        /// Foreign key to the currently active Lease.
        /// Null when the unit is Available or UnderMaintenance.
        /// Set to the approved lease's ID on approval; cleared on termination.
        /// Configured with OnDelete = SetNull so the pointer is cleared if
        /// the lease record is deleted.
        /// </summary>
        public int? CurrentLeaseId { get; set; }

        [ForeignKey("CurrentLeaseId")]
        public Lease? CurrentLease { get; set; }

        // ── Navigation Properties ────────────────────────────────────────────────

        /// <summary>
        /// Complete history of all leases ever created for this unit (including
        /// rejected, terminated, and expired ones). Used for audit and reporting.
        /// </summary>
        public ICollection<Lease> LeaseHistory { get; set; } = new List<Lease>();

        /// <summary>
        /// All maintenance requests logged for this unit.
        /// Used by the Property Manager to view the maintenance history of a unit
        /// before re-leasing it to a new tenant.
        /// </summary>
        public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
    }
}
