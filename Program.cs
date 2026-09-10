using CommunitySportsBooking.Models;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// ADD MVC
// =====================================================

builder.Services.AddControllersWithViews();

// =====================================================
// DATABASE HELPER
// =====================================================

builder.Services.AddScoped<DatabaseHelper>();

// =====================================================
// SESSION SUPPORT
// =====================================================

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// =====================================================
// BUILD APPLICATION
// =====================================================

var app = builder.Build();

// =====================================================
// HTTP PIPELINE
// =====================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// =====================================================
// SESSION MUST COME BEFORE AUTHORIZATION
// =====================================================

app.UseSession();

app.UseAuthorization();

// =====================================================
// DEFAULT ROUTE
// =====================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();