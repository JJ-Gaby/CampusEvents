using CampusEvents.Data;
using CampusEvents.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
	?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
	options.User.RequireUniqueEmail = true;
	options.Password.RequiredLength = 8;
	options.Password.RequireDigit = true;
	options.Password.RequireLowercase = true;
	options.Password.RequireUppercase = true;
	options.Password.RequireNonAlphanumeric = false;
	options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
	options.LoginPath = "/Account/Login";
	options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

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

app.MapStaticAssets();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();

if (app.Environment.IsDevelopment())
{
	using var scope = app.Services.CreateScope();
	var services = scope.ServiceProvider;
	var context = services.GetRequiredService<ApplicationDbContext>();
	await DbInitializer.SeedAsync(context, services.GetRequiredService<TimeProvider>());
	await IdentitySeedData.SeedAsync(services, builder.Configuration);
}

app.Run();

// Allows the existing integration-test project to host the real application.
public partial class Program { }
