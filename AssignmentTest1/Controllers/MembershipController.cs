using AssignmentTest1.Data;
using AssignmentTest1.Models.Entities;
using AssignmentTest1.Models.ViewModels;
using AssignmentTest1.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Numerics;
using System.Security.Claims;

namespace AssignmentTest1.Controllers
{
    public class MembershipController : Controller
    {
        private readonly FitBookDbContext _context;
        private readonly ILogger<MembershipController> _logger;

        public MembershipController(FitBookDbContext context, ILogger<MembershipController> logger)
        {
            _context = context;
            _logger = logger;
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
                    .ThenInclude(s => s.User)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();

            // ✅ 为每个计划统计活跃数和不同用户数
            var stats = new Dictionary<int, (int Active, int UniqueUsers, int Total)>();
            foreach (var plan in plans)
            {
                var subscriptions = plan.Subscriptions ?? new List<MemberSubscription>();

                var active = subscriptions.Count(s => s.Status == "Active");

                // ✅ 关键：使用 Select(s => s.UserId).Distinct().Count() 去重
                var uniqueUsers = subscriptions
                    .Select(s => s.UserId)
                    .Distinct()
                    .Count();

                var total = subscriptions.Count;

                stats[plan.PlanId] = (active, uniqueUsers, total);
            }
            ViewBag.PlanStats = stats;

            return View(plans);
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
                    model.DurationDays = 0;
                    model.MaxBookings = 10;
                    model.PTSessions = 0;
                    model.Description = "Pay per class, no expiry";
                    break;

                case "unlimited":
                    model.PlanName = "Unlimited Plan";
                    model.DurationDays = 30;
                    model.MaxBookings = 0;
                    model.PTSessions = 0;
                    model.Description = "Unlimited classes for 30 days";
                    break;

                case "longterm":
                    model.PlanName = "Long-term Plan";
                    model.DurationDays = 180;
                    model.MaxBookings = 0;
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

                default:
                    model.PlanName = "Custom Plan";
                    model.DurationDays = 30;
                    model.MaxBookings = 8;
                    break;
            }

            ViewBag.PlanType = type;
            return View(model);
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
                IncludesInbody = model.IncludesInbody,
                IncludesGymAccess = model.IncludesGymAccess,
                Excludes = model.Excludes,
                LoyaltyDiscount = model.LoyaltyDiscount,
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
                IncludesInbody = plan.IncludesInbody,
                IncludesGymAccess = plan.IncludesGymAccess,
                Excludes = plan.Excludes,
                LoyaltyDiscount = plan.LoyaltyDiscount,
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
            plan.IncludesInbody = model.IncludesInbody;
            plan.IncludesGymAccess = model.IncludesGymAccess;
            plan.Excludes = model.Excludes;
            plan.LoyaltyDiscount = model.LoyaltyDiscount;
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

        // GET: /Membership/PlanSubscribers/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PlanSubscribers(int id)
        {
            var plan = await _context.MembershipPlans
                .Include(p => p.Subscriptions)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(p => p.PlanId == id);

            if (plan == null)
            {
                return NotFound();
            }

            return View(plan);
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

            // ✅ 计算剩余次数
            int bookingsRemaining = 0;
            if (subscription.Plan.MaxBookings > 0)
            {
                // 当前周期内已使用的预订数
                var usedBookings = await _context.Bookings
                    .Where(b => b.MemberId == memberId && (b.Status == "Confirmed" || b.Status == "Attended"))
                    .Where(b => b.Schedule != null && b.Schedule.ScheduleDate >= DateOnly.FromDateTime(subscription.StartDate)
                             && b.Schedule.ScheduleDate <= DateOnly.FromDateTime(subscription.EndDate))
                    .CountAsync();

                bookingsRemaining = subscription.Plan.MaxBookings - usedBookings;
                if (bookingsRemaining < 0) bookingsRemaining = 0;
            }
            else
            {
                bookingsRemaining = 999; // 无限
            }

            ViewBag.DaysRemaining = daysRemaining > 0 ? daysRemaining : 0;
            ViewBag.BookingsRemaining = bookingsRemaining > 0 ? bookingsRemaining : 0;

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

        // ============================================================
        // PAYMENT FUNCTIONS
        // ============================================================

        // GET: /Membership/MakePayment/5
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> MakePayment(int id)
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

            // ✅ 保存 PlanId 到 TempData（供 QR 扫描使用）
            TempData["QRPlanId"] = plan.PlanId;

            var qrData = $"{plan.PlanName}|{plan.Price}";
            var qrCodeImage = GenerateQRCodeImage(qrData);
            ViewBag.QRCodeImage = qrCodeImage;

            var model = new PaymentViewModel
            {
                PlanId = plan.PlanId,
                PlanName = plan.PlanName,
                Price = plan.Price,
                DurationDays = plan.DurationDays,
                MaxBookings = plan.MaxBookings,
                PTSessions = plan.PTSessions,
                Benefits = plan.Benefits,
                Description = plan.Description,
                IncludesGymAccess = plan.IncludesGymAccess,
                IncludesInbody = plan.IncludesInbody
            };

            ViewBag.Plan = plan;
            return View(model);
        }

        // GET: /Membership/QRPaymentScan
        [Authorize(Roles = "Member")]
        public IActionResult QRPaymentScan()
        {
            // ✅ 从 TempData 获取 PlanId
            var planId = TempData["QRPlanId"] as int?;
            if (planId == null || planId == 0)
            {
                // 如果 TempData 没有，尝试从 Session 或默认
                planId = 1; // 或者返回错误
            }

            ViewBag.PlanId = planId;
            return View();
        }

        // POST: /Membership/MakePayment - 处理支付
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> MakePayment(PaymentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // 如果验证失败，重新加载配套信息
                var planReload = await _context.MembershipPlans.FindAsync(model.PlanId);
                if (planReload != null)
                {
                    model.PlanName = planReload.PlanName;
                    model.Price = planReload.Price;
                    model.DurationDays = planReload.DurationDays;
                    model.MaxBookings = planReload.MaxBookings;
                    model.PTSessions = planReload.PTSessions;
                    model.Benefits = planReload.Benefits;
                    model.Description = planReload.Description;
                    model.IncludesGymAccess = planReload.IncludesGymAccess;
                    model.IncludesInbody = planReload.IncludesInbody;
                }
                return View(model);
            }

            var plan = await _context.MembershipPlans.FindAsync(model.PlanId);
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

            // 创建订阅
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

            // 生成交易号
            var transactionId = GenerateTransactionId();
            var qrCodeData = GenerateQRCodeData();

            // 生成 QR Code 图片
            string? qrCodeImageBase64 = null;
            if (model.PaymentMethod == "QR")
            {
                qrCodeImageBase64 = GenerateQRCodeImage($"{qrCodeData}|{transactionId}|{plan.PlanName}|{plan.Price}");
                Console.WriteLine($"QR Code Generated: {qrCodeImageBase64 != null}");
            }

            // 创建支付记录
            var payment = new Payment
            {
                UserId = memberId,
                SubscriptionId = subscription.SubscriptionId,
                PlanId = plan.PlanId,
                Amount = plan.Price,
                PaymentMethod = model.PaymentMethod,
                Status = "Pending",
                TransactionId = transactionId,
                PaymentDate = DateTime.Now,
                CardLastFour = !string.IsNullOrEmpty(model.CardNumber) && model.CardNumber.Replace(" ", "").Length >= 4
                    ? model.CardNumber.Replace(" ", "").Substring(model.CardNumber.Replace(" ", "").Length - 4)
                    : null,
                CardType = model.PaymentMethod == "Card" ? "Visa" : null,
                QRCodeData = qrCodeData,
                QRCodeImage = qrCodeImageBase64
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // 更新订阅的 PaymentId
            subscription.PaymentId = payment.PaymentId;
            _context.Update(subscription);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Payment successful! You have subscribed to '{plan.PlanName}'.";
            return RedirectToAction("PaymentSuccess", new { id = payment.PaymentId });
        }

        // POST: /Membership/ConfirmQRPayment
        [HttpPost]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> ConfirmQRPayment([FromBody] QRPaymentConfirmModel model)
        {
            try
            {
                // 1. 基础校验
                if (model == null)
                {
                    _logger.LogWarning("ConfirmQRPayment called with null model");
                    return Json(new { success = false, message = "Invalid request data." });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    _logger.LogWarning("ConfirmQRPayment called with unauthenticated user");
                    return Json(new { success = false, message = "User not logged in." });
                }
                var memberId = int.Parse(userIdClaim);
                _logger.LogInformation($"ConfirmQRPayment started for MemberId: {memberId}, TransactionId: {model.TransactionId}");

                // 2. 检查是否已有活跃订阅
                var existingSubscription = await _context.MemberSubscriptions
                    .FirstOrDefaultAsync(s => s.UserId == memberId && s.Status == "Active");
                if (existingSubscription != null)
                {
                    _logger.LogWarning($"Member {memberId} already has active subscription");
                    return Json(new { success = false, message = "You already have an active subscription." });
                }

                // 3. 获取配套信息（从 model 或数据库）
                MembershipPlan? plan = null;

                // 如果 TransactionId 为空，生成一个
                if (string.IsNullOrEmpty(model?.TransactionId))
                {
                    model.TransactionId = $"QR-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 20);
                }

                // 尝试根据 TransactionId 查找已有的 Payment（如果已经存在则复用）
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.TransactionId == model.TransactionId);

                if (payment != null)
                {
                    // 如果已存在支付记录，获取其 Plan
                    if (payment.PlanId.HasValue)
                    {
                        plan = await _context.MembershipPlans.FindAsync(payment.PlanId.Value);
                    }
                    if (plan == null)
                    {
                        _logger.LogError($"Plan not found for existing payment {payment.PaymentId}");
                        return Json(new { success = false, message = "Plan information missing." });
                    }
                }
                else
                {
                    // 新支付：必须提供 PlanName
                    if (string.IsNullOrEmpty(model?.PlanName))
                    {
                        _logger.LogWarning("ConfirmQRPayment called without PlanName and no existing payment");
                        return Json(new { success = false, message = "Plan name is required." });
                    }

                    plan = await _context.MembershipPlans
                        .FirstOrDefaultAsync(p => p.PlanName == model.PlanName && p.IsActive);

                    if (plan == null)
                    {
                        _logger.LogWarning($"Plan not found: {model.PlanName}");
                        return Json(new { success = false, message = "Plan not found." });
                    }
                }

                // 4. 使用事务确保数据一致性
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 5. 创建 Subscription（必须先创建，以获取 SubscriptionId）
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
                    await _context.SaveChangesAsync(); // 生成 SubscriptionId

                    _logger.LogInformation($"Subscription created: {subscription.SubscriptionId}");

                    // 6. 创建或更新 Payment
                    if (payment == null)
                    {
                        payment = new Payment
                        {
                            UserId = memberId,
                            SubscriptionId = subscription.SubscriptionId, // ✅ 现在有值了
                            PlanId = plan.PlanId,
                            Amount = plan.Price,
                            PaymentMethod = "QR",
                            Status = "Successful", // QR 支付假设已成功
                            TransactionId = model.TransactionId,
                            PaymentDate = DateTime.Now
                        };
                        _context.Payments.Add(payment);
                    }
                    else
                    {
                        // 更新已有支付记录（理论上不应发生，但以防万一）
                        payment.SubscriptionId = subscription.SubscriptionId;
                        payment.Status = "Successful";
                        payment.PaymentDate = DateTime.Now;
                        _context.Payments.Update(payment);
                    }

                    await _context.SaveChangesAsync();

                    // 7. 更新 Subscription 的 PaymentId（双向关联）
                    subscription.PaymentId = payment.PaymentId;
                    _context.MemberSubscriptions.Update(subscription);
                    await _context.SaveChangesAsync();

                    // 8. 提交事务
                    await transaction.CommitAsync();

                    _logger.LogInformation($"QR payment confirmed successfully: PaymentId={payment.PaymentId}, SubscriptionId={subscription.SubscriptionId}");

                    return Json(new
                    {
                        success = true,
                        paymentId = payment.PaymentId,
                        subscriptionId = subscription.SubscriptionId,
                        transactionId = payment.TransactionId
                    });
                }
                catch (Exception ex)
                {
                    // 回滚事务
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, $"Error confirming QR payment for MemberId: {memberId}");
                    return Json(new { success = false, message = "An error occurred while processing payment. Please try again." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ConfirmQRPayment");
                return Json(new { success = false, message = "An unexpected error occurred." });
            }
        }

        // POST: /Membership/CreateSubscriptionFromQR
        [HttpPost]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> CreateSubscriptionFromQR([FromBody] QRSubscriptionModel model)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Json(new { success = false, message = "User not logged in." });
                }
                var memberId = int.Parse(userIdClaim);

                // ✅ 检查是否已有活跃订阅
                var existingSubscription = await _context.MemberSubscriptions
                    .FirstOrDefaultAsync(s => s.UserId == memberId && s.Status == "Active");

                if (existingSubscription != null)
                {
                    return Json(new { success = false, message = "You already have an active subscription." });
                }

                // ✅ 获取配套
                var plan = await _context.MembershipPlans.FindAsync(model.PlanId);
                if (plan == null)
                {
                    return Json(new { success = false, message = "Plan not found." });
                }

                // ✅ 创建订阅
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

                // ✅ 创建支付记录
                var transactionId = $"QR-{DateTime.Now:yyyyMMddHHmmss}";
                var payment = new Payment
                {
                    UserId = memberId,
                    SubscriptionId = subscription.SubscriptionId,
                    PlanId = plan.PlanId,
                    Amount = plan.Price,
                    PaymentMethod = "QR",
                    Status = "Successful",
                    TransactionId = transactionId,
                    PaymentDate = DateTime.Now
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // ✅ 更新订阅的 PaymentId
                subscription.PaymentId = payment.PaymentId;
                await _context.SaveChangesAsync();

                return Json(new { success = true, transactionId = transactionId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: /Membership/PaymentSuccess/5
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> PaymentSuccess(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Subscription)
                    .ThenInclude(s => s.Plan)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // GET: /Membership/QRPaymentSuccess
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> QRPaymentSuccess(string transactionId)
        {
            ViewBag.TransactionId = transactionId;

            // ✅ 查找最近的支付记录
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                var memberId = int.Parse(userIdClaim);
                var payment = await _context.Payments
                    .Include(p => p.Plan)
                    .Include(p => p.Subscription)
                    .Where(p => p.UserId == memberId && p.Status == "Successful")
                    .OrderByDescending(p => p.PaymentDate)
                    .FirstOrDefaultAsync();

                if (payment != null)
                {
                    ViewBag.PlanName = payment.Plan?.PlanName ?? "N/A";
                    ViewBag.Amount = payment.Amount;
                    ViewBag.PaymentId = payment.PaymentId;

                    // ✅ 获取 Expiry Date
                    if (payment.Subscription != null)
                    {
                        ViewBag.ExpiryDate = payment.Subscription.EndDate.ToString("dd/MM/yyyy");
                    }
                    else
                    {
                        ViewBag.ExpiryDate = DateTime.Now.AddDays(payment.Plan?.DurationDays ?? 30).ToString("dd/MM/yyyy");
                    }
                }
            }

            return View();
        }

        // ============================================================
        // PDF RECEIPT FUNCTIONS
        // ============================================================

        // GET: /Membership/DownloadReceipt/5
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> DownloadReceipt(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Plan)
                .Include(p => p.Subscription)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null)
            {
                return NotFound();
            }

            // 验证当前用户
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || int.Parse(userIdClaim) != payment.UserId)
            {
                return Forbid();
            }

            // ✅ 获取 Expiry Date（从 Subscription 获取 EndDate）
            DateTime? expiryDate = null;
            if (payment.Subscription != null)
            {
                expiryDate = payment.Subscription.EndDate;
            }
            else
            {
                // 如果没有 Subscription，用 DurationDays 计算
                expiryDate = DateTime.Now.AddDays(payment.Plan.DurationDays);
            }

            var pdfService = new PDFReceiptService();
            var pdfBytes = pdfService.GeneratePaymentReceipt(payment, payment.User, payment.Plan);

            var fileName = $"Receipt_{payment.TransactionId}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        // GET: /Membership/DownloadBookingReceipt/5
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> DownloadBookingReceipt(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Member)
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Class)
                        .ThenInclude(c => c.Trainer)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            // 验证当前用户
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || int.Parse(userIdClaim) != booking.MemberId)
            {
                return Forbid();
            }

            var pdfService = new PDFReceiptService();
            var pdfBytes = pdfService.GenerateBookingReceipt(
                booking,
                booking.Member,
                booking.Schedule.Class,
                booking.Schedule
            );

            var fileName = $"Booking_#{booking.BookingId}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        // ============================================================
        // HELPER METHODS
        // ============================================================

        // Helper: 生成交易号
        private string GenerateTransactionId()
        {
            return $"PAY-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }

        // Helper: 生成 QR Code 数据
        private string GenerateQRCodeData()
        {
            return $"FITBOOK-PAY-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";
        }

        // Helper: 生成 QR Code 图片（返回 Base64 字符串）
        private string? GenerateQRCodeImage(string data)
        {
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new QRCode(qrCodeData);
                using var qrBitmap = qrCode.GetGraphic(20);
                using var ms = new MemoryStream();
                qrBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return Convert.ToBase64String(ms.ToArray());
            }
            catch
            {
                return null;
            }
        }
    }
}