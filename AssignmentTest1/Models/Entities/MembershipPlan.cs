using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssignmentTest1.Models.Entities
{
    public class MembershipPlan
    {
        [Key]
        public int PlanId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Plan Name")]
        public string PlanName { get; set; } = string.Empty;

        [Required]
        [Range(0, 9999.99)]
        [DataType(DataType.Currency)]
        [Display(Name = "Price (RM)")]
        public decimal Price { get; set; }

        [Required]
        [Range(1, 365)]
        [Display(Name = "Duration (Days)")]
        public int DurationDays { get; set; }

        // ✅ 新增：可预订课程次数（0 = 无限）
        [Display(Name = "Max Bookings")]
        public int MaxBookings { get; set; } = 0;

        // ✅ 新增：包含私教次数（0 = 不包含）
        [Display(Name = "PT Sessions")]
        public int PTSessions { get; set; } = 0;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        // ✅ 新增：权益列表（用换行分割）
        [Display(Name = "Benefits")]
        public string? Benefits { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        // Navigation
        public ICollection<MemberSubscription>? Subscriptions { get; set; }
    }
}