using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssignmentTest1.Data;
using AssignmentTest1.Models.Entities;
using System.Security.Claims;

using AssignmentTest1.Models.ViewModels;    
namespace AssignmentTest1.Controllers
{
    public class TrainerController : Controller
    {
        private readonly FitBookDbContext _context;

        public TrainerController(FitBookDbContext context)
        {
            _context = context;
        }

        // GET: /Trainer/Index - Admin 查看所有 Trainer
        [Authorize(Roles = "Admin")]  // ← 只有 Admin 可以访问
        public async Task<IActionResult> Index()
        {
            var trainers = await _context.Users
                .Where(u => u.Role == "Trainer")
                .OrderBy(u => u.FullName)
                .ToListAsync();
            return View(trainers);
        }

        // GET: /Trainer/Create - Admin 创建 Trainer
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Trainer/Create - Admin 创建 Trainer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(TrainerCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 检查邮箱是否已存在
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());

            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email already exists");
                return View(model);
            }

            var trainer = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = "Trainer",
                Phone = model.Phone,
                CreatedAt = DateTime.Now,
                IsLocked = false,
                FailedLoginCount = 0
            };

            _context.Users.Add(trainer);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Trainer '{trainer.FullName}' created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Trainer/Edit/5 - Admin 编辑 Trainer
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var trainer = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id && u.Role == "Trainer");

            if (trainer == null)
            {
                return NotFound();
            }

            var model = new TrainerEditViewModel
            {
                UserId = trainer.UserId,
                FullName = trainer.FullName,
                Email = trainer.Email,
                Phone = trainer.Phone,
                IsLocked = trainer.IsLocked
            };

            return View(model);
        }

        // POST: /Trainer/Edit/5 - Admin 更新 Trainer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, TrainerEditViewModel model)
        {
            if (id != model.UserId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var trainer = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id && u.Role == "Trainer");

            if (trainer == null)
            {
                return NotFound();
            }

            // 检查邮箱是否被其他用户使用
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower() && u.UserId != id);

            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email already exists");
                return View(model);
            }

            trainer.FullName = model.FullName;
            trainer.Email = model.Email;
            trainer.Phone = model.Phone;
            trainer.IsLocked = model.IsLocked;

            _context.Update(trainer);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Trainer '{trainer.FullName}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Trainer/Delete/5 - Admin 删除 Trainer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var trainer = await _context.Users
                .Include(u => u.Classes)
                .FirstOrDefaultAsync(u => u.UserId == id && u.Role == "Trainer");

            if (trainer == null)
            {
                return NotFound();
            }

            // 检查是否有课程分配
            if (trainer.Classes != null && trainer.Classes.Any())
            {
                TempData["Error"] = $"Cannot delete '{trainer.FullName}' because they have assigned classes.";
                return RedirectToAction(nameof(Index));
            }

            var trainerName = trainer.FullName;
            _context.Users.Remove(trainer);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Trainer '{trainerName}' deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Trainer/MyClasses - 查看我的课程
        public async Task<IActionResult> MyClasses()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            var trainerId = int.Parse(userIdClaim);

            var classes = await _context.FitnessClasses
                .Where(c => c.TrainerId == trainerId)
                .Include(c => c.Schedules)
                .ThenInclude(s => s.Bookings)
                .OrderBy(c => c.ClassName)
                .ToListAsync();

            return View(classes);
        }

        // GET: /Trainer/MySchedule - 查看我的日程
        public async Task<IActionResult> MySchedule()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            var trainerId = int.Parse(userIdClaim);

            var schedules = await _context.ClassSchedules
                .Include(s => s.Class)
                .Include(s => s.Bookings)
                .ThenInclude(b => b.Member)
                .Where(s => s.Class != null && s.Class.TrainerId == trainerId)
                .OrderBy(s => s.ScheduleDate)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            return View(schedules);
        }

        // GET: /Trainer/ClassDetails/5 - 查看课程详情（含学员名单）
        public async Task<IActionResult> ClassDetails(int id)
        {
            var fitnessClass = await _context.FitnessClasses
                .Include(c => c.Trainer)
                .Include(c => c.Schedules)
                    .ThenInclude(s => s.Bookings)
                        .ThenInclude(b => b.Member)
                .FirstOrDefaultAsync(c => c.ClassId == id);

            if (fitnessClass == null)
            {
                return NotFound();
            }

            // 验证该课程属于当前教练
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            var trainerId = int.Parse(userIdClaim);
            if (fitnessClass.TrainerId != trainerId)
            {
                return Forbid();
            }

            return View(fitnessClass);
        }

        // GET: /Trainer/ScheduleDetails/5 - 查看日程详情（含学员名单）
        public async Task<IActionResult> ScheduleDetails(int id)
        {
            var schedule = await _context.ClassSchedules
                .Include(s => s.Class)
                    .ThenInclude(c => c.Trainer)
                .Include(s => s.Bookings)
                    .ThenInclude(b => b.Member)
                .FirstOrDefaultAsync(s => s.ScheduleId == id);

            if (schedule == null)
            {
                return NotFound();
            }

            // 验证该日程属于当前教练
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            var trainerId = int.Parse(userIdClaim);
            if (schedule.Class == null || schedule.Class.TrainerId != trainerId)
            {
                return Forbid();
            }

            return View(schedule);
        }

        // POST: /Trainer/MarkAttendance/5 - 标记出勤
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAttendance(int bookingId, bool attended)
        {
            var booking = await _context.Bookings
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Class)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                return NotFound();
            }

            // 验证该预订属于当前教练的课程
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            var trainerId = int.Parse(userIdClaim);
            if (booking.Schedule?.Class?.TrainerId != trainerId)
            {
                return Forbid();
            }

            if (attended)
            {
                booking.Status = "Attended";
            }
            else
            {
                booking.Status = "Confirmed"; // 重置为已确认
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Attendance marked successfully!";
            return RedirectToAction("MySchedule");
        }

        // POST: /Trainer/CancelBooking/5 - 取消预订（教练取消学员预订）
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Class)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                return NotFound();
            }

            // 验证该预订属于当前教练的课程
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            var trainerId = int.Parse(userIdClaim);
            if (booking.Schedule?.Class?.TrainerId != trainerId)
            {
                return Forbid();
            }

            booking.Status = "Cancelled";
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Booking cancelled successfully!";
            return RedirectToAction("MySchedule");
        }
    }
}