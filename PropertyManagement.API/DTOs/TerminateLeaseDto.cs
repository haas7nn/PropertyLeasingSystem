using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.DTOs
{
    public class TerminateLeaseDto
    {
        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
    }
}