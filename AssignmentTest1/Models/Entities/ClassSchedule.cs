using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssignmentTest1.Models.Entities
{
    public class ClassSchedule
    {
        [Key]
        public int ScheduleId { get; set; }

        [Required]
        [ForeignKey("Class")]
        public int ClassId { get; set; }
        public FitnessClass? Class { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateOnly ScheduleDate { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeOnly StartTime { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeOnly EndTime { get; set; }

        [Required]
        [StringLength(100)]
        public string Venue { get; set; } = string.Empty;

        public int CurrentBookings { get; set; } = 0;

        // Navigation
        public ICollection<Booking>? Bookings { get; set; }
        public ICollection<Waitlist>? Waitlists { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}