using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.DTOs
{
    public class AssignStaffDto
    {
        [Required]
        public string StaffId { get; set; } = string.Empty;
    }
}