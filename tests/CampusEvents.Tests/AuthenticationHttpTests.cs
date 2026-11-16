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

public class AuthenticationHttpTests
{
	private const string ParticipantEmail = "student@example.test";
	private const string ParticipantPassword = "CampusTestPassword1!";

	private static FakeTimeProvider FixedClock()
	{
		var clock = new FakeTimeProvider(new DateTimeOffset(2026, 4, 15, 16, 0, 0, TimeSpan.Zero));
		clock.SetLocalTimeZone(TimeZoneInfo.CreateCustomTimeZone(
			"CemsFixedUtcMinus4", TimeSpan.FromHours(-4),
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

			// Supply the isolated SQL Server connection through test-host configuration.
			builder.ConfigureAppConfiguration((_, configuration) =>
				configuration.AddInMemoryCollection(new Dictionary<string, string?>
				{
					["ConnectionStrings:DefaultConnection"] = database.ConnectionString
				}));

			// Replace the EF Core registration to use the same isolated database.
			builder.ConfigureTestServices(services =>
			{
				services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
				services.RemoveAll<ApplicationDbContext>();
				services.AddDbContext<ApplicationDbContext>(options =>
					options.UseSqlServer(database.ConnectionString));

				services.RemoveAll<TimeProvider>();
				services.AddSingleton<TimeProvider>(clock);

				// Register the Organizer authorization controller in the integration-test host.
				services.AddControllersWithViews()
					.AddApplicationPart(typeof(TestOnlyOrganizerController).Assembly);
			});
		});
	}

	private static HttpClient CreateBrowserClient(WebApplicationFactory<Program> host)
	{
		return host.CreateClient(new WebApplicationFactoryClientOptions
		{
			AllowAutoRedirect = false,
			HandleCookies = true,
			BaseAddress = new Uri("https://localhost")
		});
	}

	private static Uri RedirectLocation(HttpResponseMessage response)
	{
		Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
		var location = Assert.IsType<Uri>(response.Headers.Location);
		var destination = location.IsAbsoluteUri ? location : new Uri(new Uri("https://localhost"), location);
		Assert.Equal("https", destination.Scheme);
		Assert.Equal("localhost", destination.Host);
		return destination;
	}

	private static async Task CreateParticipantAsync(WebApplicationFactory<Program> host)
	{
		using var scope = host.Services.CreateScope();
		var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
		var user = new ApplicationUser
		{
			UserName = ParticipantEmail,
			Email = ParticipantEmail,
			DisplayName = "Student Tester"
		};
		var result = await users.CreateAsync(user, ParticipantPassword);
		Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(error => error.Description)));
	}

	private static async Task SignInThroughLoginFormAsync(HttpClient browser)
	{
		// Read the rendered login form and use its antiforgery token.
		var token = await AntiforgeryTokenAsync(browser, "/Account/Login");

		var fields = new Dictionary<string, string>
		{
			["Email"] = ParticipantEmail,
			["Password"] = ParticipantPassword,
			["ReturnUrl"] = "/Participant",
			["__RequestVerificationToken"] = token
		};
		var login = await browser.PostAsync("/Account/Login", new FormUrlEncodedContent(fields));

		var location = RedirectLocation(login);
		Assert.Equal("/Participant", location.AbsolutePath);
		Assert.True(login.Headers.TryGetValues("Set-Cookie", out var cookies));
		Assert.Contains(".AspNetCore.Identity.Application", string.Join(";", cookies!));
	}

	[Fact]
	public async Task Anonymous_ParticipantRequest_RedirectsToLoginWithLocalReturnUrl()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		using var browser = CreateBrowserClient(host);

		var response = await browser.GetAsync("/Participant");
		var location = RedirectLocation(response);
		Assert.Equal("/Account/Login", location.AbsolutePath);
		var query = QueryHelpers.ParseQuery(location.Query);
		Assert.Equal("/Participant", query["ReturnUrl"].ToString());
		browser.DefaultRequestHeaders.Add("X-User-Id", "seed-administrator-001");
		using var forged = await browser.GetAsync("/Participant");
		Assert.Equal("/Account/Login", RedirectLocation(forged).AbsolutePath);
	}

	[Fact]
	public async Task AuthenticatedParticipant_WithIdentityCookie_CanAccessParticipantPage()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		await CreateParticipantAsync(host);
		using var browser = CreateBrowserClient(host);

		await SignInThroughLoginFormAsync(browser);
		var response = await browser.GetAsync("/Participant");
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Contains("Participant Area", await response.Content.ReadAsStringAsync());
	}

	[Fact]
	public async Task AuthenticatedParticipant_RoleProtectedProbe_IsDenied()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		await CreateParticipantAsync(host);
		using var browser = CreateBrowserClient(host);
		await SignInThroughLoginFormAsync(browser);

		var response = await browser.GetAsync("/__test-only/organizer");
		Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		if (response.Headers.Location is Uri header)
		{
			var destination = header.IsAbsoluteUri ? header : new Uri(new Uri("https://localhost"), header);
			Assert.Equal("localhost", destination.Host);
			Assert.Equal("/Account/AccessDenied", destination.AbsolutePath);
		}
	}

	private static async Task<string> AntiforgeryTokenAsync(HttpClient browser, string getPath)
	{
		var page = await browser.GetAsync(getPath);
		Assert.Equal(HttpStatusCode.OK, page.StatusCode);
		var html = await page.Content.ReadAsStringAsync();
		// Locate the hidden input by name, regardless of HTML attribute order.
		foreach (Match input in Regex.Matches(html, @"<input\b[^>]*>", RegexOptions.IgnoreCase))
		{
			var name = Regex.Match(input.Value, @"\bname\s*=\s*['""']([^'""']+)['""']", RegexOptions.IgnoreCase);
			if (!name.Success || name.Groups[1].Value != "__RequestVerificationToken")
			{
				continue;
			}

			var value = Regex.Match(input.Value, @"\bvalue\s*=\s*['""']([^'""']+)['""']", RegexOptions.IgnoreCase);
			Assert.True(value.Success, $"Missing antiforgery token value on {getPath}");
			return WebUtility.HtmlDecode(value.Groups[1].Value);
		}

		throw new InvalidOperationException($"Missing antiforgery token on {getPath}");
	}

	private static async Task<HttpResponseMessage> RegisterThroughFormAsync(HttpClient browser, string email)
	{
		var token = await AntiforgeryTokenAsync(browser, "/Account/Register");
		return await browser.PostAsync("/Account/Register", new FormUrlEncodedContent(
			new Dictionary<string, string>
			{
				["DisplayName"] = "New Student",
				["Email"] = email,
				["Password"] = ParticipantPassword,
				["ConfirmPassword"] = ParticipantPassword,
				["__RequestVerificationToken"] = token
			}));
	}

	private static async Task<HttpResponseMessage> LoginAttemptAsync(
		HttpClient browser, string email, string password, string? returnUrl = null)
	{
		var token = await AntiforgeryTokenAsync(browser, "/Account/Login");
		return await browser.PostAsync("/Account/Login", new FormUrlEncodedContent(
			new Dictionary<string, string>
			{
				["Email"] = email,
				["Password"] = password,
				["ReturnUrl"] = returnUrl ?? string.Empty,
				["__RequestVerificationToken"] = token
			}));
	}

	[Fact]
	public async Task DuplicateRegistration_IsRejectedWithoutCreatingSecondAccount()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		using var browser = CreateBrowserClient(host);

		using var first = await RegisterThroughFormAsync(browser, "newstudent@example.test");
		RedirectLocation(first);
		using var second = await RegisterThroughFormAsync(browser, "newstudent@example.test");
		Assert.Equal(HttpStatusCode.OK, second.StatusCode);
		var html = await second.Content.ReadAsStringAsync();
		Assert.Contains("already taken", html, StringComparison.OrdinalIgnoreCase);

		using var scope = host.Services.CreateScope();
		var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
		Assert.Equal(1, await users.Users.CountAsync());
	}

	[Fact]
	public async Task UnknownEmail_AndWrongPassword_ShowSameGenericLoginError()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		await CreateParticipantAsync(host);
		using var browser = CreateBrowserClient(host);

		using var unknown = await LoginAttemptAsync(browser,
			"unknown@example.test", "IncorrectPassword1!");
		using var wrong = await LoginAttemptAsync(browser,
			ParticipantEmail, "IncorrectPassword1!");
		Assert.Equal(HttpStatusCode.OK, unknown.StatusCode);
		Assert.Equal(HttpStatusCode.OK, wrong.StatusCode);
		Assert.Contains("Invalid login attempt.", await unknown.Content.ReadAsStringAsync());
		Assert.Contains("Invalid login attempt.", await wrong.Content.ReadAsStringAsync());
		var denied = await browser.GetAsync("/Participant");
		Assert.Equal("/Account/Login", RedirectLocation(denied).AbsolutePath);
	}

	[Fact]
	public async Task ExternalReturnUrl_FallsBackToLocalEventsPage()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		await CreateParticipantAsync(host);
		using var browser = CreateBrowserClient(host);

		using var response = await LoginAttemptAsync(browser,
			ParticipantEmail, ParticipantPassword, "https://attacker.example/path");
		var destination = RedirectLocation(response);
		Assert.StartsWith("/Events", destination.AbsolutePath);
	}

	[Fact]
	public async Task Logout_RejectsMissingToken_ThenEndsSignedInSession()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		await CreateParticipantAsync(host);
		using var browser = CreateBrowserClient(host);
		await SignInThroughLoginFormAsync(browser);

		using var withoutToken = await browser.PostAsync(
			"/Account/Logout", new FormUrlEncodedContent(new Dictionary<string, string>()));
		Assert.Equal(HttpStatusCode.BadRequest, withoutToken.StatusCode);
		using var stillSignedIn = await browser.GetAsync("/Participant");
		Assert.Equal(HttpStatusCode.OK, stillSignedIn.StatusCode);

		var token = await AntiforgeryTokenAsync(browser, "/Participant");
		using var logout = await browser.PostAsync("/Account/Logout",
			new FormUrlEncodedContent(new Dictionary<string, string>
			{
				["__RequestVerificationToken"] = token
			}));
		RedirectLocation(logout);
		using var afterLogout = await browser.GetAsync("/Participant");
		Assert.Equal("/Account/Login", RedirectLocation(afterLogout).AbsolutePath);
	}

	[Fact]
	public async Task OrganizerRole_AllowsTestOnlyProtectedEndpoint()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		await CreateParticipantAsync(host);
		using (var scope = host.Services.CreateScope())
		{
			var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
			var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
			Assert.True((await roles.CreateAsync(new IdentityRole(RoleNames.Organizer))).Succeeded);
			var user = await users.FindByEmailAsync(ParticipantEmail);
			Assert.NotNull(user);
			Assert.True((await users.AddToRoleAsync(user, RoleNames.Organizer)).Succeeded);
		}

		using var browser = CreateBrowserClient(host);
		await SignInThroughLoginFormAsync(browser);
		using var response = await browser.GetAsync("/__test-only/organizer");
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	[Theory]
	[InlineData("")]
	[InlineData(RoleNames.Organizer)]
	[InlineData(RoleNames.Administrator)]
	public async Task ParticipantPage_DisplaysAuthenticatedNameForEachRole(string role)
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		await CreateParticipantAsync(host);
		if (!string.IsNullOrEmpty(role))
		{
			using var scope = host.Services.CreateScope();
			var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
			var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
			Assert.True((await roles.CreateAsync(new IdentityRole(role))).Succeeded);
			var user = await users.FindByEmailAsync(ParticipantEmail);
			Assert.NotNull(user);
			Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
		}
		using var browser = CreateBrowserClient(host);
		await SignInThroughLoginFormAsync(browser);
		using var response = await browser.GetAsync("/Participant");
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var html = await response.Content.ReadAsStringAsync();
		Assert.Contains("Signed in as", html);
		Assert.Contains("<strong>" + ParticipantEmail + "</strong>", html);
		Assert.Contains("You are authenticated and can access this protected page.", html);
	}

	[Fact]
	public async Task OrganizerRoleTransition_RequiresTrustedMembershipAndFreshSignIn()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		await CreateParticipantAsync(host);
		using var participant = CreateBrowserClient(host);
		await SignInThroughLoginFormAsync(participant);
		Assert.Equal(HttpStatusCode.OK, (await participant.GetAsync("/Participant")).StatusCode);
		Assert.Equal(HttpStatusCode.Forbidden, (await participant.GetAsync("/__test-only/organizer")).StatusCode);
		using (var scope = host.Services.CreateScope())
		{
			var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
			var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
			Assert.True((await roles.CreateAsync(new IdentityRole(RoleNames.Organizer))).Succeeded);
			var user = await users.FindByEmailAsync(ParticipantEmail);
			Assert.NotNull(user);
			Assert.True((await users.AddToRoleAsync(user, RoleNames.Organizer)).Succeeded);
		}

		Assert.Equal(HttpStatusCode.Forbidden, (await participant.GetAsync("/__test-only/organizer")).StatusCode);
		using var organizer = CreateBrowserClient(host);
		await SignInThroughLoginFormAsync(organizer);
		Assert.Equal(HttpStatusCode.OK, (await organizer.GetAsync("/Participant")).StatusCode);
		Assert.Equal(HttpStatusCode.OK, (await organizer.GetAsync("/__test-only/organizer")).StatusCode);
	}

	[Theory]
	[InlineData("missing-display")]
	[InlineData("mismatch")]
	[InlineData("weak")]
	[InlineData("duplicate")]
	public async Task Registration_RejectedInput_DoesNotCreateUnintendedAccount(string scenario)
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		using var browser = CreateBrowserClient(host);
		var email = "validation@example.test";
		if (scenario == "duplicate")
		{
			using var registered = await RegisterThroughFormAsync(browser, email);
			RedirectLocation(registered);
		}
		var token = await AntiforgeryTokenAsync(browser, "/Account/Register");
		var password = scenario == "weak" ? "weak" : ParticipantPassword;
		using var response = await browser.PostAsync("/Account/Register", new FormUrlEncodedContent(
			new Dictionary<string, string>
			{
				["DisplayName"] = scenario == "missing-display" ? "" : "Validation Student",
				["Email"] = email, ["Password"] = password,
				["ConfirmPassword"] = scenario == "mismatch" ? "DifferentPassword1!" : password,
				["__RequestVerificationToken"] = token
			}));
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var html = await response.Content.ReadAsStringAsync();
		var expectedError = scenario switch
		{
			"missing-display" => "The DisplayName field is required.",
			"mismatch" => "do not match.",
			"weak" => "Passwords must be at least",
			_ => "already taken"
		};
		Assert.Contains(expectedError, WebUtility.HtmlDecode(html), StringComparison.OrdinalIgnoreCase);
		using var scope = host.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		Assert.Equal(scenario == "duplicate" ? 1 : 0, await context.Users.CountAsync());
	}

	[Fact]
	public async Task LegacyAdministratorEmailConflict_IsRejectedBeforeRoleProvisioning()
	{
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		using var host = CreateHost(database, FixedClock());
		using var scope = host.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		context.Users.Add(new ApplicationUser
		{
			Id = "legacy-account", Email = "admin@example.test", UserName = "admin@example.test",
			NormalizedEmail = null, NormalizedUserName = null, DisplayName = "Legacy account"
		});
		await context.SaveChangesAsync();
		var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
		{
			["SeedAccounts:Administrator:Email"] = "admin@example.test",
			["SeedAccounts:Administrator:Password"] = "DisposableAdminPassword1!",
			["SeedAccounts:Organizer:Email"] = "organizer@example.test",
			["SeedAccounts:Organizer:Password"] = "DisposableOrganizerPassword2!"
		}).Build();
		var error = await Assert.ThrowsAsync<InvalidOperationException>(() => IdentitySeedData.SeedAsync(scope.ServiceProvider, configuration));
		Assert.Contains("Administrator identity conflicts", error.Message);
		Assert.Equal(0, await context.Roles.CountAsync());
		Assert.Equal(0, await context.UserRoles.CountAsync());
	}
}
