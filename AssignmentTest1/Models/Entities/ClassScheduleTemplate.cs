using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssignmentTest1.Models.Entities
{
    public class ClassScheduleTemplate
    {
        [Key]
        public int TemplateId { get; set; }

        [Required]
        [ForeignKey("Class")]
        public int ClassId { get; set; }
        public FitnessClass? Class { get; set; }

        [Required]
        public string DayOfWeek { get; set; } = string.Empty; // Monday, Tuesday, etc.

        [Required]
        public TimeOnly StartTime { get; set; }

        [Required]
        public TimeOnly EndTime { get; set; }

        [Required]
        [StringLength(100)]
        public string Venue { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}