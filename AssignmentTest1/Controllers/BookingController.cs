using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssignmentTest1.Data;
using AssignmentTest1.Models.Entities;
using System.Security.Claims;

namespace AssignmentTest1.Controllers
{
    public class BookingController : Controller
    {
        private readonly FitBookDbContext _context;

        public BookingController(FitBookDbContext context)
        {
            _context = context;
        }

        // ============ MEMBER FUNCTIONS ============

        // GET: /Booking/ClassCatalog - 浏览课程目录
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> ClassCatalog()
        {
            var classes = await _context.FitnessClasses
                .Include(c => c.Trainer)
                .Include(c => c.Schedules)
                    .ThenInclude(s => s.Bookings)
                .Where(c => c.IsActive)
                .OrderBy(c => c.ClassName)
                .ToListAsync();
            return View(classes);
        }

        // GET: /Booking/ClassDetails/5 - 课程详情（含可用日程）
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> ClassDetails(int id)
        {
            var fitnessClass = await _context.FitnessClasses
                .Include(c => c.Trainer)
                .Include(c => c.Schedules)
                    .ThenInclude(s => s.Bookings)
                .FirstOrDefaultAsync(c => c.ClassId == id);

            if (fitnessClass == null)
            {
                return NotFound();
            }

            // 只显示未来的日程
            var upcomingSchedules = fitnessClass.Schedules?
                .Where(s => s.ScheduleDate >= DateOnly.FromDateTime(DateTime.Now))
                .OrderBy(s => s.ScheduleDate)
                .ThenBy(s => s.StartTime)
                .ToList();

            ViewBag.UpcomingSchedules = upcomingSchedules;
            return View(fitnessClass);
        }

        // POST: /Booking/Book/5 - 预订课程
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> Book(int scheduleId)
        {
            var schedule = await _context.ClassSchedules
                .Include(s => s.Class)
                .Include(s => s.Bookings)
                .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);

            if (schedule == null)
            {
                return NotFound();
            }

            // 获取当前会员 ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }
            var memberId = int.Parse(userIdClaim);

            // 检查是否已经预订
            var existingBooking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.MemberId == memberId && b.ScheduleId == scheduleId && b.Status != "Cancelled");

            if (existingBooking != null)
            {
                TempData["Error"] = "You have already booked this class.";
                return RedirectToAction("ClassDetails", new { id = schedule.ClassId });
            }

            // 检查是否满额
            var confirmedCount = schedule.Bookings?.Count(b => b.Status == "Confirmed" || b.Status == "Attended") ?? 0;
            if (confirmedCount >= schedule.Class.MaxCapacity)
            {
                // 加入 Waitlist
                var waitlist = new Waitlist
                {
                    MemberId = memberId,
                    ScheduleId = scheduleId,
                    JoinedAt = DateTime.Now,
                    IsPromoted = false
                };
                _context.Waitlists.Add(waitlist);
                await _context.SaveChangesAsync();

                TempData["Info"] = "Class is full. You have been added to the waitlist.";
                return RedirectToAction("ClassDetails", new { id = schedule.ClassId });
            }

            // 创建预订
            var booking = new Booking
            {
                MemberId = memberId,
                ScheduleId = scheduleId,
                BookedAt = DateTime.Now,
                Status = "Confirmed"
            };

            _context.Bookings.Add(booking);
            schedule.CurrentBookings = confirmedCount + 1;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Successfully booked '{schedule.Class.ClassName}'!";
            return RedirectToAction("MyBookings");
        }

        // GET: /Booking/MyBookings - 我的预订
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> MyBookings()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }
            var memberId = int.Parse(userIdClaim);

            var bookings = await _context.Bookings
                .Where(b => b.MemberId == memberId)
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Class)
                        .ThenInclude(c => c.Trainer)
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync();

            return View(bookings);
        }

        // POST: /Booking/Cancel/5 - 取消预订
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> Cancel(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Schedule)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                return NotFound();
            }

            // 验证属于当前用户
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }
            var memberId = int.Parse(userIdClaim);

            if (booking.MemberId != memberId)
            {
                return Forbid();
            }

            // 只能取消 Confirmed 状态的预订
            if (booking.Status != "Confirmed")
            {
                TempData["Error"] = "This booking cannot be cancelled.";
                return RedirectToAction("MyBookings");
            }

            booking.Status = "Cancelled";
            await _context.SaveChangesAsync();

            // 检查 Waitlist 是否有会员可以替补
            var waitlist = await _context.Waitlists
                .Where(w => w.ScheduleId == booking.ScheduleId && !w.IsPromoted)
                .OrderBy(w => w.JoinedAt)
                .FirstOrDefaultAsync();

            if (waitlist != null)
            {
                waitlist.IsPromoted = true;

                // 创建新预订给 waitlist 会员
                var newBooking = new Booking
                {
                    MemberId = waitlist.MemberId,
                    ScheduleId = booking.ScheduleId,
                    BookedAt = DateTime.Now,
                    Status = "Confirmed"
                };
                _context.Bookings.Add(newBooking);
                await _context.SaveChangesAsync();

                TempData["Info"] = "Booking cancelled. A waitlist member has been promoted.";
            }

            TempData["Success"] = "Booking cancelled successfully!";
            return RedirectToAction("MyBookings");
        }

        // ============ ADMIN FUNCTIONS ============

        // GET: /Booking/AdminIndex - Admin 查看所有预订
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIndex()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Member)
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Class)
                        .ThenInclude(c => c.Trainer)
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync();

            return View(bookings);
        }

        // POST: /Booking/AdminCancel/5 - Admin 取消预订
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminCancel(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Schedule)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                return NotFound();
            }

            booking.Status = "Cancelled";
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Booking cancelled successfully!";
            return RedirectToAction("AdminIndex");
        }

        // POST: /Booking/AdminConfirm/5 - Admin 确认预订
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminConfirm(int bookingId)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                return NotFound();
            }

            booking.Status = "Confirmed";
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Booking confirmed successfully!";
            return RedirectToAction("AdminIndex");
        }
    }
}