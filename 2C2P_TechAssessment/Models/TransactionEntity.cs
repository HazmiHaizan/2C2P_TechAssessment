using System;
using System.ComponentModel.DataAnnotations;

namespace _2C2P_TechAssessment.Models
{
    public class TransactionEntity
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string TransactionId { get; set; }

        [Required]
        public decimal Amount {  get; set; }

        [Required]
        [MaxLength(3)]
        public string CurrencyCode { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; }

        [Required]
        [MaxLength(10)]
        public string Status { get; set; } 
    }
}
