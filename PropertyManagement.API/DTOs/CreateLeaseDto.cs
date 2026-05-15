using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.DTOs
{
    public class CreateLeaseDto
    {
        [Required]
        public int UnitId { get; set; }

        [Required]
        public string TenantId { get; set; } = string.Empty;

        // Required — the controller uses .Value without null check. Making these
        // nullable-but-required here catches missing values before execution.
        [Required(ErrorMessage = "Start date is required.")]
        public DateTime? StartDate { get; set; }

        [Required(ErrorMessage = "End date is required.")]
        public DateTime? EndDate { get; set; }

        [Required]
        [Range(0.01, 1_000_000)]
        public decimal MonthlyRent { get; set; }

        [Required]
        [Range(0.01, 1_000_000)]
        public decimal SecurityDeposit { get; set; }

        [StringLength(1000)]
        public string? ScreeningNotes { get; set; }
    }
}
