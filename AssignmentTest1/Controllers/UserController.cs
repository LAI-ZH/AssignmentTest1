using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssignmentTest1.Data;
using AssignmentTest1.Models.Entities;
using AssignmentTest1.Models.ViewModels;
using BCrypt.Net;
using System.Security.Claims;

namespace AssignmentTest1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly FitBookDbContext _context;

        public UserController(FitBookDbContext context)
        {
            _context = context;
        }

        // GET: /User/Index
        public async Task<IActionResult> Index(
            string? search = null,
            string? role = null,
            string? status = null,
            string? sortBy = "FullName",
            string? sortOrder = "asc",
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.Users
                .Include(u => u.Subscriptions)
                    .ThenInclude(s => s.Plan)
                .AsQueryable();

            // ===== 搜索 (Searching) =====
            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim();
                query = query.Where(u =>
                    u.FullName.Contains(search) ||
                    u.Email.Contains(search) ||
                    u.Phone.Contains(search));
            }

            // ===== 筛选 (Filtering) =====
            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.Role == role);
            }

            // ✅ 按状态筛选（新增）
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "Active")
                    query = query.Where(u => u.IsLocked == false);
                else if (status == "Locked")
                    query = query.Where(u => u.IsLocked == true);
            }

            // ===== 排序 (Sorting) =====
            sortBy = string.IsNullOrEmpty(sortBy) ? "FullName" : sortBy;
            sortOrder = string.IsNullOrEmpty(sortOrder) ? "asc" : sortOrder;

            query = sortBy.ToLower() switch
            {
                "id" => sortOrder == "asc" ? query.OrderBy(u => u.UserId) : query.OrderByDescending(u => u.UserId),
                "fullname" => sortOrder == "asc" ? query.OrderBy(u => u.FullName) : query.OrderByDescending(u => u.FullName),
                "email" => sortOrder == "asc" ? query.OrderBy(u => u.Email) : query.OrderByDescending(u => u.Email),
                "role" => sortOrder == "asc" ? query.OrderBy(u => u.Role) : query.OrderByDescending(u => u.Role),
                "lastlogin" => sortOrder == "asc" ? query.OrderBy(u => u.LastLoginAt) : query.OrderByDescending(u => u.LastLoginAt),
                _ => sortOrder == "asc" ? query.OrderBy(u => u.FullName) : query.OrderByDescending(u => u.FullName)
            };

            // ===== 分页 (Paging) =====
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new UserIndexViewModel
            {
                Users = users,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalCount = totalCount,
                SearchTerm = search,
                SelectedRole = role,
                SortBy = sortBy,
                SortOrder = sortOrder
            };

            ViewBag.Roles = new List<string> { "Admin", "Trainer", "Member" };
            return View(model);
        }

        // GET: /User/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.Users
                .Include(u => u.Bookings)
                    .ThenInclude(b => b.Schedule)
                    .ThenInclude(s => s.Class)
                .Include(u => u.Subscriptions)
                    .ThenInclude(s => s.Plan)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: /User/Create
        public IActionResult Create()
        {
            ViewBag.Roles = new List<string> { "Admin", "Trainer" };
            return View();
        }

        // POST: /User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new List<string> { "Admin", "Trainer" };
                return View(model);
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());

            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email already exists");
                ViewBag.Roles = new List<string> { "Admin", "Trainer" };
                return View(model);
            }

            var user = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = model.Role,
                Phone = model.Phone,
                CreatedAt = DateTime.Now,
                IsLocked = false,
                FailedLoginCount = 0
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"User '{user.FullName}' created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /User/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var currentUserEmail = User.Identity?.Name;
            var isOwnAccount = currentUserEmail == user.Email;

            var model = new EditUserViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                IsLocked = user.IsLocked,
                LockReason = user.LockReason,
                LastLoginAt = user.LastLoginAt,
                IsOwnAccount = isOwnAccount
            };

            ViewBag.Roles = new List<string> { "Admin", "Trainer", "Member" };
            return View(model);
        }

        // POST: /User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditUserViewModel model)
        {
            if (id != model.UserId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new List<string> { "Admin", "Trainer", "Member" };
                return View(model);
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var currentUserEmail = User.Identity?.Name;
            var isOwnAccount = currentUserEmail == user.Email;

            if (isOwnAccount)
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower() && u.UserId != id);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email already exists");
                    ViewBag.Roles = new List<string> { "Admin", "Trainer", "Member" };
                    return View(model);
                }

                user.FullName = model.FullName;
                user.Email = model.Email;
                user.Phone = model.Phone;
                user.Role = model.Role;
            }
            else
            {
                user.IsLocked = model.IsLocked;
                user.LockReason = model.LockReason;
            }

            _context.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"User '{user.FullName}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /User/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users
                .Include(u => u.Bookings)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            if (user.Bookings != null && user.Bookings.Any())
            {
                TempData["Error"] = $"Cannot delete '{user.FullName}' because they have bookings.";
                return RedirectToAction(nameof(Index));
            }

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (id == currentUserId)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Index));
            }

            var userName = user.FullName;
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"User '{userName}' deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}