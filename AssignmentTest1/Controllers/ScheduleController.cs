using AssignmentTest1.Data;
using AssignmentTest1.Models.Entities;
using AssignmentTest1.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AssignmentTest1.Controllers
{
    public class ScheduleController : Controller
    {
        private readonly FitBookDbContext _context;

        public ScheduleController(FitBookDbContext context)
        {
            _context = context;
        }

        // GET: /Schedule/Index
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var schedules = await _context.ClassSchedules
                .Include(s => s.Class)
                .Include(s => s.Bookings)
                .OrderBy(s => s.ScheduleDate)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
            return View(schedules);
        }

        // GET: /Schedule/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            var classes = await GetClasses();
            ViewBag.Classes = new SelectList(classes, "ClassId", "ClassName");
            return View();
        }

        // POST: /Schedule/Create
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ScheduleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var classes = await GetClasses();
                ViewBag.Classes = new SelectList(classes, "ClassId", "ClassName");
                return View(model);
            }

            // 检查是否有冲突
            var conflict = await _context.ClassSchedules
                .AnyAsync(s => s.ClassId == model.ClassId &&
                               s.ScheduleDate == model.ScheduleDate &&
                               s.StartTime < model.EndTime &&
                               s.EndTime > model.StartTime);

            if (conflict)
            {
                ModelState.AddModelError("", "This time slot conflicts with an existing schedule.");
                var classes = await GetClasses();
                ViewBag.Classes = new SelectList(classes, "ClassId", "ClassName");
                return View(model);
            }

            var schedule = new ClassSchedule
            {
                ClassId = model.ClassId,
                ScheduleDate = model.ScheduleDate,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                Venue = model.Venue,
                CurrentBookings = 0
            };

            _context.ClassSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Schedule created successfully for {schedule.ScheduleDate.ToString("dd/MM/yyyy")}!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Schedule/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var schedule = await _context.ClassSchedules
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.ScheduleId == id);

            if (schedule == null)
            {
                return NotFound();
            }

            var model = new ScheduleViewModel
            {
                ScheduleId = schedule.ScheduleId,
                ClassId = schedule.ClassId,
                ScheduleDate = schedule.ScheduleDate,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                Venue = schedule.Venue
            };

            var classes = await GetClasses();
            ViewBag.Classes = new SelectList(classes, "ClassId", "ClassName", schedule.ClassId);
            return View(model);
        }

        // POST: /Schedule/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ScheduleViewModel model)
        {
            if (id != model.ScheduleId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var classes = await GetClasses();
                ViewBag.Classes = new SelectList(classes, "ClassId", "ClassName", model.ClassId);
                return View(model);
            }

            var schedule = await _context.ClassSchedules
                .Include(s => s.Bookings)
                .FirstOrDefaultAsync(s => s.ScheduleId == id);

            if (schedule == null)
            {
                return NotFound();
            }

            // 检查冲突
            var conflict = await _context.ClassSchedules
                .AnyAsync(s => s.ClassId == model.ClassId &&
                               s.ScheduleId != id &&
                               s.ScheduleDate == model.ScheduleDate &&
                               s.StartTime < model.EndTime &&
                               s.EndTime > model.StartTime);

            if (conflict)
            {
                ModelState.AddModelError("", "This time slot conflicts with an existing schedule.");
                var classes = await GetClasses();
                ViewBag.Classes = new SelectList(classes, "ClassId", "ClassName", model.ClassId);
                return View(model);
            }

            schedule.ClassId = model.ClassId;
            schedule.ScheduleDate = model.ScheduleDate;
            schedule.StartTime = model.StartTime;
            schedule.EndTime = model.EndTime;
            schedule.Venue = model.Venue;

            _context.Update(schedule);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Schedule updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Schedule/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var schedule = await _context.ClassSchedules
                .Include(s => s.Bookings)
                .FirstOrDefaultAsync(s => s.ScheduleId == id);

            if (schedule == null)
            {
                return NotFound();
            }

            if (schedule.Bookings != null && schedule.Bookings.Any())
            {
                TempData["Error"] = $"Cannot delete this schedule because it has {schedule.Bookings.Count} booking(s).";
                return RedirectToAction(nameof(Index));
            }

            var date = schedule.ScheduleDate.ToString("dd/MM/yyyy");
            _context.ClassSchedules.Remove(schedule);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Schedule for {date} deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Schedule/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var schedule = await _context.ClassSchedules
                .Include(s => s.Class!)
                    .ThenInclude(c => c.Trainer!)
                .Include(s => s.Bookings!)
                    .ThenInclude(b => b.Member!)
                .FirstOrDefaultAsync(s => s.ScheduleId == id);

            if (schedule == null)
            {
                return NotFound();
            }

            // ✅ 检查用户角色，决定显示什么内容
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // 如果是 Trainer，只能看自己的课程
            if (role == "Trainer")
            {
                var trainerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (schedule.Class?.TrainerId != trainerId)
                {
                    return Forbid();
                }
            }

            return View(schedule);
        }

        private async Task<List<ClassSelectViewModel>> GetClasses()
        {
            return await _context.FitnessClasses
                .Where(c => c.IsActive)
                .Select(c => new ClassSelectViewModel
                {
                    ClassId = c.ClassId,
                    ClassName = c.ClassName
                })
                .ToListAsync();
        }
        // GET: /Schedule/ManageTemplates - 管理日程模板
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageTemplates()
        {
            var templates = await _context.ClassScheduleTemplates
                .Include(t => t.Class!)
                    .ThenInclude(c => c.Trainer)
                .OrderBy(t => t.DayOfWeek)
                .ThenBy(t => t.StartTime)
                .ToListAsync();
            return View(templates);
        }

        // GET: /Schedule/CreateTemplate
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTemplate()
        {
            var classes = await GetClasses();
            ViewBag.Classes = new SelectList(classes, "ClassId", "ClassName");
            return View();
        }

        // POST: /Schedule/CreateTemplate
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTemplate(ScheduleTemplateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var classes = await GetClasses();
                ViewBag.Classes = new SelectList(classes, "ClassId", "ClassName");
                return View(model);
            }

            var template = new ClassScheduleTemplate
            {
                ClassId = model.ClassId,
                DayOfWeek = model.DayOfWeek,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                Venue = model.Venue,
                IsActive = true
            };

            _context.ClassScheduleTemplates.Add(template);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Template created successfully!";
            return RedirectToAction(nameof(ManageTemplates));
        }
        // 在 ScheduleController.cs 中添加

        // POST: /Schedule/GenerateSchedules
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GenerateSchedules(DateTime? startDate, int weeks = 1)
        {
            var today = DateOnly.FromDateTime(startDate ?? DateTime.Now);
            var endDate = today.AddDays(weeks * 7);

            // 获取所有激活的模板
            var templates = await _context.ClassScheduleTemplates
                .Include(t => t.Class)
                .Where(t => t.IsActive)
                .ToListAsync();

            int createdCount = 0;
            int skippedCount = 0;

            // 遍历每一天
            for (var date = today; date < endDate; date = date.AddDays(1))
            {
                var dayOfWeek = date.DayOfWeek.ToString();

                // 查找该天对应的模板
                var dayTemplates = templates.Where(t => t.DayOfWeek == dayOfWeek);

                foreach (var template in dayTemplates)
                {
                    // 检查是否已经存在该日期的日程
                    var exists = await _context.ClassSchedules
                        .AnyAsync(s => s.ClassId == template.ClassId
                            && s.ScheduleDate == date
                            && s.StartTime == template.StartTime);

                    if (exists)
                    {
                        skippedCount++;
                        continue;
                    }

                    // 创建日程
                    var schedule = new ClassSchedule
                    {
                        ClassId = template.ClassId,
                        ScheduleDate = date,
                        StartTime = template.StartTime,
                        EndTime = template.EndTime,
                        Venue = template.Venue,
                        CurrentBookings = 0
                    };

                    _context.ClassSchedules.Add(schedule);
                    createdCount++;
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Generated {createdCount} schedules. Skipped {skippedCount} existing.";

            // 返回 JSON 用于 AJAX 调用
            return Json(new { created = createdCount, skipped = skippedCount });
        }
    }
}