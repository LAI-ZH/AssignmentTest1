using AssignmentTest1.Models.Entities;

namespace AssignmentTest1.Models.ViewModels
{
    public class ClassIndexViewModel
    {
        public IEnumerable<FitnessClass> Classes { get; set; } = new List<FitnessClass>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; } = 0;

        public string? SearchTerm { get; set; }
        public string? SelectedCategory { get; set; }
        public string? SelectedStatus { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }

        public List<string> Categories { get; set; } = new List<string>();
    }
}