using System.ComponentModel.DataAnnotations;

namespace AssignmentTest1.Models.Entities
{
    public class MembershipPlan
    {
        [Key]
        public int PlanId { get; set; }

        [Required]
        [StringLength(100)]
        public string PlanName { get; set; } = string.Empty;

        [Required]
        [Range(0, 9999.99)]
        public decimal Price { get; set; }

        [Range(0, 365)]
        public int DurationDays { get; set; }

        public int MaxBookings { get; set; } = 0;  // 0 = Unlimited

        public int PTSessions { get; set; } = 0;

        public bool IncludesInbody { get; set; } = false;

        public bool IncludesGymAccess { get; set; } = false;

        [StringLength(200)]
        public string? Excludes { get; set; }

        [StringLength(500)]
        public string? LoyaltyDiscount { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? Benefits { get; set; }

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;

        public ICollection<MemberSubscription>? Subscriptions { get; set; }
    }
}