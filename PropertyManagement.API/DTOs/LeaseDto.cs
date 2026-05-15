namespace PropertyManagement.API.DTOs
{
    public class LeaseDto
    {
        public int LeaseId { get; set; }
        public int UnitId { get; set; }
        public string UnitNumber { get; set; } = string.Empty;
        public string BuildingName { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string TenantEmail { get; set; } = string.Empty;
        public DateTime ApplicationDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal MonthlyRent { get; set; }
        public decimal SecurityDeposit { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }
        public string? ScreeningNotes { get; set; }
    }
}