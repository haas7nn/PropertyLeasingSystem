using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.DTOs
{
    public class CreatePaymentDto
    {
        [Required]
        public int LeaseId { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        [Range(0.01, 1000000)]
        public decimal AmountDue { get; set; }
    }
}