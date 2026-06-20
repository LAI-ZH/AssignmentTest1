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
        [Range(0, 9999.99, ErrorMessage = "Price must be between 0 and 9999.99")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Required]
        [Range(1, 365, ErrorMessage = "Duration must be between 1 and 365 days")]
        public int DurationDays { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<MemberSubscription>? Subscriptions { get; set; }
    }
}