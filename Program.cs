using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReliableAcademy.Data;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection connection string is missing.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(
    connectionString,
    sqlServerOptions => sqlServerOptions.MigrationsHistoryTable("RCI_EFMigrationsHistory")));
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Student portal accounts use generated usernames and do not require email addresses.
    options.User.RequireUniqueEmail = false;
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();
if (app.Environment.IsDevelopment()) app.UseMigrationsEndPoint();
else { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("areas", "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

await DbInitializer.SeedRolesAsync(app.Services);
await DbInitializer.SeedAdminAsync(app.Services, builder.Configuration);
app.Run();
