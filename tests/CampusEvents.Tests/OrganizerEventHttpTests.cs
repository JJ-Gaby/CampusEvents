using System.Net;
using System.Text.RegularExpressions;
using CampusEvents.Data;
using CampusEvents.Models;
using CampusEvents.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace CampusEvents.Tests;

public class OrganizerEventHttpTests
{
	private const string OwnerEmail = "owner@example.test";
	private const string OtherEmail = "other@example.test";
	private const string ParticipantEmail = "participant@example.test";
	private const string Password = "CampusTestPassword1!";

	private static FakeTimeProvider FixedClock()
	{
		var clock = new FakeTimeProvider(new DateTimeOffset(2026, 4, 15, 16, 0, 0, TimeSpan.Zero));
		clock.SetLocalTimeZone(TimeZoneInfo.CreateCustomTimeZone("CemsFixedUtcMinus4", TimeSpan.FromHours(-4),
			"CEMS fixed UTC-04 test zone", "CEMS fixed UTC-04 test zone"));
		return clock;
	}

	private static WebApplicationFactory<Program> CreateHost(IsolatedSqlServerDatabase database, FakeTimeProvider clock)
	{
		return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
		{
			builder.UseEnvironment("Testing");
			builder.UseSetting("ConnectionStrings:DefaultConnection", database.ConnectionString);

			// Resolve the application content root for the integration-test host.
			string solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
			builder.UseContentRoot(Path.Combine(solutionRoot, "src", "CampusEvents"));
			builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
				{
					["ConnectionStrings:DefaultConnection"] = database.ConnectionString
				}));
			builder.ConfigureTestServices(services =>
			{
				services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
				services.RemoveAll<ApplicationDbContext>();
				services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(database.ConnectionString));
				services.RemoveAll<TimeProvider>();
				services.AddSingleton<TimeProvider>(clock);
			});
		});
	}

	private static HttpClient Browser(WebApplicationFactory<Program> host)
	{
		return host.CreateClient(new WebApplicationFactoryClientOptions
		{
			AllowAutoRedirect = false,
			HandleCookies = true,
			BaseAddress = new Uri("https://localhost")
		});
	}

	private static Uri Location(HttpResponseMessage response)
	{
		Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
		var raw = Assert.IsType<Uri>(response.Headers.Location);
		var destination = raw.IsAbsoluteUri
			? raw : new Uri(new Uri("https://localhost"), raw);
		Assert.Equal("https", destination.Scheme);
		Assert.Equal("localhost", destination.Host);
		return destination;
	}

	private static async Task<string> FormTokenAsync(HttpClient browser, string path)
	{
		var response = await browser.GetAsync(path);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var html = await response.Content.ReadAsStringAsync();
		var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
		Assert.True(match.Success, "Expected the server-rendered antiforgery token.");
		return WebUtility.HtmlDecode(match.Groups[1].Value);
	}

	private static async Task SignInAsync(HttpClient browser, string email)
	{
		var token = await FormTokenAsync(browser, "/Account/Login");
		var post = new Dictionary<string, string>
		{
			["Email"] = email,
			["Password"] = Password,
			["ReturnUrl"] = "/Events/Manage",
			["__RequestVerificationToken"] = token
		};
		var response = await browser.PostAsync("/Account/Login", new FormUrlEncodedContent(post));
		Assert.Equal("/Events/Manage", Location(response).AbsolutePath);
		Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
		Assert.Contains(".AspNetCore.Identity.Application", string.Join(";", cookies!));
	}

	private static async Task<(string ownerId, string otherId)> AccountsAsync(WebApplicationFactory<Program> host)
	{
		using var scope = host.Services.CreateScope();
		var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
		var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
		var role = await roles.CreateAsync(new IdentityRole(RoleNames.Organizer));
		Assert.True(role.Succeeded);

		async Task<ApplicationUser> Add(string email, bool organizer)
		{
			var user = new ApplicationUser
			{
				UserName = email, Email = email, DisplayName = email
			};
			var created = await users.CreateAsync(user, Password);
			Assert.True(created.Succeeded);
			if (organizer)
			{
				var assigned = await users.AddToRoleAsync(user, RoleNames.Organizer);
				Assert.True(assigned.Succeeded);
			}
			return user;
		}

		var owner = await Add(OwnerEmail, true);
		var other = await Add(OtherEmail, true);
		await Add(ParticipantEmail, false); // Authenticated baseline: no Participant role.
		return (owner.Id, other.Id);
	}

	private static async Task<int> CategoryAsync(IsolatedSqlServerDatabase database)
	{
		await using var db = database.CreateContext();
		var category = new Category { Name = "Academic" };
		db.Categories.Add(category);
		await db.SaveChangesAsync();
		return category.Id;
	}

	private static async Task<int> EventAsync(
		IsolatedSqlServerDatabase database, int categoryId,
		string ownerId, string title, EventStatus status)
	{
		await using var db = database.CreateContext();
		var item = new Event
		{
			Title = title, Description = "Existing description",
			Location = "Library Room 7", CategoryId = categoryId,
			StartDateTime = new DateTime(2026, 5, 1, 10, 0, 0),
			EndDateTime = new DateTime(2026, 5, 1, 12, 0, 0),
			Capacity = 30, CreatedAt = new DateTime(2026, 4, 1, 9, 0, 0),
			OrganizerId = ownerId, Status = status
		};
		db.Events.Add(item);
		await db.SaveChangesAsync();
		return item.Id;
	}

	private static Dictionary<string, string> ValidFields(int categoryId, string token)
	{
		return new Dictionary<string, string>
		{
			["Title"] = "Robotics Workshop",
			["Description"] = "Hands-on lab",
			["CategoryId"] = categoryId.ToString(),
			["Location"] = "Engineering Hall",
			["StartDateTime"] = "2026-05-08T10:00",
			["EndDateTime"] = "2026-05-08T12:00",
			["Capacity"] = "40",
			["__RequestVerificationToken"] = token
		};
	}

	private static async Task<HttpResponseMessage> SendFormAsync(
		HttpClient browser, string path, Dictionary<string, string> fields)
	{
		return await browser.PostAsync(path, new FormUrlEncodedContent(fields));
	}

	private static async Task<Event> ReadEventAsync(IsolatedSqlServerDatabase database, int id)
	{
		await using var db = database.CreateContext();
		return await db.Events.AsNoTracking().SingleAsync(e => e.Id == id);
	}

	private static string[] EventFields(Event item) => new[]
	{
		item.Title, item.Description, item.Location,
		item.CategoryId.ToString(), item.StartDateTime.ToString("O"),
		item.EndDateTime.ToString("O"), item.Capacity.ToString(),
		item.OrganizerId, item.Status.ToString(), item.CreatedAt.ToString("O")
	};

	[Fact]
	public async Task Manage_RealRoleAndOwnerScoping_IsEnforced()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		var (ownerId, otherId) = await AccountsAsync(host);
		var category = await CategoryAsync(database);
		await EventAsync(database, category, ownerId, "My Draft", EventStatus.Draft);
		await EventAsync(database, category, ownerId, "My Published", EventStatus.Published);
		await EventAsync(database, category, ownerId, "My Cancelled", EventStatus.Cancelled);
		await EventAsync(database, category, otherId, "Private Rival Draft", EventStatus.Draft);
		using var anonymous = Browser(host);
		Assert.Equal("/Account/Login", Location(await anonymous.GetAsync("/Events/Manage")).AbsolutePath);

		using var participant = Browser(host);
		await SignInAsync(participant, ParticipantEmail);
		// For this conventional MVC page, cookie authorization redirects to AccessDenied.
		var denied = Location(await participant.GetAsync("/Events/Manage"));
		Assert.Equal("/Account/AccessDenied", denied.AbsolutePath);

		using var organizer = Browser(host);
		await SignInAsync(organizer, OwnerEmail);
		var result = await organizer.GetAsync("/Events/Manage");
		Assert.Equal(HttpStatusCode.OK, result.StatusCode);
		var html = await result.Content.ReadAsStringAsync();
		Assert.Contains("My Draft", html);
		Assert.Contains("My Published", html);
		Assert.Contains("My Cancelled", html);
		Assert.DoesNotContain("Private Rival Draft", html);
	}

	[Fact]
	public async Task Create_ValidPost_ForcesAuthenticatedOwnerAndDraft()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		var clock = FixedClock();
		using var host = CreateHost(database, clock);
		var (ownerId, otherId) = await AccountsAsync(host);
		var category = await CategoryAsync(database);
		var existing = await EventAsync(database, category, otherId, "Unrelated Event", EventStatus.Draft);
		var before = EventFields(await ReadEventAsync(database, existing));
		using var browser = Browser(host);
		await SignInAsync(browser, OwnerEmail);
		var fields = ValidFields(category, await FormTokenAsync(browser, "/Events/Create"));
		fields["OrganizerId"] = otherId; // Forged fields must not bind to EventInputModel.
		fields["Status"] = "Published";
		fields["CreatedAt"] = "2000-01-01T00:00:00";
		var result = await SendFormAsync(browser, "/Events/Create", fields);
		Assert.Equal("/Events/Manage", Location(result).AbsolutePath); // PRG
		await using var db = database.CreateContext();
		var created = await db.Events.AsNoTracking()
			.SingleAsync(e => e.Title == "Robotics Workshop");
		Assert.Equal(ownerId, created.OrganizerId);
		Assert.Equal(EventStatus.Draft, created.Status);
		Assert.Equal(clock.GetLocalNow().DateTime, created.CreatedAt);
		Assert.Equal(40, created.Capacity);
		Assert.Equal(category, created.CategoryId);
		Assert.Equal(2, await db.Events.CountAsync());
		Assert.Equal(before, EventFields(await ReadEventAsync(database, existing)));
	}

	[Theory]
	[InlineData("required")]
	[InlineData("whitespace")]
	[InlineData("time")]
	[InlineData("capacity")]
	[InlineData("category")]
	public async Task Create_InvalidInput_RedisplaysFormWithoutWrite(string invalid)
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		var (ownerId, _) = await AccountsAsync(host);
		var category = await CategoryAsync(database);
		var sentinel = await EventAsync(database, category, ownerId, "Stable Existing Draft", EventStatus.Draft);
		var snapshot = EventFields(await ReadEventAsync(database, sentinel));
		using var browser = Browser(host);
		await SignInAsync(browser, OwnerEmail);
		var fields = ValidFields(category, await FormTokenAsync(browser, "/Events/Create"));
		switch (invalid)
		{
			case "required": fields["Title"] = ""; break;
			case "whitespace": fields["Title"] = "   "; break;
			case "time": fields["EndDateTime"] = "2026-05-08T09:00"; break;
			case "capacity": fields["Capacity"] = "0"; break;
			case "category": fields["CategoryId"] = "999999"; break;
		}
		var result = await SendFormAsync(browser, "/Events/Create", fields);
		Assert.Equal(HttpStatusCode.OK, result.StatusCode);
		var html = await result.Content.ReadAsStringAsync();
		Assert.Contains("Engineering Hall", html); // Submitted valid input retained.
		Assert.Contains("Academic", html); // Options rebuilt.
		Assert.Contains("validation-summary", html);
		Assert.Contains(invalid switch
		{
			"required" => "Title",
			"whitespace" => "Title",
			"time" => "End date and time must be after",
			"capacity" => "Capacity must be greater than zero",
			_ => "Select an existing Category"
		}, html);
		await using var db = database.CreateContext();
		Assert.Equal(1, await db.Events.CountAsync());
		Assert.Equal(snapshot, EventFields(await ReadEventAsync(database, sentinel)));
	}

	[Theory]
	[InlineData("foreign")]
	[InlineData("published")]
	[InlineData("cancelled")]
	public async Task Edit_ForeignOrIneligibleEvent_DoesNotMutate(string kind)
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		var (ownerId, otherId) = await AccountsAsync(host);
		var category = await CategoryAsync(database);
		var id = await EventAsync(database, category,
			kind == "foreign" ? otherId : ownerId,
			"Protected Original Event",
			kind == "published" ? EventStatus.Published :
			kind == "cancelled" ? EventStatus.Cancelled : EventStatus.Draft);
		var snapshot = EventFields(await ReadEventAsync(database, id));
		using var browser = Browser(host);
		await SignInAsync(browser, OwnerEmail);
		var get = await browser.GetAsync($"/Events/Edit/{id}");
		Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
		// A valid token comes from an authorized GET; the altered route cannot bypass the POST query.
		var fields = ValidFields(category, await FormTokenAsync(browser, "/Events/Create"));
		fields["Title"] = "Forged Update";
		fields["OrganizerId"] = ownerId;
		fields["Status"] = "Draft";
		var post = await SendFormAsync(browser, $"/Events/Edit/{id}", fields);
		Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
		Assert.Equal(snapshot, EventFields(await ReadEventAsync(database, id)));
	}

	[Fact]
	public async Task Edit_OwnedDraft_ValidAndInvalidPosts_PreserveBoundaries()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		var (ownerId, otherId) = await AccountsAsync(host);
		var category = await CategoryAsync(database);
		var id = await EventAsync(database, category, ownerId, "Original Draft", EventStatus.Draft);
		var unrelated = await EventAsync(database, category, otherId, "Other Draft", EventStatus.Draft);
		var unrelatedBefore = EventFields(await ReadEventAsync(database, unrelated));
		var initial = await ReadEventAsync(database, id);
		using var browser = Browser(host);
		await SignInAsync(browser, OwnerEmail);
		var token = await FormTokenAsync(browser, $"/Events/Edit/{id}");
		var fields = ValidFields(category, token);
		fields["Title"] = "Saved Draft Update";
		fields["OrganizerId"] = otherId;
		fields["Status"] = "Published";
		var saved = await SendFormAsync(browser, $"/Events/Edit/{id}", fields);
		Assert.Equal("/Events/Manage", Location(saved).AbsolutePath);
		var persisted = await ReadEventAsync(database, id);
		Assert.Equal("Saved Draft Update", persisted.Title);
		Assert.Equal(ownerId, persisted.OrganizerId);
		Assert.Equal(EventStatus.Draft, persisted.Status);
		Assert.Equal(initial.CreatedAt, persisted.CreatedAt);
		Assert.Equal("Hands-on lab", persisted.Description);
		var validState = EventFields(persisted);
		var invalid = ValidFields(category, await FormTokenAsync(browser, $"/Events/Edit/{id}"));
		invalid["Title"] = "Not Persisted";
		invalid["EndDateTime"] = "2026-05-08T08:00";
		var rejected = await SendFormAsync(browser, $"/Events/Edit/{id}", invalid);
		Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);
		var html = await rejected.Content.ReadAsStringAsync();
		Assert.Contains("Not Persisted", html);
		Assert.Contains("End date and time must be after", html);
		Assert.Contains("Academic", html);
		Assert.Equal(validState, EventFields(await ReadEventAsync(database, id)));
		Assert.Equal(unrelatedBefore, EventFields(await ReadEventAsync(database, unrelated)));
	}

	[Fact]
	public async Task Create_EndExactlyAtStart_RedisplaysErrorsWithoutChangingEvents()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		var (ownerId, _) = await AccountsAsync(host);
		var category = await CategoryAsync(database);
		var sentinel = await EventAsync(database, category, ownerId, "Existing Boundary Draft", EventStatus.Draft);
		var before = EventFields(await ReadEventAsync(database, sentinel));
		using var browser = Browser(host);
		await SignInAsync(browser, OwnerEmail);
		var fields = ValidFields(category, await FormTokenAsync(browser, "/Events/Create"));
		fields["EndDateTime"] = fields["StartDateTime"];
		using var response = await SendFormAsync(browser, "/Events/Create", fields);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var html = await response.Content.ReadAsStringAsync();
		Assert.Contains("End date and time must be after the start date and time.", html);
		Assert.Contains("Engineering Hall", html);
		Assert.Contains("Robotics Workshop", html);
		Assert.Contains("Academic", html);
		await using var verify = database.CreateContext();
		Assert.Equal(1, await verify.Events.CountAsync());
		Assert.Equal(before, EventFields(await ReadEventAsync(database, sentinel)));
	}

	[Fact]
	public async Task OwnershipAndProtectedFields_TamperingCannotChangeEitherOwnersDraft()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		var clock = FixedClock();
		using var host = CreateHost(database, clock);
		var (ownerId, otherId) = await AccountsAsync(host);
		var category = await CategoryAsync(database);
		var ownedId = await EventAsync(database, category, ownerId, "Owned Draft", EventStatus.Draft);
		var foreignId = await EventAsync(database, category, otherId, "Foreign Draft", EventStatus.Draft);
		var ownedBefore = EventFields(await ReadEventAsync(database, ownedId));
		var foreignBefore = EventFields(await ReadEventAsync(database, foreignId));
		using var browser = Browser(host);
		await SignInAsync(browser, OwnerEmail);
		var fields = ValidFields(category, await FormTokenAsync(browser, $"/Events/Edit/{ownedId}"));
		fields["OrganizerId"] = ownerId;
		fields["Status"] = "Draft";
		using var foreignPost = await SendFormAsync(browser, $"/Events/Edit/{foreignId}", fields);
		Assert.Equal(HttpStatusCode.NotFound, foreignPost.StatusCode);
		Assert.Equal(ownedBefore, EventFields(await ReadEventAsync(database, ownedId)));
		Assert.Equal(foreignBefore, EventFields(await ReadEventAsync(database, foreignId)));

		fields = ValidFields(category, await FormTokenAsync(browser, "/Events/Create"));
		fields["OrganizerId"] = otherId;
		fields["Status"] = "Published";
		fields["CreatedAt"] = "2000-01-01T00:00";
		using var created = await SendFormAsync(browser, "/Events/Create", fields);
		Assert.Equal("/Events/Manage", Location(created).AbsolutePath);
		await using var verify = database.CreateContext();
		var newEvent = await verify.Events.AsNoTracking().SingleAsync(item => item.Title == "Robotics Workshop");
		Assert.Equal(ownerId, newEvent.OrganizerId);
		Assert.Equal(EventStatus.Draft, newEvent.Status);
		Assert.Equal(clock.GetLocalNow().DateTime, newEvent.CreatedAt);
		Assert.Equal(3, await verify.Events.CountAsync());
		Assert.Equal(ownedBefore, EventFields(await ReadEventAsync(database, ownedId)));
		Assert.Equal(foreignBefore, EventFields(await ReadEventAsync(database, foreignId)));
	}
}
