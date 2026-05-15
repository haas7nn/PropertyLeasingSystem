namespace PropertyManagement.API.DTOs
{
    public class PaymentDto
    {
        public int PaymentId { get; set; }
        public int LeaseId { get; set; }
        public string TenantEmail { get; set; } = string.Empty;
        public string UnitNumber { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public decimal AmountDue { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public string? ReceiptNumber { get; set; }
    }
}