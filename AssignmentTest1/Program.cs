using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using AssignmentTest1.Data;
using AssignmentTest1.Models.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container. 
builder.Services.AddControllersWithViews();

// ✅ Register DbContext with connection string
builder.Services.AddDbContext<FitBookDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
    
// Authentication
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

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<FitBookDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var adminExists = await context.Users.AnyAsync(u => u.Email == "testwebbased465@gmail.com");

        if (!adminExists)
        {
            var admin = new User
            {
                FullName = "Admin User",
                Email = "testwebbased465@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123"),
                Role = "Admin",
                Phone = "012-3456789",
                CreatedAt = DateTime.Now,
                IsLocked = false,
                FailedLoginCount = 0
            };

            context.Users.Add(admin);
            await context.SaveChangesAsync();

            logger.LogInformation("✅ Admin user seeded successfully!");
            logger.LogInformation("   Email: testwebbased465@gmail.com");
            logger.LogInformation("   Password: Admin123");
        }
        else
        {
            logger.LogInformation("ℹ️ Admin user already exists.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error seeding admin user.");
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();