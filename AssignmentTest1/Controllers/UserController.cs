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

        // GET: /User/Index - 用户列表
        public async Task<IActionResult> Index(string search = null, string role = null)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u =>
                    u.FullName.Contains(search) ||
                    u.Email.Contains(search));
            }

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.Role == role);
            }

            var users = await query
                .OrderBy(u => u.Role)
                .ThenBy(u => u.FullName)
                .ToListAsync();

            ViewBag.SearchTerm = search;
            ViewBag.SelectedRole = role;
            ViewBag.Roles = new List<string> { "Admin", "Trainer", "Member" };

            return View(users);
        }

        // GET: /User/Details/5 - 用户详情
        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.Users
                .Include(u => u.Bookings)
                    .ThenInclude(b => b.Schedule)
                    .ThenInclude(s => s.Class)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: /User/Create - 创建用户页面
        public IActionResult Create()
        {
            ViewBag.Roles = new List<string> { "Admin", "Trainer" };  // ✅ 只显示 Admin + Trainer
            return View();
        }

        // POST: /User/Create - 创建用户
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

        // GET: /User/Edit/5 - 编辑用户页面
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

        // POST: /User/Edit/5 - 更新用户
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

                if (model.IsLocked)
                {
                    user.LockUntil = DateTime.Now.AddYears(10);
                }
                else
                {
                    user.LockUntil = null;
                    user.LockReason = null;
                }
            }

            _context.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"User '{user.FullName}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /User/Delete/5 - 删除用户
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