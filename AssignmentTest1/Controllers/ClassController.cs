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
    public class ClassController : Controller
    {
        private readonly FitBookDbContext _context;

        public ClassController(FitBookDbContext context)
        {
            _context = context;
        }

        // GET: /Class/Index - 课程列表
        public async Task<IActionResult> Index()
        {
            var classes = await _context.FitnessClasses
                .Include(c => c.Trainer)
                .Include(c => c.Schedules)
                .OrderBy(c => c.ClassName)
                .ToListAsync();
            return View(classes);
        }

        // GET: /Class/Create - 创建课程页面
        public async Task<IActionResult> Create()
        {
            var trainers = await GetTrainers();
            ViewBag.Trainers = new SelectList(trainers, "TrainerId", "FullName");
            return View();
        }

        // POST: /Class/Create - 创建课程
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClassViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var trainers = await GetTrainers();
                ViewBag.Trainers = new SelectList(trainers, "TrainerId", "FullName");
                return View(model);
            }

            var fitnessClass = new FitnessClass
            {
                ClassName = model.ClassName,
                Category = model.Category,
                Description = model.Description,
                MaxCapacity = model.MaxCapacity,
                TrainerId = model.TrainerId,
                IsActive = true
            };

            _context.FitnessClasses.Add(fitnessClass);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Class '{fitnessClass.ClassName}' created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Class/Edit/5 - 编辑课程页面
        public async Task<IActionResult> Edit(int id)
        {
            var fitnessClass = await _context.FitnessClasses.FindAsync(id);
            if (fitnessClass == null)
            {
                return NotFound();
            }

            var model = new ClassViewModel
            {
                ClassId = fitnessClass.ClassId,
                ClassName = fitnessClass.ClassName,
                Category = fitnessClass.Category,
                Description = fitnessClass.Description,
                MaxCapacity = fitnessClass.MaxCapacity,
                TrainerId = fitnessClass.TrainerId,
                IsActive = fitnessClass.IsActive
            };

            var trainers = await GetTrainers();
            ViewBag.Trainers = new SelectList(trainers, "TrainerId", "FullName", fitnessClass.TrainerId);
            return View(model);
        }

        // POST: /Class/Edit/5 - 更新课程
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ClassViewModel model)
        {
            if (id != model.ClassId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var trainers = await GetTrainers();
                ViewBag.Trainers = new SelectList(trainers, "TrainerId", "FullName", model.TrainerId);
                return View(model);
            }

            var fitnessClass = await _context.FitnessClasses.FindAsync(id);
            if (fitnessClass == null)
            {
                return NotFound();
            }

            fitnessClass.ClassName = model.ClassName;
            fitnessClass.Category = model.Category;
            fitnessClass.Description = model.Description;
            fitnessClass.MaxCapacity = model.MaxCapacity;
            fitnessClass.TrainerId = model.TrainerId;
            fitnessClass.IsActive = model.IsActive;

            _context.Update(fitnessClass);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Class '{fitnessClass.ClassName}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Class/Delete/5 - 删除课程
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var fitnessClass = await _context.FitnessClasses
                .Include(c => c.Schedules)
                .FirstOrDefaultAsync(c => c.ClassId == id);

            if (fitnessClass == null)
            {
                return NotFound();
            }

            // 检查是否有日程
            if (fitnessClass.Schedules != null && fitnessClass.Schedules.Any())
            {
                TempData["Error"] = $"Cannot delete '{fitnessClass.ClassName}' because it has schedules.";
                return RedirectToAction(nameof(Index));
            }

            var className = fitnessClass.ClassName;
            _context.FitnessClasses.Remove(fitnessClass);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Class '{className}' deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Class/Details/5 - 课程详情
        public async Task<IActionResult> Details(int id)
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

            return View(fitnessClass);
        }

        private async Task<List<TrainerSelectViewModel>> GetTrainers()
        {
            return await _context.Users
                .Where(u => u.Role == "Trainer")
                .Select(u => new TrainerSelectViewModel
                {
                    TrainerId = u.UserId,
                    FullName = u.FullName
                })
                .ToListAsync();
        }
    }
}