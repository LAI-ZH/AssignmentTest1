using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssignmentTest1.Data;            
using AssignmentTest1.Models.Entities;
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
        // GET: /Membership/MemberPlans - 会员配套列表
        [Authorize(Roles = "Member")]
public async Task<IActionResult> MemberPlans()
{
    var plans = await _context.MembershipPlans
        .Where(p => p.IsActive)
        .OrderBy(p => p.DisplayOrder)
        .ToListAsync();

    // 获取会员当前订阅
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

// GET: /Membership/Subscribe/5 - 会员选择配套
[Authorize(Roles = "Member")]
public async Task<IActionResult> Subscribe(int id)
{
    var plan = await _context.MembershipPlans.FindAsync(id);
    if (plan == null)
    {
        return NotFound();
    }

    // 检查是否已有活跃订阅
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

// POST: /Membership/Subscribe/5 - 确认购买配套
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

    // 再次检查是否有活跃订阅
    var existingSubscription = await _context.MemberSubscriptions
        .FirstOrDefaultAsync(s => s.UserId == memberId && s.Status == "Active");

    if (existingSubscription != null)
    {
        TempData["Error"] = "You already have an active subscription.";
        return RedirectToAction("MemberPlans");
    }

    // 创建订阅
    var subscription = new MemberSubscription
    {
        UserId = memberId,
        PlanId = plan.PlanId,
        StartDate = DateTime.Now,
        EndDate = DateTime.Now.AddDays(plan.DurationDays),
        Status = "Active",
        BookingsUsed = 0,
        PT_Used = 0
    };

    _context.MemberSubscriptions.Add(subscription);
    await _context.SaveChangesAsync();

    TempData["Success"] = $"You have successfully subscribed to '{plan.PlanName}'!";
    return RedirectToAction("MySubscription");
}

// GET: /Membership/MySubscription - 我的配套
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
        // 检查是否有过期的订阅
        var expiredSubscription = await _context.MemberSubscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.UserId == memberId && s.Status == "Expired");

        ViewBag.ExpiredSubscription = expiredSubscription;
        return View("NoSubscription");
    }

    // 计算剩余天数
    var daysRemaining = (subscription.EndDate - DateTime.Now).Days;

    // 计算剩余次数
    var bookingsRemaining = subscription.Plan.MaxBookings == 0
        ? 999
        : subscription.Plan.MaxBookings - subscription.BookingsUsed;

    ViewBag.DaysRemaining = daysRemaining;
    ViewBag.BookingsRemaining = bookingsRemaining;

    return View(subscription);
}

// POST: /Membership/CancelSubscription - 取消订阅
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