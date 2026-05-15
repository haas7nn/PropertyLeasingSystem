using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.DTOs
{
    public class SubmitMaintenanceDto
    {
        [Required]
        public int UnitId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Priority { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;
    }
}
