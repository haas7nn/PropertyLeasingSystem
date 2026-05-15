using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.DTOs
{
    public class UpdateMaintenanceStatusDto
    {
        [Required]
        public string Status { get; set; } = string.Empty;

        public string? ResolutionNotes { get; set; }
    }
}