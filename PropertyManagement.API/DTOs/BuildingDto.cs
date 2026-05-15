namespace PropertyManagement.API.DTOs
{
    public class BuildingDto
    {
        public int BuildingId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Location { get; set; }
        public int TotalUnits { get; set; }
        public int AvailableUnits { get; set; }
        public int OccupiedUnits { get; set; }
    }
}