using CivicVoice.Application.Interfaces;
using CivicVoice.Application.Services;
using CivicVoice.Infrastructure.Data;
using CivicVoice.Infrastructure.Repositories;
using CivicVoice.Web.Filters;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Database & EF Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<CivicVoiceDbContext>(options =>
    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("CivicVoice.Infrastructure")));

// 2. ASP.NET Core Identity for Role-Based Access Control
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Simplified requirements for development/evaluation ease
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 4;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<CivicVoiceDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// 3. Distributed Cache & Session (Phase 8: harmless preference storage)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 4. Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IGrievanceRepository, GrievanceRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IWardRepository, WardRepository>();
builder.Services.AddScoped<IAssignmentRepository, AssignmentRepository>();
builder.Services.AddScoped<IStatusHistoryRepository, StatusHistoryRepository>();
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();

// 5. Application Services
builder.Services.AddScoped<IGrievanceService, GrievanceService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IWardService, WardService>();

// 6. Action Filter (Phase 8: audit logging)
builder.Services.AddScoped<AuditLoggingActionFilter>();

// 7. Controllers and Views with Global Audit Filter
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<AuditLoggingActionFilter>();
});

var app = builder.Build();

// 8. Automatic Database Migration and Demonstration Data Seeding
try
{
    await DbInitializer.SeedAsync(app.Services);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred during database migration or seeding.");
}

// 9. HTTP Pipeline Configuration
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Session must precede Authentication & Authorization
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// 10. MVC Routing: Conventional Routes, Route Constraints, and Attribute Routing
// Custom route with constraint (Phase 8: integer grievance ID constraint)
app.MapControllerRoute(
    name: "grievance_details",
    pattern: "complaint/{id:int}",
    defaults: new { controller = "Grievances", action = "Details" });

// Custom conventional route
app.MapControllerRoute(
    name: "grievances_by_status",
    pattern: "grievances/status/{status}",
    defaults: new { controller = "Grievances", action = "Index" });

// Default conventional route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
