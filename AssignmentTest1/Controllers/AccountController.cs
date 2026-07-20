using AssignmentTest1.Data;
using AssignmentTest1.Models.Entities;
using AssignmentTest1.Models.ViewModels;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AssignmentTest1.Controllers
{
    public class AccountController : Controller
    {
        private readonly FitBookDbContext _context;

        public AccountController(FitBookDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                if (role == "Admin")
                    return RedirectToAction("Index", "Dashboard");
                else if (role == "Trainer")
                    return RedirectToAction("TrainerDashboard", "Dashboard");
                else if (role == "Member")
                    return RedirectToAction("MemberDashboard", "Dashboard");
                else
                    return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            if (user.IsLocked)
            {
                ModelState.AddModelError("", $"Account is locked. Please contact administrator.");
                return View(model);
            }

            if (user.LockUntil.HasValue && user.LockUntil.Value > DateTime.Now)
            {
                ModelState.AddModelError("", $"Account is temporarily locked. Try again after {user.LockUntil.Value:HH:mm}");
                return View(model);
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                user.FailedLoginCount++;

                if (user.FailedLoginCount >= 3)
                {
                    user.IsLocked = true;
                    user.LockUntil = DateTime.Now.AddMinutes(15);
                    await _context.SaveChangesAsync();
                    ModelState.AddModelError("", "Account locked for 15 minutes due to multiple failed attempts");
                    return View(model);
                }

                await _context.SaveChangesAsync();
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            user.LastLoginAt = DateTime.Now;
            user.FailedLoginCount = 0;
            user.LockUntil = null;
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (user.Role == "Admin")
                return RedirectToAction("Index", "Dashboard");
            else if (user.Role == "Trainer")
                return RedirectToAction("TrainerDashboard", "Dashboard");
            else
                return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());

            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email already registered");
                return View(model);
            }
            var user = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = "Member",
                Phone = model.Phone,
                Gender = model.Gender,
                CreatedAt = DateTime.Now,
                IsLocked = false,
                FailedLoginCount = 0
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (user.Role == "Admin")
                return RedirectToAction("Index", "Dashboard");
            else if (user.Role == "Trainer")
                return RedirectToAction("TrainerDashboard", "Dashboard");
            else if (user.Role == "Member")
                return RedirectToAction("MemberDashboard", "Dashboard");
            else
                return RedirectToAction("Index", "Home");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());

            if (user != null)
            {
                // TODO: Send email with password reset link
                TempData["Success"] = "Password reset link has been sent to your email.";
            }
            else
            {
                TempData["Success"] = "If your email exists in our system, you will receive a password reset link.";
            }

            return RedirectToAction("Login");
        }

        // GET: /Account/ChangePassword
        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: /Account/ChangePassword
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect");
                return View(model);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password changed successfully!";
            return RedirectToAction("Index", "Home");
        }
    }
}