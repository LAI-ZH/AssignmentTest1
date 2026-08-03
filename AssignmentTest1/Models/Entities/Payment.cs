using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssignmentTest1.Models.Entities
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        [ForeignKey("Subscription")]
        public int SubscriptionId { get; set; }
        public MemberSubscription? Subscription { get; set; }

        [ForeignKey("Plan")]
        public int? PlanId { get; set; }
        public MembershipPlan? Plan { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = string.Empty;  // Card / QR

        [Required]
        public string Status { get; set; } = "Pending";  // Pending / Successful / Failed

        public string? TransactionId { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        // Card 支付信息 (模拟)
        public string? CardLastFour { get; set; }
        public string? CardType { get; set; }  // Visa / Mastercard

        public string? QRCodeData { get; set; }
        public string? QRCodeImage { get; set; }
    }
}
