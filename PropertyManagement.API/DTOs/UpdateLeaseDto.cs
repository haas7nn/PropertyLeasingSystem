using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.DTOs
{
    /// <summary>
    /// All fields are optional so callers can PATCH-style update only what changed.
    /// e.g. the PM can update ScreeningNotes without re-sending MonthlyRent.
    /// The API controller applies only non-null fields.
    /// </summary>
    public class UpdateLeaseDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Range still applies when the caller provides a value — null means skip.
        [Range(0.01, 1_000_000)]
        public decimal? MonthlyRent { get; set; }

        [Range(0.01, 1_000_000)]
        public decimal? SecurityDeposit { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        [StringLength(1000)]
        public string? ScreeningNotes { get; set; }
    }
}
