using CampusEvents.Data;
using Microsoft.EntityFrameworkCore;

// Services registration

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
	?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

// Supply the system clock to time-dependent application code.
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

var app = builder.Build();

// Middleware configuration

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Routes/endpoints mapping

app.MapStaticAssets();
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}")
	.WithStaticAssets();

if (app.Environment.IsDevelopment())
{
	using var scope = app.Services.CreateScope();
	var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
	var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
	await DbInitializer.SeedAsync(context, timeProvider);
}

app.Run();
