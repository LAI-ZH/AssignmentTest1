using System.ComponentModel.DataAnnotations;

namespace AssignmentTest1.Models.ViewModels
{
    public class EditUserViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Role is required")]
        [Display(Name = "Role")]
        public string Role { get; set; } = "Member";

        [Display(Name = "Lock Account")]
        public bool IsLocked { get; set; }

        [Display(Name = "Lock Reason")]
        public string? LockReason { get; set; }

        [Display(Name = "Last Login")]
        public DateTime? LastLoginAt { get; set; }

        public bool IsOwnAccount { get; set; }
    }
}