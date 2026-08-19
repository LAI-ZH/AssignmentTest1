using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using AssignmentTest1.Data;
using AssignmentTest1.Models.Entities;
using AssignmentTest1.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register DbContext
builder.Services.AddDbContext<FitBookDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// ✅ 注册 EmailService
builder.Services.AddScoped<EmailService>();

// ✅ 注册 CaptchaService
builder.Services.AddScoped<CaptchaService>();
builder.Services.AddHttpContextAccessor();

// ✅ Add Session (for Captcha)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ✅ Register CaptchaService
builder.Services.AddScoped<CaptchaService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "FitBookAuth";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ============================================================
// SEED DATA
// ============================================================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<FitBookDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // 1. 检查是否有课程
        if (!context.FitnessClasses.Any())
        {
            // 获取 Trainer IDs
            var sarah = await context.Users.FirstOrDefaultAsync(u => u.Email == "sarah@fitbook.com");
            var alex = await context.Users.FirstOrDefaultAsync(u => u.Email == "alex@fitbook.com");
            var mike = await context.Users.FirstOrDefaultAsync(u => u.Email == "mike@fitbook.com");
            var john = await context.Users.FirstOrDefaultAsync(u => u.Email == "john@fitbook.com");
            var lisa = await context.Users.FirstOrDefaultAsync(u => u.Email == "lisa@fitbook.com");

            if (sarah != null && alex != null && mike != null && john != null && lisa != null)
            {
                var classes = new[]
                {
                    new FitnessClass { ClassName = "Yoga for beginner", Category = "Yoga", Description = "Perfect for beginners to learn basic yoga poses", MaxCapacity = 20, TrainerId = sarah.UserId, IsActive = true },
                    new FitnessClass { ClassName = "HIIT Bootcamp", Category = "HIIT", Description = "High-intensity interval training for fat burning", MaxCapacity = 15, TrainerId = alex.UserId, IsActive = true },
                    new FitnessClass { ClassName = "Zumba", Category = "Dance", Description = "Fun dance workout with Latin rhythms", MaxCapacity = 25, TrainerId = mike.UserId, IsActive = true },
                    new FitnessClass { ClassName = "Body Pump", Category = "Strength", Description = "Full body barbell workout", MaxCapacity = 20, TrainerId = john.UserId, IsActive = true },
                    new FitnessClass { ClassName = "Pilates", Category = "Core", Description = "Core strengthening and flexibility", MaxCapacity = 18, TrainerId = lisa.UserId, IsActive = true },
                    new FitnessClass { ClassName = "Yoga Flow", Category = "Yoga", Description = "Vinyasa flow connecting breath with movement", MaxCapacity = 20, TrainerId = sarah.UserId, IsActive = true },
                    new FitnessClass { ClassName = "Boxing", Category = "Boxing", Description = "Cardio boxing workout", MaxCapacity = 15, TrainerId = mike.UserId, IsActive = true },
                    new FitnessClass { ClassName = "Core & Abs", Category = "Core", Description = "Intense core training", MaxCapacity = 15, TrainerId = lisa.UserId, IsActive = true },
                    new FitnessClass { ClassName = "Dance Cardio", Category = "Dance", Description = "High-energy dance workout", MaxCapacity = 25, TrainerId = mike.UserId, IsActive = true },
                    new FitnessClass { ClassName = "Strength Training", Category = "Strength", Description = "Weight training fundamentals", MaxCapacity = 20, TrainerId = john.UserId, IsActive = true },
                    new FitnessClass { ClassName = "Evening Yoga", Category = "Yoga", Description = "Relaxing yoga to end your day", MaxCapacity = 20, TrainerId = sarah.UserId, IsActive = true }
                };

                context.FitnessClasses.AddRange(classes);
                await context.SaveChangesAsync();
                logger.LogInformation("✅ Classes seeded!");
            }
        }

        // 2. 检查是否有课程模板
        if (!context.ClassScheduleTemplates.Any())
        {
            // 获取 Class IDs
            var classMap = await context.FitnessClasses
                .ToDictionaryAsync(c => c.ClassName, c => c.ClassId);

            var templates = new[]
            {
                // 周一
                new ClassScheduleTemplate { ClassId = classMap["Yoga for beginner"], DayOfWeek = "Monday", StartTime = new TimeOnly(7, 0), EndTime = new TimeOnly(8, 0), Venue = "Studio A" },
                new ClassScheduleTemplate { ClassId = classMap["Body Pump"], DayOfWeek = "Monday", StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0), Venue = "Gym" },
                new ClassScheduleTemplate { ClassId = classMap["Core & Abs"], DayOfWeek = "Monday", StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(13, 0), Venue = "Studio B" },
                new ClassScheduleTemplate { ClassId = classMap["HIIT Bootcamp"], DayOfWeek = "Monday", StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(19, 0), Venue = "Gym" },

                // 周二
                new ClassScheduleTemplate { ClassId = classMap["Yoga Flow"], DayOfWeek = "Tuesday", StartTime = new TimeOnly(7, 0), EndTime = new TimeOnly(8, 0), Venue = "Studio A" },
                new ClassScheduleTemplate { ClassId = classMap["Zumba"], DayOfWeek = "Tuesday", StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0), Venue = "Studio B" },
                new ClassScheduleTemplate { ClassId = classMap["Strength Training"], DayOfWeek = "Tuesday", StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(15, 0), Venue = "Gym" },
                new ClassScheduleTemplate { ClassId = classMap["Boxing"], DayOfWeek = "Tuesday", StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(19, 0), Venue = "Boxing Room" },

                // 周三
                new ClassScheduleTemplate { ClassId = classMap["Yoga for beginner"], DayOfWeek = "Wednesday", StartTime = new TimeOnly(7, 0), EndTime = new TimeOnly(8, 0), Venue = "Studio A" },
                new ClassScheduleTemplate { ClassId = classMap["HIIT Bootcamp"], DayOfWeek = "Wednesday", StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0), Venue = "Gym" },
                new ClassScheduleTemplate { ClassId = classMap["Pilates"], DayOfWeek = "Wednesday", StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(13, 0), Venue = "Studio B" },
                new ClassScheduleTemplate { ClassId = classMap["Dance Cardio"], DayOfWeek = "Wednesday", StartTime = new TimeOnly(19, 0), EndTime = new TimeOnly(20, 0), Venue = "Studio B" },

                // 周四
                new ClassScheduleTemplate { ClassId = classMap["Yoga Flow"], DayOfWeek = "Thursday", StartTime = new TimeOnly(7, 0), EndTime = new TimeOnly(8, 0), Venue = "Studio A" },
                new ClassScheduleTemplate { ClassId = classMap["Body Pump"], DayOfWeek = "Thursday", StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0), Venue = "Gym" },
                new ClassScheduleTemplate { ClassId = classMap["Core & Abs"], DayOfWeek = "Thursday", StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(15, 0), Venue = "Studio B" },
                new ClassScheduleTemplate { ClassId = classMap["Zumba"], DayOfWeek = "Thursday", StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(19, 0), Venue = "Studio B" },

                // 周五
                new ClassScheduleTemplate { ClassId = classMap["Yoga for beginner"], DayOfWeek = "Friday", StartTime = new TimeOnly(7, 0), EndTime = new TimeOnly(8, 0), Venue = "Studio A" },
                new ClassScheduleTemplate { ClassId = classMap["Strength Training"], DayOfWeek = "Friday", StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0), Venue = "Gym" },
                new ClassScheduleTemplate { ClassId = classMap["Pilates"], DayOfWeek = "Friday", StartTime = new TimeOnly(16, 0), EndTime = new TimeOnly(17, 0), Venue = "Studio B" },
                new ClassScheduleTemplate { ClassId = classMap["Boxing"], DayOfWeek = "Friday", StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(19, 0), Venue = "Boxing Room" },

                // 周六
                new ClassScheduleTemplate { ClassId = classMap["Yoga for beginner"], DayOfWeek = "Saturday", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(9, 0), Venue = "Studio A" },
                new ClassScheduleTemplate { ClassId = classMap["HIIT Bootcamp"], DayOfWeek = "Saturday", StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0), Venue = "Gym" },
                new ClassScheduleTemplate { ClassId = classMap["Zumba"], DayOfWeek = "Saturday", StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(13, 0), Venue = "Studio B" },

                // 周日
                new ClassScheduleTemplate { ClassId = classMap["Yoga Flow"], DayOfWeek = "Sunday", StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0), Venue = "Studio A" },
                new ClassScheduleTemplate { ClassId = classMap["Pilates"], DayOfWeek = "Sunday", StartTime = new TimeOnly(10, 30), EndTime = new TimeOnly(11, 30), Venue = "Studio B" }
            };

            context.ClassScheduleTemplates.AddRange(templates);
            await context.SaveChangesAsync();
            logger.LogInformation("✅ Class schedule templates seeded!");
        }

        // 3. 生成本周日程
        var today = DateOnly.FromDateTime(DateTime.Now);
        var daysOffset = (int)today.DayOfWeek - 1;
        if (daysOffset < 0) daysOffset = 6;
        var monday = today.AddDays(-daysOffset);

        // 删除本周已有的日程
        var existingSchedules = await context.ClassSchedules
            .Where(s => s.ScheduleDate >= monday && s.ScheduleDate < monday.AddDays(7))
            .ToListAsync();

        if (existingSchedules.Any())
        {
            context.ClassSchedules.RemoveRange(existingSchedules);
            await context.SaveChangesAsync();
        }

        // 从模板生成本周日程
        var templatesList = await context.ClassScheduleTemplates
            .Include(t => t.Class)
            .Where(t => t.IsActive)
            .ToListAsync();

        var dayOfWeekMap = new Dictionary<string, int>
        {
            { "Monday", 0 },
            { "Tuesday", 1 },
            { "Wednesday", 2 },
            { "Thursday", 3 },
            { "Friday", 4 },
            { "Saturday", 5 },
            { "Sunday", 6 }
        };

        var newSchedules = new List<ClassSchedule>();

        foreach (var template in templatesList)
        {
            var dayOffset = dayOfWeekMap[template.DayOfWeek];
            var scheduleDate = monday.AddDays(dayOffset);

            newSchedules.Add(new ClassSchedule
            {
                ClassId = template.ClassId,
                ScheduleDate = scheduleDate,
                StartTime = template.StartTime,
                EndTime = template.EndTime,
                Venue = template.Venue,
                CurrentBookings = 0
            });
        }

        if (newSchedules.Any())
        {
            context.ClassSchedules.AddRange(newSchedules);
            await context.SaveChangesAsync();
            logger.LogInformation($"✅ {newSchedules.Count} schedules generated for this week!");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error seeding data");
    }
}
// ============================================================

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
