using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssignmentTest1.Models.Entities
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Member"; // Admin, Trainer, Member

        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(20)]
        public string? Phone { get; set; }

        public string? ProfilePhoto { get; set; }

        public bool IsLocked { get; set; } = false;

        public int FailedLoginCount { get; set; } = 0;

        public DateTime? LockUntil { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public ICollection<MemberSubscription>? Subscriptions { get; set; }
        public ICollection<FitnessClass>? Classes { get; set; }
        public ICollection<Booking>? Bookings { get; set; }
        public ICollection<Waitlist>? Waitlists { get; set; }
        public ICollection<TrainerPhoto>? TrainerPhotos { get; set; }
    }
}