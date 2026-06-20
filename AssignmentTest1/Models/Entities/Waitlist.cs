using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssignmentTest1.Models.Entities
{
    public class Waitlist
    {
        [Key]
        public int WaitlistId { get; set; }

        [Required]
        [ForeignKey("Member")]
        public int MemberId { get; set; }
        public User? Member { get; set; }

        [Required]
        [ForeignKey("Schedule")]
        public int ScheduleId { get; set; }
        public ClassSchedule? Schedule { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.Now;

        public bool IsPromoted { get; set; } = false;
    }
}