using System.ComponentModel.DataAnnotations;

namespace AssignmentTest1.Models.ViewModels
{
    public class ScheduleTemplateViewModel
    {
        public int TemplateId { get; set; }

        [Required(ErrorMessage = "Class is required")]
        [Display(Name = "Class")]
        public int ClassId { get; set; }

        [Required(ErrorMessage = "Day of week is required")]
        [Display(Name = "Day of Week")]
        public string DayOfWeek { get; set; } = string.Empty;

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

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // 用于显示课程名称
        public string? ClassName { get; set; }
    }
}