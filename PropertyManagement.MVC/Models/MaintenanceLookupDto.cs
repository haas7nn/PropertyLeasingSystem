namespace PropertyManagement.MVC.Models
{
    public class MaintenanceLookupDto
    {
        public string TicketNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime SubmittedDate { get; set; }
        public string Building { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string? AssignedStaff { get; set; }
        public string? ResolutionNotes { get; set; }
        public DateTime? ClosedDate { get; set; }

        // Sentinel fields — set when the API is unreachable so the controller
        // can distinguish "not found" from "service unavailable" without null checks.
        public bool ApiDown { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
