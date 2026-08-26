using AssignmentTest1.Models.Entities;

namespace AssignmentTest1.Models.ViewModels
{
    public class BookingIndexViewModel
    {
        public IEnumerable<Booking> Bookings { get; set; } = new List<Booking>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; } = 0;

        public string? SearchTerm { get; set; }
        public string? SelectedStatus { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
    }
}