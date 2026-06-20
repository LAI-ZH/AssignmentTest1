using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssignmentTest1.Data;

namespace AssignmentTest1.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly FitBookDbContext _context;

        public DashboardController(FitBookDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalClasses = await _context.FitnessClasses.CountAsync();
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.TotalTrainers = await _context.Users
                .Where(u => u.Role == "Trainer")
                .CountAsync();

            var recentBookings = await _context.Bookings
                .Include(b => b.Member)
                .Include(b => b.Schedule)
                .ThenInclude(s => s.Class)
                .OrderByDescending(b => b.BookedAt)
                .Take(5)
                .ToListAsync();
            ViewBag.RecentBookings = recentBookings;

            var upcomingClasses = await _context.ClassSchedules
                .Include(cs => cs.Class)
                .Where(cs => cs.ScheduleDate >= DateOnly.FromDateTime(DateTime.Now))
                .OrderBy(cs => cs.ScheduleDate)
                .ThenBy(cs => cs.StartTime)
                .Take(5)
                .ToListAsync();
            ViewBag.UpcomingClasses = upcomingClasses;

            return View();
        }

        [Authorize(Roles = "Trainer")]
        public async Task<IActionResult> TrainerDashboard()
        {
            var trainerId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            var myClasses = await _context.FitnessClasses
                .Where(c => c.TrainerId == trainerId)
                .Include(c => c.Schedules)
                .ToListAsync();
            ViewBag.MyClasses = myClasses;

            var upcomingSchedules = await _context.ClassSchedules
                .Include(cs => cs.Class)
                .Where(cs => cs.Class.TrainerId == trainerId)
                .Where(cs => cs.ScheduleDate >= DateOnly.FromDateTime(DateTime.Now))
                .OrderBy(cs => cs.ScheduleDate)
                .ThenBy(cs => cs.StartTime)
                .Take(10)
                .ToListAsync();
            ViewBag.UpcomingSchedules = upcomingSchedules;

            var totalStudents = await _context.Bookings
                .Include(b => b.Schedule)
                .ThenInclude(s => s.Class)
                .Where(b => b.Schedule.Class.TrainerId == trainerId)
                .Where(b => b.Status == "Confirmed" || b.Status == "Attended")
                .CountAsync();
            ViewBag.TotalStudents = totalStudents;

            return View();
        }

        [Authorize(Roles = "Member")]
        public async Task<IActionResult> MemberDashboard()
        {
            var memberId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            var myBookings = await _context.Bookings
                .Where(b => b.MemberId == memberId)
                .Include(b => b.Schedule)
                .ThenInclude(s => s.Class)
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync();
            ViewBag.MyBookings = myBookings;

            var upcomingBookings = await _context.Bookings
                .Where(b => b.MemberId == memberId)
                .Where(b => b.Status == "Confirmed")
                .Include(b => b.Schedule)
                .ThenInclude(s => s.Class)
                .Where(b => b.Schedule.ScheduleDate >= DateOnly.FromDateTime(DateTime.Now))
                .OrderBy(b => b.Schedule.ScheduleDate)
                .ThenBy(b => b.Schedule.StartTime)
                .Take(5)
                .ToListAsync();
            ViewBag.UpcomingBookings = upcomingBookings;

            ViewBag.TotalBookings = await _context.Bookings
                .Where(b => b.MemberId == memberId)
                .CountAsync();

            return View();
        }
    }
}