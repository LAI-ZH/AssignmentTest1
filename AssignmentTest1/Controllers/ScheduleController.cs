using AssignmentTest1.Data;
using AssignmentTest1.Models.Entities;
using AssignmentTest1.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AssignmentTest1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ScheduleController : Controller
    {
        private readonly FitBookDbContext _context;

        public ScheduleController(FitBookDbContext context)
        {
            _context = context;
        }

        // GET: /Schedule/Index
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
        public async Task<IActionResult> Create()
        {
            var classes = await GetClasses();
            ViewBag.Classes = new SelectList(classes, "ClassId", "ClassName");
            return View();
        }

        // POST: /Schedule/Create
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
        public async Task<IActionResult> Details(int id)
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
    }
}