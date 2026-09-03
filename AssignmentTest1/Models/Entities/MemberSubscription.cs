using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssignmentTest1.Models.Entities
{
    public class MemberSubscription
    {
        [Key]
        public int SubscriptionId { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        [ForeignKey("Plan")]
        public int PlanId { get; set; }
        public MembershipPlan? Plan { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public string Status { get; set; } = "Active"; // Active / Expired / Cancelled

        // ✅ 新增字段
        public int BookingsUsed { get; set; } = 0;
        public int PT_Used { get; set; } = 0;
        public DateTime? CancelledAt { get; set; }
        public bool AutoRenew { get; set; } = false;
        public int? PaymentId { get; set; }
        public Payment? Payment { get; set; }
        public ICollection<PrivateSession>? PrivateSessions { get; set; }
    }
}