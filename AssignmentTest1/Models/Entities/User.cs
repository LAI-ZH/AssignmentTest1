using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssignmentTest1.Models.Entities
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Member";

        [Phone]
        [StringLength(20)]
        public string? Phone { get; set; }

        public string? ProfilePhoto { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        public bool IsLocked { get; set; } = false;

        public int FailedLoginCount { get; set; } = 0;

        public DateTime? LockUntil { get; set; }

        public string? LockReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastLoginAt { get; set; }

        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }

        // Navigation Properties
        public ICollection<Payment>? Payments { get; set; }
        public ICollection<MemberSubscription>? Subscriptions { get; set; }
        public ICollection<FitnessClass>? Classes { get; set; }
        public ICollection<Booking>? Bookings { get; set; }
        public ICollection<Waitlist>? Waitlists { get; set; }
        public ICollection<TrainerPhoto>? TrainerPhotos { get; set; }
    }
}