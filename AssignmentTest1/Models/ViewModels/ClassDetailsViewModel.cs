using AssignmentTest1.Models.Entities;

namespace AssignmentTest1.Models.ViewModels
{
    public class ClassDetailsViewModel
    {
        public FitnessClass? Class { get; set; }
        public List<ScheduleWithBookingInfo>? UpcomingSchedules { get; set; }
    }

    public class ScheduleWithBookingInfo
    {
        public ClassSchedule? Schedule { get; set; }
        public int ConfirmedCount { get; set; }
        public bool IsFull { get; set; }
        public bool HasBooked { get; set; }
        public int WaitlistCount { get; set; }
    }
}