using System.ComponentModel.DataAnnotations;

namespace AssignmentTest1.Models.ViewModels
{
    public class ClassViewModel
    {
        public int ClassId { get; set; }

        [Required(ErrorMessage = "Class name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Class name must be between 2 and 100 characters")]
        [Display(Name = "Class Name")]
        public string ClassName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [StringLength(50, ErrorMessage = "Category must be less than 50 characters")]
        [Display(Name = "Category")]
        public string Category { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description must be less than 500 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Max capacity is required")]
        [Range(1, 100, ErrorMessage = "Max capacity must be between 1 and 100")]
        [Display(Name = "Max Capacity")]
        public int MaxCapacity { get; set; }

        [Required(ErrorMessage = "Trainer is required")]
        [Display(Name = "Trainer")]
        public int TrainerId { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

}