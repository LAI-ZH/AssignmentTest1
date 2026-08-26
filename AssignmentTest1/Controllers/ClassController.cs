using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssignmentTest1.Data;
using AssignmentTest1.Models.Entities;
using AssignmentTest1.Models.ViewModels;

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

        // GET: /Class/Index
        public async Task<IActionResult> Index(
            string? search = null,
            string? category = null,
            string? status = null,
            string? sortBy = "ClassName",
            string? sortOrder = "asc",
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.FitnessClasses
                .Include(c => c.Trainer)
                .Include(c => c.Schedules)
                .AsQueryable();

            // ===== 搜索 (Searching) =====
            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim();
                query = query.Where(c =>
                    c.ClassName.Contains(search) ||
                    c.Category.Contains(search) ||
                    c.Trainer.FullName.Contains(search));
            }

            // ===== 筛选 (Filtering) =====
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(c => c.Category == category);
            }

            // ✅ 按状态筛选（新增）
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "Active")
                    query = query.Where(c => c.IsActive == true);
                else if (status == "Inactive")
                    query = query.Where(c => c.IsActive == false);
            }

            // ===== 排序 (Sorting) =====
            sortBy = string.IsNullOrEmpty(sortBy) ? "ClassName" : sortBy;
            sortOrder = string.IsNullOrEmpty(sortOrder) ? "asc" : sortOrder;

            query = sortBy.ToLower() switch
            {
                "id" => sortOrder == "asc" ? query.OrderBy(c => c.ClassId) : query.OrderByDescending(c => c.ClassId),
                "classname" => sortOrder == "asc" ? query.OrderBy(c => c.ClassName) : query.OrderByDescending(c => c.ClassName),
                "category" => sortOrder == "asc" ? query.OrderBy(c => c.Category) : query.OrderByDescending(c => c.Category),
                "trainer" => sortOrder == "asc" ? query.OrderBy(c => c.Trainer.FullName) : query.OrderByDescending(c => c.Trainer.FullName),
                "capacity" => sortOrder == "asc" ? query.OrderBy(c => c.MaxCapacity) : query.OrderByDescending(c => c.MaxCapacity),
                "schedules" => sortOrder == "asc" ? query.OrderBy(c => c.Schedules.Count) : query.OrderByDescending(c => c.Schedules.Count),
                "status" => sortOrder == "asc" ? query.OrderBy(c => c.IsActive) : query.OrderByDescending(c => c.IsActive),
                _ => sortOrder == "asc" ? query.OrderBy(c => c.ClassName) : query.OrderByDescending(c => c.ClassName)
            };

            // ===== 分页 (Paging) =====
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var classes = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 获取所有类别（用于筛选下拉菜单）
            var categories = await _context.FitnessClasses
                .Select(c => c.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var model = new ClassIndexViewModel
            {
                Classes = classes,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalCount = totalCount,
                SearchTerm = search,
                SelectedCategory = category,
                SelectedStatus = status,
                SortBy = sortBy,
                SortOrder = sortOrder,
                Categories = categories
            };

            return View(model);
        }

        // GET: /Class/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Trainers = await GetTrainers();
            return View();
        }

        // POST: /Class/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClassViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Trainers = await GetTrainers();
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

        // GET: /Class/Edit/5
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

            ViewBag.Trainers = await GetTrainers();
            return View(model);
        }

        // POST: /Class/Edit/5
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
                ViewBag.Trainers = await GetTrainers();
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

        // POST: /Class/Delete/5
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

        // GET: /Class/Details/5
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