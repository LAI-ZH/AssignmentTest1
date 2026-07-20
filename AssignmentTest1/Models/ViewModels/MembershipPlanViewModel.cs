using System.ComponentModel.DataAnnotations;

namespace AssignmentTest1.Models.ViewModels
{
    public class MembershipPlanViewModel
    {
        public int PlanId { get; set; }

        [Required(ErrorMessage = "Plan name is required")]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Plan Name")]
        public string PlanName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Range(0, 9999.99)]
        [DataType(DataType.Currency)]
        [Display(Name = "Price (RM)")]
        public decimal Price { get; set; }

        [Display(Name = "Duration (Days)")]
        [Range(0, 365)]
        public int DurationDays { get; set; }

        [Display(Name = "Max Bookings")]
        [Range(0, 999)]
        public int MaxBookings { get; set; }

        [Display(Name = "PT Sessions")]
        [Range(0, 99)]
        public int PTSessions { get; set; }

        [Display(Name = "Includes Inbody Test")]
        public bool IncludesInbody { get; set; }

        [Display(Name = "Includes Gym Access")]
        public bool IncludesGymAccess { get; set; }

        [StringLength(200)]
        [Display(Name = "Excludes")]
        public string? Excludes { get; set; }

        [StringLength(500)]
        [Display(Name = "Loyalty Discount")]
        public string? LoyaltyDiscount { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [StringLength(500)]
        [Display(Name = "Benefits")]
        public string? Benefits { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }
    }
}