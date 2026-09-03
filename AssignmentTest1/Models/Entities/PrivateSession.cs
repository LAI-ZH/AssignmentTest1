using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssignmentTest1.Models.Entities
{
    public class PrivateSession
    {
        [Key]
        public int PrivateSessionId { get; set; }

        [Required]
        [ForeignKey("Member")]
        public int MemberId { get; set; }
        public User? Member { get; set; }

        [Required]
        [ForeignKey("Trainer")]
        public int TrainerId { get; set; }
        public User? Trainer { get; set; }

        [Required]
        [ForeignKey("Subscription")]
        public int SubscriptionId { get; set; }
        public MemberSubscription? Subscription { get; set; }

        [Required]
        public DateOnly PreferredDate { get; set; }

        [Required]
        public TimeOnly PreferredTime { get; set; }

        public string? Notes { get; set; }

        [Required]
        public string Status { get; set; } = "Pending"; // Pending / Confirmed / Completed / Cancelled

        public DateTime BookedAt { get; set; } = DateTime.Now;

        public DateTime? ConfirmedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
    }
}