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
using System.Net.Mail;
using AssignmentTest1.Helpers; 

namespace AssignmentTest1.Controllers
{
    public class AccountController : Controller
    {
        private readonly FitBookDbContext _context;
        private readonly IConfiguration _configuration;

        public AccountController(FitBookDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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

        // ============================================================
        // FORGOT PASSWORD FUNCTIONS
        // ============================================================

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
            Console.WriteLine($"=== ForgotPassword Called ===");
            Console.WriteLine($"Email: {model.Email}");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());

            Console.WriteLine($"User found: {user != null}");

            if (user == null)
            {
                TempData["Success"] = "If your email exists, a password reset link has been sent.";
                return RedirectToAction("Login");
            }

            user.ResetToken = Guid.NewGuid().ToString();
            user.ResetTokenExpiry = DateTime.Now.AddHours(1);
            await _context.SaveChangesAsync();

            Console.WriteLine($"Reset Token: {user.ResetToken}");

            var resetLink = $"{Request.Scheme}://{Request.Host}/Account/ResetPassword?token={user.ResetToken}&email={user.Email}";
            Console.WriteLine($"Reset Link: {resetLink}");

            try
            {
                // 发送邮件
                var mail = new MailMessage();
                mail.To.Add(new MailAddress(user.Email, user.FullName));
                mail.Subject = "FitBook - Reset Your Password";
                mail.IsBodyHtml = true;
                mail.Body = $"<p>Click <a href='{resetLink}'>here</a> to reset your password.</p>";

                var emailHelper = new EmailHelper(_configuration);
                emailHelper.SendEmail(mail);
                Console.WriteLine("Email sent successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            TempData["Success"] = "A password reset link has been sent to your email.";
            return RedirectToAction("Login");
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Invalid reset link.";
                return RedirectToAction("Login");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.ResetToken == token);

            if (user == null || user.ResetTokenExpiry < DateTime.Now)
            {
                TempData["Error"] = "Invalid or expired reset link.";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            };

            return View(model);
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower() && u.ResetToken == model.Token);

            if (user == null || user.ResetTokenExpiry < DateTime.Now)
            {
                TempData["Error"] = "Invalid or expired reset link.";
                return RedirectToAction("Login");
            }

            // 更新密码
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            await _context.SaveChangesAsync();

            // 发送确认邮件
            var confirmMail = new MailMessage();
            confirmMail.To.Add(new MailAddress(user.Email, user.FullName));
            confirmMail.Subject = "FitBook - Password Reset Successful";
            confirmMail.IsBodyHtml = true;

            confirmMail.Body = $@"
        <html>
        <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <div style='background: #22C55E; padding: 20px; text-align: center; border-radius: 8px 8px 0 0;'>
                <h1 style='color: white; margin: 0;'>FitBook</h1>
            </div>
            <div style='border: 1px solid #E5E7EB; padding: 30px; border-radius: 0 0 8px 8px;'>
                <h2 style='color: #1A1A2E;'>Password Reset Successful</h2>
                <p style='color: #6B7280;'>Hello <strong>{user.FullName}</strong>,</p>
                <p style='color: #6B7280;'>Your password has been successfully reset.</p>
                <p style='color: #6B7280;'>You can now log in with your new password.</p>
                <div style='text-align: center; margin: 30px 0;'>
                    <a href='{Request.Scheme}://{Request.Host}/Account/Login' 
                       style='background: #4F6EF7; color: white; padding: 12px 40px; text-decoration: none; border-radius: 6px; font-weight: 600; display: inline-block;'>
                        Login Now
                    </a>
                </div>
                <hr style='border: 0.5px solid #F3F4F6; margin: 20px 0;' />
                <p style='color: #9CA3AF; font-size: 12px; text-align: center;'>
                    &copy; 2026 FitBook - Fitness Class Booking System
                </p>
            </div>
        </body>
        </html>
    ";

            var emailHelper = new EmailHelper(_configuration);
            emailHelper.SendEmail(confirmMail);

            TempData["Success"] = "Password reset successfully! Please login with your new password.";
            return RedirectToAction("Login");
        }
    }
}