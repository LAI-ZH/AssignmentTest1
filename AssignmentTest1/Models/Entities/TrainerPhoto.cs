using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssignmentTest1.Models.Entities
{
    public class TrainerPhoto
    {
        [Key]
        public int PhotoId { get; set; }

        [Required]
        [ForeignKey("Trainer")]
        public int TrainerId { get; set; }
        public User? Trainer { get; set; }

        [Required]
        public string FileName { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}