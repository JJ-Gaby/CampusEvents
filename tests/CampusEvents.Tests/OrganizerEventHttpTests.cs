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
[InlineData("cancelled")]
public async Task Edit_ForeignOrIneligibleEvent_DoesNotMutate(string kind)
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	using var host = CreateHost(database, FixedClock());
	var (ownerId, otherId) = await AccountsAsync(host);
	var category = await CategoryAsync(database);
	var id = await EventAsync(database, category, kind == "foreign" ? otherId : ownerId, "Protected Original Event",
		kind == "cancelled" ? EventStatus.Cancelled : EventStatus.Draft);
	var snapshot = EventFields(await ReadEventAsync(database, id));
	using var browser = Browser(host);
	await SignInAsync(browser, OwnerEmail);

	var get = await browser.GetAsync($"/Events/Edit/{id}");
	if (kind == "foreign") Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
	else Assert.Equal("/Events/Manage", Location(get).AbsolutePath);

	var fields = ValidFields(category, await FormTokenAsync(browser, "/Events/Create"));
	fields["Title"] = "Forged Update";
	fields["OrganizerId"] = ownerId;
	fields["Status"] = "Draft";
	var post = await SendFormAsync(browser, $"/Events/Edit/{id}", fields);
	if (kind == "foreign") Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
	else Assert.Equal("/Events/Manage", Location(post).AbsolutePath);

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

private static async Task SetStartAsync(IsolatedSqlServerDatabase database, int id, DateTime start)
{
	await using var db = database.CreateContext();
	var item = await db.Events.SingleAsync(e => e.Id == id);
	item.StartDateTime = start;
	item.EndDateTime = start.AddHours(2);
	await db.SaveChangesAsync();
}

private static async Task<HttpResponseMessage> CommandAsync(HttpClient browser, string path)
{
	var token = await FormTokenAsync(browser, "/Events/Create");
	var fields = new Dictionary<string, string> { ["__RequestVerificationToken"] = token };
	return await SendFormAsync(browser, path, fields);
}

[Fact]
public async Task Publish_OwnedFutureDraft_UpdatesStateAndPublicDiscovery()
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	using var host = CreateHost(database, FixedClock());
	var (ownerId, _) = await AccountsAsync(host);
	var category = await CategoryAsync(database);
	var id = await EventAsync(database, category, ownerId, "Future Robotics Forum", EventStatus.Draft);
	using var organizer = Browser(host);
	using var visitor = Browser(host);
	await SignInAsync(organizer, OwnerEmail);

	var query = $"/Events?search=Robotics&categoryId={category}&pageSize=25";
	var before = await visitor.GetAsync(query);
	Assert.Equal(HttpStatusCode.OK, before.StatusCode);
	Assert.DoesNotContain("Future Robotics Forum", await before.Content.ReadAsStringAsync());

	var publish = await CommandAsync(organizer, $"/Events/Publish/{id}");
	Assert.Equal("/Events/Manage", Location(publish).AbsolutePath);
	Assert.Equal(EventStatus.Published, (await ReadEventAsync(database, id)).Status);
	var published = await visitor.GetAsync(query);
	Assert.Equal(HttpStatusCode.OK, published.StatusCode);
	Assert.Contains("Future Robotics Forum", await published.Content.ReadAsStringAsync());

	var cancel = await CommandAsync(organizer, $"/Events/Cancel/{id}");
	Assert.Equal("/Events/Manage", Location(cancel).AbsolutePath);
	Assert.Equal(EventStatus.Cancelled, (await ReadEventAsync(database, id)).Status);
	var absent = await visitor.GetAsync(query);
	Assert.Equal(HttpStatusCode.OK, absent.StatusCode);
	Assert.DoesNotContain("Future Robotics Forum", await absent.Content.ReadAsStringAsync());
	var details = await visitor.GetAsync($"/Events/Details/{id}");
	Assert.Equal(HttpStatusCode.OK, details.StatusCode);
	Assert.Contains("This Event has been cancelled", await details.Content.ReadAsStringAsync());
}

[Theory]
[InlineData("published")]
[InlineData("cancelled")]
[InlineData("at-start")]
[InlineData("past-start")]
public async Task Publish_RejectedStatesOrTimes_NeverWrite(string reason)
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	var clock = FixedClock();
	using var host = CreateHost(database, clock);
	var (ownerId, _) = await AccountsAsync(host);
	var category = await CategoryAsync(database);
	var status = reason switch
	{
		"published" => EventStatus.Published,
		"cancelled" => EventStatus.Cancelled,
		_ => EventStatus.Draft
	};
	var id = await EventAsync(database, category, ownerId, "Guarded Event", status);
	var now = clock.GetLocalNow().DateTime;
	if (reason == "at-start") await SetStartAsync(database, id, now);
	if (reason == "past-start") await SetStartAsync(database, id, now.AddMinutes(-1));
	var snapshot = EventFields(await ReadEventAsync(database, id));
	using var browser = Browser(host);
	await SignInAsync(browser, OwnerEmail);

	var response = await CommandAsync(browser, $"/Events/Publish/{id}");
	Assert.Equal("/Events/Manage", Location(response).AbsolutePath);
	Assert.Equal(snapshot, EventFields(await ReadEventAsync(database, id)));
	var feedback = await browser.GetAsync("/Events/Manage");
	Assert.Contains("error-message", await feedback.Content.ReadAsStringAsync());
}

[Theory]
[InlineData("draft")]
[InlineData("cancelled")]
[InlineData("at-start")]
[InlineData("past-start")]
public async Task Cancel_RefusesIneligibleStateOrStart(string reason)
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	var clock = FixedClock();
	using var host = CreateHost(database, clock);
	var (ownerId, _) = await AccountsAsync(host);
	var category = await CategoryAsync(database);
	var status = reason switch
	{
		"draft" => EventStatus.Draft,
		"cancelled" => EventStatus.Cancelled,
		_ => EventStatus.Published
	};
	var id = await EventAsync(database, category, ownerId, "Guarded Cancel", status);
	var now = clock.GetLocalNow().DateTime;
	if (reason == "at-start") await SetStartAsync(database, id, now);
	if (reason == "past-start") await SetStartAsync(database, id, now.AddMinutes(-1));
	var snapshot = EventFields(await ReadEventAsync(database, id));
	using var browser = Browser(host);
	await SignInAsync(browser, OwnerEmail);

	Assert.Equal("/Events/Manage", Location(await CommandAsync(browser, $"/Events/Cancel/{id}")).AbsolutePath);
	Assert.Equal(snapshot, EventFields(await ReadEventAsync(database, id)));
	var feedback = await browser.GetAsync("/Events/Manage");
	Assert.Contains("error-message", await feedback.Content.ReadAsStringAsync());
}

[Fact]
public async Task Delete_ConfirmationGetIsSafe_AndPostDeletesDraft()
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	using var host = CreateHost(database, FixedClock());
	var (ownerId, _) = await AccountsAsync(host);
	var category = await CategoryAsync(database);
	var id = await EventAsync(database, category, ownerId, "Disposable Draft", EventStatus.Draft);
	var snapshot = EventFields(await ReadEventAsync(database, id));
	using var browser = Browser(host);
	await SignInAsync(browser, OwnerEmail);

	var confirmation = await browser.GetAsync($"/Events/Delete/{id}");
	Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode);
	Assert.Contains("Permanently Delete Draft", await confirmation.Content.ReadAsStringAsync());
	Assert.Equal(snapshot, EventFields(await ReadEventAsync(database, id)));
	var safeGet = await browser.GetAsync($"/Events/DeleteConfirmed/{id}");
	Assert.Equal(HttpStatusCode.NotFound, safeGet.StatusCode);
	Assert.Equal(snapshot, EventFields(await ReadEventAsync(database, id)));

	Assert.Equal("/Events/Manage", Location(await CommandAsync(browser, $"/Events/DeleteConfirmed/{id}")).AbsolutePath);
	await using var verify = database.CreateContext();
	Assert.False(await verify.Events.AnyAsync(e => e.Id == id));
}

[Theory]
[InlineData("published")]
[InlineData("cancelled")]
[InlineData("foreign")]
public async Task Delete_RefusesPublishedCancelledAndForeign(string reason)
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	using var host = CreateHost(database, FixedClock());
	var (ownerId, otherId) = await AccountsAsync(host);
	var category = await CategoryAsync(database);
	var id = await EventAsync(database, category, reason == "foreign" ? otherId : ownerId,
		"Retained Event", reason == "published" ? EventStatus.Published :
		reason == "cancelled" ? EventStatus.Cancelled : EventStatus.Draft);
	var snapshot = EventFields(await ReadEventAsync(database, id));
	using var browser = Browser(host);
	await SignInAsync(browser, OwnerEmail);

	Assert.Equal(HttpStatusCode.NotFound, (await browser.GetAsync($"/Events/Delete/{id}")).StatusCode);
	var response = await CommandAsync(browser, $"/Events/DeleteConfirmed/{id}");
	if (reason == "foreign") Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	else Assert.Equal("/Events/Manage", Location(response).AbsolutePath);
	Assert.Equal(snapshot, EventFields(await ReadEventAsync(database, id)));
}

[Fact]
public async Task Edit_Published_ValidAndInvalidChanges()
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	using var host = CreateHost(database, FixedClock());
	var (ownerId, otherId) = await AccountsAsync(host);
	var category = await CategoryAsync(database);
	var id = await EventAsync(database, category, ownerId, "Original Published", EventStatus.Published);
	var created = (await ReadEventAsync(database, id)).CreatedAt;
	using var browser = Browser(host);
	await SignInAsync(browser, OwnerEmail);
	var token = await FormTokenAsync(browser, $"/Events/Edit/{id}");
	var fields = ValidFields(category, token);
	fields["Title"] = "Revised Published Event";
	fields["OrganizerId"] = otherId;
	fields["Status"] = "Cancelled";
	fields["CreatedAt"] = "2000-01-01T00:00:00";

	Assert.Equal("/Events/Manage", Location(await SendFormAsync(browser, $"/Events/Edit/{id}", fields)).AbsolutePath);
	var saved = await ReadEventAsync(database, id);
	Assert.Equal("Revised Published Event", saved.Title);
	Assert.Equal("Hands-on lab", saved.Description);
	Assert.Equal(category, saved.CategoryId);
	Assert.Equal("Engineering Hall", saved.Location);
	Assert.Equal(new DateTime(2026, 5, 8, 10, 0, 0), saved.StartDateTime);
	Assert.Equal(new DateTime(2026, 5, 8, 12, 0, 0), saved.EndDateTime);
	Assert.Equal(40, saved.Capacity);
	Assert.Equal(ownerId, saved.OrganizerId);
	Assert.Equal(EventStatus.Published, saved.Status);
	Assert.Equal(created, saved.CreatedAt);

	fields["CategoryId"] = "999999";
	fields["Title"] = "Not Saved";
	var invalid = await SendFormAsync(browser, $"/Events/Edit/{id}", fields);
	Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
	var html = await invalid.Content.ReadAsStringAsync();
	Assert.Contains("Select an existing Category", html);
	Assert.Equal(EventFields(saved), EventFields(await ReadEventAsync(database, id)));
}

[Fact]
public async Task Edit_CancelledIsRefused()
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	using var host = CreateHost(database, FixedClock());
	var (ownerId, _) = await AccountsAsync(host);
	var category = await CategoryAsync(database);
	var id = await EventAsync(database, category, ownerId, "Frozen Cancelled", EventStatus.Cancelled);
	var snapshot = EventFields(await ReadEventAsync(database, id));
	using var browser = Browser(host);
	await SignInAsync(browser, OwnerEmail);

	Assert.Equal("/Events/Manage", Location(await browser.GetAsync($"/Events/Edit/{id}")).AbsolutePath);
	var fields = ValidFields(category, await FormTokenAsync(browser, "/Events/Create"));
	Assert.Equal("/Events/Manage", Location(await SendFormAsync(browser, $"/Events/Edit/{id}", fields)).AbsolutePath);
	Assert.Equal(snapshot, EventFields(await ReadEventAsync(database, id)));
}

[Theory]
[InlineData("Publish")]
[InlineData("Cancel")]
[InlineData("DeleteConfirmed")]
[InlineData("Edit")]
public async Task LifecyclePosts_RequireRoleAndAntiforgery(string action)
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	using var host = CreateHost(database, FixedClock());
	var (ownerId, _) = await AccountsAsync(host);
	var category = await CategoryAsync(database);
	var id = await EventAsync(database, category, ownerId, "Protected Draft", EventStatus.Draft);
	var snapshot = EventFields(await ReadEventAsync(database, id));
	var path = $"/Events/{action}/{id}";

	using var anonymous = Browser(host);
	var anonymousResponse = await anonymous.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>()));
	Assert.Equal("/Account/Login", Location(anonymousResponse).AbsolutePath);

	using var participant = Browser(host);
	await SignInAsync(participant, ParticipantEmail);
	var participantToken = await FormTokenAsync(participant, "/Participant");
	var fields = new Dictionary<string, string> { ["__RequestVerificationToken"] = participantToken };
	var forbidden = await SendFormAsync(participant, path, fields);
	Assert.Equal("/Account/AccessDenied", Location(forbidden).AbsolutePath);

	using var organizer = Browser(host);
	await SignInAsync(organizer, OwnerEmail);
	var noToken = await organizer.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>()));
	Assert.Equal(HttpStatusCode.BadRequest, noToken.StatusCode);
	Assert.Equal(snapshot, EventFields(await ReadEventAsync(database, id)));
}

[Theory]
[InlineData("Publish")]
[InlineData("Cancel")]
public async Task Lifecycle_ForeignOwnerCannotMutate(string action)
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	using var host = CreateHost(database, FixedClock());
	var (_, otherId) = await AccountsAsync(host);
	var category = await CategoryAsync(database);
	var status = action == "Publish" ? EventStatus.Draft : EventStatus.Published;
	var id = await EventAsync(database, category, otherId, "Other Organizer Event", status);
	var snapshot = EventFields(await ReadEventAsync(database, id));
	using var browser = Browser(host);
	await SignInAsync(browser, OwnerEmail);

	var result = await CommandAsync(browser, $"/Events/{action}/{id}");
	Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
	Assert.Equal(snapshot, EventFields(await ReadEventAsync(database, id)));
}

[Theory]
[InlineData(-1)]
[InlineData(0)]
[InlineData(1)]
public async Task Publish_OneSecondBoundary_UsesStrictFutureRule(int seconds)
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	var clock = FixedClock();
	using var host = CreateHost(database, clock);
	var (ownerId, _) = await AccountsAsync(host);
	var category = await CategoryAsync(database);
	var id = await EventAsync(database, category, ownerId, "Boundary Draft", EventStatus.Draft);
	await SetStartAsync(database, id, clock.GetLocalNow().DateTime.AddSeconds(seconds));
	var before = EventFields(await ReadEventAsync(database, id));
	using var browser = Browser(host);
	await SignInAsync(browser, OwnerEmail);
	Assert.Equal("/Events/Manage", Location(await CommandAsync(browser, $"/Events/Publish/{id}")).AbsolutePath);
	var after = await ReadEventAsync(database, id);
	if (seconds <= 0) Assert.Equal(before, EventFields(after));
	else Assert.Equal(EventStatus.Published, after.Status);
}

[Theory]
[InlineData("Publish")]
[InlineData("Cancel")]
[InlineData("DeleteConfirmed")]
[InlineData("Edit")]
public async Task Lifecycle_ForgedRouteAndFields_PreserveForeignEvent(string action)
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	using var host = CreateHost(database, FixedClock());
	var (_, otherId) = await AccountsAsync(host);
	var category = await CategoryAsync(database);
	var id = await EventAsync(database, category, otherId, "Foreign Target",
		action == "Cancel" ? EventStatus.Published : EventStatus.Draft);
	var before = EventFields(await ReadEventAsync(database, id));
	using var browser = Browser(host);
	await SignInAsync(browser, OwnerEmail);
	var fields = ValidFields(category, await FormTokenAsync(browser, "/Events/Create"));
	fields["OrganizerId"] = otherId;
	fields["Status"] = "Published";
	using var response = await SendFormAsync(browser, $"/Events/{action}/{id}", fields);
	Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	Assert.Equal(before, EventFields(await ReadEventAsync(database, id)));
}

	[Theory]
	[InlineData(0, false)]
	[InlineData(-1, false)]
	[InlineData(1, true)]
	public async Task RegistrationCancellation_StartBoundaryAndCancelledEvent(int seconds, bool cancelled)
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		var clock = FixedClock();
		using var host = CreateHost(database, clock);
		var (ownerId, _) = await AccountsAsync(host);
		var category = await CategoryAsync(database);
		var eventId = await EventAsync(database, category, ownerId, "Cancellation Boundary",
			cancelled ? EventStatus.Cancelled : EventStatus.Published);
		await SetStartAsync(database, eventId, clock.GetLocalNow().DateTime.AddSeconds(seconds));
		int registrationId;
		await using (var setup = database.CreateContext())
		{
			var userId = await setup.Users.Where(u => u.Email == ParticipantEmail).Select(u => u.Id).SingleAsync();
			var row = new Registration { UserId = userId, EventId = eventId,
				CreatedAt = clock.GetLocalNow().DateTime.AddDays(-1) };
			setup.Registrations.Add(row);
			await setup.SaveChangesAsync();
			registrationId = row.Id;
		}
		using var browser = Browser(host);
		await SignInAsync(browser, ParticipantEmail);
		Assert.Equal("/Registrations/Mine", Location(await RegistrationCommandAsync(browser,
			$"/Registrations/Cancel/{registrationId}")).AbsolutePath);
		var mine = await browser.GetAsync("/Registrations/Mine");
		await AssertFeedbackAsync(mine, seconds <= 0 ? "error-message" : "status-message",
			seconds <= 0 ? "alert" : "status", seconds <= 0
				? "A Registration can be cancelled only before the Event begins."
				: "Your Registration was cancelled");
		await using var verify = database.CreateContext();
		Assert.Equal(seconds <= 0, await verify.Registrations.AnyAsync(r => r.Id == registrationId));
	}

	[Fact]
	public async Task Registration_DuplicateAndFullPosts_ReportFeedbackWithoutExtraRows()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		var (ownerId, _) = await AccountsAsync(host);
		var category = await CategoryAsync(database);
		var eventId = await EventAsync(database, category, ownerId, "Two Places", EventStatus.Published);
		await using (var setup = database.CreateContext())
		{
			(await setup.Events.SingleAsync(e => e.Id == eventId)).Capacity = 2;
			await setup.SaveChangesAsync();
		}
		using var participant = Browser(host);
		using var other = Browser(host);
		using var owner = Browser(host);
		await SignInAsync(participant, ParticipantEmail);
		await SignInAsync(other, OtherEmail);
		await SignInAsync(owner, OwnerEmail);
		Assert.Equal($"/Events/Details/{eventId}", Location(await RegistrationCommandAsync(participant,
			$"/Registrations/Register/{eventId}")).AbsolutePath);
		await RegistrationCommandAsync(participant, $"/Registrations/Register/{eventId}");
		await AssertFeedbackAsync(await participant.GetAsync($"/Events/Details/{eventId}"),
			"error-message", "alert", "You are already registered");
		await RegistrationCommandAsync(other, $"/Registrations/Register/{eventId}");
		await RegistrationCommandAsync(owner, $"/Registrations/Register/{eventId}");
		await AssertFeedbackAsync(await owner.GetAsync($"/Events/Details/{eventId}"),
			"error-message", "alert", "No places are currently available");
		await using var verify = database.CreateContext();
		Assert.Equal(2, await verify.Registrations.CountAsync(r => r.EventId == eventId));
	}

	private static async Task<HttpResponseMessage> RegistrationCommandAsync(HttpClient browser, string path)
	{
		var token = await FormTokenAsync(browser, "/Registrations/Mine");
		return await SendFormAsync(browser, path, new Dictionary<string, string>
		{
			["__RequestVerificationToken"] = token
		});
	}

	private static async Task AssertFeedbackAsync(
		HttpResponseMessage response, string cssClass, string role, string expectedMessage)
	{
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var html = await response.Content.ReadAsStringAsync();
		var pattern = $@"<p class=""{cssClass}"" role=""{role}"">(?<message>[\s\S]*?)</p>";
		var match = Regex.Match(html, pattern);
		Assert.True(match.Success, $"Expected a {role} message in the {cssClass} element.");
		Assert.Contains(expectedMessage, WebUtility.HtmlDecode(match.Groups["message"].Value));
	}

	[Fact]
	public async Task Register_ValidPost_PersistsAndConfirms()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		var (ownerId, _) = await AccountsAsync(host);
		var categoryId = await CategoryAsync(database);
		var eventId = await EventAsync(database, categoryId, ownerId, "Successful Registration", EventStatus.Published);
		using var browser = Browser(host);
		await SignInAsync(browser, ParticipantEmail);
		var token = await FormTokenAsync(browser, $"/Events/Details/{eventId}");
		var fields = new Dictionary<string, string> { ["__RequestVerificationToken"] = token };
		var post = await SendFormAsync(browser, $"/Registrations/Register/{eventId}", fields);
		Assert.Equal($"/Events/Details/{eventId}", Location(post).AbsolutePath);
		var details = await browser.GetAsync($"/Events/Details/{eventId}");
		Assert.Equal(HttpStatusCode.OK, details.StatusCode);
		var html = await details.Content.ReadAsStringAsync();
		await AssertFeedbackAsync(details, "status-message", "status", "Registration confirmed.");
		Assert.Matches(@"Current registrations</dt>\s*<dd>1</dd>", html);
		Assert.Matches(@"Remaining capacity</dt>\s*<dd>29</dd>", html);
		Assert.Contains("You already have a current Registration", html);
		await using var check = database.CreateContext();
		var participantId = await check.Users.Where(user => user.Email == ParticipantEmail)
			.Select(user => user.Id).SingleAsync();
		var registration = await check.Registrations
			.SingleAsync(row => row.UserId == participantId && row.EventId == eventId);
		Assert.Equal(FixedClock().GetLocalNow().DateTime, registration.CreatedAt);
		Assert.Equal(1, await check.Registrations.CountAsync(row => row.EventId == eventId));
	}

	[Fact]
	public async Task Register_DraftPost_ExplainsUnavailability()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		var (ownerId, _) = await AccountsAsync(host);
		var categoryId = await CategoryAsync(database);
		var publishedId = await EventAsync(database, categoryId, ownerId, "Token Source", EventStatus.Published);
		var draftId = await EventAsync(database, categoryId, ownerId, "Private Draft", EventStatus.Draft);
		using var browser = Browser(host);
		await SignInAsync(browser, ParticipantEmail);
		var token = await FormTokenAsync(browser, $"/Events/Details/{publishedId}");
		var fields = new Dictionary<string, string> { ["__RequestVerificationToken"] = token };
		var post = await SendFormAsync(browser, $"/Registrations/Register/{draftId}", fields);
		Assert.Equal("/Events", Location(post).AbsolutePath);
		var catalog = await browser.GetAsync(Location(post).PathAndQuery);
		await AssertFeedbackAsync(catalog, "error-message", "alert", "Registration is unavailable for this Event.");
		var draftDetails = await browser.GetAsync($"/Events/Details/{draftId}");
		Assert.Equal(HttpStatusCode.NotFound, draftDetails.StatusCode);
		await using var check = database.CreateContext();
		Assert.Equal(0, await check.Registrations.CountAsync(row => row.EventId == draftId));
	}

	[Fact]
	public async Task Register_RequiresAuthenticationAndAntiforgery()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		var (ownerId, _) = await AccountsAsync(host);
		var categoryId = await CategoryAsync(database);
		var eventId = await EventAsync(database, categoryId, ownerId, "Protected Registration", EventStatus.Published);
		using var anonymous = Browser(host);
		var anonymousPost = await anonymous.PostAsync($"/Registrations/Register/{eventId}",
			new FormUrlEncodedContent(new Dictionary<string, string>()));
		Assert.Equal("/Account/Login", Location(anonymousPost).AbsolutePath);
		using var participant = Browser(host);
		await SignInAsync(participant, ParticipantEmail);
		var noToken = await participant.PostAsync($"/Registrations/Register/{eventId}",
			new FormUrlEncodedContent(new Dictionary<string, string>()));
		Assert.Equal(HttpStatusCode.BadRequest, noToken.StatusCode);
		await using var check = database.CreateContext();
		Assert.Equal(0, await check.Registrations.CountAsync(r => r.EventId == eventId));
	}

	[Fact]
	public async Task OwnCancellation_DoesNotDeleteAnotherUsersRegistration()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		var (ownerId, otherId) = await AccountsAsync(host);
		var categoryId = await CategoryAsync(database);
		var eventId = await EventAsync(database, categoryId, ownerId, "Cancellation Scope", EventStatus.Published);
		int ownId;
		int foreignId;
		await using (var setup = database.CreateContext())
		{
			var participantId = await setup.Users.Where(u => u.Email == ParticipantEmail).Select(u => u.Id).SingleAsync();
			var own = new Registration { UserId = participantId, EventId = eventId,
				CreatedAt = FixedClock().GetLocalNow().DateTime };
			var foreign = new Registration { UserId = otherId, EventId = eventId,
				CreatedAt = FixedClock().GetLocalNow().DateTime };
			setup.Registrations.AddRange(own, foreign);
			await setup.SaveChangesAsync();
			ownId = own.Id;
			foreignId = foreign.Id;
		}
		using var browser = Browser(host);
		await SignInAsync(browser, ParticipantEmail);
		var token = await FormTokenAsync(browser, "/Registrations/Mine");
		var fields = new Dictionary<string, string> { ["__RequestVerificationToken"] = token };
		var foreignPost = await SendFormAsync(browser, $"/Registrations/Cancel/{foreignId}", fields);
		Assert.Equal(HttpStatusCode.NotFound, foreignPost.StatusCode);
		await using (var check = database.CreateContext())
			Assert.Equal(2, await check.Registrations.CountAsync(r => r.EventId == eventId));
		var ownPost = await SendFormAsync(browser, $"/Registrations/Cancel/{ownId}", fields);
		Assert.Equal("/Registrations/Mine", Location(ownPost).AbsolutePath);
		var mine = await browser.GetAsync("/Registrations/Mine");
		await AssertFeedbackAsync(mine, "status-message", "status",
			"Your Registration was cancelled and the place is available again.");
		await using var verify = database.CreateContext();
		Assert.Equal(1, await verify.Registrations.CountAsync(r => r.EventId == eventId));
		Assert.True(await verify.Registrations.AnyAsync(r => r.Id == foreignId));
	}

	[Fact]
	public async Task PublishedCapacity_BelowCurrentCount_IsRejected()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		var (ownerId, otherId) = await AccountsAsync(host);
		var categoryId = await CategoryAsync(database);
		var eventId = await EventAsync(database, categoryId, ownerId, "Capacity Floor", EventStatus.Published);
		await using (var setup = database.CreateContext())
		{
			var participantId = await setup.Users.Where(u => u.Email == ParticipantEmail).Select(u => u.Id).SingleAsync();
			setup.Registrations.AddRange(new Registration
				{
					UserId = participantId, EventId = eventId, CreatedAt = FixedClock().GetLocalNow().DateTime
				},
				new Registration { UserId = otherId, EventId = eventId, CreatedAt = FixedClock().GetLocalNow().DateTime });
			await setup.SaveChangesAsync();
		}
		var before = EventFields(await ReadEventAsync(database, eventId));
		using var browser = Browser(host);
		await SignInAsync(browser, OwnerEmail);
		var fields = ValidFields(categoryId, await FormTokenAsync(browser, $"/Events/Edit/{eventId}"));
		fields["Capacity"] = "1";
		var response = await SendFormAsync(browser, $"/Events/Edit/{eventId}", fields);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Contains("Capacity cannot be lower", await response.Content.ReadAsStringAsync());
		Assert.Equal(before, EventFields(await ReadEventAsync(database, eventId)));
	}

	[Fact]
	public async Task DraftDelete_WithRegistration_IsRefused()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		var (ownerId, _) = await AccountsAsync(host);
		var categoryId = await CategoryAsync(database);
		var eventId = await EventAsync(database, categoryId, ownerId, "Protected Draft", EventStatus.Draft);
		await using (var setup = database.CreateContext())
		{
			var participantId = await setup.Users.Where(u => u.Email == ParticipantEmail).Select(u => u.Id).SingleAsync();
			setup.Registrations.Add(new Registration { UserId = participantId, EventId = eventId,
				CreatedAt = FixedClock().GetLocalNow().DateTime });
			await setup.SaveChangesAsync();
		}
		using var browser = Browser(host);
		await SignInAsync(browser, OwnerEmail);
		var response = await CommandAsync(browser, $"/Events/DeleteConfirmed/{eventId}");
		Assert.Equal("/Events/Manage", Location(response).AbsolutePath);
		var manage = await browser.GetAsync("/Events/Manage");
		await AssertFeedbackAsync(manage, "error-message", "alert",
			"This Draft Event cannot be deleted while current Registrations exist.");
		await using var check = database.CreateContext();
		Assert.True(await check.Events.AnyAsync(e => e.Id == eventId));
		Assert.True(await check.Registrations.AnyAsync(r => r.EventId == eventId));
	}

	private static async Task<int> PrepareCapacityCompetitionAsync(
		IsolatedSqlServerDatabase database, int categoryId, string ownerId, string existingUserId)
	{
		var eventId = await EventAsync(database, categoryId, ownerId, "Capacity Coordination", EventStatus.Published);
		await using var db = database.CreateContext();
		var eventItem = await db.Events.SingleAsync(item => item.Id == eventId);
		eventItem.Capacity = 2;
		db.Registrations.Add(new Registration
		{
			UserId = existingUserId, EventId = eventId, CreatedAt = FixedClock().GetLocalNow().DateTime
		});
		await db.SaveChangesAsync();
		return eventId;
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task RegisterAndCapacityEdit_OrderedWritesPreserveInvariant(bool registerFirst)
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		var (ownerId, existingUserId) = await AccountsAsync(host);
		var categoryId = await CategoryAsync(database);
		var eventId = await PrepareCapacityCompetitionAsync(database, categoryId, ownerId, existingUserId);
		using var organizer = Browser(host);
		using var participant = Browser(host);
		await SignInAsync(organizer, OwnerEmail);
		await SignInAsync(participant, ParticipantEmail);
		var edit = ValidFields(categoryId, await FormTokenAsync(organizer, $"/Events/Edit/{eventId}"));
		edit["Capacity"] = "1";
		var token = await FormTokenAsync(participant, $"/Events/Details/{eventId}");
		var register = new Dictionary<string, string> { ["__RequestVerificationToken"] = token };
		if (registerFirst)
		{
			var registrationResponse = await SendFormAsync(participant, $"/Registrations/Register/{eventId}", register);
			Assert.Equal($"/Events/Details/{eventId}", Location(registrationResponse).AbsolutePath);
			var registrationDetails = await participant.GetAsync($"/Events/Details/{eventId}");
			await AssertFeedbackAsync(registrationDetails, "status-message", "status", "Registration confirmed.");
			var editResponse = await SendFormAsync(organizer, $"/Events/Edit/{eventId}", edit);
			Assert.Equal(HttpStatusCode.OK, editResponse.StatusCode);
			Assert.Contains("Capacity cannot be lower", await editResponse.Content.ReadAsStringAsync());
		}
		else
		{
			var editResponse = await SendFormAsync(organizer, $"/Events/Edit/{eventId}", edit);
			Assert.Equal("/Events/Manage", Location(editResponse).AbsolutePath);
			var registrationResponse = await SendFormAsync(participant, $"/Registrations/Register/{eventId}", register);
			Assert.Equal($"/Events/Details/{eventId}", Location(registrationResponse).AbsolutePath);
			var registrationDetails = await participant.GetAsync($"/Events/Details/{eventId}");
			await AssertFeedbackAsync(registrationDetails, "error-message", "alert", "No places are currently available.");
		}
		await using var check = database.CreateContext();
		var count = await check.Registrations.CountAsync(row => row.EventId == eventId);
		var capacity = await check.Events.Where(item => item.Id == eventId).Select(item => item.Capacity).SingleAsync();
		Assert.Equal(registerFirst ? 2 : 1, count);
		Assert.Equal(registerFirst ? 2 : 1, capacity);
	}

	[Fact]
	public async Task RegisterAndCapacityEdit_CompetingPostsPreserveInvariant()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		var (ownerId, existingUserId) = await AccountsAsync(host);
		var categoryId = await CategoryAsync(database);
		var eventId = await PrepareCapacityCompetitionAsync(database, categoryId, ownerId, existingUserId);
		using var organizer = Browser(host);
		using var participant = Browser(host);
		await SignInAsync(organizer, OwnerEmail);
		await SignInAsync(participant, ParticipantEmail);
		var edit = ValidFields(categoryId, await FormTokenAsync(organizer, $"/Events/Edit/{eventId}"));
		edit["Capacity"] = "1";
		var token = await FormTokenAsync(participant, $"/Events/Details/{eventId}");
		var register = new Dictionary<string, string> { ["__RequestVerificationToken"] = token };
		var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		async Task<HttpResponseMessage> SubmitAsync(HttpClient browser, string route, Dictionary<string, string> fields)
		{
			await gate.Task;
			return await SendFormAsync(browser, route, fields);
		}
		var registrationTask = SubmitAsync(participant, $"/Registrations/Register/{eventId}", register);
		var editTask = SubmitAsync(organizer, $"/Events/Edit/{eventId}", edit);
		gate.SetResult(true);
		var responses = await Task.WhenAll(registrationTask, editTask);
		Assert.Equal($"/Events/Details/{eventId}", Location(responses[0]).AbsolutePath);
		await using var check = database.CreateContext();
		var count = await check.Registrations.CountAsync(row => row.EventId == eventId);
		var capacity = await check.Events.Where(item => item.Id == eventId).Select(item => item.Capacity).SingleAsync();
		Assert.True(count <= capacity);
		var registrationDetails = await participant.GetAsync($"/Events/Details/{eventId}");
		if (count == 2 && capacity == 2)
		{
			Assert.Equal(HttpStatusCode.OK, responses[1].StatusCode);
			Assert.Contains("Capacity cannot be lower", await responses[1].Content.ReadAsStringAsync());
			await AssertFeedbackAsync(registrationDetails, "status-message", "status", "Registration confirmed.");
		}
		else
		{
			Assert.Equal(1, count);
			Assert.Equal(1, capacity);
			Assert.Equal("/Events/Manage", Location(responses[1]).AbsolutePath);
			await AssertFeedbackAsync(registrationDetails, "error-message", "alert", "No places are currently available.");
		}
	}
}
