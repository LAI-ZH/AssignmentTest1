using System.ComponentModel.DataAnnotations;

namespace AssignmentTest1.Models.ViewModels
{
    public class ScheduleViewModel
    {
        public int ScheduleId { get; set; }

        [Required(ErrorMessage = "Class is required")]
        [Display(Name = "Class")]
        public int ClassId { get; set; }

        [Required(ErrorMessage = "Date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Date")]
        public DateOnly ScheduleDate { get; set; }

        [Required(ErrorMessage = "Start time is required")]
        [DataType(DataType.Time)]
        [Display(Name = "Start Time")]
        public TimeOnly StartTime { get; set; }

        [Required(ErrorMessage = "End time is required")]
        [DataType(DataType.Time)]
        [Display(Name = "End Time")]
        public TimeOnly EndTime { get; set; }

        [Required(ErrorMessage = "Venue is required")]
        [StringLength(100, ErrorMessage = "Venue must be less than 100 characters")]
        [Display(Name = "Venue")]
        public string Venue { get; set; } = string.Empty;
    }

    public class ClassSelectViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
    }
}