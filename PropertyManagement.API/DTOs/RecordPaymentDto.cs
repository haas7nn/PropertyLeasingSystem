using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.DTOs
{
    public class RecordPaymentDto
    {
        [Required]
        [Range(0.01, 1000000)]
        public decimal AmountPaid { get; set; }

        [Required]
        [StringLength(100)]
        public string PaymentMethod { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ReceiptNumber { get; set; }
    }
}