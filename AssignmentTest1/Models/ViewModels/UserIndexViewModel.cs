using AssignmentTest1.Models.Entities;

namespace AssignmentTest1.Models.ViewModels
{
    public class UserIndexViewModel
    {
        public IEnumerable<User> Users { get; set; } = new List<User>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; } = 0;

        public string? SearchTerm { get; set; }
        public string? SelectedRole { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }

        public List<string> Roles { get; set; } = new List<string> { "Admin", "Trainer", "Member" };
    }
}