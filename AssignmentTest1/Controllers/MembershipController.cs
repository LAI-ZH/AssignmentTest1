using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssignmentTest1.Data;
using AssignmentTest1.Models.Entities;
using AssignmentTest1.Models.ViewModels;
using System.Security.Claims;

namespace AssignmentTest1.Controllers
{
    public class MembershipController : Controller
    {
        private readonly FitBookDbContext _context;

        public MembershipController(FitBookDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // ADMIN FUNCTIONS - 只有 Admin 可以访问
        // ============================================================

        // GET: /Membership/Plans
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Plans()
        {
            var plans = await _context.MembershipPlans
                .Include(p => p.Subscriptions)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();
            return View(plans);
        }

        // POST: /Membership/CreatePlan
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreatePlan(MembershipPlanViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var plan = new MembershipPlan
            {
                PlanName = model.PlanName,
                Price = model.Price,
                DurationDays = model.DurationDays,
                MaxBookings = model.MaxBookings,
                PTSessions = model.PTSessions,
                Description = model.Description,
                Benefits = model.Benefits,
                IsActive = model.IsActive,
                DisplayOrder = model.DisplayOrder
            };

            _context.MembershipPlans.Add(plan);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Plan '{plan.PlanName}' created successfully!";
            return RedirectToAction(nameof(Plans));
        }

        // GET: /Membership/ChoosePlanType
        [Authorize(Roles = "Admin")]
        public IActionResult ChoosePlanType()
        {
            return View();
        }

        // GET: /Membership/CreatePlan?type=xxx
        [Authorize(Roles = "Admin")]
        public IActionResult CreatePlan(string type)
        {
            var model = new MembershipPlanViewModel();

            // 根据类型设置默认值
            switch (type)
            {
                case "monthly":
                    model.PlanName = "Monthly Plan";
                    model.DurationDays = 30;
                    model.MaxBookings = 8;
                    model.PTSessions = 0;
                    model.Description = "Monthly subscription with class limit";
                    break;

                case "pass":
                    model.PlanName = "Class Pass";
                    model.DurationDays = 0;  // 0 = no expiry
                    model.MaxBookings = 10;
                    model.PTSessions = 0;
                    model.Description = "Pay per class, no expiry";
                    break;

                case "unlimited":
                    model.PlanName = "Unlimited Plan";
                    model.DurationDays = 30;
                    model.MaxBookings = 0;  // 0 = unlimited
                    model.PTSessions = 0;
                    model.Description = "Unlimited classes for 30 days";
                    break;

                case "longterm":
                    model.PlanName = "Long-term Plan";
                    model.DurationDays = 180;
                    model.MaxBookings = 0;  // unlimited
                    model.PTSessions = 0;
                    model.Description = "6 months / 1 year with loyalty discount";
                    model.IncludesInbody = true;
                    model.IncludesGymAccess = true;
                    break;

                case "peak":
                    model.PlanName = "Off-Peak Plan";
                    model.DurationDays = 30;
                    model.MaxBookings = 0;
                    model.PTSessions = 0;
                    model.Description = "Access during off-peak hours";
                    break;

                default: // custom
                    model.PlanName = "Custom Plan";
                    model.DurationDays = 30;
                    model.MaxBookings = 8;
                    break;
            }

            ViewBag.PlanType = type;
            return View(model);
        }

        // GET: /Membership/EditPlan/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditPlan(int id)
        {
            var plan = await _context.MembershipPlans.FindAsync(id);
            if (plan == null)
            {
                return NotFound();
            }

            var model = new MembershipPlanViewModel
            {
                PlanId = plan.PlanId,
                PlanName = plan.PlanName,
                Price = plan.Price,
                DurationDays = plan.DurationDays,
                MaxBookings = plan.MaxBookings,
                PTSessions = plan.PTSessions,
                Description = plan.Description,
                Benefits = plan.Benefits,
                IsActive = plan.IsActive,
                DisplayOrder = plan.DisplayOrder
            };

            return View(model);
        }

        // POST: /Membership/EditPlan/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditPlan(int id, MembershipPlanViewModel model)
        {
            if (id != model.PlanId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var plan = await _context.MembershipPlans.FindAsync(id);
            if (plan == null)
            {
                return NotFound();
            }

            plan.PlanName = model.PlanName;
            plan.Price = model.Price;
            plan.DurationDays = model.DurationDays;
            plan.MaxBookings = model.MaxBookings;
            plan.PTSessions = model.PTSessions;
            plan.Description = model.Description;
            plan.Benefits = model.Benefits;
            plan.IsActive = model.IsActive;
            plan.DisplayOrder = model.DisplayOrder;

            _context.Update(plan);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Plan '{plan.PlanName}' updated successfully!";
            return RedirectToAction(nameof(Plans));
        }

        // POST: /Membership/DeletePlan/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePlan(int id)
        {
            var plan = await _context.MembershipPlans
                .Include(p => p.Subscriptions)
                .FirstOrDefaultAsync(p => p.PlanId == id);

            if (plan == null)
            {
                return NotFound();
            }

            if (plan.Subscriptions != null && plan.Subscriptions.Any())
            {
                TempData["Error"] = $"Cannot delete '{plan.PlanName}' because it has active subscriptions.";
                return RedirectToAction(nameof(Plans));
            }

            var planName = plan.PlanName;
            _context.MembershipPlans.Remove(plan);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Plan '{planName}' deleted successfully!";
            return RedirectToAction(nameof(Plans));
        }

        // ============================================================
        // MEMBER FUNCTIONS - 只有 Member 可以访问
        // ============================================================

        // GET: /Membership/MemberPlans
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> MemberPlans()
        {
            var plans = await _context.MembershipPlans
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                var memberId = int.Parse(userIdClaim);
                var currentSubscription = await _context.MemberSubscriptions
                    .Include(s => s.Plan)
                    .FirstOrDefaultAsync(s => s.UserId == memberId && s.Status == "Active");

                ViewBag.CurrentSubscription = currentSubscription;
            }

            return View(plans);
        }

        // GET: /Membership/Subscribe/5
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> Subscribe(int id)
        {
            var plan = await _context.MembershipPlans.FindAsync(id);
            if (plan == null)
            {
                return NotFound();
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }
            var memberId = int.Parse(userIdClaim);

            var existingSubscription = await _context.MemberSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == memberId && s.Status == "Active");

            if (existingSubscription != null)
            {
                TempData["Error"] = "You already have an active subscription. Please cancel it first.";
                return RedirectToAction("MemberPlans");
            }

            ViewBag.Plan = plan;
            return View(plan);
        }

        // POST: /Membership/Subscribe/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> Subscribe(int id, string confirm)
        {
            var plan = await _context.MembershipPlans.FindAsync(id);
            if (plan == null)
            {
                return NotFound();
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }
            var memberId = int.Parse(userIdClaim);

            var existingSubscription = await _context.MemberSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == memberId && s.Status == "Active");

            if (existingSubscription != null)
            {
                TempData["Error"] = "You already have an active subscription.";
                return RedirectToAction("MemberPlans");
            }

            var subscription = new MemberSubscription
            {
                UserId = memberId,
                PlanId = plan.PlanId,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(plan.DurationDays),
                Status = "Active",
                BookingsUsed = 0,
                PT_Used = 0,
                AutoRenew = false
            };

            _context.MemberSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"You have successfully subscribed to '{plan.PlanName}'!";
            return RedirectToAction("MySubscription");
        }

        // GET: /Membership/MySubscription
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> MySubscription()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }
            var memberId = int.Parse(userIdClaim);

            var subscription = await _context.MemberSubscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.UserId == memberId && s.Status == "Active");

            if (subscription == null)
            {
                var expiredSubscription = await _context.MemberSubscriptions
                    .Include(s => s.Plan)
                    .FirstOrDefaultAsync(s => s.UserId == memberId && s.Status == "Expired");

                ViewBag.ExpiredSubscription = expiredSubscription;
                return View("NoSubscription");
            }

            var daysRemaining = (subscription.EndDate - DateTime.Now).Days;
            var bookingsRemaining = subscription.Plan.MaxBookings == 0
                ? 999
                : subscription.Plan.MaxBookings - subscription.BookingsUsed;

            ViewBag.DaysRemaining = daysRemaining;
            ViewBag.BookingsRemaining = bookingsRemaining;

            return View(subscription);
        }

        // POST: /Membership/CancelSubscription
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> CancelSubscription()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }
            var memberId = int.Parse(userIdClaim);

            var subscription = await _context.MemberSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == memberId && s.Status == "Active");

            if (subscription == null)
            {
                TempData["Error"] = "No active subscription found.";
                return RedirectToAction("MySubscription");
            }

            subscription.Status = "Cancelled";
            subscription.CancelledAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your subscription has been cancelled.";
            return RedirectToAction("MySubscription");
        }
    }
}