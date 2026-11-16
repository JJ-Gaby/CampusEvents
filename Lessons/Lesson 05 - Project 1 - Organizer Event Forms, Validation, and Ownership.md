# **Lesson 5 — Organizer Event Forms, Validation, and Ownership**

Revision: 2026.11.09.1

# Table of Contents

- [Review of Last Lesson](#review-of-last-lesson)
- [Learning Objectives](#learning-objectives)
- [SRS Requirements and Use Cases](#srs-requirements-and-use-cases)
- [Guided Feature Preview](#guided-feature-preview)
- [Guided Design and Coding](#guided-design-and-coding)
	- [Protect and query the Organizer management list](#protect-and-query-the-organizer-management-list)
	- [Render the owned-event management view](#render-the-owned-event-management-view)
	- [Add role-aware navigation without replacing server authorization](#add-role-aware-navigation-without-replacing-server-authorization)
	- [Add organizer-page styling](#add-organizer-page-styling)
	- [Inspect the management-page checkpoint](#inspect-the-management-page-checkpoint)
	- [Define an allowlisted Event input model](#define-an-allowlisted-event-input-model)
	- [Prepare Category options and the create GET](#prepare-category-options-and-the-create-get)
	- [Render an accessible create form](#render-an-accessible-create-form)
	- [Add event-form styling](#add-event-form-styling)
	- [Validate and persist create submissions](#validate-and-persist-create-submissions)
	- [Trace binding, validation, and create persistence](#trace-binding-validation-and-create-persistence)
		- [MVC binds the model](#mvc-binds-the-model)
		- [The controller verifies Category existence](#the-controller-verifies-category-existence)
		- [Invalid requests return before persistence](#invalid-requests-return-before-persistence)
		- [Resolve the current user](#resolve-the-current-user)
		- [Protected fields are assigned on the server](#protected-fields-are-assigned-on-the-server)
		- [Persistence happens only after validation](#persistence-happens-only-after-validation)
		- [POST/Redirect/GET](#postredirectget)
	- [Preserve attempted values on failed submissions](#preserve-attempted-values-on-failed-submissions)
	- [Reload an owned Draft for edit GET](#reload-an-owned-draft-for-edit-get)
	- [Render the draft-only edit form](#render-the-draft-only-edit-form)
	- [Re-query ownership and eligibility before edit POST](#re-query-ownership-and-eligibility-before-edit-post)
	- [Review the cumulative public and Organizer controller](#review-the-cumulative-public-and-organizer-controller)
- [Guided Discussion](#guided-discussion)
	- [Stateful form workflows and HTTP request contracts](#stateful-form-workflows-and-http-request-contracts)
	- [Model binding, trust boundaries, and input contracts](#model-binding-trust-boundaries-and-input-contracts)
	- [Layered validation and relational integrity](#layered-validation-and-relational-integrity)
	- [Validation feedback and form reconstruction](#validation-feedback-and-form-reconstruction)
	- [Role authorization and resource ownership](#role-authorization-and-resource-ownership)
	- [Current-state eligibility and guarded mutations](#current-state-eligibility-and-guarded-mutations)
	- [Persistence boundaries and rejected writes](#persistence-boundaries-and-rejected-writes)
	- [Verification of protected workflows](#verification-of-protected-workflows)
- [Guided Verification and Validation](#guided-verification-and-validation)
	- [Automated](#automated)
		- [Five focused regression scenarios](#five-focused-regression-scenarios)
		- [Add the HTTP and persistence tests](#add-the-http-and-persistence-tests)
		- [Run the cumulative suite](#run-the-cumulative-suite)
	- [Manual](#manual)
		- [Create validation scenarios](#create-validation-scenarios)
		- [Missing required input](#missing-required-input)
		- [End is not after start](#end-is-not-after-start)
		- [Non-positive capacity](#non-positive-capacity)
		- [Nonexistent Category manipulation](#nonexistent-category-manipulation)
		- [Valid create](#valid-create)
		- [Verification matrix](#verification-matrix)
		- [Browser and CSS verification](#browser-and-css-verification)
		- [Verify server-assigned owner and status](#verify-server-assigned-owner-and-status)
		- [Verify failed create does not partially persist](#verify-failed-create-does-not-partially-persist)
		- [Verify failed edit does not partially persist](#verify-failed-edit-does-not-partially-persist)
		- [Demonstrate the changed-id ownership attack](#demonstrate-the-changed-id-ownership-attack)
		- [Regression check the public catalog](#regression-check-the-public-catalog)
		- [Regression check authentication](#regression-check-authentication)
	- [SRS Traceability Matrix Update with Evidence](#srs-traceability-matrix-update-with-evidence)
- [Guided Exercises](#guided-exercises)
	- [Exercise: extend the management list with location](#exercise-extend-the-management-list-with-location)
	- [Exercise: add an independent invalid-input regression](#exercise-add-an-independent-invalid-input-regression)
	- [Exercise: attempt ownership and protected-field tampering](#exercise-attempt-ownership-and-protected-field-tampering)
	- [Exercise: evaluate the form-validation design](#exercise-evaluate-the-form-validation-design)
- [Troubleshooting Common Problems](#troubleshooting-common-problems)
	- [Diagnostic walkthroughs](#diagnostic-walkthroughs)
		- [Putting Organizer authorization on the whole Controller](#putting-organizer-authorization-on-the-whole-controller)
		- [Binding the persisted `Event` entity directly](#binding-the-persisted-event-entity-directly)
		- [Adding hidden `OrganizerId` or `Status` inputs](#adding-hidden-organizerid-or-status-inputs)
		- [Forgetting `[ValidateAntiForgeryToken]`](#forgetting-validateantiforgerytoken)
		- [Forgetting to repopulate categories](#forgetting-to-repopulate-categories)
		- [Checking only `CategoryId > 0`](#checking-only-categoryid--0)
		- [Mutating before ownership is proven](#mutating-before-ownership-is-proven)
		- [Using a hidden edit button as authorization](#using-a-hidden-edit-button-as-authorization)
		- [Calling `SaveChangesAsync()` before validation is complete](#calling-savechangesasync-before-validation-is-complete)
		- [Forgetting that ModelState preserves attempted values](#forgetting-that-modelstate-preserves-attempted-values)
		- [Adding a migration for `EventInputModel`](#adding-a-migration-for-eventinputmodel)
		- [Accidentally adding lifecycle behavior](#accidentally-adding-lifecycle-behavior)
- [Checking Your Understanding](#checking-your-understanding)
- [Summary](#summary)
- [Checkpoint](#checkpoint)
- [Next Lesson Preview](#next-lesson-preview)
- [Appendix](#appendix)
	- [Input models, domain models, and mapping strategies](#input-models-domain-models-and-mapping-strategies)
	- [Validation architecture and invariant placement](#validation-architecture-and-invariant-placement)
	- [Concurrent updates, optimistic concurrency, and transaction boundaries](#concurrent-updates-optimistic-concurrency-and-transaction-boundaries)
	- [Resource-based authorization and access-control design](#resource-based-authorization-and-access-control-design)

# Review of Last Lesson

Lesson 4 established the difference between authenticated identity, role-based access, and authorization of specific actions. You integrated trusted identity services into the request pipeline, protected state changes against forgery, and compared browser evidence with isolated HTTP and database tests. Those principles provide the trust foundation for accepting editable data from an Organizer.

CEMS already has a SQL Server-backed public Event catalog, search, Category filtering, deterministic 10/25/50 pagination, Event Details, Identity registration and login/logout, and the `Organizer` and `Administrator` roles. Events already reference their owning `ApplicationUser`, but Event management is still limited to Development seed data. You will add an Organizer workflow while preserving public discovery.


# Learning Objectives

By the end of the lesson, you should be able to:

1. **Analyze** how form retrieval, submission, validation, persistence, and redirection establish a reliable multi-request interaction.
2. **Design** purpose-specific request models that expose only permitted inputs and protect server-controlled state.
3. **Compare and apply** field-level, cross-field, database-backed, and relational-constraint validation, identifying the responsibility of each layer.
4. **Explain and evaluate** how invalid submissions preserve attempted values and meaningful feedback while leaving persisted state unchanged.
5. **Distinguish and enforce** authentication, role authorization, resource ownership, and current-state eligibility as separate access decisions.
6. **Evaluate** resource reloads and operation-specific mutation boundaries to prevent unauthorized or stale changes.
7. **Apply** request-forgery protections and redirect-after-write behavior to state-changing web interactions.
8. **Design and interpret** reproducible positive, negative, and boundary tests that compare HTTP responses with database invariants.
9. **Assess** incremental changes for integration regressions, security, accessibility, and traceable repository history.

These competencies apply across server-rendered systems and other application architectures; the project below supplies a concrete implementation setting.


# SRS Requirements and Use Cases

The approved CEMS SRS governs product behavior. **UC-005 — Create and Manage an Owned Event** is the principal use case: an authenticated Organizer creates an owned Draft Event, views owned Events, and modifies only eligible resources. You will implement Create and owned-Draft Edit with server-enforced ownership, validation, and persistence safeguards.

| SRS reference | Required behavior / constraint | Lesson contribution |
| --- | --- | --- |
| `UC-005`, `FR-EVT-001` | Organizer creates an owned Event | Protected Create GET/POST assigns owner from authenticated identity |
| `FR-EVT-002`, `FR-EVT-003` | New Event is Draft; required form collects title, description, Category, location, start/end date-time, and capacity | Explicit Draft assignment and purpose-specific form |
| `FR-EVT-004`, `BR-EVT-001`, `BR-EVT-003`, `BR-EVT-004` | Reject invalid values; chronological, capacity, and Category rules hold | Layered validation before persistence |
| `FR-EVT-005` | Organizer sees owned Events of all statuses | Ownership-filtered Manage list |
| `FR-EVT-006` | Organizer modifies eligible owned Event | Draft-only Edit GET/POST and server-side eligibility query |
| `FR-ACC-006`, `BR-AUT-001`, `BR-AUT-002`, `BR-AUT-004`, `NFR-SEC-001–003` | Protected capabilities, ownership, server authority and safe mutations | Role policies, owner query, and antiforgery on POST |
| `NFR-USA-002`, `NFR-ACC-001–003`, `NFR-COM-002` | Understandable, accessible, responsive forms and feedback | Semantic controls, retained input, validation display and manual viewport/keyboard checks |
| `NFR-REL-001–002`, `NFR-SEC-004` | Durable valid writes, no partially applied rejected changes, secret-safe configuration | Persist only after successful checks; reuse protected setup |
| `AC-003`, `AC-005`, `AC-008–011` | Final product meets Organizer workflow, integrity, persistence, error, test, and quality expectations | Verify the Organizer Create and owned-Draft Edit workflow |

**Acceptance contract:** An authenticated Organizer views owned Events, creates a valid Draft with server-assigned owner and status, and edits eligible owned Drafts. Invalid submissions retain usable feedback and leave persisted records unchanged. Requests targeting foreign-owned or ineligible Events are rejected; public browsing and authentication continue to operate.

Preserve public catalog and account behavior as you integrate Organizer management.

<br>

`Organizer` denotes the authenticated role permitted to manage owned Events. `Draft`, `Published`, and `Cancelled` are persisted Event statuses; Category references existing relational data. Use the approved requirement identifiers in the traceability record.

# Feature Preview

Start with the existing public Event catalog, Category data, Identity accounts, and seeded Organizer role. An authenticated Organizer opens **Organizer Events** in the shared navigation and reaches `/Events/Manage`. The page lists only Events owned by that signed-in Organizer, including existing Draft, Published, and Cancelled records. A Draft has an Edit action; other statuses remain readable in the management list.

Choose **Create Event**. Enter a title, description, existing Category, location, start and end times, and positive capacity. A valid submission produces a new Event with the authenticated Organizer as owner and Draft as its initial status, then returns to Manage. An invalid submission, such as an end time equal to the start time, displays a specific error and retains the entered values without inserting a record.

Select **Edit** on an owned Draft to revise its permitted fields. Direct requests using an Event belonging to another Organizer, or an owned Published or Cancelled Event, do not open an editable resource. The server applies these checks for both page retrieval and submission. The existing public Published/upcoming catalog continues to exclude the new Draft.

![Organizer management page showing owned Events and Draft Edit links](images/lesson-05/organizer-manage-target.png)

![Organizer management page showing owned Events and Draft Edit links](images/lesson-05/organizer-manage-target-with-edit.png)

![Rejected Create submission showing chronology feedback and retained values](images/lesson-05/create-invalid-chronology-target.png)

# Design and Coding

## Inspect the inherited solution

Inspect your existing ***Campus Events*** solution before making changes.

Make sure that you staged and commited the previous lesson:

```cmd
git show -s HEAD
```

**Expected output:** should contain "Lesson 04: Implemented and Verified".

Otherwise, build, test, run, stage, and commit:

```cmd
dotnet build
dotnet test
dotnet run --project src/CampusEvents
git add .
git commit -m "Lesson 04: Implemented and Verified"
```

Then, create a new branch:

```cmd
git switch -c lesson-05
```

## Protect and query the Organizer management list

Before building a form, give the Organizer a place to see owned Events. We will protect only the new Organizer actions.

**File: `Controllers/EventsController.cs`**

Add these `using` directives if they are not already present:

```csharp
using CampusEvents.Models;
using CampusEvents.Security;
using CampusEvents.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
```

Extend the controller fields and constructor without removing any existing dependency. Public browsing already uses the injected `TimeProvider`, while the new Organizer actions need `UserManager<ApplicationUser>`:

```csharp
private readonly ILogger<EventsController> _logger;
private readonly ApplicationDbContext _context;
private readonly TimeProvider _timeProvider;
private readonly UserManager<ApplicationUser> _userManager;

public EventsController(
	ILogger<EventsController> logger,
	ApplicationDbContext context,
	TimeProvider timeProvider,
	UserManager<ApplicationUser> userManager)
{
	_logger = logger;
	_context = context;
	_timeProvider = timeProvider;
	_userManager = userManager;
}
```

Now add:

```csharp
[HttpGet]
[Authorize(Roles = RoleNames.Organizer)]
public async Task<IActionResult> Manage()
{
	var organizer = await _userManager.GetUserAsync(User);

	if (organizer is null)
	{
		return Challenge();
	}

	var events = await _context.Events
		.AsNoTracking()
		.Include(eventItem => eventItem.Category)
		.Where(eventItem => eventItem.OrganizerId == organizer.Id)
		.OrderBy(eventItem => eventItem.StartDateTime)
		.ThenBy(eventItem => eventItem.Id)
		.ToListAsync();

	return View(events);
}
```

`[Authorize(Roles = RoleNames.Organizer)]` requires the exact Organizer role established in Lesson 4. `GetUserAsync(User)` resolves the authenticated principal back to CEMS's persisted `ApplicationUser`.

The database query contains:

```csharp
.Where(eventItem => eventItem.OrganizerId == organizer.Id)
```

That is why the management list is scoped to the current Organizer. `FR-EVT-005` requires the management list to include the current Organizer's Events across statuses. The public catalog filters for Published upcoming Events, while Organizer management lists the current owner’s Events across statuses.

<br>

## Render the owned-event management view

The management page has one main heading, one Create link, and a list of the current Organizer's Events. Use semantic headings and a definition list for Event facts, so the page remains understandable even before the additional management styles are applied.

The `button-primary` and `button-secondary` classes come from the shared stylesheet you used for account forms. The new list and metadata classes will receive their own styles under **Add Organizer-Page Styling**.

**File: `Views/Events/Manage.cshtml`**

Create:

```cshtml
@using CampusEvents.Models
@model IReadOnlyList<Event>

@{
	ViewData["Title"] = "Manage Events";
}

<header class="page-heading">
	<div>
		<h1>Manage My Events</h1>
		<p>
			View Events owned by your Organizer account.
		</p>
	</div>

	<a asp-controller="Events"
		asp-action="Create"
		class="button-primary">
		Create Event
	</a>
</header>

@if (Model.Count == 0)
{
	<p role="status">
		You do not own any Events yet.
	</p>
}
else
{
	<div class="event-management-list">
		@foreach (var eventItem in Model)
		{
			<article class="event-management-item">
				<div>
					<h2>@eventItem.Title</h2>

					<dl class="event-meta">
						<div>
							<dt>Status</dt>
							<dd>@eventItem.Status</dd>
						</div>
						<div>
							<dt>Category</dt>
							<dd>@eventItem.Category.Name</dd>
						</div>
						<div>
							<dt>Starts</dt>
							<dd>
								<time datetime="@eventItem.StartDateTime.ToString("yyyy-MM-ddTHH:mm:ss")">
									@eventItem.StartDateTime.ToString("f")
								</time>
							</dd>
						</div>
						<div>
							<dt>Capacity</dt>
							<dd>@eventItem.Capacity</dd>
						</div>
					</dl>
				</div>

				@if (eventItem.Status == EventStatus.Draft)
				{
					<a asp-controller="Events"
						asp-action="Edit"
						asp-route-id="@eventItem.Id"
						class="button-secondary">
						Edit
					</a>
				}
			</article>
		}
	</div>
}
```

<br>

## Add role-aware navigation without replacing server authorization

Lesson 4 already imports `CampusEvents.Security` in the layout and uses role checks for role labels. Now that `/Events/Manage` actually exists, add a navigation link for Organizers.

**File: `Views/Shared/_Layout.cshtml`**

Inside the authenticated navigation section, after the existing Participant Area link, add:

```cshtml
@if (User.IsInRole(RoleNames.Organizer))
{
	<li>
		<a class="site-nav-link"
			asp-controller="Events"
			asp-action="Manage">
			Organizer Events
		</a>
	</li>
}
```

The conditional link is a convenience. The `[Authorize(Roles = RoleNames.Organizer)]` remains the real role boundary.

An authenticated Participant without Organizer role can type:

```text
/Events/Manage
```

The server still refuses access.

<br>

## Add organizer-page styling

**File: `wwwroot/css/site.css`**

Append these styles below the existing account and navigation rules.

```css
.page-heading {
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	justify-content: space-between;
	gap: 1rem;
	margin-bottom: 1.5rem;
}

.page-heading > div {
	flex: 1 1 18rem;
	min-width: 0;
}

.page-heading h1 {
	margin-bottom: 0.25rem;
}

.page-heading p {
	margin: 0;
}

.event-management-list {
	display: grid;
	gap: 1rem;
}

.event-management-item {
	display: flex;
	flex-wrap: wrap;
	align-items: start;
	justify-content: space-between;
	gap: 1rem;
	padding: 1rem;
	border: 1px solid #d0d7de;
	border-radius: 0.35rem;
}

.event-management-item > div {
	flex: 1 1 20rem;
	min-width: 0;
}

.event-management-item h2 {
	margin-top: 0;
	overflow-wrap: anywhere;
}

.event-meta {
	display: grid;
	gap: 0.5rem;
	margin: 0;
}

.event-meta > div {
	display: grid;
	grid-template-columns: minmax(5rem, 7rem) minmax(0, 1fr);
	gap: 0.5rem;
}

.event-meta dt {
	font-weight: 700;
}

.event-meta dd {
	margin: 0;
	overflow-wrap: anywhere;
}

@media (max-width: 40rem) {
	.event-meta > div {
		grid-template-columns: minmax(0, 1fr);
		gap: 0.1rem;
	}

	.event-management-item .button-secondary {
		width: 100%;
	}
}
```

The heading and Event rows use Flexbox so controls can wrap when the available width shrinks. The definition list uses CSS Grid to align each label with its value on wider screens; the media query stacks those pairs and makes the Edit link easier to use on narrower screens. Long titles and metadata can wrap without pushing the page outside the viewport.

The shared `_Layout.cshtml` loads `site.css` for the Organizer management and form views.

<br>

## Inspect the management-page checkpoint

Build and run it:

```cmd
dotnet build
dotnet run --project CampusEvents\CampusEvents.csproj
```

Expected behavior:

1. a normal authenticated Participant does not gain `/Events/Manage`
2. the seeded organizer can reach `/Events/Manage`
3. the list contains events whose `OrganizerId` matches that Organizer
4. Draft, Published, and Cancelled rows may all appear
5. only Draft rows show an Edit link
6. the public `/Events` page still behaves exactly as before

In the browser, open the management page at a wide width and again at a narrow mobile width. The Event metadata should remain readable, long titles should wrap, and the Create and Edit links should be reachable by keyboard with a visible focus indicator. In developer tools, verify that `css/site.css` loads through the shared layout.

<br>

## Define an allowlisted Event input model

The Organizer form needs a type that represents only submitted Event-editing fields.

**File: `ViewModels/EventInputModel.cs`**

Create:

```csharp
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CampusEvents.ViewModels;

public class EventInputModel : IValidatableObject
{
	[Required]
	public string Title { get; set; } = string.Empty;

	[Required]
	public string Description { get; set; } = string.Empty;

	[Required]
	[Range(1, int.MaxValue, ErrorMessage = "Select a Category.")]
	[Display(Name = "Category")]
	public int? CategoryId { get; set; }

	[Required]
	public string Location { get; set; } = string.Empty;

	[Required]
	[DataType(DataType.DateTime)]
	[Display(Name = "Start date and time")]
	public DateTime? StartDateTime { get; set; }

	[Required]
	[DataType(DataType.DateTime)]
	[Display(Name = "End date and time")]
	public DateTime? EndDateTime { get; set; }

	[Required]
	[Range(1, int.MaxValue, ErrorMessage = "Capacity must be greater than zero.")]
	public int? Capacity { get; set; }

	public IReadOnlyList<SelectListItem> CategoryOptions { get; set; } = Array.Empty<SelectListItem>();

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (StartDateTime.HasValue && EndDateTime.HasValue && StartDateTime.Value >= EndDateTime.Value)
		{
			yield return new ValidationResult("End date and time must be after the start date and time.",
				new[] { nameof(EndDateTime) });
		}
	}
}
```

`EventInputModel` represents submitted values independently of the persisted `Event` entity. Nullable `int?` and `DateTime?` properties distinguish omitted numeric and date/time inputs (`null`) from supplied values; non-nullable value types would initialize to `0` or `DateTime.MinValue`. `[Required]` checks missing values, `[Range]` enforces positive capacity, and `IValidatableObject` requires the end time to follow the start time. Under the default MVC string-binding configuration, empty or whitespace-only text becomes `null` and fails `[Required]`. After successful validation, the controller reads the required nullable values through `.Value` and trims accepted text before persistence.

The controller checks the selected Category against persisted records and enforces the authenticated Organizer's ownership when editing. Event-to-Category and Event-to-Organizer foreign keys, together with `CK_Events_CapacityPositive` and `CK_Events_StartBeforeEnd`, protect database integrity. These layers provide specific form feedback while preserving a consistent Event schema that already supports the Create and Edit workflows.

Build it:

```cmd
dotnet build
```

<br>

## Prepare Category options and the create GET

Create and Edit both need the same category list. Add these private helpers inside `EventsController`.

**File: `Controllers/EventsController.cs`**

Add:

```csharp
private async Task PopulateCategoryOptionsAsync(EventInputModel model)
{
	var categories = await _context.Categories
		.AsNoTracking()
		.OrderBy(category => category.Name)
		.ToListAsync();

	model.CategoryOptions = categories
		.Select(category => new SelectListItem(category.Name, category.Id.ToString()))
		.ToList();
}

private async Task ValidateCategoryAsync(EventInputModel model)
{
	if (!model.CategoryId.HasValue || model.CategoryId.Value <= 0)
	{
		return;
	}

	var categoryExists = await _context.Categories
		.AnyAsync(category => category.Id == model.CategoryId.Value);

	if (!categoryExists)
	{
		ModelState.AddModelError(nameof(model.CategoryId), "Select an existing Category.");
	}
}
```

The Create GET prepares an empty form plus valid Category options:

```csharp
[HttpGet]
[Authorize(Roles = RoleNames.Organizer)]
public async Task<IActionResult> Create()
{
	var model = new EventInputModel();

	await PopulateCategoryOptionsAsync(model);

	return View(model);
}
```

<br>

## Render an accessible create form

Use the shared `.form-field`, `.field-label`, `.field-control`, `.validation-summary`, `.validation-message`, and button classes for the controls in this View. Those styles are already in `site.css`, and `_Layout.cshtml` loads them for every page. **Add Event-Form Styling** will add only the spacing and layout specific to Event forms.

**File: `Views/Events/Create.cshtml`**

Create:

```cshtml
@model CampusEvents.ViewModels.EventInputModel

@{
	ViewData["Title"] = "Create Event";
}

<h1>Create Event</h1>

<p>
	New Events are saved as Draft and belong to your Organizer account.
</p>

<form asp-controller="Events"
		asp-action="Create"
		method="post"
		asp-antiforgery="true"
		class="event-form">

	<div asp-validation-summary="All"
		class="validation-summary"
		role="alert"></div>

	<div class="form-field">
		<label asp-for="Title" class="field-label"></label>
		<input asp-for="Title" class="field-control" required />
		<span asp-validation-for="Title" class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="Description" class="field-label"></label>
		<textarea asp-for="Description"
			class="field-control event-description"
			rows="6"
			required></textarea>
		<span asp-validation-for="Description" class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="CategoryId" class="field-label"></label>
		<select asp-for="CategoryId"
			asp-items="Model.CategoryOptions"
			class="field-control"
			required>
			<option value="">Select a Category</option>
		</select>
		<span asp-validation-for="CategoryId" class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="Location" class="field-label"></label>
		<input asp-for="Location" class="field-control" required />
		<span asp-validation-for="Location" class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="StartDateTime" class="field-label"></label>
		<input asp-for="StartDateTime"
			type="datetime-local"
			asp-format="{0:yyyy-MM-ddTHH:mm}"
			class="field-control"
			required />
		<span asp-validation-for="StartDateTime" class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="EndDateTime" class="field-label"></label>
		<input asp-for="EndDateTime"
			type="datetime-local"
			asp-format="{0:yyyy-MM-ddTHH:mm}"
			class="field-control"
			required />
		<span asp-validation-for="EndDateTime" class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="Capacity" class="field-label"></label>
		<input asp-for="Capacity"
			type="number"
			min="1"
			class="field-control"
			required />
		<span asp-validation-for="Capacity" class="validation-message"></span>
	</div>

	<div class="form-actions">
		<button type="submit" class="button-primary">Save Draft</button>

		<a asp-controller="Events"
			asp-action="Manage"
			class="button-secondary">
			Cancel
		</a>
	</div>
</form>
```

The form uses native HTML controls and hints such as `required`, `min="1"`, and `type="datetime-local"` to support valid input. The server still validates every submitted value because an HTTP request can omit or alter these hints.

The `asp-for`, `asp-items`, `asp-validation-for`, and `asp-validation-summary` attributes are Razor Tag Helpers. They connect the View to the input model, Category options, and server-generated `ModelState` errors.

The authoritative sequence remains:

```text
request
	↓
model binding
	↓
server-side validation
	↓
ModelState
	↓
controller rules
	↓
database integrity
```

<br>

## Add event-form styling

**File: `wwwroot/css/site.css`**

Append these Event-specific rules to the existing stylesheet:

```css
.event-form {
	width: min(100%, 42rem);
	display: grid;
	gap: 1rem;
}

.event-description {
	min-height: 8rem;
	resize: vertical;
}

.form-actions {
	display: flex;
	flex-wrap: wrap;
	gap: 0.75rem;
}

@media (max-width: 40rem) {
	.form-actions > * {
		flex: 1 1 100%;
	}
}
```

`.event-form` constrains the form to a readable width and spaces its field groups. `.event-description` provides enough room for longer text while allowing vertical resizing. `.form-actions` keeps the primary submit control and Cancel link together, then stacks them on narrow screens. The inputs, buttons, field labels, and validation messages continue to use the shared rules introduced with the account forms.

<br>

## Validate and persist create submissions

The Create POST processes the submitted form in five stages:

1. Bind the submitted fields to `EventInputModel`.
2. Validate the form and selected Category.
3. Resolve the authenticated Organizer.
4. Assign the editable values and server-managed Event properties.
5. Save the validated Event and redirect to Manage.

**File: `Controllers/EventsController.cs`**

Add:

```csharp
[HttpPost]
[Authorize(Roles = RoleNames.Organizer)]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(EventInputModel model)
{
	await ValidateCategoryAsync(model);

	if (!ModelState.IsValid)
	{
		await PopulateCategoryOptionsAsync(model);
		return View(model);
	}

	var organizer = await _userManager.GetUserAsync(User);

	if (organizer is null)
	{
		return Challenge();
	}

	// One captured local wall-clock value for this successful Create operation.
	var now = _timeProvider.GetLocalNow().DateTime;

	var eventItem = new Event
	{
		Title = model.Title.Trim(),
		Description = model.Description.Trim(),
		CategoryId = model.CategoryId!.Value,
		Location = model.Location.Trim(),
		StartDateTime = model.StartDateTime!.Value,
		EndDateTime = model.EndDateTime!.Value,
		Capacity = model.Capacity!.Value,

		OrganizerId = organizer.Id,
		Status = EventStatus.Draft,
		CreatedAt = now
	};

	_context.Events.Add(eventItem);
	await _context.SaveChangesAsync();

	return RedirectToAction(nameof(Manage));
}
```

<br>

## Trace binding, validation, and create persistence

Follow a successful Create submission from the browser to the database:

1. MVC binds the posted fields into `EventInputModel` and evaluates Data Annotations and cross-field validation.
2. The controller checks that the submitted `CategoryId` refers to an existing Category. Invalid input returns the form with `ModelState` errors and rebuilt Category options.
3. The controller resolves the authenticated Organizer and maps the approved fields into a new `Event`. It assigns `OrganizerId` from that identity, `EventStatus.Draft`, and `CreatedAt` from the injected `TimeProvider`.
4. The DbContext inserts the Event after all required checks succeed. The action redirects to Manage, so the browser follows the successful POST with a fresh GET.

```csharp
await ValidateCategoryAsync(model);
if (!ModelState.IsValid)
{
	await PopulateCategoryOptionsAsync(model);
	return View(model);
}
```

The browser supplies editable values; the server supplies protected ownership, status, and creation metadata. `TimeProvider.GetLocalNow().DateTime` yields the application's local wall-clock `DateTime` convention, consistent with the existing Event model and controllable test clock. Keep the application's host time-zone policy consistent across Event date handling.

A valid request reaches `_context.Events.Add(eventItem)` and `SaveChangesAsync()` once. An invalid request returns the form before the insert and preserves the Organizer's attempted values.

## Preserve attempted values on failed submissions

Suppose the Organizer submits:

```text
Title: Campus Robotics Showcase
Location: Engineering Atrium
Capacity: 0
Start: October 8, 2:00 PM
End: October 8, 1:00 PM
```

The action returns `View(model)`, supplying Razor with: `EventInputModel` and `ModelState` attempted values/errors. The Tag Helpers in the cshtml help redisplay the attempted values. The user should correct only the invalid fields rather than retyping every valid field. That directly supports `NFR-USA-002`.

<br>

## Reload an owned Draft for edit GET

An Edit request starts with a persisted Event. Load the owned Draft, then populate the same input contract used by Create.

**File: `Controllers/EventsController.cs`**

Add:

```csharp
[HttpGet]
[Authorize(Roles = RoleNames.Organizer)]
public async Task<IActionResult> Edit(int id)
{
	var organizer = await _userManager.GetUserAsync(User);

	if (organizer is null)
	{
		return Challenge();
	}

	var eventItem = await _context.Events.AsNoTracking().FirstOrDefaultAsync(eventItem =>
		eventItem.Id == id && eventItem.OrganizerId == organizer.Id && eventItem.Status == EventStatus.Draft);

	if (eventItem is null)
	{
		return EventNotFound();
	}

	var model = new EventInputModel
	{
		Title = eventItem.Title,
		Description = eventItem.Description,
		CategoryId = eventItem.CategoryId,
		Location = eventItem.Location,
		StartDateTime = eventItem.StartDateTime,
		EndDateTime = eventItem.EndDateTime,
		Capacity = eventItem.Capacity
	};

	await PopulateCategoryOptionsAsync(model);

	return View(model);
}
```

The query combines resource identity, Organizer ownership, and Draft eligibility:

```csharp
eventItem.Id == id &&
eventItem.OrganizerId == organizer.Id &&
eventItem.Status == EventStatus.Draft
```

The URL supplies `id`; the server also checks Organizer ownership and Draft eligibility. Changing: `/Events/Edit/10` to `/Events/Edit/11` does not transfer ownership. If Event 11 is not an owned Draft Event, the query returns `null` and the action returns `EventNotFound()`. Responding `NotFound` also avoids using the organizer workflow to reveal whether an inaccessible ID belongs to another user or simply does not exist.

<br>

## Render the draft-only edit form

Use the same `.event-form`, `.form-field`, `.field-control`, and `.form-actions` classes as Create, so both Organizer forms have consistent labels, controls, buttons, and validation feedback. They share the stylesheet and differ in their model values and POST destination.

**File: `Views/Events/Edit.cshtml`**

Create:

```cshtml
@model CampusEvents.ViewModels.EventInputModel

@{
	ViewData["Title"] = "Edit Event";
	var eventId = ViewContext.RouteData.Values["id"];
}

<h1>Edit Draft Event</h1>

<p>Update the details of this Draft Event.</p>

<form asp-controller="Events"
	asp-action="Edit"
	asp-route-id="@eventId"
	method="post"
	asp-antiforgery="true"
	class="event-form">

	<div asp-validation-summary="All" class="validation-summary" role="alert"></div>

	<div class="form-field">
		<label asp-for="Title" class="field-label"></label>
		<input asp-for="Title" class="field-control" required />
		<span asp-validation-for="Title" class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="Description" class="field-label"></label>
		<textarea asp-for="Description"
			class="field-control event-description"
			rows="6"
			required></textarea>
		<span asp-validation-for="Description" class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="CategoryId" class="field-label"></label>
		<select asp-for="CategoryId"
			asp-items="Model.CategoryOptions"
			class="field-control"
			required>
			<option value="">Select a Category</option>
		</select>
		<span asp-validation-for="CategoryId" class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="Location" class="field-label"></label>
		<input asp-for="Location" class="field-control" required />
		<span asp-validation-for="Location" class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="StartDateTime" class="field-label"></label>
		<input asp-for="StartDateTime"
			type="datetime-local"
			asp-format="{0:yyyy-MM-ddTHH:mm}"
			class="field-control"
			required />
		<span asp-validation-for="StartDateTime" class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="EndDateTime" class="field-label"></label>
		<input asp-for="EndDateTime"
			type="datetime-local"
			asp-format="{0:yyyy-MM-ddTHH:mm}"
			class="field-control"
			required />
		<span asp-validation-for="EndDateTime" class="validation-message"></span>
	</div>

	<div class="form-field">
		<label asp-for="Capacity" class="field-label"></label>
		<input asp-for="Capacity"
			type="number"
			min="1"
			class="field-control"
			required />
		<span asp-validation-for="Capacity" class="validation-message"></span>
	</div>

	<div class="form-actions">
		<button type="submit" class="button-primary">Save Changes</button>

		<a asp-controller="Events"
			asp-action="Manage"
			class="button-secondary">
			Cancel
		</a>
	</div>
</form>
```

<br>

## Re-query ownership and eligibility before edit POST

The Edit POST binds editable values into `EventInputModel`, reloads the persisted Event using the authenticated Organizer and Draft eligibility, and maps validated changes onto that authorized resource. Follow this request path:

```text
POST /Events/Edit/{id} with editable input
             |
             v
Resolve authenticated Organizer
             |
             v
Query owned Draft by route ID
             |
             +-- No match --> Return NotFound
             |
             v
Validate input and Category
             |
             +-- Invalid --> Redisplay Edit with errors
             |
             v
Map approved editable properties
             |
             v
SaveChangesAsync --> Redirect to Manage
```

**File: `Controllers/EventsController.cs`**

Add:

```csharp
[HttpPost]
[Authorize(Roles = RoleNames.Organizer)]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, EventInputModel model)
{
	var organizer = await _userManager.GetUserAsync(User);

	if (organizer is null)
	{
		return Challenge();
	}

	var eventItem = await _context.Events.FirstOrDefaultAsync(eventItem =>
		eventItem.Id == id && eventItem.OrganizerId == organizer.Id && eventItem.Status == EventStatus.Draft);

	if (eventItem is null)
	{
		return EventNotFound();
	}

	await ValidateCategoryAsync(model);

	if (!ModelState.IsValid)
	{
		await PopulateCategoryOptionsAsync(model);
		return View(model);
	}

	eventItem.Title = model.Title.Trim();
	eventItem.Description = model.Description.Trim();
	eventItem.CategoryId = model.CategoryId!.Value;
	eventItem.Location = model.Location.Trim();
	eventItem.StartDateTime = model.StartDateTime!.Value;
	eventItem.EndDateTime = model.EndDateTime!.Value;
	eventItem.Capacity = model.Capacity!.Value;

	await _context.SaveChangesAsync();

	return RedirectToAction(nameof(Manage));
}
```

Re-query the owned Draft before applying any requested changes:

```text
identify resource
	↓
authorize resource
	↓
validate requested changes
	↓
mutate allowed properties
	↓
persist
```

That makes `NFR-REL-002` easier to reason about:

- authorization failure → no mutation
- validation failure → no mutation
- validated, authorized requests reach the property-assignment block

The Edit action maps only approved Event details. It preserves server-managed `OrganizerId`, `Status`, and `CreatedAt`, while dedicated lifecycle operations govern changes in Event status.

Build and run it:

```cmd
dotnet build
dotnet run --project CampusEvents\CampusEvents.csproj
```

Expected routes now include:

```text
GET	/Events/Manage
GET	/Events/Create
POST /Events/Create
GET	/Events/Edit/{id}
POST /Events/Edit/{id}
```

All five are Organizer-only. The two POST routes also require anti-forgery validation.

<br>

## Review the cumulative public and Organizer controller

The same controller supports both **anonymous public browsing** and **protected Organizer workflows**. This complete reference keeps the established public Index/Details behavior together with the new Manage/Create/Edit actions. It is not a reason to remove the catalog filters or put `[Authorize]` on the controller class.

**File: `CampusEvents/Controllers/EventsController.cs`**

```csharp
using CampusEvents.Data;
using CampusEvents.Models;
using CampusEvents.Security;
using CampusEvents.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Controllers;

public class EventsController : Controller
{
	private readonly ILogger<EventsController> _logger;
	private readonly ApplicationDbContext _context;
	private readonly TimeProvider _timeProvider;
	private readonly UserManager<ApplicationUser> _userManager;
	private const int DefaultPageSize = 10;
	private static readonly int[] AllowedPageSizes = { 10, 25, 50 };

	public EventsController(
		ILogger<EventsController> logger,
		ApplicationDbContext context,
		TimeProvider timeProvider,
		UserManager<ApplicationUser> userManager)
	{
		_logger = logger;
		_context = context;
		_timeProvider = timeProvider;
		_userManager = userManager;
	}

	private ViewResult EventNotFound()
	{
		Response.StatusCode = StatusCodes.Status404NotFound;
		return View("EventNotFound");
	}

	// The public catalog must remain accessible to anonymous visitors.
	public async Task<IActionResult> Index(string? search, int? categoryId, int? page, int? pageSize)
	{
		_logger.LogInformation("Loading Published upcoming CEMS events with public filters and paging.");
		var pageSizeWasSupplied = Request.Query.ContainsKey(nameof(pageSize));
		if (pageSizeWasSupplied && (!pageSize.HasValue || !AllowedPageSizes.Contains(pageSize.Value)))
		{
			var errorViewModel = new ErrorPageViewModel
			{
				Title = "Invalid Page Size",
				Message = "Choose a page size of 10, 25, or 50.",
				ReturnUrl = Url.Action("Index", "Events"),
				ReturnLabel = "Return to Events"
			};
			Response.StatusCode = StatusCodes.Status400BadRequest;
			return View("BadRequest", errorViewModel);
		}

		var currentPageSize = pageSize.GetValueOrDefault(DefaultPageSize);
		var now = _timeProvider.GetLocalNow().DateTime;
		IQueryable<Event> query = _context.Events
			.AsNoTracking()
			.Include(item => item.Category)
			.Where(item => item.Status == EventStatus.Published &&
				item.StartDateTime > now);

		if (!string.IsNullOrWhiteSpace(search))
		{
			var term = search.Trim();
			query = query.Where(item => item.Title.Contains(term) || item.Location.Contains(term));
		}
		if (categoryId.HasValue)
		{
			query = query.Where(item => item.CategoryId == categoryId.Value);
		}

		var totalCount = await query.CountAsync();
		var totalPages = (int)Math.Ceiling(totalCount / (double)currentPageSize);
		var currentPage = page.GetValueOrDefault(1);
		if (currentPage < 1) currentPage = 1;
		if (totalPages > 0 && currentPage > totalPages) currentPage = totalPages;
		if (totalPages == 0) currentPage = 1;

		var events = await query
			.OrderBy(item => item.StartDateTime)
			.ThenBy(item => item.Id)
			.Skip((currentPage - 1) * currentPageSize)
			.Take(currentPageSize)
			.ToListAsync();
		var categories = await _context.Categories
			.AsNoTracking()
			.OrderBy(category => category.Name)
			.ToListAsync();

		return View(new EventSearchViewModel
		{
			Events = events,
			Categories = categories,
			Search = search,
			CategoryId = categoryId,
			CurrentPage = currentPage,
			PageSize = currentPageSize,
			PageSizeOptions = AllowedPageSizes,
			TotalCount = totalCount,
			TotalPages = totalPages
		});
	}

	public async Task<IActionResult> Details(int id)
	{
		var eventItem = await _context.Events
			.AsNoTracking()
			.Include(item => item.Category)
			.Include(item => item.Organizer)
			.SingleOrDefaultAsync(item => item.Id == id && item.Status == EventStatus.Published);

		if (eventItem is null) return EventNotFound();

		return View(eventItem);
	}

	[HttpGet]
	[Authorize(Roles = RoleNames.Organizer)]
	public async Task<IActionResult> Manage()
	{
		var organizer = await _userManager.GetUserAsync(User);
		if (organizer is null) return Challenge();
		var events = await _context.Events
			.AsNoTracking()
			.Include(item => item.Category)
			.Where(item => item.OrganizerId == organizer.Id)
			.OrderBy(item => item.StartDateTime)
			.ThenBy(item => item.Id)
			.ToListAsync();
		return View(events);
	}

	private async Task PopulateCategoryOptionsAsync(EventInputModel model)
	{
		var categories = await _context.Categories
			.AsNoTracking()
			.OrderBy(category => category.Name)
			.ToListAsync();
		model.CategoryOptions = categories
			.Select(category => new SelectListItem(category.Name, category.Id.ToString()))
			.ToList();
	}

	private async Task ValidateCategoryAsync(EventInputModel model)
	{
		if (!model.CategoryId.HasValue || model.CategoryId.Value <= 0) return;
		var exists = await _context.Categories
			.AnyAsync(category => category.Id == model.CategoryId.Value);
		if (!exists)
		{
			ModelState.AddModelError(nameof(model.CategoryId), "Select an existing Category.");
		}
	}

	[HttpGet]
	[Authorize(Roles = RoleNames.Organizer)]
	public async Task<IActionResult> Create()
	{
		var model = new EventInputModel();
		await PopulateCategoryOptionsAsync(model);
		return View(model);
	}

	[HttpPost]
	[Authorize(Roles = RoleNames.Organizer)]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(EventInputModel model)
	{
		await ValidateCategoryAsync(model);
		if (!ModelState.IsValid)
		{
			await PopulateCategoryOptionsAsync(model);
			return View(model);
		}

		var organizer = await _userManager.GetUserAsync(User);
		if (organizer is null) return Challenge();
		var now = _timeProvider.GetLocalNow().DateTime;
		var item = new Event
		{
			Title = model.Title.Trim(),
			Description = model.Description.Trim(),
			CategoryId = model.CategoryId!.Value,
			Location = model.Location.Trim(),
			StartDateTime = model.StartDateTime!.Value,
			EndDateTime = model.EndDateTime!.Value,
			Capacity = model.Capacity!.Value,
			OrganizerId = organizer.Id,
			Status = EventStatus.Draft,
			CreatedAt = now
		};
		_context.Events.Add(item);
		await _context.SaveChangesAsync();
		return RedirectToAction(nameof(Manage));
	}

	[HttpGet]
	[Authorize(Roles = RoleNames.Organizer)]
	public async Task<IActionResult> Edit(int id)
	{
		var organizer = await _userManager.GetUserAsync(User);
		if (organizer is null) return Challenge();
		var item = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e =>
			e.Id == id && e.OrganizerId == organizer.Id && e.Status == EventStatus.Draft);
		if (item is null) return EventNotFound();
		var model = new EventInputModel
		{
			Title = item.Title,
			Description = item.Description,
			CategoryId = item.CategoryId,
			Location = item.Location,
			StartDateTime = item.StartDateTime,
			EndDateTime = item.EndDateTime,
			Capacity = item.Capacity
		};
		await PopulateCategoryOptionsAsync(model);
		return View(model);
	}

	[HttpPost]
	[Authorize(Roles = RoleNames.Organizer)]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Edit(int id, EventInputModel model)
	{
		var organizer = await _userManager.GetUserAsync(User);
		if (organizer is null) return Challenge();
		var item = await _context.Events.FirstOrDefaultAsync(e =>
			e.Id == id && e.OrganizerId == organizer.Id && e.Status == EventStatus.Draft);
		if (item is null) return EventNotFound();
		await ValidateCategoryAsync(model);
		if (!ModelState.IsValid)
		{
			await PopulateCategoryOptionsAsync(model);
			return View(model);
		}
		item.Title = model.Title.Trim();
		item.Description = model.Description.Trim();
		item.CategoryId = model.CategoryId!.Value;
		item.Location = model.Location.Trim();
		item.StartDateTime = model.StartDateTime!.Value;
		item.EndDateTime = model.EndDateTime!.Value;
		item.Capacity = model.Capacity!.Value;
		await _context.SaveChangesAsync();
		return RedirectToAction(nameof(Manage));
	}
}
```


# Guided Discussion

The management list, Create form, and Draft Edit flow apply the same trust boundary: the browser proposes editable values, while the server determines ownership, eligibility, and permitted persistence.

## Stateful form workflows and HTTP request contracts

A form interaction spans distinct HTTP requests, even when users experience it as a single task. A GET renders an editable representation; a POST submits a proposed change; a successful POST redirects to a GET of the management page. This **POST/Redirect/GET (PRG)** sequence gives refresh and navigation a stable retrieval endpoint after a successful write. A rejected POST instead returns the form with the submitted model and validation errors so the user can repair the request without losing context.

| Request | Server decision | Observable result |
| --- | --- | --- |
| `GET /Events/Create` | Check Organizer access and load Categories | Empty form with available choices |
| Invalid `POST /Events/Create` | Bind, validate and reject without saving | Form with attempted values and field feedback |
| Valid `POST /Events/Create` | Assign trusted fields, persist and redirect | Fresh `GET /Events/Manage` reflects the owned Draft |
| `GET /Events/Edit/{id}` | Verify current owner and Draft status | Populated edit form or inaccessible resource |
| `POST /Events/Edit/{id}` | Recheck owner/status and validate proposed fields | Persist allowed changes or preserve stored values |

The redirect completes a successful mutation with a fresh management-page GET; invalid submissions return the bound form with field feedback and retain stored Event values.

## Model binding, trust boundaries, and input contracts

Model binding converts submitted fields into typed action input. Authorization and server-assigned values establish the permissions and trusted state for the operation. An attacker can send additional fields such as `OrganizerId`, `Status`, or `CreatedAt` even when the form contains no corresponding controls. A hidden field is also client-controlled input.

`EventInputModel` intentionally contains only the properties accepted for this operation. The controller maps validated values to `Event` explicitly, uses the authenticated user for ownership, and assigns the initial Draft state and creation time on the server. This allowlist restricts the persisted values to the operation contract. Antiforgery protection addresses cross-site request submission, while authorization and ownership checks govern resource access.

The general principle is to treat request models as operation contracts rather than persistence schemas. Separate input contracts can express distinct allowed fields and validation rules when operations require them.

## Layered validation and relational integrity

Different checks answer different questions. They should cooperate rather than substitute for one another.

| Layer | Organizer workflow example | Responsibility |
| --- | --- | --- |
| Field-level | Required title, description, location, date/time and positive capacity | Reject absent or individually invalid proposed values |
| Cross-field | End must follow start | Validate a relationship between submitted values |
| Database-backed | Submitted `CategoryId` refers to an existing Category | Establish current reference validity with trusted data |
| Relational constraint | Event's Category foreign key | Protect stored relationships even when application validation is missing or overtaken by another write |

Data Annotations express straightforward property rules; `IValidatableObject` supports the chronology rule across fields; the controller checks Category existence; SQL Server enforces the configured foreign key. Check the Category reference against the persisted Categories table. Browser constraints help the user enter values, but server checks remain authoritative. These layers also have different failure presentations: a meaningful validation message can be shown before attempting persistence, whereas a database failure needs controlled handling.

## Validation feedback and form reconstruction

The submitted `EventInputModel`, `ModelState` errors, and Category option list serve different purposes. The model represents the proposed editable values, `ModelState` records binding and validation outcomes (including attempted input), and the Category list is trusted reference data loaded by the server. A failed POST must reconstruct that list before redisplaying the form. Razor inputs can then display attempted values while the application shows field-level errors and a validation summary.

This preserves the user's work while keeping rejected data out of the database. Clear labels, an error summary, visible focus, and logical field associations make the recovery path usable with both keyboard navigation and assistive technologies. On invalid submissions, retain the attempted values in the form and preserve the stored Event.

## Role authorization and resource ownership

An authenticated user is identified by the server-managed account session. The Organizer role permits entry into the management workflow; an owner-scoped query then selects Events associated with the authenticated Organizer. A user who changes the Event ID in a URL or POST still encounters the same ownership predicate. Apply these checks in the controller actions that handle the request; the availability of navigation links serves usability.

Having Administrator membership **in addition** to Organizer does not widen these ordinary Organizer Edit/Create/Publish/Delete queries. Administrator cancellation of another owner's eligible Published Event is a different workflow introduced in Lesson 8; the explicit dual-role **registration read** exception in that lesson does not authorize foreign-owner Event edits. This is an example of privileges being operation-specific rather than an automatic hierarchy. Keep the cross-owner negative GET and POST tests even for a dual-role principal; do not add `OR IsInRole(Administrator)` to an Organizer mutation lookup.

The management list filters Events by the current Organizer ID at query time. For Edit, the controller retrieves a resource using both the requested Event ID and the authenticated Organizer ID. When no resource matches, `EventNotFound()` provides the same response for missing and inaccessible IDs.

## Current-state eligibility and guarded mutations

An editable Event must satisfy **both ownership and current Draft status** when the server handles the operation. This current-state condition is checked on Edit GET and re-evaluated on Edit POST, because a resource may change between those requests.

```text
Authenticated request
    → Organizer role authorized?
    → Event selected by ID and authenticated Organizer ID?
    → Event currently Draft?
    → Proposed editable values valid?
    → Assign permitted fields and persist
```

A previously opened form supplies proposed values, while the server's resource query supplies ownership and eligibility at the time of retrieval. This separation makes a forged ID and a stale Edit form follow the same protected mutation path.

| Organizer access | Owns Event | Current status | Valid input | Result |
| --- | --- | --- | --- | --- |
| Allowed | Yes | Draft | Yes | Save permitted fields |
| Allowed | Yes | Draft | No | Redisplay validation errors |
| Allowed | No | Draft | Yes | Reject access |
| Allowed | Yes | Published or Cancelled | Yes | Reject ineligible Edit |
| Denied | Any | Any | Any | Reject protected request |

## Persistence boundaries and rejected writes

A valid request should change only the fields allowed by the operation. For Create, this means selecting server-owned values before the insert. For Edit, it means loading the eligible tracked Event and validating the proposed changes before assigning editable properties and calling `SaveChangesAsync()`. For rejected requests, the expected database invariant is that no unauthorized insert or update appears.

An in-memory assignment, EF change tracking, and a database commit are distinct stages. Validation-before-assignment reduces the chance of accidentally saving partially changed tracked entities. Concurrent edits introduce an additional consistency question; the Appendix compares ways to detect and control conflicting changes. See [Concurrent updates, optimistic concurrency, and transaction boundaries](#concurrent-updates-optimistic-concurrency-and-transaction-boundaries) for additional designs.

Place input mapping, owner lookup, validation, and persistence coordination in clearly identifiable responsibilities. When repeated operations grow, compare dedicated validators and authorization policies using consistency and focused testability as criteria.

## Verification of protected workflows

Verify protected workflows using HTTP assertions for denial, form responses, redirects, and antiforgery behavior, alongside SQL assertions for inserted or unchanged owner, status, dates, and editable fields. A valid Create with extra `OrganizerId` or `Status` parameters tests the input allowlist, while a foreign-owned Edit tests authorization even when the request's editable values are otherwise valid. Invalid timing or Category input tests that form feedback corresponds to no database mutation.

Automated SQL Server-backed requests make these scenarios repeatable with controlled identities and data. Browser checks cover accessibility, mobile presentation, focus, and comprehensible error recovery. Together, HTTP, persisted-state, and browser observations establish the behavior of a protected form workflow.

# Guided Verification and Validation

Run the isolated HTTP/database regression tests for request, validation, persistence, and authorization contracts. Inspect the Organizer forms in the browser for accessible interaction and understandable feedback, then record the observed outcomes against the SRS.

<br>

## Automated

| Test / case | Behavior or boundary | Expected assertion / evidence |
| --- | --- | --- |
| Inherited catalog regressions (10 expected discoverable cases) | Public eligibility, search, filtering, Details, and pagination | Existing catalog contracts remain intact |
| Inherited authentication regressions (8 expected discoverable cases) | Registration, login, logout, roles, antiforgery, and redirects | Existing account and access contracts remain intact |
| R1 — Role and owned Manage (1 case) | Anonymous, Participant, and Organizer requests; ownership filtering | Protected access; only the signed-in Organizer's Events appear |
| R2 — Valid Create (1 case) | Submitted fields, forged protected fields, and server assignment | New Draft belongs to the authenticated Organizer; persisted values and redirect match the contract |
| R3 — Invalid Create (5 theory cases) | Missing Title, whitespace-only Title, invalid chronology, zero Capacity, unknown Category | Relevant validation feedback; no unintended Event insertion |
| R4 — Disallowed Edit (3 theory cases) | Foreign Draft, owned Published, owned Cancelled; GET and POST | Ineligible resources rejected and stored records unchanged |
| R5 — Owned Draft Edit (1 case) | Valid update followed by invalid submission | Only valid approved fields persist; owner, status, and created timestamp remain unchanged |

Use the existing `CampusEvents.Tests` SQL Server fixture to create and migrate a uniquely named, isolated database with controlled seed data. It connects to Windows LocalDB `(localdb)\MSSQLLocalDB` by default or an authorized `CEMS_TEST_SQLSERVER` instance targeted at `master`, then drops its test database during cleanup.

Run `WebApplicationFactory<Program>` in **Testing** with real Identity services, authentication cookies, antiforgery tokens, controlled Event data, and a fixed `FakeTimeProvider`. Set `AllowAutoRedirect = false` to inspect redirects and `HandleCookies = true` to retain the authenticated session. Use `[Theory]` for the R3 and R4 variants; the R1–R5 inventory specifies eleven expected discoverable cases across five groups.

<br>

### Add the HTTP and persistence tests

**File: `CampusEvents.Tests/OrganizerEventHttpTests.cs`**

Create the following file in the **existing test project**, alongside `AuthenticationHttpTests.cs` and the SQL Server fixture. Use the established Lesson 4 test-project packages, `Microsoft.AspNetCore.Mvc.Testing` **10.0.13** and `Microsoft.Extensions.TimeProvider.Testing` **10.11.0**.

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
}
```

**Keep the existing public-catalog tests compatible with the constructor**

When you added `UserManager<ApplicationUser>` to the `EventsController` constructor, you also changed how the controller must be constructed in the existing Lesson 3 direct-action tests. Those tests call the public `Index` and `Details` methods directly, so the old three-argument fixture no longer compiles. Keep its real SQL Server fixture, four-argument `Index` calls, filtering tests, and 10/25/50 pagination cases; update its controller construction to include the fourth argument.

**File: `CampusEvents.Tests/EventsControllerTests.cs`** — retain this existing file in the sibling test project with the following complete, corrected class:

```csharp
using CampusEvents.Controllers;
using CampusEvents.Data;
using CampusEvents.Models;
using CampusEvents.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace CampusEvents.Tests;

public class EventsControllerTests
{
	private static FakeTimeProvider FixedClock()
	{
		var clock = new FakeTimeProvider(new DateTimeOffset(2026, 4, 15, 16, 0, 0, TimeSpan.Zero));
		var zone = TimeZoneInfo.CreateCustomTimeZone("CemsFixedUtcMinus4", TimeSpan.FromHours(-4),
			"CEMS fixed UTC-04 test zone", "CEMS fixed UTC-04 test zone");
		clock.SetLocalTimeZone(zone);
		return clock;
	}

	private static Event MakeEvent(
		string title, EventStatus status, DateTime start,
		int categoryId, string organizerId, string location = "Campus Student Center")
	{
		return new Event
		{
			Title = title,
			Description = "CEMS relational regression example",
			Location = location,
			StartDateTime = start,
			EndDateTime = start.AddHours(2),
			Capacity = 50,
			Status = status,
			CreatedAt = start.AddDays(-7),
			CategoryId = categoryId,
			OrganizerId = organizerId
		};
	}

	private static EventsController CreateController(
		ApplicationDbContext context, FakeTimeProvider clock, string queryString = "")
	{
		var httpContext = new DefaultHttpContext();
		httpContext.Request.QueryString = new QueryString(queryString);

		// This direct-action fixture exercises anonymous catalog methods only.
		// Organizer actions are exercised through real Identity HTTP hosts below.
		var controller = new EventsController(NullLogger<EventsController>.Instance, context, clock, null!)
		{
			ControllerContext = new ControllerContext
			{
				HttpContext = httpContext
			}
		};

		// Direct action calls need a URL helper for the error View's return URL.
		controller.Url = new EventsIndexUrlHelper(controller.ControllerContext);
		return controller;
	}

	// This tiny test helper only supplies the server-created return link.
	private sealed class EventsIndexUrlHelper : IUrlHelper
	{
		public EventsIndexUrlHelper(ActionContext actionContext)
		{
			ActionContext = actionContext;
		}

		public ActionContext ActionContext { get; }
		public string? Action(UrlActionContext context) => "/Events";
		public string? Content(string? contentPath) => contentPath;
		public bool IsLocalUrl(string? url) => url is not null && url.StartsWith("/", StringComparison.Ordinal);
		public string? Link(string? routeName, object? values) => null;
		public string? RouteUrl(UrlRouteContext context) => null;
	}

	[Fact]
	public async Task Index_ReturnsPublishedUpcomingEventsInChronologicalOrder()
	{
		var clock = FixedClock();
		var now = clock.GetLocalNow().DateTime;
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();

		await using (var arrange = database.CreateContext())
		{
			var category = new Category { Name = "Academic" };
			var organizer = new ApplicationUser
			{
				Id = "catalog-test-organizer", DisplayName = "Campus Events Office"
			};
			arrange.Categories.Add(category);
			arrange.Users.Add(organizer);
			await arrange.SaveChangesAsync();

			arrange.Events.AddRange(
				MakeEvent("Later Workshop", EventStatus.Published, now.AddDays(10), category.Id, organizer.Id),
				MakeEvent("Earlier Seminar", EventStatus.Published, now.AddDays(2), category.Id, organizer.Id),
				MakeEvent("Draft Planning", EventStatus.Draft, now.AddDays(3), category.Id, organizer.Id),
				MakeEvent("Cancelled Mixer", EventStatus.Cancelled, now.AddDays(4), category.Id, organizer.Id),
				MakeEvent("Past Panel", EventStatus.Published, now.AddDays(-2), category.Id, organizer.Id));
			await arrange.SaveChangesAsync();
		}

		await using var verify = database.CreateContext();
		var controller = CreateController(verify, clock);
		var view = Assert.IsType<ViewResult>(await controller.Index(null, null, null, null));
		var model = Assert.IsType<EventSearchViewModel>(view.Model);

		Assert.Collection(model.Events,
			item => Assert.Equal("Earlier Seminar", item.Title),
			item => Assert.Equal("Later Workshop", item.Title));
		Assert.Equal(2, model.TotalCount);
		Assert.Equal(1, model.TotalPages);
		Assert.Equal(10, model.PageSize);
		Assert.All(model.Events, item => Assert.Equal(EventStatus.Published, item.Status));
		Assert.All(model.Events, item => Assert.NotNull(item.Category));
	}

	[Fact]
	public async Task Details_DraftOrMissing_ReturnsEventNotFoundWith404()
	{
		var clock = FixedClock();
		var now = clock.GetLocalNow().DateTime;
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		int draftId;

		await using (var arrange = database.CreateContext())
		{
			var category = new Category { Name = "Social" };
			var organizer = new ApplicationUser
			{
				Id = "details-test-organizer", DisplayName = "Student Activities"
			};
			arrange.Categories.Add(category);
			arrange.Users.Add(organizer);
			await arrange.SaveChangesAsync();
			var draft = MakeEvent("Unpublished Mixer", EventStatus.Draft, now.AddDays(5), category.Id, organizer.Id);
			arrange.Events.Add(draft);
			await arrange.SaveChangesAsync();
			draftId = draft.Id;
		}

		await using var verify = database.CreateContext();
		foreach (var requestedId in new[] { draftId, int.MaxValue })
		{
			var controller = CreateController(verify, clock);
			var result = Assert.IsType<ViewResult>(await controller.Details(requestedId));
			Assert.Equal("EventNotFound", result.ViewName);
			Assert.Equal(StatusCodes.Status404NotFound, controller.Response.StatusCode);
		}
	}

	[Fact]
	public async Task Index_SearchAndCategory_ReturnsOnlyMatchingPublicEvents()
	{
		var clock = FixedClock();
		var now = clock.GetLocalNow().DateTime;
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		int academicId;

		await using (var arrange = database.CreateContext())
		{
			var academic = new Category { Name = "Academic" };
			var social = new Category { Name = "Social" };
			var organizer = new ApplicationUser
			{
				Id = "filter-test-organizer", DisplayName = "Student Learning Office"
			};
			arrange.Categories.AddRange(academic, social);
			arrange.Users.Add(organizer);
			await arrange.SaveChangesAsync();
			academicId = academic.Id;

			arrange.Events.AddRange(
				MakeEvent("Library Workshop", EventStatus.Published, now.AddDays(2), academic.Id, organizer.Id),
				MakeEvent("Study Skills", EventStatus.Published, now.AddDays(3), academic.Id, organizer.Id, "Library Annex"),
				MakeEvent("Library Mixer", EventStatus.Published, now.AddDays(4), social.Id, organizer.Id),
				MakeEvent("Library Draft", EventStatus.Draft, now.AddDays(5), academic.Id, organizer.Id),
				MakeEvent("Library Cancelled", EventStatus.Cancelled, now.AddDays(6), academic.Id, organizer.Id),
				MakeEvent("Library Archive", EventStatus.Published, now.AddDays(-2), academic.Id, organizer.Id),
				MakeEvent("Career Fair", EventStatus.Published, now.AddDays(7), academic.Id, organizer.Id));
			await arrange.SaveChangesAsync();
		}

		await using var verify = database.CreateContext();
		var controller = CreateController(verify, clock, $"?search=%20Library%20&categoryId={academicId}&page=1&pageSize=10");
		var view = Assert.IsType<ViewResult>(await controller.Index(" Library ", academicId, 1, 10));
		var model = Assert.IsType<EventSearchViewModel>(view.Model);

		Assert.Collection(model.Events,
			item => Assert.Equal("Library Workshop", item.Title),
			item => Assert.Equal("Study Skills", item.Title));
		Assert.Equal(2, model.TotalCount);
		Assert.Equal(1, model.TotalPages);
		Assert.Equal(" Library ", model.Search);
		Assert.Equal(academicId, model.CategoryId);
		Assert.Equal(10, model.PageSize);
	}

	[Theory]
	[InlineData(10, 6, 5)]
	[InlineData(25, 3, 5)]
	[InlineData(50, 2, 5)]
	public async Task Index_Pagination_UsesAllowedSizesAndStablePageBoundaries(
		int pageSize, int expectedPages, int expectedLastPageCount)
	{
		var clock = FixedClock();
		var now = clock.GetLocalNow().DateTime;
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();

		await using (var arrange = database.CreateContext())
		{
			var category = new Category { Name = "Academic" };
			var organizer = new ApplicationUser
			{
				Id = "paging-test-organizer", DisplayName = "Campus Programs"
			};
			arrange.Categories.Add(category);
			arrange.Users.Add(organizer);
			await arrange.SaveChangesAsync();

			for (var number = 1; number <= 55; number++)
			{
				arrange.Events.Add(MakeEvent(
					$"Campus Workshop {number:00}", EventStatus.Published,
					now.AddDays(1 + (number - 1) / 2), category.Id, organizer.Id));
			}
			await arrange.SaveChangesAsync();
		}

		await using var verify = database.CreateContext();
		var controller = CreateController(verify, clock, $"?page=999&pageSize={pageSize}");
		var lastView = Assert.IsType<ViewResult>(await controller.Index(null, null, 999, pageSize));
		var last = Assert.IsType<EventSearchViewModel>(lastView.Model);

		Assert.Equal(55, last.TotalCount);
		Assert.Equal(expectedPages, last.TotalPages);
		Assert.Equal(expectedPages, last.CurrentPage);
		Assert.Equal(expectedLastPageCount, last.Events.Count);
		Assert.Equal(pageSize, last.PageSize);
		Assert.Equal(new[] { 10, 25, 50 }, last.PageSizeOptions);
		Assert.True(last.Events.Zip(last.Events.Skip(1), (a, b) => a.StartDateTime < b.StartDateTime ||
			(a.StartDateTime == b.StartDateTime && a.Id < b.Id)).All(ordered => ordered));

		controller.Request.QueryString = new QueryString($"?page=0&pageSize={pageSize}");
		var firstView = Assert.IsType<ViewResult>(await controller.Index(null, null, 0, pageSize));
		var first = Assert.IsType<EventSearchViewModel>(firstView.Model);
		Assert.Equal(1, first.CurrentPage);
		Assert.Equal(pageSize, first.Events.Count);
	}

	[Theory]
	[InlineData("1", 1)]
	[InlineData("abc", null)]
	[InlineData("-10", -10)]
	public async Task Index_UnsupportedPageSize_ReturnsBadRequestViewAnd400(string requestedSize, int? boundValue)
	{
		var clock = FixedClock();
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		await using var verify = database.CreateContext();
		var controller = CreateController(verify, clock, $"?pageSize={requestedSize}");

		var view = Assert.IsType<ViewResult>(await controller.Index(null, null, null, boundValue));
		var error = Assert.IsType<ErrorPageViewModel>(view.Model);
		Assert.Equal("BadRequest", view.ViewName);
		Assert.Equal(StatusCodes.Status400BadRequest, controller.Response.StatusCode);
		Assert.Equal("Invalid Page Size", error.Title);
		Assert.Equal("/Events", error.ReturnUrl);
	}
}
```

In the direct-action catalog tests, `null!` supplies the constructor argument for a dependency unused by the public `Index` and `Details` actions. The null-forgiving operator suppresses a nullable warning. For protected Organizer scenarios, use `OrganizerEventHttpTests.cs` with the ASP.NET Core test host, dependency-injected Identity services, SQL-backed users, the login form, antiforgery token, and issued authentication cookie.

Preserve per-test database naming, migration execution, fixture-owned cleanup, and the existing assertions when updating the constructor.

<br>

### Run the cumulative suite

From the **outer** solution directory, where `CampusEvents.slnx`, `CampusEvents/`, and `CampusEvents.Tests/` are siblings:

```bat
REM The SDK must be .NET 10.x; both project targets are net10.0.
dotnet --info
dotnet restore .\CampusEvents.slnx
dotnet build .\CampusEvents.slnx --no-restore
dotnet test .\CampusEvents.Tests\CampusEvents.Tests.csproj --no-build --no-restore --logger "console;verbosity=normal"
```

On Windows, run these SQL Server tests with the default `(localdb)\MSSQLLocalDB` instance using the configured fixture. For another authorized SQL Server test endpoint, optionally set `CEMS_TEST_SQLSERVER` to a `master` connection with restricted testing credentials. Do not copy Development's `DefaultConnection` or Administrator credentials into test configuration. Report LocalDB connection, permission, or migration failures as test-environment problems, and use an isolated database. Confirm that each fixture-owned database is migrated and cleaned up.

When reviewing the results, distinguish individual `[Fact]` and `[Theory]` executions from test method count. After running the tests, record the observed results and inspect the Create/Edit redirect together with persisted field values. Browser/manual checks below remain necessary to verify labels, keyboard use, visual feedback, navigation links and rendered form state.

Keep the existing SQL Server schema and migrations. `EventInputModel` is a request-specific model rather than an EF entity; use the existing catalog and Identity migrations. Test databases use the already committed initial catalog and Identity migrations via `MigrateAsync()`.

<br>

## Manual

Use an authorized Development database and the seeded Organizer identity. Use a second Organizer and representative Draft, Published, and Cancelled records only for scenarios that require them; create such test records in an authorized environment without changing historical seed keys. Note initial rows before any write and distinguish client-side form blocking from a request that actually reaches the server. Run the following scenarios and record the observed status, page, fields, and database outcome.

<br>

### Create validation scenarios

The browser may stop a form with empty required fields or an out-of-range number before it sends a POST. That behavior demonstrates native HTML validation. To inspect how the server handles the same invalid values, use browser developer tools to temporarily disable the relevant HTML constraint for a single test, or send an authenticated HTTP request. Leave the C# validation rules intact and keep the final Razor form attributes in place. Verify that a POST actually reached the server before concluding that `ModelState` rejected it.

Run:

```cmd
dotnet build
dotnet run --project CampusEvents\CampusEvents.csproj
```

Sign in as the seeded organizer and open:

```text
/Events/Create
```

Use these scenarios.

<br>

### Missing required input

Leave one or more required fields empty.

Expected:

- server-side validation rejects the request
- the form is redisplayed
- validation feedback identifies the problem
- other valid values remain present
- no event is persisted

<br>

### End is not after start

Submit:

- Start: 10:00
- End: 09:00

Expected:

- `End date and time must be after the start date and time.`
- no event is persisted

<br>

### Non-positive capacity

Submit:

- Capacity = 0

Expected:

- `Capacity must be greater than zero.`
- no event is persisted

<br>

### Nonexistent Category manipulation

> **NOTE**: The normal `<select>` contains only real Categories. Use browser developer tools to temporarily change the selected option value to an ID that does not exist.

Submit:

- 9999

Expected:

- `Select an existing Category.`
- no event is persisted

The server validates the Category identifier against persisted records, including values modified in the browser.

<br>

### Valid create

Submit valid Event data.

Expected:

- a new Event row is saved
- the browser redirects to `/Events/Manage`
- the Event appears in the management list
- the new Event status is `Draft`
- the new Event owner is the authenticated Organizer
- refreshing `/Events/Manage` still shows the Event

The last point demonstrates persistence across requests.

<br>

### Verification matrix

Work through these scenarios against your local Development database.

For each scenario, compare the HTTP response and relevant persisted Event state with the expected result.


| Scenario | Prerequisites, inputs, and steps | Expected result and evidence |
| --- | --- | --- |
| Participant requests `/Events/Manage` | Sign in as Participant and request `/Events/Manage`. | Access denied or forbidden; no Event changes. |
| Organizer requests `/Events/Manage` | Sign in as Organizer and request `/Events/Manage`. | Management page renders. |
| Manage list | Create Events owned by two Organizers; view Manage as one of them. | Only the signed-in Organizer’s Events appear. |
| Missing required Create value | Submit Create with a blank required field. | Validation message and retained values; no row inserted. |
| End ≤ Start | Submit Create with End equal to or before Start. | Chronology error; no row inserted. |
| Capacity ≤ 0 | Submit zero or negative Capacity. | Capacity error; no row inserted. |
| Nonexistent Category ID | Change the submitted Category value to an absent identifier. | Category error; no row inserted. |
| Valid Create | Submit complete, valid data as Organizer. | Redirect to Manage; new Draft has current owner and server-assigned metadata. |
| Refresh after Create | Refresh the management page after the successful redirect. | The same created Event is present once. |
| Owned Draft Edit GET | Open Edit for an Event owned by the signed-in Organizer in Draft status. | The form displays the Event’s editable values. |
| Invalid owned Edit POST | Submit an invalid chronology while changing another field. | Redisplayed validation feedback; persisted Event is unchanged. |
| Valid owned Edit POST | Submit valid changes to an owned Draft. | Redirect to Manage; editable values update and protected properties remain unchanged. |
| Another owner’s Event ID | As one Organizer, request GET and POST Edit using another Organizer’s Event ID. | Resource access refused; foreign Event unchanged. |
| Published/Cancelled Edit ID | Request Edit GET and POST for an owned Published or Cancelled Event. | Current Draft-only eligibility refuses the request; Event unchanged. |
| Public catalog regression | Browse, search, filter, paginate, and open public Details after Organizer edits. | Existing discovery, page sizing, and navigation remain correct. |

<br>

### Browser and CSS verification

After checking the server and database results, inspect the rendered Organizer pages:

1. Open Manage, Create, and Edit with a narrow viewport; confirm that metadata, form fields, action buttons, and navigation fit without horizontal scrolling.
2. Navigate by keyboard using Tab and Shift+Tab. Confirm that links, inputs, the Category selector, and form buttons have visible focus indicators.
3. Check that every form control has a visible label and that server-side validation errors are shown near the relevant input and in the validation summary.
4. Submit an invalid request that reaches the server. Confirm that the form displays the errors and retains the other submitted values; then correct the form and submit again.
5. Check the browser Network panel for a successful `css/site.css` request. The Organizer Views should inherit the shared layout and styles without additional stylesheet or JavaScript dependencies.

Use browser checks to assess responsive layout, keyboard navigation, and accessible feedback. Use HTTP and database assertions to verify request behavior and persisted state.

<br>

### Verify server-assigned owner and status

After a successful Create, inspect the new Event through your database tooling or debugger.

The important values are:

```text
OrganizerId = current authenticated Organizer's ApplicationUser.Id
Status		= Draft
CreatedAt	= captured application-local time when Create succeeded
```

Those values must be true even though the Create form did not contain either field.

That is exactly the point.

Protected fields were assigned from server state.

<br>

### Verify failed create does not partially persist

Before submitting an invalid Create form, note the current Event count.

Submit invalid data.

Then inspect the database again.

Expected:

```text
Event count unchanged
```

The action returned before:

```csharp
_context.Events.Add(eventItem);
await _context.SaveChangesAsync();
```

That is the structural reason no partial Event is created.

<br>

### Verify failed edit does not partially persist

Choose an owned Draft Event.

Record several existing values, such as:

```text
Title
StartDateTime
EndDateTime
Capacity
```

Submit an Edit where:

- Title is changed to a valid new value;
- End time violates the timing rule.

Expected:

- validation fails;
- the form shows the submitted Title and timing values;
- the database still contains the original Title and original timing values.

Why?

The POST assigns editable entity properties after:

```csharp
if (!ModelState.IsValid)
{
	...
	return View(model);
}
```

The valid Title survives **in the form**, but not in persisted data until the whole request is valid.

<br>

### Demonstrate the changed-id ownership attack

The browser normally creates an Edit link only for Events in the current Organizer's management list.

Users can change the URL; authorization is enforced by the server-side resource lookup.

Use an Event whose `OrganizerId` is different from the currently signed-in Organizer.

For example, if your own Event is:

```text
/Events/Edit/12
```

and another owner's Event is ID `19`, request:

```text
/Events/Edit/19
```

Expected:

```text
404 / Not Found
```

because the server query requires:

```csharp
eventItem.Id == id &&
eventItem.OrganizerId == organizer.Id &&
eventItem.Status == EventStatus.Draft
```

Then attempt the same idea against the POST by changing the route ID before submission.

Expected:

- the inaccessible Event is not returned by the ownership query;
- no property assignment runs against that Event;
- `SaveChangesAsync()` does not modify that Event.

If your current Development seed has Events for only the preserved `seed-organizer-001` owner, use instructor-prepared alternate ownership test data for this scenario rather than weakening or changing the product authorization code.

<br>

### Regression check the public catalog

The existing `EventsController` now handles both public catalog and protected Organizer requests.

That makes regression checking especially important.

Verify:

```text
/Events
```

still:

- shows only Published/upcoming Events;
- preserves title/location search;
- preserves Category filtering;
- preserves filter values;
- preserves chronological ordering by start time and Event `Id`;
- shows the total matching count, defaults to ten per page, and lets visitors select **10, 25, or 50** Events per page;
- keeps `search`, `categoryId`, and `pageSize` in First, Previous, Next, and Last navigation links;
- clamps negative/zero/malformed `page` to the first page and an out-of-range positive page to the last available page;
- responds to a supplied unsupported or malformed `pageSize` with **HTTP 400** and the existing shared `BadRequest` View;
- returns to page 1 when new search or Category criteria are submitted;
- handles first, last, and zero-result pages with the established feedback and preserves page size when Clear removes the search/category filters;
- retains `Index(string? search, int? categoryId, int? page, int? pageSize)` with server-side Published/upcoming filtering, stable StartDateTime-then-ID ordering, and the existing controlled `Details` 404 behavior.

Use the Lesson 3 multi-page dataset to check a second catalog page, and verify an existing Event Details URL.

Preserve the existing public catalog routes and behavior alongside Organizer management.

<br>

### Regression check authentication

Verify that Lesson 4 behavior still works:

- Register remains available to anonymous visitors.
- Login still authenticates through Identity.
- Invalid Login remains generic.
- Logout remains POST-only.
- `/Participant` remains authentication-only.
- Organizer role labels still appear for the Organizer.
- The new Organizer Events link is visible only when appropriate.
- Direct requests remain protected even when a navigation link is absent.

<br>

## SRS Traceability Matrix Update with Evidence

Update the working traceability record using the approved SRS identifiers. For each row, record the implementation location, verification procedure, observed result, execution date, and requirement status.

| Requirement / Reference | Implementation / Artifact | Verification / Evidence | Status | Notes |
| --- | --- | --- | --- | --- |
| `UC-005`, `FR-EVT-001–003` | `EventsController.Create`, `EventInputModel`, `Create.cshtml` | Create POST/Manage GET; persisted owner, Draft status, and created timestamp | `PARTIAL` | Draft creation and server-assigned ownership implemented; additional UC-005 management behavior remains tracked |
| `FR-EVT-005`, `BR-AUT-002` | `Manage`, `Manage.cshtml` | Unauthorized requests denied; owned list excludes foreign Events | `IMPLEMENTED—UNVERIFIED` | Owner-scoped query and role restriction |
| `FR-EVT-004`, `BR-EVT-001`, `BR-EVT-003–004` | `EventInputModel`, Category lookup, Create/Edit POST | Rejected required/whitespace, chronology, capacity and Category inputs; unchanged Event rows | `PARTIAL` | Field, time, capacity, and Category validation implemented for Draft Create/Edit |
| `FR-EVT-006`, `BR-AUT-004` | Edit GET/POST with ownership and Draft predicates | Owned Draft edit persists approved fields; foreign/Published/Cancelled GET/POST leave rows unchanged | `PARTIAL` | Owned-Draft Edit implemented; the wider FR-EVT-006 lifecycle scope remains tracked |
| `NFR-SEC-003`, `NFR-REL-001–002` | Antiforgery, guarded assignments and save | Tokenless POST rejected; valid write persists; rejected mutations leave rows unchanged | `IMPLEMENTED—UNVERIFIED` | Cross-feature security and reliability remain continuing obligations |
| `NFR-USA-002`, `NFR-ACC-001–003`, `NFR-COM-002` | Organizer Razor views and stylesheet | Keyboard focus, responsive layouts, labels, error feedback, and retained input values | `IMPLEMENTED—UNVERIFIED` | Accessible form recovery |
| `FR-SRH-009`, `FR-ACC-001–007`, `AC-010` | Inherited catalog, Identity and cumulative test suite | Search/pagination and authentication regression outcomes | `PARTIAL` | Retained catalog and account contracts |

**Evidence context:** Run `dotnet build` and `dotnet test`, record the test results, and compare the manual inspector output with the expected scenarios. Both are necessary to evidence that requirements have been implemented and verified.

Confirm that your project builds, passes all tests, and runs without errors.

Stage and commit:

```cmd
git add .
git commit -m "Lesson 05: Implemented and Verified"
```

**NOTE:** Never stage or commit source code that does not build, fails a test, or runs with errors.

# Guided Exercises

Complete the exercises in order using the working Organizer Create/Edit checkpoint and the existing `CampusEvents.Tests` project. Use authorized Development data for interface work and GUID-named isolated SQL Server databases for automated mutations. Each exercise identifies its additional starting data and relevant SRS requirements.

<br>

## Exercise: extend the management list with location

**Purpose and expected outcome:** Transfer an established semantic rendering pattern to a new display requirement; practice a focused, compatible change (Objective 9).

**Prerequisites and SRS ties:** An Organizer account with at least one owned Draft Event; `FR-EVT-005`, `NFR-ACC-002–003`.

**Task:** Starting from the working Organizer `/Events/Manage` View, add `Location` as another `dt`/`dd` pair in the existing `.event-meta` definition list. Do not change the controller predicate, schema, or Event entity.

**Hints or checkpoints:** Follow the adjacent Category and Capacity entries; the Event already has `Location`.

**Deliverable:** A minimal `Manage.cshtml` diff and a brief comparison of the desktop and narrow-width rendering.

**Completion and verification:** Location appears for each owned Event, is associated with its definition-list label, and remains readable at desktop and narrow widths; `dotnet build` succeeds.

**Challenge (optional):** Explain why including the Location property in a View cannot replace the server-side owner filter.

<br>

## Exercise: add an independent invalid-input regression

**Purpose and expected outcome:** Design a new negative test that distinguishes submitted values, feedback, and stored state (Objectives 3–4 and 8).

**Prerequisites and SRS ties:** An authenticated Organizer, a valid Category, and an isolated SQL Server test database; `FR-EVT-004`, `AC-010`.

**Task:** Add an independent boundary test that submits an Event with `EndDateTime` exactly equal to `StartDateTime`. Assert the chronology validation message, preservation of valid companion inputs and Category options, and unchanged Event rows. Contrast this exact-boundary result with the guided test where the end precedes the start.

**Hints or checkpoints:** Start from valid form fields, set the end to the exact submitted start instant, and submit with an authenticated cookie and an antiforgery token obtained from the Create GET. Compare the persisted rows before and after.

**Deliverable:** One new boundary-test method with response, validation-feedback, and database assertions.

**Completion and verification:** The exact-boundary submission redisplays the form with chronology feedback, retains the other inputs, and leaves the Event dataset unchanged.

**Challenge (optional):** Explain how browser `required` feedback and server-side `[Required]` validation cooperate to enforce the input contract.

<br>

## Exercise: attempt ownership and protected-field tampering

**Purpose and expected outcome:** Analyze how two independent trust boundaries resist a crafted request (Objectives 2, 5–6, and 8).

**Prerequisites and SRS ties:** Two Organizer accounts, each owning a separate Draft Event in an isolated test database; `BR-AUT-002`, `FR-EVT-006`.

**Task:** With two SQL-backed Organizer accounts and distinct owned Draft Events in the isolated test host, submit a valid-looking Edit request using the other Organizer's route ID. Separately submit unexpected `OrganizerId` and `Status` fields to Create.

**Hints or checkpoints:** Check role membership separately from the owner predicate. Inspect status and redirect location before requerying both existing Events and the new record.

**Deliverable:** HTTP assertions, before/after database evidence, and a short threat-boundary diagram.

**Completion and verification:** Foreign-owned data remains unchanged; any successful Create uses the signed-in organizer and Draft status regardless of posted extras.

**Challenge (optional):** Evaluate whether a hidden Edit link adds security beyond discoverability.

<br>

## Exercise: evaluate the form-validation design

**Purpose and expected outcome:** Compare validation placement and produce a maintainable design decision (Objectives 1, 3, 7, and 9).

**Prerequisites and SRS ties:** Completed Create and Edit POST actions and their input-validation rules; `FR-EVT-004`, `FR-EVT-006`.

**Task:** Draw the Create and Edit POST decision paths from binding through database write. Identify where each of these belongs: required field, chronology, Category existence, authenticated owner, Draft eligibility, foreign-key integrity, and antiforgery. Propose one safe refactoring that reduces duplication while preserving Create/Edit behavior.

**Hints or checkpoints:** Assign each concern to its enforcement boundary: database constraints preserve stored integrity, form validation provides actionable feedback, antiforgery validates the request token, and authorization checks the current principal and resource. Check `ModelState` before assigning editable values.

**Deliverable:** Annotated decision diagram, brief trade-off analysis, and a targeted regression checklist.

**Completion and verification:** Every decision has an explicit trust boundary and failure outcome, and the proposed change preserves compatibility with prior lessons.

**Challenge (optional):** Describe the additional persisted information needed to enforce a Published-Event capacity floor based on active registrations.


# Troubleshooting Common Problems

| Symptom | Likely cause / diagnosis | Correction / verification |
| --- | --- | --- |
| Public Events suddenly require Organizer role | Controller-level `[Authorize(Roles = ...)]` unintentionally covers public actions. | Keep role authorization on management actions only; recheck anonymous catalog. |
| Form binds owner or status from browser | Persisted `Event` used as input contract, allowing overposting. | Bind `EventInputModel`; map allowlisted fields explicitly. |
| Hidden owner/status fields can be changed | Hidden inputs are still untrusted client fields. | Remove protected inputs and set owner/Status on server; try tampering test. |
| State-changing POST lacks antiforgery | Missing token attribute or form antiforgery markup. | Restore token generation/validation; verify tokenless POST fails. |
| Category dropdown disappears after invalid POST | Options are not repopulated before redisplaying form. | Call `PopulateCategoryOptionsAsync(model)` before `View(model)`. |
| Nonexistent positive Category ID is accepted | Numeric range check is mistaken for database existence. | Use `AnyAsync` and an invalid Category-ID request to verify rejection. |
| Foreign-owned Event is changed | Mutation occurs before server rechecks owner and eligibility. | Requery by authenticated owner and Draft state before writing; verify unchanged DB. |
| Direct Edit URL bypasses button visibility | UI-only Edit hiding is mistaken for access control. | Enforce ownership and Draft eligibility in GET and POST and test direct requests. |
| Rejected request partially persists fields | SaveChanges runs before validation and authorization finish. | Perform all checks before mutation/save; compare before/after Event fields. |
| Failed form loses attempted input | POST redisplays a fresh empty model. | Return submitted input model with ModelState errors and rebuilt options. |
| Unexpected migration for `EventInputModel` | Transient request model has been treated as an EF entity. | Remove accidental DbSet/entity registration; inspect generated migration before applying. |
| Unexpected lifecycle commands appear in the Organizer workflow | The controller exposes operations beyond the current Draft Create/Edit contract. | Restrict the implemented Organizer actions to the specified Create/Edit workflow and keep existing public actions accessible. |

Use the following focused walkthroughs to diagnose symptoms that need code or environment-specific inspection.

<br>

## Diagnostic walkthroughs


<br>

### Putting Organizer authorization on the whole Controller

If you add:

```csharp
[Authorize(Roles = RoleNames.Organizer)]
public class EventsController : Controller
```

you would accidentally protect established public Event browsing and Details.

Protect only the new Organizer actions.

<br>

### Binding the persisted `Event` entity directly

Avoid:

```csharp
public async Task<IActionResult> Create(Event eventItem)
```

for the Organizer Create form.

That makes it too easy for properties outside the intended form contract to become bindable.

Use:

```csharp
EventInputModel
```

and map permitted values deliberately.

<br>

### Adding hidden `OrganizerId` or `Status` inputs

Do not add:

```html
<input type="hidden" name="OrganizerId" ...>
<input type="hidden" name="Status" ...>
```

Those values would still come from the browser.

Assign them on the server.

<br>

### Forgetting `[ValidateAntiForgeryToken]`

Create and Edit POSTs change application state.

Keep:

```csharp
[ValidateAntiForgeryToken]
```

and:

```html
asp-antiforgery="true"
```

<br>

### Forgetting to repopulate categories

If a failed form renders with an empty Category list, check for:

```csharp
await PopulateCategoryOptionsAsync(model);
```

before:

```csharp
return View(model);
```

<br>

### Checking only `CategoryId > 0`

Check Category existence using the trusted Categories table.

Keep the database-backed `AnyAsync(...)` check.

<br>

### Mutating before ownership is proven

Re-query the resource using the authenticated Organizer and Draft status before applying editable values.

<br>

### Using a hidden edit button as authorization

The management View may hide Edit for non-Draft Events.

The Edit action must still check:

```text
current owner
+
Draft status
```

because direct requests bypass UI visibility.

<br>

### Calling `SaveChangesAsync()` before validation is complete

The correct order is:

```text
validation
	↓
authorization/resource eligibility
	↓
mutation
	↓
SaveChangesAsync
```

Complete validation and authorization before mutating and saving tracked fields.

<br>

### Forgetting that ModelState preserves attempted values

When a POST fails, retain the submitted input model and its validation state.

Return the submitted model after rebuilding only server-generated supporting state such as Category options.

<br>

### Adding a migration for `EventInputModel`

Keep `EventInputModel` in `ViewModels` as a request-specific contract over the existing Event schema. Create migrations when the persisted entity model or database schema changes.

<br>

### Keeping the Organizer management interface aligned with the implemented workflow

Render the owned-Event list and the Draft Edit action using the existing status and ownership fields. Provide Create and Edit links for the operations implemented here. The management page uses the current Event status to determine the available editing action.


# Checking Your Understanding

Explain your reasoning before revealing each answer.

<details><summary>Why does the Organizer Create form use `EventInputModel` instead of accepting a complete `Event` entity?</summary>

`EventInputModel` exposes only the fields the Organizer is allowed to submit. Protected Event state such as `OrganizerId` and `Status` is not bindable from the form, reducing over-posting risk and keeping those values under server control.

</details>

<details><summary>Why must the Create POST repopulate `CategoryOptions` before returning a failed form?</summary>

The browser submits the selected `CategoryId`, not the trusted server-side list of Category options. When validation fails and the View is returned, the controller must rebuild that option list so Razor can render the Category select again.

</details>

<details><summary>What is the difference between the Organizer role check and the Event ownership check in Edit?</summary>

The Organizer role check determines whether the user may enter Organizer functionality at all. The ownership check determines whether that Organizer may manage the specific requested Event. Both are server-side authorization decisions.

</details>

<details><summary>Why does changing `/Events/Edit/12` to another Event ID not grant access to that Event?</summary>

The server validates the route ID through a scoped query. It re-queries for an Event that matches the requested ID, the current Organizer's ID, and the allowed Draft status. A different owner's Event therefore does not satisfy the query.

</details>

<details><summary>Why are `OrganizerId` and `Status` assigned after validation in the Create POST instead of being hidden form fields?</summary>

Hidden form fields are still user-controlled request input. The server already knows the authenticated Organizer and the required initial status, so it assigns `OrganizerId` from the current `ApplicationUser` and forces `Status` to `EventStatus.Draft`.

</details>

<details><summary>Why does an invalid Edit POST leave the persisted Event unchanged even if some submitted fields were valid?</summary>

The controller copies submitted values onto the tracked Event after `ModelState` passes validation. On validation failure it returns the input model to the View before the mutation block and before `SaveChangesAsync()`.

</details>

<details><summary>Why is the Edit link shown only for Draft Events if the Edit action already checks Draft status on the server?</summary>

The conditional link gives the user an interface that advertises only currently available actions. The server check is still required because users can bypass the rendered UI and send direct requests.

</details>

<details><summary>Which operations belong to the Draft Create/Edit workflow?</summary>

Publish, Cancel, and Delete require guarded lifecycle transitions. Here, apply ownership and Draft eligibility to Create and Edit.

</details>

<details><summary>Why must a protected Edit POST reload the Event after an authorized Edit GET has already succeeded?</summary>

The Event may have changed since the GET, or the POST may target a different ID. Reloading and checking current ownership and eligible status at mutation time prevents trusting stale pages and client-provided identifiers.

</details>

<details><summary>How can you verify that a rejected POST preserved the stored Event?</summary>

Issue an invalid POST, inspect the rejection response, and compare the affected database rows before and after the request. 

</details>


# Summary

You **analyzed multi-request form lifecycles** by separating retrieval, untrusted submission, server decisions, persistence, and redirection. You **designed purpose-specific input contracts** and explained how their restricted fields reduce overposting while protected values remain server-controlled. You **compared complementary validation layers**, locating local field rules, cross-field invariants, current persisted references, and relational constraints at their appropriate boundaries.

You **evaluated rejection paths** that retain attempted input and explanatory feedback without applying partial changes. You **distinguished access levels**—authenticated identity, role permission, ownership, and resource eligibility—and explained why current state must be reloaded immediately before a mutation. You **applied request-forgery and redirect-after-write patterns** without confusing either with authorization. You **designed and interpreted repeatable HTTP/database tests** for success, denial, boundary, and regression cases, while recognizing the limits of automated versus direct interface observation. Finally, you **practiced incremental engineering discipline** through focused changes, accessible presentation checks, traceable evidence, and reviewed repository history. These patterns transfer to other application stacks and persistence technologies.


# Checkpoint

**Inherited baseline:** CEMS is a .NET 10 ASP.NET Core MVC/Razor application with SQL Server-backed `Event`, `Category`, and Identity user records; a seeded Organizer owner and foreign-key relationships; public Published/upcoming Events, Details, title/location search, Category filtering, and stable 10/25/50 pagination; authenticated account flows; the `Organizer` and `Administrator` roles; `/Participant`; project-owned CSS; and an existing isolated SQL Server regression-test project. The existing database relationships and Organizer role provide the foundation for the new management endpoints.

**Organizer workflow additions:**

- Organizer-only `GET /Events/Manage` lists the signed-in Organizer's Events across Draft, Published, and Cancelled statuses, with a conditional Draft Edit link.
- Organizer-only `GET/POST /Events/Create` provides a purpose-specific, validated form, server-selected Category options, server-assigned `OrganizerId`, forced `EventStatus.Draft`, application-clock `CreatedAt`, persistence, and redirect to Manage.
- Organizer-only `GET/POST /Events/Edit/{id}` loads an eligible owned Draft, verifies ownership and Draft eligibility again on POST, validates permitted fields, and persists only authorized changes.
- The `EventInputModel` defines the editable request contract independently of the persisted `Event`. The new Views share the existing Razor layout, accessible controls, and project-owned CSS. Authenticated navigation adds an Organizer Events link.
- Source for Organizer HTTP/persistence regression cases is added to the existing `CampusEvents.Tests` project; the existing public controller test constructor is adapted for the additional controller dependency where required.
- **Files introduced:** `CampusEvents/ViewModels/EventInputModel.cs`, `CampusEvents/Views/Events/Manage.cshtml`, `CampusEvents/Views/Events/Create.cshtml`, `CampusEvents/Views/Events/Edit.cshtml`, and `CampusEvents.Tests/OrganizerEventHttpTests.cs`. **Files extended:** `CampusEvents/Controllers/EventsController.cs`, `CampusEvents/Views/Shared/_Layout.cshtml`, `CampusEvents/wwwroot/css/site.css`, and the existing catalog controller tests as needed. These changes use the existing Event/Identity schema and application projects.

**Resulting application checkpoint:** The Organizer workflow creates an owned Draft, lists owned Events across statuses, and edits owned Draft Events. Edit rejects Published and Cancelled Events and resources owned by another Organizer.


# Next Lesson Preview

Lesson 6 adds **Event Lifecycle, Business Rules, and Protected Mutations** to the owned Event workflow. You will implement controlled Draft-to-Published and Published-to-Cancelled transitions, Draft deletion with confirmation, and lifecycle-aware Edit eligibility while reusing the role, ownership, validation, antiforgery, and persistence boundaries established here.

# Appendix

## Input models, domain models, and mapping strategies

The Organizer forms accept browser-editable properties through `EventInputModel`. The controller maps authorized, validated values to `Event`. This division separates the **request contract** from the **persistence model**. Both can evolve independently: adding a server-generated audit field to `Event`, for example, should not silently add a field that the browser can control.

| Approach | Benefit | Risk or cost | Applicability to Organizer forms |
| --- | --- | --- | --- |
| Bind the persistence entity directly | Few types and assignments | Makes persisted properties easier to expose, and couples requests to schema changes | Exposes broader input surface |
| One allowlisted input model for Create and Edit | Explicit editable surface, less duplicated form logic | Shared constraints may become awkward if the operations diverge | Selected for Create and Draft Edit |
| Separate Create and Edit input models | Clear operation-specific validation and allowed fields | More classes and mapping code | Useful when operations acquire distinct requirements |
| Explicit manual mapping | Property assignments are visible and reviewable | Repetition with many fields | Appropriate for small protected mutations |
| Mapping library | Standardizes repetitive mappings | Configuration can conceal unintended property transfer | Requires explicit allowlist review |

A **view model** describes data a particular view needs; an **input model** identifies values accepted from a request; a **domain entity** represents persisted application state; a **DTO** transfers selected data between boundaries. A class can play more than one role, but naming and responsibility remain useful design distinctions. The management page may render an `Event` query, while a mutation benefits from a narrower input contract.

Consider an Edit operation with different editable fields for Draft and Published Events. A separate Edit contract could make the permitted surface self-documenting. Choose operation-specific types when distinct input contracts improve clarity and protect permitted fields.

## Validation architecture and invariant placement

The Create workflow combines property checks, chronology, an existing Category lookup, and database integrity. These checks answer different questions even when they act on the same request. **Input validation** determines whether submitted values form a meaningful proposal. **Business validation** determines whether the operation is permitted under current application rules. **Database constraints** protect persisted facts even if another code path bypasses application checks.

Declarative attributes are concise for stable local rules such as required data and ranges. Cross-field validation can be expressed by an object-level validator. Database-backed rules need data access and an explicit decision about how failure is returned to the user. A dedicated validation service becomes useful when several controllers or operations must enforce the same rule and the rule is difficult to keep consistent in each caller; it also introduces a boundary and dependencies that must be tested.

| Rule | Application layer | Persistence safeguard |
| --- | --- | --- |
| Title is present | Field-level model validation with useful feedback | Column nullability may reject null, but not every empty/whitespace policy |
| End follows start | Cross-field validation | An additional check constraint is possible if required |
| Category exists | Query current Categories and add a field error | Foreign key rejects a nonexistent reference |
| Authenticated Organizer owns Event | Authorized, scoped resource lookup | A foreign key does not itself enforce current-user ownership |
| Event is Draft for this Edit | Current-state operation predicate | A status column alone does not enforce a caller's workflow permission |

Express each rule at the layers responsible for its user feedback and persisted integrity. A database foreign key protects consistency; an application error makes recovery understandable. When extending a design, assign each invariant an owner and define the result of failure. Compare both paths in tests rather than assuming validation attributes alone cover every write.

## Concurrent updates, optimistic concurrency, and transaction boundaries

The Edit POST re-queries an owned Draft and checks the resource's current status to authorize the operation at retrieval time. Concurrent writers can still act between retrieval and persistence. Consider two valid Organizer requests editing the same Draft:

```text
Request A reads Draft, Title = "Original"
Request B reads Draft, Title = "Original"
Request A changes Title to "Updated A" and saves
Request B changes Title to "Updated B" and saves
Final stored title may be "Updated B": A's change is lost
```

A **lost update** occurs when one writer overwrites another writer's work without detecting the conflicting change. In another race, a resource's status could change after one request checks Draft eligibility and before that request commits. This is a **time-of-check/time-of-use** risk. Re-querying at the start narrows stale assumptions but does not remove the gap.

**Optimistic concurrency** associates the stored record with a version token, such as a SQL Server `rowversion`. An update includes the version originally read; if no row matches, the application detects a conflict and can reload or ask the user to review changes. This is different from an input-validation error: both requests might contain valid fields but disagree about which persisted version is being edited.

**Transactions** group related database operations into a commit or rollback boundary. Atomicity prevents a group of changes from persisting only in part, while isolation controls interactions with concurrent transactions. Isolation level and locking decisions affect contention and anomalies; a transaction alone does not imply that every business rule is protected from every race. Constraints, concurrency tokens, and carefully selected transaction boundaries are complementary tools rather than synonyms.

The current workflow applies guarded single-resource updates. For a larger booking or capacity workflow, identify the invariants that require concurrent enforcement, then choose the database or application mechanism that can actually enforce them. Exercise concurrent invariants with overlapping requests that expose conflicting updates.

## Resource-based authorization and access-control design

The Organizer role gate determines whether a signed-in user may access the Organizer workflow. The Edit query then scopes the resource to the current Organizer and eligible Draft status. These are separate forms of access control: a **role** grants a general class of capability; a **resource decision** evaluates that capability against the individual record and requested operation.

For this small feature, an owner-and-status predicate in each read and write path is easy to inspect. As operations multiply, repeating predicates in many controllers creates a consistency risk. A policy or resource-authorization handler can centralize a named rule, provided it is evaluated against the actual current resource and the call site still enforces the outcome. Alternatively, a carefully designed application service can own the resource lookup and authorized mutation together. Additional indirection is useful when it reduces repeated policy logic rather than merely relocating code.

| Design | Advantage | Review concern |
| --- | --- | --- |
| Inline scoped query | Clear SQL filtering and one direct path to a protected record | Keep predicates consistent across GET, POST, and later commands |
| Resource authorization handler | Reusable explicit policy over a selected resource | Ensure the resource is loaded and the handler is invoked on each operation |
| Application service operation | Can couple authorization, domain rule, and mutation | Keep dependencies and failure contracts understandable |

For a foreign-owned Event, the controller returns `EventNotFound()` and keeps its details private. Applications sometimes choose a generic NotFound response to avoid revealing whether a protected ID exists; others distinguish forbidden access when existence is intentionally public. The important property is **consistent enforcement**, with no changes to unauthorized records. Link visibility and form controls remain presentation choices, never substitutes for server authority.
