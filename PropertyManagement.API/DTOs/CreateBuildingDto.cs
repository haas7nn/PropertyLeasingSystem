using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.DTOs
{
    public class CreateBuildingDto
    {
        [Required(ErrorMessage = "Building name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Location cannot exceed 100 characters")]
        public string? Location { get; set; }

        [Required]
        [Range(1, 1000, ErrorMessage = "Total units must be between 1 and 1000")]
        public int TotalUnits { get; set; }
    }
}