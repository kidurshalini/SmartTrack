using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartTrack.Common;
using SmartTrack.Models;

using SmartTrack.Services;

var builder = WebApplication.CreateBuilder(args);


// =========================================================
// MVC
// =========================================================

builder.Services.AddControllersWithViews();


// =========================================================
// HTTP CLIENT
// =========================================================

builder.Services.AddHttpClient();


// =========================================================
// DATABASE
// =========================================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found."
    );

builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
        options.UseSqlServer(connectionString)
);


// =========================================================
// IDENTITY
// =========================================================

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(
        options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequiredLength = 8;

            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan =
                TimeSpan.FromMinutes(15);

            options.User.RequireUniqueEmail = true;
        }
    )
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();


// =========================================================
// OCR FLASK API
// PORT 5000
// =========================================================

builder.Services.AddHttpClient(
    "SmartTrackOCR",
    client =>
    {
        client.BaseAddress =
            new Uri("http://127.0.0.1:5000/");
    }
);


// =========================================================
// AI FLASK API
// PORT 5001
// =========================================================

builder.Services.AddHttpClient<SmartTrackAIService>(
    client =>
    {
        client.BaseAddress =
            new Uri("http://127.0.0.1:5001/");
    }
);
builder.Services.AddScoped<SmartTrackPurchaseHistoryService>();
builder.Services.AddScoped<SmartTrackNotificationService>();
builder.Services.AddScoped<SmartTrackDashboardService>();
builder.Services.AddScoped<ShoppingListService>();
builder.Services.AddScoped<SmartTrackStockBackgroundService>();


builder.Services.AddScoped<SmartTrackStockService>();
// Add services
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
// =========================================================
// SESSION
// =========================================================

builder.Services.AddSession();


// =========================================================
// BUILD APPLICATION
// =========================================================

var app = builder.Build();


// =========================================================
// SEED ROLES
// =========================================================

using (var scope = app.Services.CreateScope())
{
    await SeedData.SeedRole(
        scope.ServiceProvider
    );
}


// =========================================================
// HTTP PIPELINE
// =========================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseSession();

app.UseAuthorization();


// =========================================================
// DEFAULT ROUTE
// =========================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"
);


// =========================================================
// RUN
// =========================================================

app.Run();