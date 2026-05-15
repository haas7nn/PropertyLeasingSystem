using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.DTOs
{
    public class CreateUnitDto
    {
        [Required]
        public int BuildingId { get; set; }

        [Required]
        [StringLength(50)]
        public string UnitNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty;

        [Range(0, 20)]
        public int? Bedrooms { get; set; }

        [Range(0, 20)]
        public int? Bathrooms { get; set; }

        [Required]
        [Range(0.01, 100000)]
        public decimal SizeInSqFt { get; set; }

        [Required]
        [Range(0.01, 1000000)]
        public decimal MonthlyRent { get; set; }

        [StringLength(1000)]
        public string? Amenities { get; set; }
    }
}