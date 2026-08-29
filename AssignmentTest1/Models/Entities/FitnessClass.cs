using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssignmentTest1.Models.Entities
{
    public class FitnessClass
    {
        [Key]
        public int ClassId { get; set; }

        [Required]
        [StringLength(100)]
        public string ClassName { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty; // Yoga, HIIT, Pilates

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(1, 100)]
        public int MaxCapacity { get; set; }

        [Required]
        [ForeignKey("Trainer")]
        public int TrainerId { get; set; }
        public User? Trainer { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<ClassSchedule> Schedules { get; set; } = new List<ClassSchedule>();
    }
}