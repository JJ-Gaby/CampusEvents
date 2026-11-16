# **Lesson 4 — Identity, Authentication, Roles, and Protected Access**

Revision: 2026.10.25.11 (document version 4.1.17)

<br>

# Table of Contents

- [Review of Last Lesson](#review-of-last-lesson)
- [Learning Objectives](#learning-objectives)
- [SRS Requirements and Use Cases](#srs-requirements-and-use-cases)
- [Guided Feature Preview](#guided-feature-preview)
- [Guided Design and Coding](#guided-design-and-coding)
	- [Start from the existing project and record a baseline](#start-from-the-existing-project-and-record-a-baseline)
	- [Connect the existing user and context to identity](#connect-the-existing-user-and-context-to-identity)
	- [Register identity services and authentication middleware](#register-identity-services-and-authentication-middleware)
	- [Generate and inspect the identity migration](#generate-and-inspect-the-identity-migration)
	- [Create account input models and complete the account controller](#create-account-input-models-and-complete-the-account-controller)
	- [Create semantic account forms and an access-denied page](#create-semantic-account-forms-and-an-access-denied-page)
	- [Define elevated roles and configure local development secrets](#define-elevated-roles-and-configure-local-development-secrets)
	- [Seed Administrator and Organizer access without changing event ownership](#seed-administrator-and-organizer-access-without-changing-event-ownership)
	- [Add the protected Participant endpoint](#add-the-protected-participant-endpoint)
	- [Make shared navigation reflect identity without enforcing access](#make-shared-navigation-reflect-identity-without-enforcing-access)
	- [Inspect the cumulative startup and application boundary](#inspect-the-cumulative-startup-and-application-boundary)
	- [Independent change — display the signed-in identity](#independent-change--display-the-signed-in-identity)
- [Guided Discussion](#guided-discussion)
	- [Identity, authentication, authorization, roles, and ownership](#identity-authentication-authorization-roles-and-ownership)
	- [Trust boundaries, identity services, and the authenticated request lifecycle](#trust-boundaries-identity-services-and-the-authenticated-request-lifecycle)
	- [Account registration, model binding, and layered validation](#account-registration-model-binding-and-layered-validation)
		- [Model binding, ModelState, and error presentation](#model-binding-modelstate-and-error-presentation)
	- [Protected resources, HTTP outcomes, and return navigation](#protected-resources-http-outcomes-and-return-navigation)
	- [Request forgery, safe HTTP methods, and session termination](#request-forgery-safe-http-methods-and-session-termination)
	- [Identity persistence, schema evolution, and relational continuity](#identity-persistence-schema-evolution-and-relational-continuity)
	- [Privileged role assignment, least privilege, and secret management](#privileged-role-assignment-least-privilege-and-secret-management)
	- [Security boundaries and verification strategies](#security-boundaries-and-verification-strategies)
- [Guided Verification and Validation](#guided-verification-and-validation)
	- [Automated](#automated)
		- [Preserve the SQL server test boundary and create a real HTTP host](#preserve-the-sql-server-test-boundary-and-create-a-real-http-host)
		- [Run end-to-end authentication and authorization assertions](#run-end-to-end-authentication-and-authorization-assertions)
	- [Manual](#manual)
		- [Verify registration, login, logout, and return behavior](#verify-registration-login-logout-and-return-behavior)
		- [Verify seeded-role and retained public-catalog behavior](#verify-seeded-role-and-retained-public-catalog-behavior)
	- [SRS Traceability Matrix Update with Evidence](#srs-traceability-matrix-update-with-evidence)
- [Guided Exercises](#guided-exercises)
	- [Exercise: trace the trust boundary](#exercise-trace-the-trust-boundary)
	- [Exercise: diagnose a rejected account request](#exercise-diagnose-a-rejected-account-request)
	- [Exercise: evaluate session and redirect defenses](#exercise-evaluate-session-and-redirect-defenses)
	- [Exercise: independently test a role transition](#exercise-independently-test-a-role-transition)
	- [Exercise: review the migration and repository boundary](#exercise-review-the-migration-and-repository-boundary)
- [Troubleshooting Common Problems](#troubleshooting-common-problems)
- [Checking Your Understanding](#checking-your-understanding)
- [Summary](#summary)
- [Checkpoint](#checkpoint)
- [Next Lesson Preview](#next-lesson-preview)
- [Appendix](#appendix)
	- [Authentication architecture and integration alternatives](#authentication-architecture-and-integration-alternatives)
	- [Routing, endpoint conventions, and access-control alternatives](#routing-endpoint-conventions-and-access-control-alternatives)
		- [Conventional and attribute routing](#conventional-and-attribute-routing)
		- [Policy-based and resource-based authorization](#policy-based-and-resource-based-authorization)
	- [Authentication cookies, session security, and redirect hardening](#authentication-cookies-session-security-and-redirect-hardening)
	- [Identity data architecture and migration alternatives](#identity-data-architecture-and-migration-alternatives)
	- [Security-test strategy and deployment trade-offs](#security-test-strategy-and-deployment-trade-offs)

# Review of Last Lesson

Lesson 3 distinguished persistent Event data from untrusted retrieval inputs and presentation state. You composed eligibility rules with optional search and Category constraints, counted and ordered matching results before bounded retrieval, and checked positive, negative, and boundary behavior with repeatable database tests and browser observations.

CEMS now has a SQL Server-backed, server-rendered public catalog with title/location search, Category filtering, 10/25/50 page sizes, state-preserving First/Previous/Next/Last links, and Event Details. The five-record Development seed and the ten catalog regression cases remain the starting point. Next, associate protected requests with a trustworthy authenticated user while preserving the public catalog behavior.

<br>

# Learning Objectives

By the end of this lesson, you should be able to:

1. **Distinguish** authentication, session continuity, authorization, and resource ownership as separate security decisions.
2. **Analyze** the trust boundaries crossed by credentials, browser cookies, form fields, return destinations, and server-managed roles.
3. **Design** a registration and sign-in flow that delegates password processing and identity persistence to trusted components.
4. **Evaluate** a relational identity-schema evolution for preservation of keys, foreign keys, existing records, and reproducibility.
5. **Apply** server-enforced access checks and request-forgery protections without relying on user-interface visibility.
6. **Compare** component checks, full HTTP requests, and browser verification using positive, negative, and boundary scenarios.
7. **Investigate** security and integration failures using observable responses, isolated state, controlled hypotheses, and regression checks.
8. **Practice** traceable, incremental change management while separating sensitive configuration from committed source code.


<br>

# SRS Requirements and Use Cases

The approved CEMS SRS defines the required account and protected-access behavior. The primary account workflow is **UC-002 — Create an Account and Authenticate**. You will integrate trusted account services, establish protected access, and prepare the shared identity foundation for Participant, Organizer, and Administrator workflows.

| SRS reference | Required behavior / constraint | Lesson implementation |
| --- | --- | --- |
| `UC-002`, `FR-ACC-001–002` | Register a unique Participant account | Registration GET/POST, input validation, Identity account creation, duplicate-email feedback |
| `FR-ACC-003–004` | Authenticate valid credentials and give generic failure feedback | `SignInManager`, application cookie, and a common error message |
| `FR-ACC-005`, `NFR-SEC-003` | End the current session safely | Antiforgery-protected Logout POST and subsequent protected-access check |
| `FR-ACC-006–007`, `BR-AUT-001` | Authenticate protected requests and preserve local return destinations | `/Participant`, login redirection, and local `ReturnUrl` validation |
| `BR-AUT-003–004`, `NFR-SEC-001` | Enforce privileged access on the server | Trusted Organizer and Administrator memberships and an Organizer-restricted HTTP scenario |
| `NFR-SEC-002–004`, SRS §9.3 | Use managed password authentication and safeguard state changes and secrets | Identity stores, form antiforgery, and Development user secrets |
| `NFR-SEC-005`, `NFR-USA-002`, `NFR-ACC-001–003`, `NFR-COM-002` | Provide safe errors and accessible interfaces | Validation feedback, labeled forms, responsive navigation, and access-denied presentation |
| `AC-002`, `AC-010` | Check account and protected-access behavior | Automated HTTP checks, SQL Server checks, and browser verification |

**Lesson-level acceptance contract:** Anonymous visitors can register unique accounts; valid credentials sign in, invalid credentials produce the same generic error, and logout ends the active session. Anonymous `/Participant` requests enter the local Login return flow, while signed-in users can open the protected page. A user without the `Organizer` role is denied at a server-protected role boundary. Public catalog behavior and the existing Organizer foreign key remain intact. Use the automated and manual checks to examine these behaviors in your application.

The SRS specifies observable account and access behavior. CEMS implements that behavior with ASP.NET Core Identity, a protected `/Participant` page, and controlled Development role provisioning. Record your progress against the existing SRS requirement IDs in the working traceability file.

<br>

# Feature Preview

This lesson extends the same server-rendered application used in Lessons 1–3. The public catalog at `/Events` remains available to Visitors. The account and access flow demonstrates registration, authenticated requests, role-based access, and safe session termination.

1. **Anonymous browsing:** A Visitor can still search, filter, paginate, and open eligible public Event Details.
2. **Register:** The Visitor opens the Register form, supplies a unique email, display name, and valid password, then returns to the public Events catalog. The catalog displays an account-created confirmation, and the Visitor can choose Login to authenticate. Duplicate registration is rejected with useful feedback.
3. **Protected request:** An anonymous request to `/Participant` challenges the caller and reaches Login with a local `ReturnUrl`.
4. **Sign in and return:** Valid credentials establish an application cookie; the browser follows the validated local return destination and displays the protected Participant page. Unknown email and incorrect password yield the same `Invalid login attempt.` message.
5. **Elevated access:** Sign in as an ordinary Participant and inspect the Organizer-restricted HTTP response in the verification activity.
6. **Logout:** The signed-in user submits the antiforgery-protected Logout POST. A subsequent request to `/Participant` requires authentication again.

**Screenshot placeholder — registration form:** Illustration needed: CEMS Register page with labeled fields, validation summary, visible keyboard focus, and responsive layout. Application state: anonymous Visitor opening Register.

**Screenshot placeholder — protected Participant page:** Illustration needed: `/Participant` page and signed-in navigation after returning from Login. Application state: authenticated Participant.

**Screenshot placeholder — rejected login or denied access:** Illustration needed: Login page with generic invalid-credential feedback or AccessDenied page. Application state: failed login or signed-in user denied an elevated-role operation.

**Request trace:**

```text
Anonymous GET /Participant
    → authentication challenge → GET /Account/Login?ReturnUrl=...
POST /Account/Login + valid credentials + antiforgery token
    → Identity verifies credentials → application cookie issued
    → validated local redirect → GET /Participant with cookie
POST /Account/Logout + antiforgery token
    → session ticket cleared → new GET /Participant is challenged
```

# Design and Coding

## Inspect the inherited solution

Inspect your existing ***Campus Events*** solution before making changes.

Make sure that you staged and commited the previous lesson:

```cmd
git show -s HEAD
```

**Expected output:** should contain "Lesson 03: Implemented and Verified".

Otherwise, build, test, run, stage, and commit:

```cmd
dotnet build
dotnet test
dotnet run --project src/CampusEvents
git add .
git commit -m "Lesson 03: Implemented and Verified"
```

Then, create a new branch:

```cmd
git switch -c lesson-04
```

## Connect the existing user and context to identity

Install the Identity EF integration package using the established framework version:

```cmd
dotnet package add Microsoft.AspNetCore.Identity.EntityFrameworkCore --project src/CampusEvents
```

The existing user already has a fixed primary key referenced by Events. Extend this type rather than adding a separate account object or redefining `Id`.

**File: `CampusEvents/Models/ApplicationUser.cs`**

```csharp
using Microsoft.AspNetCore.Identity;

namespace CampusEvents.Models;

public class ApplicationUser : IdentityUser
{
	public string DisplayName { get; set; } = string.Empty;

	public ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
}
```

Identity's `IdentityUser` now supplies `Id`, email, username, normalized lookup fields, password hash, stamps, and authentication state. CEMS continues to own `DisplayName` and `OrganizedEvents`. Do not declare a second user key.

Change the context base type to `IdentityDbContext<ApplicationUser>`, retain its Category and Event mappings, and call `base.OnModelCreating(modelBuilder)` so the Identity model is configured.

**File: `CampusEvents/Data/ApplicationDbContext.cs`**

```csharp
using CampusEvents.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
	{
	}

	public DbSet<Category> Categories => Set<Category>();

	public DbSet<Event> Events => Set<Event>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<ApplicationUser>(entity =>
		{
			entity.Property(user => user.DisplayName).IsRequired();
		});

		modelBuilder.Entity<Category>(entity =>
		{
			entity.Property(category => category.Name).IsRequired();
		});

		modelBuilder.Entity<Event>(entity =>
		{
			entity.Property(eventItem => eventItem.Title).IsRequired();
			entity.Property(eventItem => eventItem.Description).IsRequired();
			entity.Property(eventItem => eventItem.Location).IsRequired();

			entity.HasOne(eventItem => eventItem.Category)
				.WithMany(category => category.Events)
				.HasForeignKey(eventItem => eventItem.CategoryId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(eventItem => eventItem.Organizer)
				.WithMany(user => user.OrganizedEvents)
				.HasForeignKey(eventItem => eventItem.OrganizerId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.ToTable("Events", table =>
			{
				table.HasCheckConstraint("CK_Events_StartBeforeEnd", "[StartDateTime] < [EndDateTime]");

				table.HasCheckConstraint("CK_Events_CapacityPositive", "[Capacity] > 0");
			});
		});
	}
}
```

The inherited `Users` set is available from `IdentityDbContext`; the explicit `DbSet<Category>` and `DbSet<Event>` continue to serve domain queries. Preserve the existing Event constraints and foreign-key delete restrictions.

**Checkpoint — Identity model:** Review the updated user model and context, then generate the Identity migration.

<br>

## Register identity services and authentication middleware

Keep the current SQL Server `DefaultConnection`, `AddControllersWithViews()`, and singleton `TimeProvider.System` registration. In `CampusEvents/Program.cs`, add `using Microsoft.AspNetCore.Identity;` and `using CampusEvents.Models;` the following after `AddDbContext<ApplicationDbContext>`:

```csharp
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
```

The configured password policy requires at least eight characters, a digit, lowercase and uppercase letters. Explore recovery, verification, and operational policy choices in the Appendix. Ensure middleware appears **after** routing and **before** mapped endpoints:

```csharp
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
```

The meaningful order is routing → authentication → authorization → endpoint. `UseAuthentication` reconstructs a user from a valid ticket; `UseAuthorization` evaluates access requirements. Inspect the final integrated startup configuration in the cumulative `Program.cs` section.

**Check:** run `dotnet build`, then generate and inspect the Identity migration. Configure the classroom Development secrets before the first application startup that invokes `IdentitySeedData`.

<br>

## Generate and inspect the identity migration

Restore the approved local tool, generate a migration, and inspect the produced file *before* applying it:

```cmd
dotnet ef migrations add AddIdentityAuthentication --project src\CampusEvents --startup-project src\CampusEvents -- --environment Development
```

Verify that the tool version matches the established `10.0.13` EF Core version. Inspect the new authentication columns on `AspNetUsers`, Identity indexes, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetRoleClaims`, `AspNetUserLogins`, and `AspNetUserTokens`. For existing users, examine generated defaults for newly required Identity fields and confirm the seed initializer updates normalized usernames/emails and security stamps through `UserManager`. Do not accept an unexpected `DropTable("AspNetUsers")`, a new organizer key, or a replacement of `Events`.

On a personally authorized Development database, record the pre-upgrade Organizer ID, owned Event IDs and `OrganizerId` values, total user/Event/Category counts, and relevant constraints. A simple read-only SQL check is:

```sql
SELECT Id, DisplayName FROM dbo.AspNetUsers WHERE Id = 'seed-organizer-001';
SELECT Id, OrganizerId FROM dbo.Events WHERE OrganizerId = 'seed-organizer-001';
```

Review the generated `Up` and `Down` operations against this schema-preservation checklist:

- Preserve `AspNetUsers.Id`, the `Events.OrganizerId` foreign key, and existing Event, Category, and user rows.
- Compare existing primary keys, unique constraints, and indexes with the generated Identity indexes, especially those on normalized usernames and email addresses.
- Check new columns for nullability, column types, and defaults applied to existing rows. Review `NormalizedUserName`, `NormalizedEmail`, `SecurityStamp`, and `ConcurrencyStamp` explicitly.
- Confirm existing Event check constraints and Category relationships remain unchanged, and that the rollback operations target only the intended schema changes.

If the migration is non-destructive and consistent with the existing EF model, apply it:

```cmd
pushd CampusEvents
dotnet tool run dotnet-ef database update -- --environment Development
popd
dotnet ef database update --project src\CampusEvents --startup-project src\CampusEvents -- --environment Development
```

Repeat the read-only comparisons. Compare user IDs, Event foreign keys, relevant unique indexes and existing constraints before and after the upgrade. Confirm the existing `seed-organizer-001` row remains and every corresponding `Events.OrganizerId` is unchanged; inspect migration history and schema objects. Check both fresh-database creation and preservation of populated Development records. Resolve any proposed changes to existing user keys or persisted Event relationships before applying the migration.

<br>

**Checkpoint — Identity schema:** Inspect the upgraded SQL Server schema and compare the retained Organizer user and owned Events against the baseline. Configure both classroom accounts in Development user secrets before starting the application with Identity seeding enabled.

## Create account input models and complete the account controller

Create two purpose-specific request models for public forms—one for registration and one for login—instead of binding the persistent Identity user type. Validation attributes describe form fields and expected formats; Identity supplies separate password and uniqueness checks.

**File: `CampusEvents/ViewModels/RegisterViewModel.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace CampusEvents.ViewModels;

public class RegisterViewModel
{
	[Required]
	[StringLength(100)]
	public string DisplayName { get; set; } = string.Empty;

	[Required]
	[EmailAddress]
	public string Email { get; set; } = string.Empty;

	[Required]
	[DataType(DataType.Password)]
	public string Password { get; set; } = string.Empty;

	[Required]
	[DataType(DataType.Password)]
	[Compare(nameof(Password))]
	public string ConfirmPassword { get; set; } = string.Empty;
}
```

**File: `CampusEvents/ViewModels/LoginViewModel.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace CampusEvents.ViewModels;

public class LoginViewModel
{
	[Required]
	[EmailAddress]
	public string Email { get; set; } = string.Empty;

	[Required]
	[DataType(DataType.Password)]
	public string Password { get; set; } = string.Empty;

	public string? ReturnUrl { get; set; }
}
```

Create `CampusEvents/Controllers/AccountController.cs` with the cumulative Register GET/POST, Login GET/POST, protected Logout POST, and a browser-facing access-denied GET. The AccessDenied page explains an MVC authorization failure when the cookie handler redirects a signed-in browser.

Use `UserManager` to create accounts and `SignInManager` to verify credentials/issue or clear the authentication ticket. Local-return validation is performed **after** successful login.

**File: `CampusEvents/Controllers/AccountController.cs`**

```csharp
using CampusEvents.Models;
using CampusEvents.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CampusEvents.Controllers;

public class AccountController : Controller
{
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly SignInManager<ApplicationUser> _signInManager;

	public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
	{
		_userManager = userManager;
		_signInManager = signInManager;
	}

	[HttpGet]
	[AllowAnonymous]
	public IActionResult Register()
	{
		return View();
	}

	[HttpPost]
	[AllowAnonymous]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Register(RegisterViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var user = new ApplicationUser
		{
			UserName = model.Email,
			Email = model.Email,
			DisplayName = model.DisplayName
		};

		var result = await _userManager.CreateAsync(user, model.Password);

		if (result.Succeeded)
		{
			TempData["AccountNotice"] = "Account created. Sign in to continue.";
			return RedirectToAction("Index", "Events");
		}

		foreach (var error in result.Errors)
		{
			ModelState.AddModelError(string.Empty, error.Description);
		}

		return View(model);
	}

	[HttpGet]
	[AllowAnonymous]
	public IActionResult Login(string? returnUrl = null)
	{
		return View(new LoginViewModel
		{
			ReturnUrl = returnUrl
		});
	}

	[HttpPost]
	[AllowAnonymous]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Login(LoginViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, false);

		if (result.Succeeded)
		{
			if (Url.IsLocalUrl(model.ReturnUrl))
			{
				return LocalRedirect(model.ReturnUrl);
			}

			return RedirectToAction("Index", "Events");
		}

		ModelState.AddModelError(string.Empty, "Invalid login attempt.");

		return View(model);
	}

	[HttpPost]
	[Authorize]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Logout()
	{
		await _signInManager.SignOutAsync();

		return RedirectToAction("Index", "Events");
	}

	[HttpGet]
	[Authorize]
	public IActionResult AccessDenied()
	{
		Response.StatusCode = StatusCodes.Status403Forbidden;
		return View();
	}
}
```

Registration returns to public Events without authenticating automatically. The duplicate user/invalid-password errors from `IdentityResult` are added to `ModelState` and the model is redisplayed. The login route reports only `Invalid login attempt.` on a credential failure; an invalid external return URL falls back to the Events index.

The `AccessDenied` action requires authentication to avoid presenting role-denial messaging as a public account-enumeration endpoint. It sets **403** on its rendered HTML response. A conventional MVC role-denial challenge may first produce a redirect to this route; inspect the final response and intermediate redirect separately. .NET 10 API endpoints can return **403** directly instead of following the MVC browser redirect flow. A `Location` header on a 403 is not itself a redirect.

**Checkpoint:** run `dotnet build`; fix compilation errors before creating Views.

<br>

## Create semantic account forms and an access-denied page

Razor Tag Helpers bind labeled controls, validation spans, and a summary to the input models. Every POST form carries an antiforgery token. Keep the credentials in transient request state, never in an EF entity's extra form-only properties. Preserve shared HTML landmarks and stylesheet usage from Lesson 3.

**File: `CampusEvents/Views/Account/Register.cshtml`**

```cshtml
@model CampusEvents.ViewModels.RegisterViewModel

@{
	ViewData["Title"] = "Register";
}

<h1>Create Account</h1>

<p>
	Create a CEMS account. An authenticated account has Participant
	capabilities; no Participant role is required.
</p>

<form asp-controller="Account"
		asp-action="Register"
		method="post"
		asp-antiforgery="true"
		class="account-form">

	<div asp-validation-summary="ModelOnly"
		class="validation-summary"
		role="alert"></div>

	<div class="form-field">
		<label asp-for="DisplayName" class="field-label"></label>
		<input asp-for="DisplayName"
				class="field-control"
				autocomplete="name"
				required />
		<span asp-validation-for="DisplayName"
				class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="Email" class="field-label"></label>
		<input asp-for="Email"
				class="field-control"
				autocomplete="email"
				required />
		<span asp-validation-for="Email"
				class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="Password" class="field-label"></label>
		<input asp-for="Password"
				class="field-control"
				autocomplete="new-password"
				required />
		<span asp-validation-for="Password"
				class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="ConfirmPassword" class="field-label"></label>
		<input asp-for="ConfirmPassword"
				class="field-control"
				autocomplete="new-password"
				required />
		<span asp-validation-for="ConfirmPassword"
				class="validation-message"></span>
	</div>

	<button type="submit" class="button-primary">
		Create account
	</button>
</form>
```

**File: `CampusEvents/Views/Account/Login.cshtml`**

```cshtml
@model CampusEvents.ViewModels.LoginViewModel

@{
	ViewData["Title"] = "Login";
}

<h1>Login</h1>

<form asp-controller="Account"
		asp-action="Login"
		method="post"
		asp-antiforgery="true"
		class="account-form">

	<input asp-for="ReturnUrl" type="hidden" />

	<div asp-validation-summary="ModelOnly"
		class="validation-summary"
		role="alert"></div>

	<div class="form-field">
		<label asp-for="Email" class="field-label"></label>
		<input asp-for="Email"
				class="field-control"
				autocomplete="email"
				required />
		<span asp-validation-for="Email"
				class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="Password" class="field-label"></label>
		<input asp-for="Password"
				class="field-control"
				autocomplete="current-password"
				required />
		<span asp-validation-for="Password"
				class="validation-message"></span>
	</div>

	<button type="submit" class="button-primary">
		Login
	</button>
</form>
```

**File: `CampusEvents/Views/Account/AccessDenied.cshtml`**

```cshtml
@{
	ViewData["Title"] = "Access denied";
}

<h1>Access denied</h1>

<p>
	Your account is signed in but does not have permission to open
	that page.
</p>

<p>
	<a asp-controller="Events" asp-action="Index">Return to Events</a>
</p>
```

The hidden Login `ReturnUrl` preserves the requested local destination; the controller validates it before redirection. `asp-antiforgery="true"` works together with `[ValidateAntiForgeryToken]`; an `autocomplete` hint or HTML `required` attribute improves interaction but never replaces server checks. The access-denied page uses plain semantic text and navigation rather than displaying sensitive claims or an exception trace.

Add the following styles to the **existing** `CampusEvents/wwwroot/css/site.css` without deleting search, Event, pagination, or responsive layout styles:

**File: `CampusEvents/wwwroot/css/site.css`**

```css
.account-form {
	width: min(100%, 32rem);
	display: grid;
	gap: 1rem;
}

.form-field {
	display: grid;
	gap: 0.35rem;
}

.field-label {
	font-weight: 600;
}

.field-control {
	width: 100%;
	min-height: 2.5rem;
	padding: 0.55rem 0.65rem;
	border: 1px solid #8c959f;
	border-radius: 0.25rem;
	background: #ffffff;
	color: inherit;
	font: inherit;
}

.button-primary,
.button-secondary {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	min-height: 2.5rem;
	padding: 0.55rem 0.8rem;
	border: 1px solid #174ea6;
	border-radius: 0.25rem;
	font: inherit;
	text-decoration: none;
	cursor: pointer;
}

.button-primary {
	background: #174ea6;
	color: #ffffff;
}

.button-primary:hover {
	background: #123d84;
}

.button-secondary {
	background: #ffffff;
	color: #174ea6;
}

.button-secondary:hover {
	background: #f6f8fa;
}

.validation-summary,
.validation-message {
	color: #9a0007;
}

.validation-summary:empty,
.validation-message:empty {
	display: none;
}

.auth-status {
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	gap: 0.65rem;
}

.role-label {
	padding: 0.15rem 0.45rem;
	border: 1px solid #8c959f;
	border-radius: 999px;
	font-size: 0.85rem;
}

.logout-form {
	margin: 0;
}

.link-button {
	border: 0;
	padding: 0;
	background: transparent;
	color: #174ea6;
	font: inherit;
	text-decoration: underline;
	cursor: pointer;
}
```

Preserve the existing shared `:focus-visible`, form, and mobile navigation rules; do not overwrite the entire stylesheet with the excerpt. The account fields reuse `.field-control`, `.form-field`, and `.validation-message` in Lesson 5 Organizer forms.

**Checkpoint:** run `dotnet build`, then `dotnet run --project CampusEvents\CampusEvents.csproj`. Navigate to `/Account/Register` and `/Account/Login` to inspect label associations and error positions. Registration may now persist a user; login will require a valid stored account.

<br>

**Checkpoint — account workflow:** Open Register, create an account, and return to the Events catalog. Then sign in and out through the account forms, inspecting validation feedback on rejected input.

## Define elevated roles and configure local development secrets

The elevated Identity roles are `Organizer` and `Administrator`. All authenticated users share the Participant baseline.

**File: `CampusEvents/Security/RoleNames.cs`**

```csharp
namespace CampusEvents.Security;

public static class RoleNames
{
	public const string Organizer = "Organizer";

	public const string Administrator = "Administrator";
}
```

Generate **different** local Development credentials and store them outside the repository with Secret Manager. If the project already has a UserSecretsId, do not create an unrelated second store. The following password placeholders must be replaced locally; they are not passwords to use or commit.

```cmd
dotnet user-secrets init --project src\CampusEvents
dotnet user-secrets set "SeedAccounts:Administrator:Email" "admin@campus.local" --project src\CampusEvents
dotnet user-secrets set "SeedAccounts:Administrator:Password" "AdminDev2026!" --project src\CampusEvents
dotnet user-secrets set "SeedAccounts:Organizer:Email" "organizer@campus.local" --project src\CampusEvents
dotnet user-secrets set "SeedAccounts:Organizer:Password" "OrganizerDev2026!" --project src\CampusEvents
```

Use two genuinely strong, unique passwords meeting the configured policy. `dotnet user-secrets` stores Development configuration outside the project tree, but is **not** a production encrypted secret store. Do not put values in committed `appsettings.json`, `appsettings.Development.json`, source code, documentation screenshots, or command transcripts shared with others. Passwords in this lesson's isolated HTTP tests are disposable fixture data, not classroom account secrets.

<br>

## Seed Administrator and Organizer access without changing event ownership

Development seeding proceeds through five stages: **validate configuration → reconcile legacy account identifiers → create roles → initialize credentials → assign memberships**. The reconciliation reads existing account identifiers once in Development to resolve older records whose normalized Identity fields are empty; routine account lookup uses Identity services and indexed normalized fields. The Organizer retains user ID `seed-organizer-001` and its established Event ownership relationships. The Administrator uses the stable classroom ID `seed-administrator-001` to keep privileged account provisioning deliberate and repeatable. The seeder runs in Development, validates its configuration, and uses Identity results to make repeated initialization predictable.

Run classroom account provisioning in Development. Configured seed passwords initialize accounts requiring credentials; change established passwords through authorized Identity password-reset operations. Existing account passwords and Event ownership are preserved. The seeded Organizer retains its `ApplicationUser.Id` while `UserManager` updates the configured email used for login, normalized lookup fields, and required account metadata. Resolve provisioning errors against the existing account records.

**File: `CampusEvents/Data/IdentitySeedData.cs`**

```csharp
using CampusEvents.Models;
using CampusEvents.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace CampusEvents.Data;

public static class IdentitySeedData
{
	private const string ExistingOrganizerId = "seed-organizer-001";
	private const string ClassroomAdministratorId = "seed-administrator-001";
	private const string OrganizerDisplayName = "Campus Events Office";
	private const string AdministratorDisplayName = "CEMS Administrator";

	public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
	{
		var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
		var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

		var administratorEmail = configuration["SeedAccounts:Administrator:Email"];
		var administratorPassword = configuration["SeedAccounts:Administrator:Password"];
		var organizerEmail = configuration["SeedAccounts:Organizer:Email"];
		var organizerPassword = configuration["SeedAccounts:Organizer:Password"];

		if (string.IsNullOrWhiteSpace(administratorEmail) || string.IsNullOrWhiteSpace(administratorPassword)
			|| string.IsNullOrWhiteSpace(organizerEmail) || string.IsNullOrWhiteSpace(organizerPassword))
		{
			throw new InvalidOperationException("Configure both classroom accounts in Development user secrets.");
		}

		var normalizedAdministratorEmail = userManager.NormalizeEmail(administratorEmail);
		var normalizedOrganizerEmail = userManager.NormalizeEmail(organizerEmail);
		var normalizedAdministratorName = userManager.NormalizeName(administratorEmail);
		var normalizedOrganizerName = userManager.NormalizeName(organizerEmail);
		if (string.Equals(normalizedAdministratorEmail, normalizedOrganizerEmail, StringComparison.Ordinal))
		{
			throw new InvalidOperationException("Administrator and Organizer seed emails must be different.");
		}

		var organizerById = await userManager.FindByIdAsync(ExistingOrganizerId);
		var administratorById = await userManager.FindByIdAsync(ClassroomAdministratorId);

		// Reconcile legacy Development records with uninitialized normalized fields.
		// Routine account lookup uses UserManager and indexed Identity fields.
		var context = services.GetRequiredService<ApplicationDbContext>();
		var accountIdentifiers = await context.Users.AsNoTracking()
			.Select(user => new { user.Id, user.Email, user.UserName }).ToListAsync();

		foreach (var account in accountIdentifiers)
		{
			var email = userManager.NormalizeEmail(account.Email ?? string.Empty);
			var userName = userManager.NormalizeName(account.UserName ?? string.Empty);
			var matchesAdministrator = email == normalizedAdministratorEmail || userName == normalizedAdministratorName;
			var matchesOrganizer = email == normalizedOrganizerEmail || userName == normalizedOrganizerName;

			if (matchesAdministrator && account.Id != ClassroomAdministratorId)
			{
				throw new InvalidOperationException("Administrator identity conflicts with an existing account.");
			}

			if (matchesOrganizer && account.Id != ExistingOrganizerId)
			{
				throw new InvalidOperationException("Organizer identity conflicts with an existing account.");
			}
		}

		if (administratorById is not null &&
			!string.Equals(userManager.NormalizeEmail(administratorById.Email ?? ""), normalizedAdministratorEmail,
				StringComparison.Ordinal))
		{
			throw new InvalidOperationException("Classroom Administrator email differs from the configured address.");
		}

		var existingOrganizerEmail = organizerById?.Email;
		if (!string.IsNullOrWhiteSpace(existingOrganizerEmail))
		{
			var normalizedExistingEmail = userManager.NormalizeEmail(existingOrganizerEmail);
			if (accountIdentifiers.Any(account => account.Id != ExistingOrganizerId
				&& userManager.NormalizeEmail(account.Email ?? string.Empty) == normalizedExistingEmail))
			{
				throw new InvalidOperationException("Existing Organizer email belongs to another account.");
			}
		}

		await EnsureRoleAsync(roleManager, RoleNames.Administrator);
		await EnsureRoleAsync(roleManager, RoleNames.Organizer);
		await EnsureAdministratorAsync(userManager, administratorEmail, administratorPassword);
		await EnsureOrganizerAsync(userManager, organizerEmail, organizerPassword);
	}

	private static void EnsureSucceeded(IdentityResult result, string operation)
	{
		if (result.Succeeded)
		{
			return;
		}

		var descriptions = string.Join("; ", result.Errors.Select(error => error.Description));

		throw new InvalidOperationException($"Identity seeding failed while {operation}: {descriptions}");
	}

	private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
	{
		if (await roleManager.RoleExistsAsync(roleName))
		{
			return;
		}

		var result = await roleManager.CreateAsync(new IdentityRole(roleName));

		EnsureSucceeded(result, $"creating role '{roleName}'");
	}

	private static async Task EnsureAdministratorAsync(
		UserManager<ApplicationUser> userManager, string email, string password)
	{
		var user = await userManager.FindByIdAsync(ClassroomAdministratorId);

		if (user is null)
		{
			user = new ApplicationUser
			{
				Id = ClassroomAdministratorId,
				UserName = email,
				Email = email,
				DisplayName = AdministratorDisplayName
			};

			var createResult = await userManager.CreateAsync(user, password);

			EnsureSucceeded(createResult, "creating the classroom Administrator account");
		}
		else
		{
			if (string.IsNullOrEmpty(user.SecurityStamp))
			{
				user.SecurityStamp = Guid.NewGuid().ToString();
			}

			user.UserName = email;
			user.Email = email;
			user.DisplayName = AdministratorDisplayName;

			var updateResult = await userManager.UpdateAsync(user);

			EnsureSucceeded(updateResult, "updating the classroom Administrator account");

			if (!await userManager.HasPasswordAsync(user))
			{
				var passwordResult = await userManager.AddPasswordAsync(user, password);

				EnsureSucceeded(passwordResult, "adding the classroom Administrator password");
			}
		}

		if (!await userManager.IsInRoleAsync(user, RoleNames.Administrator))
		{
			var roleResult = await userManager.AddToRoleAsync(user, RoleNames.Administrator);

			EnsureSucceeded(roleResult, "assigning the Administrator role");
		}
	}

	private static async Task EnsureOrganizerAsync(UserManager<ApplicationUser> userManager, string email, string password)
	{
		var user = await userManager.FindByIdAsync(ExistingOrganizerId);

		if (user is null)
		{
			user = new ApplicationUser
			{
				Id = ExistingOrganizerId,
				UserName = email,
				Email = email,
				DisplayName = OrganizerDisplayName
			};

			var createResult = await userManager.CreateAsync(user, password);

			EnsureSucceeded(createResult, "creating the classroom Organizer account");
		}
		else
		{
			if (string.IsNullOrEmpty(user.SecurityStamp))
			{
				user.SecurityStamp = Guid.NewGuid().ToString();
			}

			user.UserName = email;
			user.Email = email;
			user.DisplayName = OrganizerDisplayName;

			var updateResult = await userManager.UpdateAsync(user);

			EnsureSucceeded(updateResult, "updating the existing seed Organizer identity");

			if (!await userManager.HasPasswordAsync(user))
			{
				var passwordResult = await userManager.AddPasswordAsync(user, password);

				EnsureSucceeded(passwordResult, "adding the classroom Organizer password");
			}
		}

		if (!await userManager.IsInRoleAsync(user, RoleNames.Organizer))
		{
			var roleResult = await userManager.AddToRoleAsync(user, RoleNames.Organizer);

			EnsureSucceeded(roleResult, "assigning the Organizer role");
		}
	}
}
```

For existing accounts, the migration supplies the Identity columns and their schema defaults. `UserManager.UpdateAsync` prepares normalized username/email values; `AddPasswordAsync` initializes a password when needed. The Organizer keeps its original stable ID, so existing Event ownership stays intact. Both classroom identities are resolved through their stable IDs. The initializer compares configured addresses with existing email and username values, including migrated records awaiting normalized-field updates, before creating privileged roles. `EnsureSucceeded` reports Identity errors immediately. Development secrets supply initial account credentials; existing passwords remain in place. Use an authorized password-reset operation to change an existing classroom password.

**Checkpoint — migrated Organizer account:** After starting the application with both classroom accounts configured, inspect `AspNetUsers` and the role mappings in the Development database:

```sql
SELECT Id, Email, NormalizedEmail, UserName, NormalizedUserName,
       CASE WHEN PasswordHash IS NULL THEN 0 ELSE 1 END AS HasPassword,
       CASE WHEN SecurityStamp IS NULL THEN 0 ELSE 1 END AS HasSecurityStamp
FROM dbo.AspNetUsers WHERE Id = 'seed-organizer-001';

SELECT userRoles.UserId, roles.Name
FROM dbo.AspNetUserRoles AS userRoles
JOIN dbo.AspNetRoles AS roles ON roles.Id = userRoles.RoleId
WHERE userRoles.UserId = 'seed-organizer-001';

SELECT Id, OrganizerId FROM dbo.Events WHERE OrganizerId = 'seed-organizer-001';
```

Confirm the original user key and Event references, normalized username and email, available password hash, initialized security stamp, and Organizer role. Sign in with the configured classroom Organizer credentials. Keep secrets out of screenshots and logs.

<br>

## Add the protected Participant endpoint

Create a small endpoint that demands only authentication. Use this endpoint to observe authenticated access before building more detailed Participant features.

**File: `CampusEvents/Controllers/ParticipantController.cs`**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusEvents.Controllers;

public class ParticipantController : Controller
{
	[Authorize]
	public IActionResult Index()
	{
		return View();
	}
}
```

**File: `CampusEvents/Views/Participant/Index.cshtml`**

```cshtml
@{
	ViewData["Title"] = "Participant Area";
}

<h1>Participant Area</h1>

<p>
	You are authenticated and can access this protected page.
</p>

```

`[Authorize]` on the Participant action requires an authenticated principal. An anonymous request enters the configured Login flow. A signed-in account with no elevated role should reach this page.

<br>

## Make shared navigation reflect identity without enforcing access

Update the existing project-owned shared layout to keep public Home/Events/Privacy links, display Register/Login to anonymous visitors, and display Participant Area, account identity, permitted elevated role labels, and a protected POST Logout form to authenticated users.

**File: `CampusEvents/Views/Shared/_Layout.cshtml`**

```cshtml
@using CampusEvents.Security

<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset="utf-8" />
	<meta name="viewport" content="width=device-width, initial-scale=1.0" />
	<title>@ViewData["Title"] - CampusEvents</title>
	<link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
</head>
<body>
	<header class="site-header">
		<div class="site-shell site-header-inner">
			<a class="site-brand"
				asp-controller="Home"
				asp-action="Index">CampusEvents</a>

			<nav class="site-nav" aria-label="Primary navigation">
				<ul class="site-nav-list">
					<li>
						<a class="site-nav-link"
							asp-controller="Home"
							asp-action="Index">Home</a>
					</li>
					<li>
						<a class="site-nav-link"
							asp-controller="Events"
							asp-action="Index">Events</a>
					</li>
					<li>
						<a class="site-nav-link"
							asp-controller="Home"
							asp-action="Privacy">Privacy</a>
					</li>

					@if (User.Identity?.IsAuthenticated == true)
					{
						<li>
							<a class="site-nav-link"
								asp-controller="Participant"
								asp-action="Index">Participant Area</a>
						</li>
					}
					else
					{
						<li>
							<a class="site-nav-link"
								asp-controller="Account"
								asp-action="Register">Register</a>
						</li>
						<li>
							<a class="site-nav-link"
								asp-controller="Account"
								asp-action="Login">Login</a>
						</li>
					}
				</ul>

				@if (User.Identity?.IsAuthenticated == true)
				{
					<div class="auth-status">
						<span>@User.Identity.Name</span>

						@if (User.IsInRole(RoleNames.Organizer))
						{
							<span class="role-label">Organizer</span>
						}

						@if (User.IsInRole(RoleNames.Administrator))
						{
							<span class="role-label">Administrator</span>
						}

						<form asp-controller="Account"
								asp-action="Logout"
								method="post"
								asp-antiforgery="true"
								class="logout-form">
							<button type="submit"
									class="link-button">
								Logout
							</button>
						</form>
					</div>
				}
			</nav>
		</div>
	</header>

	<main class="site-shell page-content">
		@if (TempData["AccountNotice"] is string accountNotice)
		{
			<p class="account-notice" role="status">@accountNotice</p>
		}
		@RenderBody()
	</main>

	<footer class="site-footer">
		<div class="site-shell">
			<span>&copy; CampusEvents</span>
		</div>
	</footer>
</body>
</html>
```

The layout shows account confirmation and signed-in navigation, including informative role labels. Endpoint attributes enforce the access rules. At narrow widths, check wrapping and keyboard focus using the cumulative stylesheet.

<br>

**Checkpoint — protected access:** Compare an anonymous `/Participant` request with a signed-in request, then submit Logout and repeat the protected request.

## Inspect the cumulative startup and application boundary

Use this final integrated `Program.cs` to confirm that earlier public behavior and the new Identity pipeline share one application and one SQL Server context. `DbInitializer` runs only in Development, and classroom Identity seeding follows it in the same scoped startup branch.

**File: `CampusEvents/Program.cs`**

```csharp
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
```

The final `public partial class Program { }` exposes the application entry point to `WebApplicationFactory<Program>` in the integration-test assembly. Place the Organizer authorization test controller in `CampusEvents.Tests` so the integration-test host can exercise its protected endpoint.

Before testing security endpoints, run:

```cmd
dotnet build
dotnet run --project CampusEvents\CampusEvents.csproj
```

Check these intermediate outcomes: public `/Events` and existing Details remain navigable; Register and Login forms render; a valid fresh account can log in; `/Participant` requires authentication; the classroom Organizer's owned Events still reference `seed-organizer-001`. Confirm the catalog remains accessible through the updated shared navigation. Resolve build, migration, and configuration errors before beginning verification.

<br>

## Independent change — display the signed-in identity

Make a small, **independent** change after the protected page is working. The initial `CampusEvents/Views/Participant/Index.cshtml` deliberately shows the authenticated-access message without displaying the account name. Extend this existing View so that a short paragraph immediately below its `<h1>` says **Signed in as** followed by the authenticated name in a `<strong>` element. Use `@User.Identity?.Name`, following the identity display pattern already present in the shared layout; Razor's normal HTML encoding must remain in effect.

Use the authenticated principal already available in the Razor View to display the signed-in account name. Verify the change by signing in separately as an ordinary registered account, the controlled Organizer account, and the controlled Administrator account. Each should see **its own** authenticated name on `/Participant`. In a fresh anonymous session, a direct `/Participant` request must still redirect to Login rather than expose the View.

**Completion check:** confirm the label and current account name appear for all three signed-in accounts, that the original protected-page message remains visible, and that the public `/Events` page is unchanged. Include this completed view refinement in the Lesson 4 implementation before recording the final source-control checkpoint.

<br>

# Guided Discussion

Use the completed account and protected-access workflow to examine the trust, HTTP, and persistence boundaries involved in identity management.

## Identity, authentication, authorization, roles, and ownership

**Authentication** establishes who made a request. **Authorization** determines whether that identity is allowed to invoke an operation. A cookie-backed session carries validated identity across otherwise independent HTTP requests. **Role authorization** grants access according to trusted membership; **resource ownership** further checks whether that user may act on a specific record.

CEMS uses this access model:

```text
Anonymous Visitor                  public browsing
Authenticated user                 Participant baseline
Authenticated + Organizer role     additional Organizer capabilities
Authenticated + Administrator role additional Administrator capabilities
```

CEMS treats any signed-in user as a Participant without creating a separate `Participant` role. An Organizer action combines the trusted Organizer role with an ownership check against `Event.OrganizerId`. Enforce both decisions on the server.

The design separates concerns: endpoint attributes/policies express authorization, trusted account services establish identities and roles, and domain operations check resource-specific eligibility. Putting every security decision in navigation code would couple presentation to enforcement and make direct-URL bypasses likely.

<br>

An access decision follows a sequence: **(1)** resolve a validated principal; **(2)** verify whether an endpoint requires sign-in; **(3)** apply any required role or policy; and, for resource-specific actions, **(4)** load the target record and compare its owner with the current user's immutable identifier. For an Event update, resolve the owner from the persisted Event and the current authenticated user ID.

| Caller | Public `/Events` | Protected `/Participant` | Organizer-role endpoint |
| --- | --- | --- | --- |
| Anonymous Visitor | Allowed | Login challenge | Login challenge |
| Authenticated user without elevated role | Allowed | Allowed | Forbidden |
| Authenticated Organizer | Allowed | Allowed | Allowed |
| Authenticated Administrator without Organizer role | Allowed | Allowed | Forbidden |

Role membership grants operation-level access. To authorize a change to an Event, also compare `Event.OrganizerId` with the authenticated user ID and enforce the result on the server.


## Trust boundaries, identity services, and the authenticated request lifecycle

Password storage, credential checks, role membership, user lookup, cookie issuance, and stamp validation are interdependent security responsibilities. Implementing them as ad hoc application code increases risk and maintenance cost. CEMS uses ASP.NET Core Identity for account persistence and password hashing/verification, `UserManager` for user operations, `SignInManager` for sign-in/out, and `RoleManager` for role management. The application still owns its specific `DisplayName` and `Event → Organizer` relationship.

```text
POST credentials
	→ model validation
	→ trusted credential verification
	→ signed authentication cookie returned to browser
	→ browser sends cookie on later request
	→ authentication middleware validates/reconstructs User
	→ authorization middleware checks endpoint access
	→ MVC action is reached only if permitted
```

After sign-in, the browser sends the protected authentication cookie with subsequent requests. The authentication handler validates the ticket and constructs the trusted principal. Logout invalidates the browser's current authentication ticket, rather than deleting application user data.

CEMS uses **email as `UserName`**, and requires unique emails. An immutable Identity user `Id` remains separate from a changeable email address, which is essential because an Event's `OrganizerId` refers to the user key, not to an email string.

<br>

**Where trust changes:** The browser supplies credentials and later presents opaque cookie data; Identity establishes the account ID and trusted role claims. Identity verifies account credentials, the authentication handler validates the cookie and reconstructs the principal, and authorization evaluates the trusted principal for each protected request. Server-managed roles and the validated account identifier supply the authorization decisions for protected requests.

**Responsibility map:** `UserManager` creates and looks up user records; `SignInManager` verifies sign-in and manages the application authentication ticket; `RoleManager` maintains known roles. The EF Core stores persist user and role data. Middleware authenticates the incoming HTTP request before MVC's authorization decisions. Keep application domain decisions, including Event ownership, outside password and cookie infrastructure.

**Session transition:** Registration stores an account and returns the Visitor to the Events catalog for a separate sign-in. Login creates an authentication ticket after successful credential verification. Subsequent requests carry the issued ticket. Logout ends authentication for the current browser session while the account remains available for later sign-in.


## Account registration, model binding, and layered validation

A form-specific input model accepts only intended client-supplied fields. It must not bind arbitrary Identity properties such as roles, security stamps, password hashes, or user IDs. MVC model binding and annotations validate form shape; Identity then enforces account rules, password strength, and duplicate identity checks. Neither layer replaces the other.

After creating an account, the Visitor returns to the catalog and signs in through Login. When Identity rejects a registration, redisplay entered values and validation messages, while the password remains a form credential handled by Identity.

Login errors should not reveal whether an email is registered. For both a nonexistent email and a wrong password, respond with the same user-facing message: `Invalid login attempt.` This is distinct from the duplicate registration response, where the account-creation workflow must explain why creation was rejected.

<br>

### Model binding, ModelState, and error presentation

When a form is submitted to an MVC action, **model binding** reads the submitted field names and values and tries to populate the action's input model. **Model validation** then applies constraints such as `[Required]`, `[StringLength]`, `[EmailAddress]`, and `[Compare]`. Binding failures, missing required values, and validation failures are recorded in the controller's `ModelState` before the action body executes. `[DataType(DataType.Password)]` supplies input/display metadata; it does not, by itself, enforce password strength.

The controller checks this request-validation state **before** attempting to create an account:

```csharp
if (!ModelState.IsValid)
{
	return View(model);
}
```

Returning `View(model)` redisplays attempted values with validation feedback. Razor's `asp-validation-for` renders property-specific errors beside their fields; `asp-validation-summary="ModelOnly"` renders errors not associated with a particular property. `ModelState` also retains submitted value information for ordinary form inputs, so learners can correct a field without starting again. Password inputs should not be deliberately repopulated or recorded in logs or test evidence.

A request can pass MVC's annotations and still fail **Identity's account rules**. For example, a syntactically valid email can duplicate an existing account, or a supplied password can violate the configured Identity password policy. The controller adds each failed `IdentityResult` description as a model-level error (`ModelState.AddModelError(string.Empty, error.Description)`) and returns the form. No successful user creation is claimed or sign-in cookie issued in that failure path. The login workflow adds the same generic model-level error for rejected credentials, preventing account enumeration.

When diagnosing a rejected request, inspect model binding and validation, then Identity results, then the HTTP security response as appropriate.

<br>

**Validation sequence:** Bind the account inputs, evaluate `ModelState`, and pass valid requests to Identity for account checks such as unique email and password policy; finally persistence decides whether the new user record was stored. These are independent outcomes. For example, a syntactically valid email can still be rejected as a duplicate. After account creation, inspect the redirected page and confirm the new account was persisted.

**Error boundary:** Registration may explain that account creation failed because the supplied email is already in use; Login must return the same generic `Invalid login attempt.` message whether the email is unknown or its password is wrong (`FR-ACC-004`). The UI can retain nonsecret attempted values after a failed submission, but it must not deliberately refill password fields or include submitted credentials in logs or evidence. Apply the same `ModelState` handling whenever MVC receives editable form input.


## Protected resources, HTTP outcomes, and return navigation

An anonymous request for `/Participant` should lead to Login, then return to the originally requested route after valid credentials are supplied. Preserve that navigation context in a `ReturnUrl` field through the login GET and POST. But a return URL is **untrusted user input**, even when rendered as a hidden input.

A URL such as `https://attacker.example/` must never become an unrestricted post-login redirect. Check `Url.IsLocalUrl(returnUrl)` and use `LocalRedirect` only when it is local; otherwise fall back to `/Events`. This prevents an attacker from turning a legitimate login page into an open-redirect phishing link. A local destination still requires its own authorization checks when requested.

<br>

**Challenge versus forbid:** A challenge occurs when a protected endpoint receives no authenticated principal; for this cookie-based MVC application it commonly redirects to Login and supplies a local return destination. A forbid occurs when a principal is authenticated but lacks the required role; the cookie handler may route the browser to AccessDenied, while direct HTTP observations can expose the underlying redirect or 403 depending on endpoint and request configuration. Inspect the initial HTTP status and `Location` header to trace the navigation.

Validate `ReturnUrl` as a local navigation destination, including when the value returns through a hidden form field. After valid login, a local URL can be used to navigate back to the requested page, but that destination independently repeats its authentication and authorization decisions. Invalid or nonlocal values must use the safe fallback already implemented in this lesson.


## Request forgery, safe HTTP methods, and session termination

Cookie-authenticated browsers automatically attach eligible cookies. An attacker may try to induce a signed-in browser to send a state-changing request. This **cross-site request forgery (CSRF)** risk is different from knowing the user's password. Registration and Login also deserve CSRF protection because they change account/session state; Logout must be a protected POST with a valid antiforgery token.

Use form Tag Helpers with `asp-antiforgery="true"` and `[ValidateAntiForgeryToken]` on POST actions. A forged or tokenless POST should fail before its action changes server state. `SameSite` cookie policy reduces some cross-site cookie exposure but is not a substitute for server-enforced antiforgery validation. Likewise, a safe GET retrieves content rather than deliberately changing login state; Logout is not a GET navigation link.

Client-side `required` feedback improves usability, but server-side validation remains authoritative because callers can bypass the rendered form. Validation failures must not accidentally issue a cookie, create a duplicate user, or sign a user out.

<br>


## Identity persistence, schema evolution, and relational continuity

The catalog initializer establishes the Development user and Event records. The Identity initializer then prepares those persisted accounts for authentication and trusted role membership while retaining their original keys.

Evolve the populated SQL Server database while preserving existing accounts and their Event relationships. The established `AspNetUsers` record with ID `seed-organizer-001` remains the owner referenced by `Events.OrganizerId`. Identity extends that table with account fields and adds tables for roles, claims, logins, and tokens. Inspect the migration's `Up` and `Down` operations, retaining existing user keys, Events, Categories, foreign keys, check constraints, and unique indexes.

The seeded Organizer retains ID `seed-organizer-001`, while the provisioned classroom Administrator uses `seed-administrator-001`. A conflicting configured Administrator email is rejected instead of granting that account a privileged role. The upgrade has two connected stages. **Schema migration** adds Identity columns, indexes, and the required database defaults for existing records. **Account preparation** uses `UserManager.UpdateAsync` to normalize the Organizer's username and email through the configured Identity normalizers and persist account-security metadata. `NormalizedUserName` and `NormalizedEmail` support Identity lookups; `SecurityStamp` supports validation of security-sensitive account changes; `ConcurrencyStamp` supports optimistic concurrency checks. Inspect existing rows after the upgrade and before signing in.

A conceptual schema comparison illustrates the retained ownership key:

```text
Before: AspNetUsers.Id ← Events.OrganizerId
After:  AspNetUsers.Id ← Events.OrganizerId
           │
           ├── AspNetUserRoles → AspNetRoles
           ├── AspNetUserClaims
           ├── AspNetUserLogins
           └── AspNetUserTokens
```

Compare the original Organizer ID, linked Event IDs, normalized account fields, and Identity metadata before and after preparation. Identity uniqueness depends on normalized identifiers; check that legacy usernames and emails satisfy the resulting indexes. Sign in using the configured Organizer credentials and inspect the resulting role membership. Keep passwords and account secrets in Development user secrets.

## Privileged role assignment, least privilege, and secret management

Public registration creates an ordinary account. The server assigns Identity keys and security fields, and trusted role-management operations grant elevated permissions. Granting Organizer or Administrator authority is a separate **trusted server-side operation**, performed in this lesson through controlled Development seeding rather than public registration.

**Least privilege** assigns each principal the capabilities appropriate to its purpose. Authentication establishes Participant access, while the Organizer and Administrator roles grant elevated permissions. Organizer membership opens additional Organizer endpoints but does not establish ownership of every Event. Administrator membership enables later oversight capabilities without silently implying Organizer membership. Apply Organizer and Administrator membership independently, and evaluate Event ownership for resource-specific operations.

CEMS uses Development user secrets to supply controlled seed credentials without embedding them in Markdown commands, `appsettings.json`, Git history, console logs, or test evidence. Keep Development-only provisioning fenced by the environment check, idempotent for repeated starts, and tied to the existing seeded Organizer identity so previously stored Events retain their owner. Use a reviewed administrative provisioning process and managed secrets in deployed environments.


## Security boundaries and verification strategies

Use hosted HTTP requests with a cookie-aware client to exercise authentication middleware, authorization, and antiforgery checks together. Direct controller tests support focused action-logic checks. SQL Server-backed tests can additionally assert identity persistence and relationship continuity. Human browser checks reveal actual rendered forms, focus order, layout, navigation, and clarity of error messages.

Compare positive cases, negative cases, and transitions: unknown password versus known-user wrong password; anonymous versus authenticated request; authenticated user without a role versus the same user with it; valid token versus missing token; and authenticated access before versus after logout. Inspect the response status, redirect destination, and stored-data state for the relevant scenario.

<br>


# Guided Verification and Validation

Run the automated account and access checks against isolated test data, then inspect the account workflows in the browser.

<br>

## Automated

**Security verification scenarios:**

| Scenario | Observation | Follow-up check |
| --- | --- | --- |
| Anonymous access to `/Participant` | Login redirect with local `ReturnUrl` | Sign in and revisit protected page |
| Incorrect password or unknown email | Same generic error message | Verify both credential cases |
| Participant requests Organizer-only endpoint | Forbidden response | Repeat as signed-in Organizer |
| State-changing POST without antiforgery token | Request rejected | Inspect the related account or session state |
| Logout | Authentication cookie cleared | Request `/Participant` again |
| Identity migration on existing data | User IDs and Event foreign keys preserved | Inspect role and identity tables |


Run the ten inherited catalog regression cases alongside the eight authentication and authorization HTTP cases. Complete the legacy-email conflict scenario in the manual Development verification section.


<br>

### Preserve the SQL server test boundary and create a real HTTP host

Keep the existing `CampusEvents.Tests` xUnit project, the revised Lesson 2 `IsolatedSqlServerDatabase` fixture, and the six catalog test methods covering ten cases. The fixture defaults to `(localdb)\MSSQLLocalDB` on Windows, generates a unique database name per test, applies real SQL Server migrations, and deletes only the database it created. Use the isolated fixture’s generated database and lifecycle management. For an authorized non-default SQL Server test endpoint, set only the optional `CEMS_TEST_SQLSERVER` to a connection targeting `master`. Never fall back to Development `DefaultConnection`, `EnsureCreated`, or SQLite/in-memory substitutes.

Add MVC HTTP-host testing to the *existing* test project. Keep the existing xUnit test-serialization setting because the fixture clears SQL connection pools during per-database cleanup:

```cmd
dotnet package add Microsoft.AspNetCore.Mvc.Testing --project CampusEvents.Tests
dotnet build
```

**File: `CampusEvents.Tests/AssemblyInfo.cs`**

```csharp
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

The test host reuses production middleware, real Identity services, MVC filters, and SQL Server migrations. It substitutes only an isolated test connection, a fresh `FakeTimeProvider` with explicit UTC−04 local zone, and one controller declared in the test assembly to demonstrate elevated role authorization. Test-host setup uses `Testing`, so Development classroom seeding is not executed.

**File: `CampusEvents.Tests/TestOnlyOrganizerController.cs`**

```csharp
using CampusEvents.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusEvents.Tests;

[ApiController]
[Route("__test-only/organizer")]
[Authorize(Roles = RoleNames.Organizer)]
public sealed class TestOnlyOrganizerController : ControllerBase
{
	[HttpGet]
	public IActionResult Index()
	{
		return Ok("Organizer-only test endpoint");
	}
}
```

The test host registers an Organizer-role endpoint. In .NET 10, `[ApiController]` supplies API endpoint metadata, so an authenticated Participant without the Organizer role receives **403 Forbidden**. The HTTP test also checks any returned `Location` header. The browser-facing MVC access-denied flow uses the configured AccessDenied page.

<br>

### Run end-to-end authentication and authorization assertions

Create the following eight HTTP tests. Retrieve the login form and antiforgery token with GET, submit the form with POST, and use the returned Identity cookie for authenticated requests. Exercise duplicate identities, generic failure feedback, safe return navigation, logout, antiforgery enforcement, and role membership.

**Test design:** Run each test with an isolated, freshly migrated SQL Server database. Create accounts and roles through Identity managers; check HTTP responses and persisted account state.

**File: `CampusEvents.Tests/AuthenticationHttpTests.cs`**

```csharp
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

}
```

The initial tests establish: (a) anonymous `/Participant` produces an authentication redirect retaining a local `ReturnUrl`; (b) a real Login POST issues a cookie used on a later protected GET; and (c) an authenticated user without `Organizer` is denied by a real `[Authorize(Roles = ...)]` filter. The added cases verify duplicate registration, equivalent invalid-login feedback, malicious-return fallback, antiforgery rejection and logout state change, and authorization after a trusted role assignment.

Apply the Identity migration to a disposable populated CEMS database copy and compare user IDs, account fields, and `Events.OrganizerId` before and after upgrading.

Run and interpret the cumulative suite, not just the new test file:

```cmd
dotnet build
dotnet test --no-build --logger "console;verbosity=normal"
```

Record build status, discovered test methods and cases, passed/failed/skipped counts, SQL Server endpoint type (no credentials), and whether each fixture-owned database was removed. With the five new HTTP tests plus the original three, the example contains **eight new authentication HTTP tests**; the existing catalog suite remains. xUnit case totals can differ from method counts. Record SQL Server connectivity problems, invalid connection-string overrides, migration failures, and unexpected responses in the test results. Keep fixtures connected to isolated test databases.


<br>

## Manual

### Verify legacy-account email conflicts in a disposable Development copy

1. Prepare a disposable SQL Server database with the Identity migration applied and a legacy account whose `Email` matches the configured classroom Administrator address, whose `NormalizedEmail` is empty, and whose ID differs from `seed-administrator-001`.
2. Configure the classroom account secrets for that disposable environment and run Development provisioning.
3. Confirm startup reports an Administrator identity conflict before privileged role provisioning. Inspect the roles and user-role associations to verify that the conflicting account has not received elevated membership.
4. Restore the disposable database to a clean state before continuing the normal account checks.


<br>

### Verify registration, login, logout, and return behavior

**Prerequisites:** Use a personally authorized Development SQL Server database with the inspected Identity migration applied. Run the MVC application through HTTPS and clear or isolate browser cookies before each independent user scenario. Use a new disposable Student email and a password meeting the configured policy; use the Development-only elevated accounts exclusively in the controlled course environment.

For each scenario, use the approved Development or isolated test state. Open the relevant browser page, perform the action, and inspect its request URL, response, and visible result.

| Scenario | Student action | Expected result |
| --- | --- | --- |
| New Participant | Open `/Account/Register`; submit a unique email, display name, matching valid passwords | Account created, confirmation displayed on the Events catalog, and Visitor remains signed out until Login |
| Duplicate identity | Register again with the same email | Form returns with a meaningful duplicate error; no second account appears |
| Registration boundary | Leave required input empty, mismatch confirmations, or use insufficient password | Field-specific or model-level validation feedback; no account created; non-sensitive input values retained where appropriate |
| Correct login | Open `/Account/Login`; use registered credentials | Authenticated navigation and cookie-backed access on subsequent requests |
| Unknown email | Submit unregistered email with a password | `Invalid login attempt.`; no signed-in state |
| Wrong password | Submit registered email with an incorrect password | The **same** `Invalid login attempt.` feedback; no account-existence hint |
| Anonymous direct URL | In a fresh private browser session request `/Participant` | Redirect to local Login with `/Participant` in the return query |
| Practical return | Complete Login after arriving from protected route | Browser reaches `/Participant` and sees identity text |
| External return | Open `/Account/Login?returnUrl=https://attacker.example/` and submit valid credentials | Redirect stays inside CEMS (Events fallback), never to external site |
| Signed-in role baseline | Open `/Participant` as ordinary Student, Organizer, and Administrator | All authenticated accounts have the baseline access; Participant role is not required |
| POST logout | Use the rendered Logout button while signed in; then retry `/Participant` | Logout changes session state; next protected request leads to Login |
| POST without antiforgery | Using an authorized HTTP test tool or regression test, omit the token from a state-changing POST | Request rejected; no account/session state change |
| Access denied page | Open `/Account/AccessDenied` in a browser and inspect its explanatory content | Readable access-denied page with accessible navigation |

Inspect Developer Tools → Network for request method, document response code, redirect chain, relative/absolute `Location`, and the final content. Do not paste live authentication cookies or passwords into evidence documents. Confirm that unexpected errors are handled without displaying a stack trace or database connection details.

<br>

### Verify seeded-role and retained public-catalog behavior

Using read-only database queries, confirm one Development Organizer with ID `seed-organizer-001`, its `Organizer` membership, and its original associated Events with unchanged `OrganizerId` values. Confirm the controlled Administrator has `Administrator` membership, while publicly registered accounts receive ordinary authenticated access. Check the public catalog both signed out and signed in. A recently initialized five-Event Development dataset normally has Library Research Night and Career Networking Lunch as its two Published upcoming public examples; the Draft, Cancelled, and past Event must stay absent. If your existing database is older or modified, inspect actual timestamps and statuses rather than force this expected count.

For each scenario, use the approved Development or isolated test state. Inspect its request URL, HTTP status, and visible result; record your observations once in the working traceability file.

| Scenario | Expected result and observation |
| --- | --- |
| `/Events` | Published/upcoming results only, chronological start time then stable tie-breaker. |
| Search `Library`; Category Academic | Only matching eligible Events; filters intersect. |
| Search `Movie`; Category Social | Clear no-results response if none eligible; Draft/Cancelled not leaked. |
| Select page sizes `10`, `25`, `50` | Allowed size rendered and retained in navigation links. |
| Exercise First/Previous/Next/Last with an authorized **temporary private** dataset | Correct page count, bounds, search and Category state preserved. |
| Explicit unsupported `pageSize=11` | Controlled HTTP 400 with shared BadRequest View. |
| Malformed, nonpositive, and out-of-range `page` | Lesson 3 normalization policy, not an exception. |
| Open a Published Event Details link | Existing Details contract preserved. |
| Open a missing or Draft-only Details link | Existing HTTP 404 behavior preserved. |

For multiple-page navigation, reuse Lesson 3's isolated Development pagination practice dataset and cleanup procedure. Keep shared and test databases unchanged. Recheck responsive form controls at mobile and laptop widths, labels/focus states with keyboard-only navigation, role-aware links, and the Logout button in narrow navigation. Exercise the protected endpoints through the HTTP verification scenarios above.

<br>

## SRS Traceability Matrix Update with Evidence

Use the working evidence record from Lesson 3 and add a Lesson 4 section, for example `docs/lesson-04-traceability.md`. Keep the approved SRS identifiers and record executed commands, observed outcomes, migration checks, and the commit reference. Complete the template as you work.

```md
# CEMS Lesson 4 — Working Traceability Evidence

Date: <execution-date>
Branch / commit: <reference-after-commit>
Application / test target: net10.0
Database environment: <sanitized Development endpoint or per-test LocalDB/SQL Server endpoint type>
Automated results: <discovered / executed / passed / failed / skipped>
Manual observations: <browser and date, without secrets>

| SRS reference | Implementation | Check | Result |
| --- | --- | --- | --- |
| UC-002, FR-ACC-001–002 | Register action and Identity store | Registration and duplicate email | Record result |
| FR-ACC-003–004 | Login and SignInManager | Valid and invalid credentials | Record result |
| FR-ACC-005, NFR-SEC-003 | Logout POST | Logout and CSRF protection | Record result |
| FR-ACC-006, BR-AUT-001, BR-AUT-004 | Protected endpoints | Anonymous and role access | Record result |
| FR-ACC-007 | Local return URL processing | Local and external return URL | Record result |
| NFR-SEC-001–002 | Identity configuration and roles | Migration and protected access | Record result |
| NFR-SEC-004–005 | Secrets and safe error responses | Source review and UI errors | Record result |
| AC-002, AC-010 | Account and regression suites | HTTP, database and browser checks | Record result |

Migration upgrade: <user key and Event foreign-key comparison>
```

**Evidence context:** Run `dotnet build` and `dotnet test`, record the test results, and compare the manual inspector output with the expected scenarios. Both are necessary to evidence that requirements have been implemented and verified.

Confirm that your project builds, passes all tests, and runs without errors.

Stage and commit:

```cmd
git add .
git commit -m "Lesson 04: Implemented and Verified"
```

**NOTE:** Never stage or commit source code that does not build, fails a test, or runs with errors.

# Guided Exercises

<br>

## Exercise: trace the trust boundary

**Purpose and expected outcome:** Explain how a protected browser request becomes a trusted principal and is authorized (Objectives 1–2).

**Prerequisites and SRS ties:** Start from the application and test environment at this lesson checkpoint. Related SRS references: `FR-ACC-003`, `FR-ACC-006–007`, `BR-AUT-004`.

**Task:** Diagram anonymous `GET /Participant` → Login GET → credential POST → Identity cookie → subsequent `GET /Participant`. Label each client-controlled value, each trusted server decision, and the point that authorization occurs. Compare this with a guessed direct URL and a forged `X-User-Id` header.

**Hints or checkpoints:** Distinguish a redirect from authentication; distinguish cookie presence from validated ticket.

**Deliverable:** labeled sequence diagram and a short explanation.

**Completion and verification:** Identify the authentication challenge and the server-side authorization decision in the request sequence.

**Challenge (optional):** add the role-check decision for an Organizer-only route.

<br>

## Exercise: diagnose a rejected account request

**Purpose and expected outcome:** Separate form validation, account rules, and stored-data effects (Objectives 2–3, 7).

**Prerequisites and SRS ties:** Start from the application and test environment at this lesson checkpoint. Related SRS references: `FR-ACC-001–002`, `NFR-USA-002`, `NFR-SEC-002`.

**Task:** In a disposable Development dataset, submit a missing display name, a mismatched confirmation, a weak password, and an already registered email. Predict which validation layer rejects each request, then observe the form response and confirm no unintended extra user row.

**Hints or checkpoints:** Inspect `ModelState`, `IdentityResult`, and the actual user count rather than editing Identity columns directly.

**Deliverable:** four-row table with prediction, response, and persistence result.

**Completion and verification:** errors are intelligible and rejected requests create no unintended account.

**Challenge (optional):** introduce a request with an unexpected `IsAdministrator=true` field and explain why it cannot grant a role.

<br>

## Exercise: evaluate session and redirect defenses

**Purpose and expected outcome:** Analyze request semantics and design negative security tests (Objectives 2, 5–6).

**Prerequisites and SRS ties:** Start from the application and test environment at this lesson checkpoint. Related SRS references: `FR-ACC-005`, `FR-ACC-007`, `NFR-SEC-003`.

**Task:** Reproduce a safe local `ReturnUrl`, a nonlocal redirect attempt, and a Logout POST with and without antiforgery token through a test-only browser client. Record response status and subsequent protected-resource access.

**Hints or checkpoints:** A final page after automatic redirects can conceal the intermediate 302; disable automatic redirects for inspection.

**Deliverable:** a concise request/response matrix, including the state after each POST.

**Completion and verification:** no successful off-site redirect or tokenless state change.

**Challenge (optional):** explain what SameSite mitigates and why it does not make server antiforgery checks optional.

<br>

## Exercise: independently test a role transition

**Purpose and expected outcome:** Show that role membership, not authentication alone, gates elevated access (Objectives 1, 5–7).

**Prerequisites and SRS ties:** Start from the application and test environment at this lesson checkpoint. Related SRS references: `BR-AUT-003–004`, `NFR-SEC-001`.

**Task:** Starting with a test user who can open `/Participant` but receives a 403 from the test-only Organizer endpoint, assign the `Organizer` role through `UserManager`/`RoleManager` **before a fresh sign-in**. Repeat the same HTTP requests and interpret the changed result.

**Hints or checkpoints:** Authentication tickets contain claims; sign in again after changing role state and retain the role probe in the test host.

**Deliverable:** independent regression test plus explanation of both results.

**Completion and verification:** the normal protected page works throughout, the elevated endpoint moves from denied to permitted only after trusted role assignment and renewed sign-in.

**Challenge (optional):** Explain how to combine role permission with the Event `OrganizerId` check.

<br>

## Exercise: review the migration and repository boundary

**Purpose and expected outcome:** Evaluate schema evolution and produce reproducible evidence without disclosing sensitive configuration (Objectives 4, 7–8).

**Prerequisites and SRS ties:** Start from the application and test environment at this lesson checkpoint. Related SRS references: `NFR-SEC-004`, existing Event ownership integrity, `AC-010`.

**Task:** On an authorized Development dataset, compare seeded user/Event keys before and after the generated Identity migration; inspect `Up`/`Down` for destructive operations. Review your Git diff and staged files for secret material, unplanned migration changes, and generated files.

**Hints or checkpoints:** Read the migration before applying it; compare records by primary key, not display name or email.

**Deliverable:** sanitized migration review, continuity queries, and staged-file checklist.

**Completion and verification:** documented key/FK continuity and a diff containing only intended artifacts.

**Challenge (optional):** articulate the difference between a clean test database migration and a populated-data upgrade.

<br>

# Troubleshooting Common Problems

| Symptom | Likely cause / diagnosis | Correction / verification |
| --- | --- | --- |
| `ApplicationUser` has conflicting `Id` fields | Manual `Id` remains after inheriting IdentityUser | Remove duplicate property; keep existing string keys through the inherited Identity model |
| Identity tables missing or migrations unexpectedly replace users | Context still inherits `DbContext`, missing `base.OnModelCreating`, or mismatched table/key mappings | Inspect the EF model, migration `Up`/`Down`, old `AspNetUsers` mapping, and FK continuity before applying changes |
| `UseSqlServer`/Identity types not resolved | Missing or mismatched approved packages/using directives | Inspect project target, installed package versions, and `dotnet build`; do not change database providers |
| Login POST returns 400 | Antiforgery token/cookie missing or invalid | GET the real form first; inspect token markup, posted form fields, and cookie handling |
| Valid credentials fail | Wrong email-as-username, password policy, stale role/account seed, or wrong DB | Check sanitized configuration and Identity lookup with `UserManager`; do not log raw passwords |
| Duplicate registration unexpectedly succeeds | Unique email policy/store not registered or wrong context | Inspect `RequireUniqueEmail`, one Identity store, and persisted user count |
| A failed Login reveals account existence | Different feedback for nonexistent email and wrong password | Return the same `Invalid login attempt.` message and regression-test both cases |
| Return URL leads off-site | `LocalRedirect` used without a local-URL decision | Validate with `Url.IsLocalUrl`; provide a safe local fallback |
| Logout is a link or tokenless GET | State change implemented as navigation | Replace with authenticated antiforgery-protected POST form and action |
| Protected action executes anonymously | Missing `[Authorize]` or middleware order | Trace route → authentication → authorization; send a direct anonymous HTTP request |
| Authenticated Student reaches Organizer test route | Role protection was omitted or replaced with UI visibility | Add server role boundary; test user with and without actual membership |
| AccessDenied displays 404 | Cookie `AccessDeniedPath` set but no action/view | Provide the MVC error destination; distinguish the API 403 response contract |
| Authentication test fails with 404 on test-only route | MVC test assembly not registered as application part | Inspect `ConfigureTestServices` and route attributes; do not expose probe in production |
| Identity tests do not start | LocalDB is unavailable, an optional `master` override is invalid, or migration/permissions fail | Check LocalDB installation and per-test fixture diagnostics; report NOT EXECUTED when SQL Server is unavailable, never substitute an existing Development DB |
| Antiforgery token extraction or test-only route lookup fails | Form markup differs from the test parser, or the test assembly was not registered as an MVC application part | Inspect the rendered form and the test host application-part registration; keep the Organizer authorization endpoint in the test project |
| Existing classroom Administrator conflicts with the stable ID | An earlier Development seed assigned that email to a different account | Back up the Development database, inspect account IDs and Event references, and reconcile the account with an instructor-approved migration or a clean disposable database before retrying; preserve existing user keys and ownership |
| Privileged account seeding rejects a legacy email | A pre-Identity user holds a configured email with a blank normalized field | Inspect persisted `Email`, `UserName`, and stable IDs; correct the conflicting Development configuration or account record through approved Identity operations before rerunning provisioning |
| Existing Events disappear after Identity changes | Destructive schema evolution or unintended seed reset | Stop, inspect migration and foreign keys; restore only from an authorized backup if necessary |
| Catalog loses pagination/search/Details | Shared controller/layout/CSS overwritten during Identity work | Compare against Lesson 3 checkpoint and rerun its ten catalog cases and browser regression matrix |
| Seed passwords found in Git | Secret values entered in tracked config or source | Remove them from committed content, rotate exposed credentials, and follow the institution's history-remediation policy |

<br>

### Recover from interrupted Development provisioning

If account seeding stops after a role or account update, inspect the Development database's `AspNetUsers`, `AspNetRoles`, and `AspNetUserRoles` records for the two stable classroom IDs. Confirm which operations completed and resolve the reported configuration or Identity error. Correct conflicting Development-only accounts deliberately, preserving `seed-organizer-001` and its `Events.OrganizerId` references. Restart the application to rerun the idempotent initializer, then verify normalized account fields, role membership, and login. Use a backed-up Development database or a disposable copy when practicing recovery; inspect relationships before deleting any records.


# Checking Your Understanding

<details><summary>Why does a valid login not automatically authorize editing every Event?</summary>

Authentication proves identity; role authorization checks elevated capability, and resource authorization checks ownership and modification eligibility for the particular Event. These decisions must be enforced server-side.

</details>

<details><summary>Why is Participant not an Identity role in this project?</summary>

Participant is the authenticated baseline. The additional trusted roles are Organizer and Administrator; authentication itself establishes the Participant baseline.

</details>

<details><summary>What remains untrusted after a successful authentication cookie has been validated?</summary>

The user's form fields, query values, route IDs, and return URLs remain client-controlled. Authentication establishes who made the request, not the correctness of each supplied value.

</details>

<details><summary>How does the design prevent invalid-login feedback from enumerating accounts?</summary>

A wrong password and an unknown email both yield the same generic user-facing login failure.

</details>

<details><summary>Why must the Identity migration be reviewed even if EF Core generated it successfully?</summary>

Inspect the migration operations and compare existing user keys, Event foreign keys, and stored rows before and after upgrading a representative database copy.

</details>

<details><summary>What makes a return URL safe to use after sign-in?</summary>

It must pass a trusted local-URL check; only then can the application make a local redirect. Otherwise it should choose a safe local fallback.

</details>

<details><summary>Why is an authenticated Logout POST still protected by antiforgery validation?</summary>

The browser automatically includes eligible cookies with requests. Antiforgery validation checks state-changing form submissions.

</details>

<details><summary>Which checks exercise the server authorization pipeline?</summary>

Use hosted HTTP integration tests to exercise authentication middleware, authorization filters, cookies, and antiforgery validation.

</details>

<details><summary>What is the difference between a 302 access redirect and an API 403 with a Location header?</summary>

A 302 directs the browser to a new destination. A 403 communicates forbidden access. Read both the response status and relevant headers.

</details>

<details><summary>How would you verify an Identity migration against a populated CEMS database?</summary>

Rehearse the upgrade on a populated database copy and compare user identifiers, Event foreign keys, account metadata, and stored records before and after the migration.

</details>

<details><summary>What should you report if LocalDB or another authorized SQL Server endpoint is unavailable?</summary>

If SQL Server is unavailable, document the environment blocker and restore the isolated provider-backed test environment. On Windows, first check that the default LocalDB instance exists.

</details>

<br>

# Summary

You **distinguished identity, session continuity, authorization, and ownership** by locating each decision in a complete request path. You **analyzed trust boundaries** around credentials, cookies, forms, roles, and return destinations, understanding that validated identity does not validate arbitrary client input.

You **designed account workflows around trusted Identity services** and **evaluated relational schema evolution** through preserved keys, relationships, and data. You **applied server-enforced protection** and antiforgery defenses while recognizing that navigation visibility and browser cookie policy are insufficient security boundaries.

You **practiced targeted verification** through hosted HTTP requests, database state, and browser interaction across positive, negative, and transition scenarios. You **investigated failures systematically** by comparing response statuses, redirect paths, and stored state. Finally, you **practiced incremental change management** with isolated environments, explicit traceability, reviewed commits, and secret-safe configuration. These principles apply across languages, frameworks, database systems, and deployment settings.

<br>

# Checkpoint

**Inherited baseline:** CEMS was a `net10.0` ASP.NET Core MVC application with a SQL Server-backed `Event`, `Category`, and `ApplicationUser` model, a persistent `seed-organizer-001` owner, a Development initializer with three Categories and five Events, and a public catalog supporting eligibility filtering, title/location search, Category filtering, deterministic 10/25/50 paging with preserved query state, and Event Details. The isolated SQL Server test project and shared semantic Razor layout/CSS provide the starting environment.

**New in this lesson:**

- `ApplicationUser` now extends `IdentityUser`, and `ApplicationDbContext` extends `IdentityDbContext<ApplicationUser>` while retaining Event/Category data and organizer relationships.
- The `AddIdentityAuthentication` migration evolves the relational identity schema to include users, roles, and supporting account data structures.
- Account registration, Login, Logout, and a browser-facing AccessDenied page use `UserManager`, `SignInManager`, purpose-specific input models, antiforgery-protected POSTs, and a validated local return destination.
- The authentication cookie middleware and server authorization middleware participate in the MVC pipeline.
- `Organizer` and `Administrator` are the elevated role constants; controlled Development seeding upgrades the existing Organizer user in place and supplies separate classroom Administrator access using local secrets.
- `/Participant` requires authentication without a separate Participant role; the independent view refinement displays the signed-in identity, and shared navigation reflects signed-in state and the two elevated role labels.
- The xUnit project exercises the HTTP authentication pipeline with isolated SQL Server state, deterministic time, and an Organizer-role boundary. The HTTP regression suite covers authentication and authorization scenarios alongside the retained catalog suite.

**Resulting baseline:** The application combines authenticated user identities, stable Event ownership keys, server-side authorization, and account-aware navigation with the established public catalog. You can use these capabilities when building owned Event management workflows.

<br>

# Next Lesson Preview

In Lesson 5, build **Organizer Event Forms, Validation, and Ownership**. You will use the authenticated user and Organizer role to build a protected management list and Event Create/Edit forms. Form models will accept only permitted fields, server-side validation will reject invalid state, the Organizer ID will come from the authenticated principal, and edit requests will reload Events to enforce ownership. Use the established public catalog, account flows, and database constraints as the starting application state.

# Appendix

Explore alternative authentication architectures, authorization designs, session controls, schema strategies, and testing methods. Compare each with the MVC, Razor, EF Core, and SQL Server implementation you built.

## Authentication architecture and integration alternatives

**Trusted Identity infrastructure versus application-owned credentials.** An application-owned credential store makes the project responsible for password hashing policy, account verification, lockout interactions, security-stamp handling, and secure schema maintenance. Using Identity retains an established implementation of those infrastructure responsibilities while allowing CEMS to own `DisplayName`, Events, and Organizer references. Apply CEMS-specific authorization separately to domain operations.

**Cookies versus bearer tokens.** A server-rendered browser app naturally uses an application cookie returned at login and attached on later navigation. A separately consumed API may instead use bearer credentials and different storage, expiry, and request-forgery considerations. Both approaches require explicit credential handling, expiration rules, and trust-boundary protections. For CEMS, choose cookie authentication to support navigation among server-rendered pages.

**Framework-provided account UI versus MVC-owned forms.** Identity can integrate with framework-supplied account pages, but this lesson's explicit MVC controller and Razor forms make binding, validation, request methods, redirect safety, and test observations visible. The account UI is a presentation choice; password processing stays delegated to trusted services.

## Routing, endpoint conventions, and access-control alternatives

### Conventional and attribute routing

ASP.NET Core MVC supports two primary routing approaches: **conventional routing** and **attribute routing**.

**Conventional routing** defines reusable URL patterns centrally, typically in `Program.cs`. For example:

```csharp
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
```

This pattern uses URL segments to identify the controller, action, and optional identifier. For example, `/Events` maps to `EventsController.Index()` because `Index` is the default action.

- **Benefit:** A single route definition supports many controller actions without requiring individual route declarations.
- **Trade-off:** The URLs handled by an action are less immediately apparent from the controller's source code, and specialized URL patterns may require additional route definitions.

**Attribute routing** declares routes directly on controllers or actions using attributes. For example, the Events action could instead declare its own URL and HTTP method:

```csharp
[HttpGet("/Events")]
public IActionResult Index()
{
	return View(model: "Student Research Showcase");
}
```

Here, `[HttpGet("/Events")]` explicitly maps HTTP `GET` requests for `/Events` to the `Index()` action.

- **Benefit:** The URL pattern and supported HTTP method are visible alongside the action that handles the request, making specialized endpoints easier to identify.
- **Trade-off:** As an application grows, route definitions distributed across controllers require coordination to avoid conflicting or inconsistent URL patterns.

Both approaches can coexist. CEMS uses **conventional routing** for its shared MVC URL pattern and places access requirements on the selected endpoints.

### Policy-based and resource-based authorization

Consider an Organizer editing an Event. These three approaches operate at different levels:

- **Role-based endpoint authorization** uses `[Authorize(Roles = RoleNames.Organizer)]` to require trusted Organizer membership before the edit operation executes.
- **Named authorization policies** collect reusable requirements, such as authenticated Organizer membership, behind a policy name applied to several endpoints. This centralizes rules shared by multiple actions.
- **Resource-based authorization** evaluates the loaded Event alongside the authenticated user's stable ID. An authorization handler can grant an edit when the user is an Organizer, the Event's `OrganizerId` matches the user ID, and the Event's current state permits editing.

For CEMS, role-based checks protect the endpoint and ownership checks use the persisted `Event.OrganizerId` on the particular record. Policies and resource handlers offer reusable alternatives when the number of protected operations grows.


## Authentication cookies, session security, and redirect hardening

Authentication cookies carry a ticket representing the authenticated principal. **Sliding expiration** renews a valid active ticket according to policy; **principal refresh** rebuilds signed-in claims after an identity change; and **security-stamp validation** periodically checks whether an account remains eligible for its session. After changing role membership, sign in again or refresh the principal to obtain updated claims.

Cookie lifetime choices balance usable sessions with the risk of an unattended device. A nonpersistent cookie is associated with a browser session; persistent sign-in extends the client lifetime when product requirements permit it. Configure renewal and security-stamp validation deliberately for the deployment environment.

`HttpOnly` limits script access to cookies; `Secure` limits transmission to HTTPS; `SameSite` influences cross-site attachment. Use antiforgery tokens with state-changing cookie-authenticated forms to complete these defenses. Evaluate cookie settings together with the deployment HTTPS policy.

An open redirect can be embedded in a login URL or hidden `ReturnUrl` input. The safe pattern is to validate the return destination **after** authentication and accept only local destinations through `Url.IsLocalUrl`/`LocalRedirect`, otherwise falling back to the catalog. The redirected request then follows the endpoint's authorization rules (`FR-ACC-007`).


## Identity data architecture and migration alternatives

Identity stores account records, roles, claims, logins, and tokens in related tables. Roles express trusted coarse-grained membership; claims can represent additional identity facts or policies, but neither by itself replaces a comparison between a domain record's `OrganizerId` and the current user key. A changed email should not demand rewriting Event foreign keys.

Keeping Identity and domain entities in the existing SQL Server-backed `ApplicationDbContext` allows the existing `Events.OrganizerId` relationship to remain within the relational model. A separate identity database or store suits some larger systems and requires explicit coordination of identifiers, migrations, and cross-store integrity. Compare that trade-off with CEMS’s unified relational model.

For existing-account reconciliation, a Development-only scan of raw email and username fields can identify legacy records with unpopulated normalized fields before privileged initialization. Larger databases can use a controlled, set-based backfill and database-side uniqueness checks, followed by `UserManager` operations for account security metadata. Routine login and user lookup should use Identity’s normalized, indexed fields.

For populated database upgrades, inspect generated operations and rehearse the upgrade on a disposable copy containing representative accounts, Categories, and Events. Compare stable user keys, Event foreign keys, and check constraints, and prepare a rollback or recovery procedure before updating valuable data.

## Security-test strategy and deployment trade-offs

An integration-test host can replace application services and configuration while retaining the MVC request pipeline. A dedicated SQL Server fixture provides isolated schema and account state. Place specialized authorization endpoints in the test assembly and register them with the host. When testing role updates, refresh the authenticated principal before sending the next protected request.

Provision classroom accounts through Development user secrets. Configured seed passwords initialize accounts requiring credentials; use authorized Identity password-reset operations when an existing account needs a password change. For a deployed application, use controlled account initialization, restricted role-administration permissions, managed secrets, and documented credential rotation procedures.
