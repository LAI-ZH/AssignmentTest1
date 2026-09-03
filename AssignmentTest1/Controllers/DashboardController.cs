using AssignmentTest1.Data;
using AssignmentTest1.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
            var totalAdmins = await _context.Users
                .Where(u => u.Role == "Admin")
                .CountAsync();

            var totalTrainers = await _context.Users
                .Where(u => u.Role == "Trainer")
                .CountAsync();

            var totalMembers = await _context.Users
                .Where(u => u.Role == "Member")
                .CountAsync();

            // ✅ 新增：按状态统计会员
            var activeMembers = await _context.Users
                .Where(u => u.Role == "Member" && u.IsLocked == false)
                .CountAsync();

            var lockedMembers = await _context.Users
                .Where(u => u.Role == "Member" && u.IsLocked == true)
                .CountAsync();

            // ✅ 新增：按状态统计课程
            var activeClasses = await _context.FitnessClasses
                .Where(c => c.IsActive)
                .CountAsync();

            var inactiveClasses = await _context.FitnessClasses
                .Where(c => !c.IsActive)
                .CountAsync();

            // ✅ 新增：按状态统计预订
            var confirmedBookings = await _context.Bookings
                .Where(b => b.Status == "Confirmed")
                .CountAsync();

            var attendedBookings = await _context.Bookings
                .Where(b => b.Status == "Attended")
                .CountAsync();

            var cancelledBookings = await _context.Bookings
                .Where(b => b.Status == "Cancelled")
                .CountAsync();

            var totalUsers = await _context.Users.CountAsync();

            ViewBag.TotalAdmins = totalAdmins;
            ViewBag.TotalTrainers = totalTrainers;
            ViewBag.TotalMembers = totalMembers;
            ViewBag.ActiveMembers = activeMembers;
            ViewBag.LockedMembers = lockedMembers;
            ViewBag.TotalClasses = await _context.FitnessClasses.CountAsync();
            ViewBag.ActiveClasses = activeClasses;
            ViewBag.InactiveClasses = inactiveClasses;
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.ConfirmedBookings = confirmedBookings;
            ViewBag.AttendedBookings = attendedBookings;
            ViewBag.CancelledBookings = cancelledBookings;

            //Chart 1
            ViewBag.UserRoleLabels = new[] { "Admin", "Trainer", "Member" };
            ViewBag.UserRoleData = new[] { totalAdmins, totalTrainers, totalMembers };
            ViewBag.UserRoleColors = new[] { "#DC3545", "#0DCAF0", "#198754" };

            //Chart 2
            var classData = await _context.Bookings
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Class)
                .Where(b => b.Schedule != null && b.Schedule.Class != null)
                .Select(b => new { ClassName = b.Schedule.Class.ClassName })
                .ToListAsync();

            var topClasses = classData
                .GroupBy(b => b.ClassName)
                .Select(g => new
                {
                    ClassName = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(g => g.Count)
                .Take(5)
                .ToList();

            ViewBag.TopClassLabels = topClasses.Select(g => g.ClassName).ToArray();
            ViewBag.TopClassData = topClasses.Select(g => g.Count).ToArray();
            ViewBag.TopClassColors = new[] { "#4F6EF7", "#22C55E", "#FFC107", "#DC3545", "#0DCAF0" };

            // 最近预订
            var recentBookings = await _context.Bookings
                .Include(b => b.Member)
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Class)
                .OrderByDescending(b => b.BookedAt)
                .Take(5)
                .ToListAsync();
            ViewBag.RecentBookings = recentBookings;

            // 即将开始的课程
            var upcomingClasses = await _context.ClassSchedules
                .Include(cs => cs.Class)
                .Where(cs => cs.ScheduleDate >= DateOnly.FromDateTime(DateTime.Now))
                .OrderBy(cs => cs.ScheduleDate)
                .ThenBy(cs => cs.StartTime)
                .Take(2)
                .ToListAsync();
            ViewBag.UpcomingClasses = upcomingClasses;

            return View();
        }

        // ===== TRAINER DASHBOARD =====
        [Authorize(Roles = "Trainer")]
        public async Task<IActionResult> TrainerDashboard()

        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            var trainerId = int.Parse(userIdClaim);

            // ✅ 获取私人训练预约（会员为这位教练预订的）
            var privateSessions = await _context.PrivateSessions
                .Include(ps => ps.Member)
                .Where(ps => ps.TrainerId == trainerId && ps.Status != "Cancelled")
                .OrderBy(ps => ps.PreferredDate)
                .ThenBy(ps => ps.PreferredTime)
                .ToListAsync();
            ViewBag.PrivateSessions = privateSessions;

            // 统计待确认的预约数量
            ViewBag.PendingPrivateSessions = privateSessions.Count(ps => ps.Status == "Pending");

            // 获取教练的日程（用于日历显示)
            var mySchedules = await _context.ClassSchedules
                .Include(s => s.Class)
                    .ThenInclude(c => c.Trainer)
                .Include(s => s.Bookings)
                .Where(s => s.Class != null && s.Class.TrainerId == trainerId)
                .OrderBy(s => s.ScheduleDate)
                .ThenBy(s => s.StartTime)
                .ToListAsync() ?? new List<ClassSchedule>();
            ViewBag.MySchedules = mySchedules;

            // 教练的课程
            var myClasses = await _context.FitnessClasses
                .Where(c => c.TrainerId == trainerId)
                .Include(c => c.Schedules)
                .ToListAsync() ?? new List<FitnessClass>();
            ViewBag.MyClasses = myClasses;

            // 即将开始的课程
            var upcomingSchedules = await _context.ClassSchedules
                .Include(cs => cs.Class)
                .Where(cs => cs.Class != null && cs.Class.TrainerId == trainerId)
                .Where(cs => cs.ScheduleDate >= DateOnly.FromDateTime(DateTime.Now))
                .OrderBy(cs => cs.ScheduleDate)
                .ThenBy(cs => cs.StartTime)
                .Take(10)
                .ToListAsync() ?? new List<ClassSchedule>();
            ViewBag.UpcomingSchedules = upcomingSchedules;

            // 总学生数
            var totalStudents = await _context.Bookings
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Class)
                .Where(b => b.Schedule != null && b.Schedule.Class != null && b.Schedule.Class.TrainerId == trainerId)
                .Where(b => b.Status == "Confirmed" || b.Status == "Attended")
                .CountAsync();
            ViewBag.TotalStudents = totalStudents;

            return View();
        }

        public async Task<IActionResult> MemberDashboard()
        {
            // ✅ 安全获取用户 ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            var memberId = int.Parse(userIdClaim);
            var today = DateOnly.FromDateTime(DateTime.Now);

            var allSchedules = await _context.ClassSchedules
                .Include(s => s.Class)
                    .ThenInclude(c => c.Trainer)
                .Include(s => s.Bookings)
                .OrderBy(s => s.ScheduleDate >= today)
                .ThenBy(s => s.StartTime)
                .ToListAsync() ?? new List<ClassSchedule>();
            ViewBag.AllSchedules = allSchedules;


            var myBookings = await _context.Bookings
                .Where(b => b.MemberId == memberId && b.Status == "Confirmed")
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Class)
                    .ThenInclude(c => c.Trainer)
                .ToListAsync();
            ViewBag.MyBookings = myBookings;

            ViewBag.AllSchedules = allSchedules;

            var upcomingBookings = await _context.Bookings
                .Where(b => b.MemberId == memberId)
                .Where(b => b.Status == "Confirmed")
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Class)
                .Where(b => b.Schedule != null && b.Schedule.ScheduleDate >= DateOnly.FromDateTime(DateTime.Now))
                .OrderBy(b => b.Schedule.ScheduleDate)
                .ThenBy(b => b.Schedule.StartTime)
                .Take(5)
                .ToListAsync() ?? new List<Booking>();
            ViewBag.UpcomingBookings = upcomingBookings;

            ViewBag.TotalBookings = await _context.Bookings
                .Where(b => b.MemberId == memberId)
                .CountAsync();

            return View();
        }
    }
}