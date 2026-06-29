using System.ComponentModel.DataAnnotations;

namespace AssignmentTest1.Models.ViewModels
{
    public class MembershipPlanViewModel
    {
        public int PlanId { get; set; }

        [Required(ErrorMessage = "Plan name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Plan name must be between 2 and 100 characters")]
        [Display(Name = "Plan Name")]
        public string PlanName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Range(0, 9999.99, ErrorMessage = "Price must be between 0 and 9999.99")]
        [DataType(DataType.Currency)]
        [Display(Name = "Price (RM)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Duration is required")]
        [Range(1, 365, ErrorMessage = "Duration must be between 1 and 365 days")]
        [Display(Name = "Duration (Days)")]
        public int DurationDays { get; set; }

        [Display(Name = "Max Bookings")]
        [Range(0, 999, ErrorMessage = "Max bookings must be 0 for unlimited or between 1-999")]
        public int MaxBookings { get; set; }

        [Display(Name = "PT Sessions")]
        [Range(0, 99, ErrorMessage = "PT sessions must be between 0-99")]
        public int PTSessions { get; set; }

        [StringLength(500, ErrorMessage = "Description must be less than 500 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Benefits (one per line)")]
        public string? Benefits { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;
    }
}