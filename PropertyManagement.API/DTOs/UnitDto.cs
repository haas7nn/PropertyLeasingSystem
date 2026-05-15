namespace PropertyManagement.API.DTOs
{
    public class UnitDto
    {
        public int UnitId { get; set; }
        public int BuildingId { get; set; }
        public string BuildingName { get; set; } = string.Empty;
        public string UnitNumber { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int? Bedrooms { get; set; }
        public int? Bathrooms { get; set; }
        public decimal SizeInSqFt { get; set; }
        public decimal MonthlyRent { get; set; }
        public string? Amenities { get; set; }
        public string AvailabilityStatus { get; set; } = string.Empty;
    }
}