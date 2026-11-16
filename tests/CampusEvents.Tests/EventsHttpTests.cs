using CampusEvents.Data;
using CampusEvents.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using System.Net;
using Xunit;

namespace CampusEvents.Tests;

public class EventsHttpTests
{
	private static FakeTimeProvider FixedClock()
	{
		var clock = new FakeTimeProvider(new DateTimeOffset(2026, 4, 15, 16, 0, 0, TimeSpan.Zero));
		var zone = TimeZoneInfo.CreateCustomTimeZone("CemsHttpUtcMinus4", TimeSpan.FromHours(-4), "CEMS", "CEMS");
		clock.SetLocalTimeZone(zone);
		return clock;
	}

	private static string FindWebProjectRoot()
	{
		var directory = new DirectoryInfo(AppContext.BaseDirectory);
		while (directory != null)
		{
			var candidate = Path.Combine(directory.FullName, "src", "CampusEvents");
			if (File.Exists(Path.Combine(candidate, "CampusEvents.csproj")))
			{
				return candidate;
			}
			directory = directory.Parent;
		}
		throw new DirectoryNotFoundException("Cannot locate CampusEvents/CampusEvents.csproj above test output.");
	}

	private static WebApplicationFactory<Program> CreateHost(IsolatedSqlServerDatabase database)
	{
		return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
		{
			builder.UseEnvironment("Testing");
			builder.UseSetting("ConnectionStrings:DefaultConnection", database.ConnectionString);
			builder.UseContentRoot(FindWebProjectRoot());
			builder.ConfigureTestServices(services =>
			{
				services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
				services.RemoveAll<ApplicationDbContext>();
				services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(database.ConnectionString));
				services.RemoveAll<TimeProvider>();
				services.AddSingleton<TimeProvider>(FixedClock());
			});
		});
	}

	private static async Task SeedPagedCatalogAsync(IsolatedSqlServerDatabase database)
	{
		await using var context = database.CreateContext();
		var category = new Category { Name = "Academic" };
		var organizer = new ApplicationUser { Id = "http-organizer", DisplayName = "HTTP Test Organizer" };
		context.Categories.Add(category);
		context.Users.Add(organizer);
		await context.SaveChangesAsync();
		var start = new DateTime(2026, 4, 20, 12, 0, 0);
		for (var number = 1; number <= 11; number++)
		{
			context.Events.Add(new Event
			{
				Title = $"HTTP Event {number:00}", Description = "HTTP pagination example",
				Location = "Campus", StartDateTime = start.AddDays(number),
				EndDateTime = start.AddDays(number).AddHours(2), Capacity = 50,
				Status = EventStatus.Published, CreatedAt = start.AddDays(-7),
				CategoryId = category.Id, OrganizerId = organizer.Id
			});
		}
		await context.SaveChangesAsync();
	}

	[Theory]
	[InlineData("/Events?page=abc&pageSize=10", HttpStatusCode.OK)]
	[InlineData("/Events?pageSize=abc", HttpStatusCode.BadRequest)]
	[InlineData("/Events?pageSize=0", HttpStatusCode.BadRequest)]
	public async Task Index_HandlesMalformedQueryThroughHttp(string path, HttpStatusCode expectedStatus)
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		await SeedPagedCatalogAsync(database);
		using var host = CreateHost(database);
		using var client = host.CreateClient(new WebApplicationFactoryClientOptions
		{
			AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost")
		});
		using var response = await client.GetAsync(path);
		var html = await response.Content.ReadAsStringAsync();
		Assert.Equal(expectedStatus, response.StatusCode);
		if (expectedStatus == HttpStatusCode.OK)
		{
			Assert.Contains("Page 1 of 2", html);
			Assert.Contains("HTTP Event 01", html);
			Assert.DoesNotContain("HTTP Event 11", html);
		}
		else
		{
			Assert.Contains("Choose a page size of 10, 25, or 50.", html);
			Assert.Matches("(?i)<a\\b[^>]*href=\"/Events\"[^>]*>\\s*Return to Events\\s*</a>", html);
		}
	}
}
