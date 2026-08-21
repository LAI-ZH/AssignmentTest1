using System.ComponentModel.DataAnnotations;

namespace AssignmentTest1.Models.ViewModels
{
    public class UserProfileViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [Display(Name = "Gender")]
        public string? Gender { get; set; }

        [Display(Name = "Member Since")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Last Login")]
        public DateTime? LastLoginAt { get; set; }

        // ✅ 存储头像路径 (string)
        public string? ProfilePhoto { get; set; }

        // ✅ 上传头像文件 (IFormFile)
        [Display(Name = "Profile Photo")]
        public IFormFile? ProfilePhotoFile { get; set; }

        // ✅ 当前头像路径 (用于显示)
        public string? CurrentProfilePhoto { get; set; }
    }
}