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
using AssignmentTest1.Services;

namespace AssignmentTest1.Controllers
{
    public class AccountController : Controller
    {
        private readonly FitBookDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly CaptchaService _captchaService;
        private readonly FileUploadService _fileUploadService;

        public AccountController(FitBookDbContext context, IConfiguration configuration, CaptchaService captchaService, FileUploadService fileUploadService)
        {
            _context = context;
            _configuration = configuration;
            _captchaService = captchaService;
            _fileUploadService = fileUploadService;
        }


        // ============================================================
        // CAPTCHA FUNCTIONS
        // ============================================================

        // GET: /Account/GetCaptcha
        [HttpGet]
        public IActionResult GetCaptcha()
        {
            var code = _captchaService.GenerateCaptchaCode();
            var imageBytes = _captchaService.GenerateCaptchaImage(code);

            return File(imageBytes, "image/png");
        }

        // 验证验证码（在 Login/Register 中使用）
        private bool VerifyCaptcha(string userInput)
        {
            return _captchaService.ValidateCaptcha(userInput);
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
        [IgnoreAntiforgeryToken]
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

            // ✅ 检查账户是否被锁定（永久锁定或临时锁定）
            if (user.IsLocked)
            {
                if (user.LockUntil.HasValue && user.LockUntil.Value > DateTime.Now)
                {
                    ModelState.AddModelError("", $"Account is temporarily locked. Try again after {user.LockUntil.Value:HH:mm}");
                    return View(model);
                }
                else if (!user.LockUntil.HasValue)
                {
                    ModelState.AddModelError("", "Account is permanently locked. Please contact administrator.");
                    return View(model);
                }
            }

            // ✅ 检查是否需要验证码（失败次数 >= 3 时）
            // 注意：这里使用 user.FailedLoginCount（数据库中的失败次数）
            if (user.FailedLoginCount >= 3)
            {
                if (!VerifyCaptcha(model.CaptchaCode))
                {
                    ModelState.AddModelError("", "Invalid captcha code.");
                    model.FailedAttempts = user.FailedLoginCount;  // 传递给 View
                    return View(model);
                }
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                user.FailedLoginCount++;

                if (user.FailedLoginCount >= 3)
                {
                    // ✅ 只增加失败次数，不锁定
                    await _context.SaveChangesAsync();
                    ModelState.AddModelError("", "Invalid email or password. Please enter the captcha.");
                    model.FailedAttempts = user.FailedLoginCount;
                    return View(model);
                }

                await _context.SaveChangesAsync();
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            // ✅ 密码正确，检查并更新订阅
            if (user.Role == "Member")
            {
                await CheckAndUpdateExpiredSubscriptions(user.UserId);
            }

            // ✅ 检查验证码（如果登录失败超过2次）
            if (model.FailedAttempts >= 2)
            {
                if (!VerifyCaptcha(model.CaptchaCode))
                {
                    ModelState.AddModelError("", "Invalid captcha code.");
                    model.FailedAttempts = model.FailedAttempts + 1;
                    return View(model);
                }
            }

            // ✅ 密码正确，重置失败次数
            user.FailedLoginCount = 0;
            user.IsLocked = false;
            user.LockUntil = null;
            user.LastLoginAt = DateTime.Now;
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

            // ✅ 上传头像
            string? profilePhotoPath = null;
            if (model.ProfilePhoto != null)
            {
                Console.WriteLine("✅ Uploading profile photo...");
                profilePhotoPath = await _fileUploadService.UploadFileAsync(model.ProfilePhoto);
                Console.WriteLine($"✅ Upload result: {profilePhotoPath ?? "NULL"}");
            }
            else
            {
                Console.WriteLine("❌ ProfilePhoto is NULL, skipping upload");
            }

            var user = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = "Member",
                Phone = model.Phone,
                Gender = model.Gender,
                ProfilePhoto = profilePhotoPath,
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
        // ============================================================
        // SUBSCRIPTION HELPER METHODS
        // ============================================================

        /// <summary>
        /// 检查并更新过期的订阅
        /// </summary>
        private async Task CheckAndUpdateExpiredSubscriptions(int userId)
        {
            var today = DateTime.Now.Date;

            // 查找该用户所有已过期但状态仍为 Active 的订阅
            var expiredSubscriptions = await _context.MemberSubscriptions
                .Include(s => s.Plan)
                .Where(s => s.UserId == userId
                    && s.Status == "Active"
                    && s.EndDate.Date < today)
                .ToListAsync();

            foreach (var sub in expiredSubscriptions)
            {
                if (sub.AutoRenew)
                {
                    // ✅ 自动续费
                    await RenewSubscription(sub);
                }
                else
                {
                    // ❌ 不续费，标记为过期
                    sub.Status = "Expired";
                }
            }

            if (expiredSubscriptions.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 自动续费订阅
        /// </summary>
        private async Task RenewSubscription(MemberSubscription oldSub)
        {
            // 获取计划信息
            var plan = await _context.MembershipPlans.FindAsync(oldSub.PlanId);
            if (plan == null) return;

            // 创建新的订阅
            var newSub = new MemberSubscription
            {
                UserId = oldSub.UserId,
                PlanId = oldSub.PlanId,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(plan.DurationDays),
                Status = "Active",
                BookingsUsed = 0,
                PT_Used = 0,
                AutoRenew = oldSub.AutoRenew,
                PaymentId = oldSub.PaymentId
            };

            _context.MemberSubscriptions.Add(newSub);

            // 旧订阅标记为过期
            oldSub.Status = "Expired";

            // 发送续费通知邮件（可选）
            try
            {
                var user = await _context.Users.FindAsync(oldSub.UserId);
                if (user != null)
                {
                    var emailService = new EmailService(_configuration);
                    await emailService.SendEmailAsync(
                        user.Email,
                        "Subscription Auto-Renewed - FitBook",
                        $"<p>Dear {user.FullName},</p>" +
                        $"<p>Your <strong>{plan.PlanName}</strong> subscription has been automatically renewed.</p>" +
                        $"<p><strong>New Expiry Date:</strong> {newSub.EndDate:dd/MM/yyyy}</p>" +
                        $"<p>Thank you for being a valued member!</p>"
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send renewal email: {ex.Message}");
            }
        }
    }
}