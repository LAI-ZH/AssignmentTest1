using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssignmentTest1.Data;
using AssignmentTest1.Models.Entities;
using AssignmentTest1.Models.ViewModels;

namespace AssignmentTest1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MembershipController : Controller
    {
        private readonly FitBookDbContext _context;

        public MembershipController(FitBookDbContext context)
        {
            _context = context;
        }

        // GET: /Membership/Plans - 套餐列表
        public async Task<IActionResult> Plans()
        {
            var plans = await _context.MembershipPlans
                .OrderBy(p => p.Price)
                .ToListAsync();
            return View(plans);
        }

        // GET: /Membership/CreatePlan - 创建套餐页面
        public IActionResult CreatePlan()
        {
            return View();
        }

        // POST: /Membership/CreatePlan - 创建套餐
        [HttpPost]
        [ValidateAntiForgeryToken]
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
                Description = model.Description,
                IsActive = true
            };

            _context.MembershipPlans.Add(plan);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Plan '{plan.PlanName}' created successfully!";
            return RedirectToAction(nameof(Plans));
        }

        // GET: /Membership/EditPlan/5 - 编辑套餐页面
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
                Description = plan.Description,
                IsActive = plan.IsActive
            };

            return View(model);
        }

        // POST: /Membership/EditPlan/5 - 更新套餐
        [HttpPost]
        [ValidateAntiForgeryToken]
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
            plan.Description = model.Description;
            plan.IsActive = model.IsActive;

            _context.Update(plan);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Plan '{plan.PlanName}' updated successfully!";
            return RedirectToAction(nameof(Plans));
        }

        // POST: /Membership/DeletePlan/5 - 删除套餐
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePlan(int id)
        {
            var plan = await _context.MembershipPlans
                .Include(p => p.Subscriptions)
                .FirstOrDefaultAsync(p => p.PlanId == id);

            if (plan == null)
            {
                return NotFound();
            }

            // 检查是否有订阅
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

        // GET: /Membership/Subscriptions - 所有订阅
        public async Task<IActionResult> Subscriptions()
        {
            var subscriptions = await _context.MemberSubscriptions
                .Include(s => s.User)
                .Include(s => s.Plan)
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();
            return View(subscriptions);
        }

        // GET: /Membership/SubscriptionDetails/5 - 订阅详情
        public async Task<IActionResult> SubscriptionDetails(int id)
        {
            var subscription = await _context.MemberSubscriptions
                .Include(s => s.User)
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.SubscriptionId == id);

            if (subscription == null)
            {
                return NotFound();
            }

            return View(subscription);
        }

        // POST: /Membership/CancelSubscription/5 - 取消订阅
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelSubscription(int id)
        {
            var subscription = await _context.MemberSubscriptions
                .FirstOrDefaultAsync(s => s.SubscriptionId == id);

            if (subscription == null)
            {
                return NotFound();
            }

            subscription.Status = "Cancelled";
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Subscription cancelled successfully!";
            return RedirectToAction(nameof(Subscriptions));
        }
    }
}