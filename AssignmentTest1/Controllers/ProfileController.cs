using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssignmentTest1.Data;
using AssignmentTest1.Models.Entities;
using AssignmentTest1.Models.ViewModels;
using BCrypt.Net;
using System.Security.Claims;
using AssignmentTest1.Services;

namespace AssignmentTest1.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly FitBookDbContext _context;
        private readonly FileUploadService _fileUploadService;

        public ProfileController(FitBookDbContext context, FileUploadService fileUploadService)
        {
            _context = context;
            _fileUploadService = fileUploadService;
        }

        // GET: /Profile/Index
        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = int.Parse(userIdClaim);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            // ✅ 获取会员订阅状态（仅对 Member 角色）
            string membershipStatus = "No active subscription";
            string? planName = null;
            DateTime? expiryDate = null;

            if (user.Role == "Member")
            {
                var subscription = await _context.MemberSubscriptions
                    .Include(s => s.Plan)
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "Active");

                if (subscription != null)
                {
                    // 检查是否过期
                    if (subscription.EndDate < DateTime.Now)
                    {
                        membershipStatus = "Expired";
                        planName = subscription.Plan?.PlanName;
                        expiryDate = subscription.EndDate;
                    }
                    else
                    {
                        membershipStatus = "Active";
                        planName = subscription.Plan?.PlanName;
                        expiryDate = subscription.EndDate;
                    }
                }
                else
                {
                    // 检查是否有已过期或已取消的订阅
                    var expiredSubscription = await _context.MemberSubscriptions
                        .Include(s => s.Plan)
                        .FirstOrDefaultAsync(s => s.UserId == userId && (s.Status == "Expired" || s.Status == "Cancelled"));

                    if (expiredSubscription != null)
                    {
                        membershipStatus = expiredSubscription.Status;
                        planName = expiredSubscription.Plan?.PlanName;
                        expiryDate = expiredSubscription.EndDate;
                    }
                }
            }

            // 把数据传给 View
            ViewBag.MembershipStatus = membershipStatus;
            ViewBag.PlanName = planName;
            ViewBag.ExpiryDate = expiryDate;

            var model = new UserProfileViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                ProfilePhoto = user.ProfilePhoto,
                Gender = user.Gender,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            return View(model);
        }

        // GET: /Profile/Edit
        public async Task<IActionResult> Edit()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = int.Parse(userIdClaim);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            var model = new UserProfileViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Gender = user.Gender,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                ProfilePhoto = user.ProfilePhoto,          // ✅ string
                CurrentProfilePhoto = user.ProfilePhoto    // ✅ string
            };

            return View(model);
        }

        // POST: /Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = int.Parse(userIdClaim);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            // 检查邮箱是否被其他用户使用
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower() && u.UserId != userId);

            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email already exists");
                return View(model);
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.Phone = model.Phone;
            user.Gender = model.Gender;

            // ✅ 处理头像上传
            if (model.ProfilePhotoFile != null && model.ProfilePhotoFile.Length > 0)
            {
                Console.WriteLine($"File received: {model.ProfilePhotoFile.FileName}, Size: {model.ProfilePhotoFile.Length}");

                // 删除旧头像
                if (!string.IsNullOrEmpty(user.ProfilePhoto))
                {
                    _fileUploadService.DeleteFile(user.ProfilePhoto);
                }

                // 上传新头像
                var newPhotoPath = await _fileUploadService.UploadFileAsync(model.ProfilePhotoFile);
                Console.WriteLine($"Upload result: {newPhotoPath ?? "NULL"}");

                if (newPhotoPath != null)
                {
                    user.ProfilePhoto = newPhotoPath;
                }
                else
                {
                    TempData["Error"] = "Invalid file format. Please upload JPG, PNG, or GIF.";
                    return View(model);
                }
            }

            _context.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}