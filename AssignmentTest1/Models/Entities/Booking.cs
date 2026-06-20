using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssignmentTest1.Models.Entities
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required]
        [ForeignKey("Member")]
        public int MemberId { get; set; }
        public User? Member { get; set; }  // ← THIS MUST EXIST

        [Required]
        [ForeignKey("Schedule")]
        public int ScheduleId { get; set; }
        public ClassSchedule? Schedule { get; set; }  // ← THIS MUST EXIST

        public DateTime BookedAt { get; set; } = DateTime.Now;

        [Required]
        public string Status { get; set; } = "Confirmed"; // Confirmed / Cancelled / Attended

        public string? QRCodeData { get; set; }
    }
}