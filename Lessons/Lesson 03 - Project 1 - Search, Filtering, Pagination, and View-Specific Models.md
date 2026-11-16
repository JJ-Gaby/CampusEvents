# **Lesson 3 — Search, Filtering, Pagination, and View-Specific Models**

Revision: 2026.11.09.1

# Table of Contents

- [Review of Last Lesson](#review-of-last-lesson)
- [Learning Objectives](#learning-objectives)
- [SRS Requirements and Use Cases](#srs-requirements-and-use-cases)
- [Guided Feature Preview](#guided-feature-preview)
- [Guided Design and Coding](#guided-design-and-coding)
	- [Start from the existing catalog and repository](#start-from-the-existing-catalog-and-repository)
	- [Checkpoint A — Add search and Category filtering before pagination](#checkpoint-a--add-search-and-category-filtering-before-pagination)
		- [Bind search and Category criteria from GET](#bind-search-and-category-criteria-from-get)
		- [Build the initial results model](#build-the-initial-results-model)
		- [Assemble the initial filtered response](#assemble-the-initial-filtered-response)
		- [Create the first GET form and preserve criteria](#create-the-first-get-form-and-preserve-criteria)
		- [Style the initial form in the established CSS file](#style-the-initial-form-in-the-established-css-file)
		- [Render Events and make no matches understandable](#render-events-and-make-no-matches-understandable)
		- [Check the first vertical slice before adding pages](#check-the-first-vertical-slice-before-adding-pages)
	- [Checkpoint B — Implement fixed ten-Event pagination](#checkpoint-b--implement-fixed-ten-event-pagination)
		- [Add page metadata to the response model](#add-page-metadata-to-the-response-model)
		- [Normalize page, count matches, and retrieve ten rows](#normalize-page-count-matches-and-retrieve-ten-rows)
		- [Add Previous and Next links without losing filters](#add-previous-and-next-links-without-losing-filters)
		- [Style fixed-page navigation](#style-fixed-page-navigation)
		- [Prepare a private Development dataset for fixed-page verification](#prepare-a-private-development-dataset-for-fixed-page-verification)
		- [Prove fixed pagination before making it configurable](#prove-fixed-pagination-before-making-it-configurable)
	- [Independent challenge — Selectable page sizes, First/Last, and result ranges](#independent-challenge--selectable-page-sizes-firstlast-and-result-ranges)
		- [Attempt the challenge before reading the solution](#attempt-the-challenge-before-reading-the-solution)
		- [Acceptance criteria](#acceptance-criteria)
		- [Extend the challenge — Observe HTTP 400 before improving its presentation](#extend-the-challenge--observe-http-400-before-improving-its-presentation)
	- [Worked implementation — compare only after attempting the challenge](#worked-implementation--compare-only-after-attempting-the-challenge)
		- [Final EventSearchViewModel: add the allowed choices](#final-eventsearchviewmodel-add-the-allowed-choices)
		- [Build a reusable HTML 400 response after observing the bare 400](#build-a-reusable-html-400-response-after-observing-the-bare-400)
		- [Final EventsController: valid size, bounded search, and unchanged Details](#final-eventscontroller-valid-size-bounded-search-and-unchanged-details)
		- [Final Index.cshtml: form, range, links, and empty state](#final-indexcshtml-form-range-links-and-empty-state)
		- [Final site.css: replace provisional rules with the final layout](#final-sitecss-replace-provisional-rules-with-the-final-layout)
	- [Trace the completed request and review the change](#trace-the-completed-request-and-review-the-change)
- [Guided Discussion](#guided-discussion)
	- [Read-only retrieval contracts and application state](#read-only-retrieval-contracts-and-application-state)
	- [Eligibility invariants and composable predicates](#eligibility-invariants-and-composable-predicates)
	- [Query composition, execution, and bounded retrieval](#query-composition-execution-and-bounded-retrieval)
	- [Counting, deterministic ordering, and page boundaries](#counting-deterministic-ordering-and-page-boundaries)
	- [Input validation, normalization, and HTTP semantics](#input-validation-normalization-and-http-semantics)
	- [View-specific models, preserved state, and accessible interaction](#view-specific-models-preserved-state-and-accessible-interaction)
- [Guided Verification and Validation](#guided-verification-and-validation)
	- [Automated](#automated)
		- [Preserve the isolated SQL server fixture and fixed clock](#preserve-the-isolated-sql-server-fixture-and-fixed-clock)
		- [Check malformed retrieval input through real HTTP requests](#check-malformed-retrieval-input-through-real-http-requests)
		- [Execute and interpret the regression suite](#execute-and-interpret-the-regression-suite)
	- [Manual](#manual)
		- [Verify the fresh five-event baseline](#verify-the-fresh-five-event-baseline)
		- [Use the private Development pagination dataset](#use-the-private-development-pagination-dataset)
		- [Check navigation, request boundaries, and rendering](#check-navigation-request-boundaries-and-rendering)
		- [Remove temporary practice records safely](#remove-temporary-practice-records-safely)
	- [SRS Traceability Matrix Update with Evidence](#srs-traceability-matrix-update-with-evidence)
- [Guided Exercises](#guided-exercises)
	- [Exercise: diagram the retrieval contract](#exercise-diagram-the-retrieval-contract)
	- [Exercise: diagnose an unstable page boundary](#exercise-diagnose-an-unstable-page-boundary)
	- [Exercise: add an independent regression boundary](#exercise-add-an-independent-regression-boundary)
	- [Exercise: evaluate a pagination alternative](#exercise-evaluate-a-pagination-alternative)
- [Troubleshooting Common Problems](#troubleshooting-common-problems)
- [Checking Your Understanding](#checking-your-understanding)
- [Summary](#summary)
- [Checkpoint](#checkpoint)
- [Next Lesson Preview](#next-lesson-preview)
- [Appendix](#appendix)
	- [Pagination strategies and result consistency](#pagination-strategies-and-result-consistency)
		- [Offset pages and keyset continuation](#offset-pages-and-keyset-continuation)
		- [Ordering is not a snapshot](#ordering-is-not-a-snapshot)
	- [Database search and query-performance alternatives](#database-search-and-query-performance-alternatives)
		- [Matching rules and database collation](#matching-rules-and-database-collation)
		- [Returned rows versus total query work](#returned-rows-versus-total-query-work)
	- [Pagination interface and navigation alternatives](#pagination-interface-and-navigation-alternatives)
	- [Response-model and retrieval-architecture alternatives](#response-model-and-retrieval-architecture-alternatives)
		- [Entity data versus projected read models](#entity-data-versus-projected-read-models)
		- [Controller composition versus a query service](#controller-composition-versus-a-query-service)

# Review of Last Lesson

You built a relational public catalog by distinguishing persistent domain records from the queries and rendered pages that expose them. In Lesson 2, the same MVC request path began retrieving Events and their Category data from SQL Server, selecting only Published upcoming Events in deterministic chronological order. You also added a Details endpoint, an application-local time dependency, and an isolated SQL Server regression-testing foundation.

The existing `/Events` page currently loads every eligible Event. You will retain the established public-visibility rule while allowing visitors to express retrieval criteria, inspect bounded pages, and resume a search without losing their selections. The `Event`, `Category`, organizer reference, migrations, and Details behavior are the starting baseline, not new work for this lesson.

# Learning Objectives

By the end of the lesson, you should be able to:

1. **Analyze** a read-only request contract, distinguishing persisted data, transient input criteria, and presentation state.
2. **Design** composable retrieval rules that preserve an existing eligibility invariant while optional conditions narrow the result set.
3. **Evaluate** query execution boundaries and resource limits to avoid unnecessary transfer, processing, and memory consumption.
4. **Explain and apply** total-result counting, deterministic ordering, and bounded selection in a paginated retrieval design.
5. **Compare** page-navigation patterns and data-pagination strategies, selecting an approach appropriate to usability, scale, and consistency requirements.
6. **Design and validate** defaults, permitted values, normalization rules, and controlled error responses for untrusted retrieval parameters.
7. **Separate** domain, request, and presentation responsibilities through a purpose-specific response model and state-preserving navigation.
8. **Verify and validate** retrieval behavior with reproducible positive, negative, boundary, and interface-level evidence, identifying each test's limits.
9. **Apply** incremental change management, traceability, and version control to maintain a reviewable, compatible application state.

# SRS Requirements and Use Cases

Under `UC-001`, visitors and authenticated users search and filter the public Event catalog, traverse bounded result pages, and open Event Details. Public discovery uses the existing anonymous-access catalog.

| SRS reference | Required behavior / constraint | Lesson contribution |
| --- | --- | --- |
| `UC-001` | Browse and inspect eligible Events, search/filter, traverse bounded pages, and view Details | Extend the public Index flow while preserving links to Details |
| `FR-SRH-001`, `FR-SRH-002` | Browse Published upcoming Events in chronological order | Preserve the Lesson 2 eligibility and ordering predicates in every search or page |
| `FR-SRH-003`, `FR-SRH-004` | Open Published Event Details and display specified information | Keep the existing Details action and page unchanged |
| `FR-SRH-005` | Search Published upcoming Events by at least title or location | Add an optional text predicate on those two fields |
| `FR-SRH-006` | Filter Published upcoming Events by Category | Add an optional Category ID predicate |
| `FR-SRH-007` | Explain when no public Events match | Render a meaningful empty state |
| `FR-SRH-008` | Preserve visibility of previously Published Cancelled Events through direct Details links | Preserve the inherited Details behavior |
| `FR-SRH-009` | Provide paginated public results, deterministic ordering, retained filters, and usable boundaries | Count matches and retrieve bounded pages; add page state and navigation |
| SRS §11 | Normalize missing, malformed, nonpositive, and over-range page numbers; suppress links for zero matches | Encode that exact page-number policy; separately constrain selectable page size |
| `NFR-PER-002`, `NFR-ACC-001–003`, `NFR-COM-002` | Efficient server-side retrieval; labeled, keyboard-operable, responsive pages | Filter/order/page before materialization; inspect controls and links |
| `AC-001`, `AC-010` | Public discovery acceptance and critical-behavior automated tests | Gather catalog, integration, and browser evidence |

**Scope at this checkpoint:** The existing Details endpoint is retained, while registration availability and the direct-link policy for previously Published Cancelled Events remain incomplete. The catalog adds search, Category filtering, bounded pagination, and state-preserving navigation. Maintain the remaining product requirements in the working SRS traceability record.

**Lesson-level acceptance contract:** `/Events` returns only Published future-start Events. Title/location search and Category filters intersect. The server counts all matching Events, orders by start time and ID, and returns at most the allowed page size. A missing `pageSize` defaults to 10; explicit values other than 10, 25, or 50 return HTTP 400 with a useful HTML error page. Invalid page numbers normalize according to SRS §11. A new form submission starts at page 1. Pagination links retain search, Category, and page size. Zero matches yield a readable empty state without page links. Existing Details links and not-found handling remain available.

# Feature Preview

A Visitor opens `/Events` and sees the existing relational catalog, now with a **Search events** field, a Category selection, and a page-size control. Searching for part of an Event title or location narrows the Published upcoming Events; selecting a Category narrows the same set further. Submitting the GET form starts on page 1. The URL carries the chosen criteria, so the visitor can bookmark or share the same result selection.

With more matching Events than fit on one page, the catalog shows the matching total, a result range, and navigable First/Previous/Next/Last links where applicable. Selecting Next retains the search text, Category, and page size. Selecting Clear removes the search and Category criteria while keeping the selected page size. Each Event continues to link to the inherited Details page.

For an unmatched search, the page returns HTTP 200 with a useful no-results explanation and no page links. A malformed or nonpositive page number selects page 1; an above-range number selects the last available page. An explicitly unsupported `pageSize` returns HTTP 400 with an understandable HTML explanation. 

![Target public Events catalog displaying retained criteria, matching range, and navigation links.](images/lesson-03/catalog-search-filter-pagination.png)

The intended request path is:

```text
GET /Events?search=Library&categoryId=2&page=2&pageSize=10
    → validate and normalize untrusted retrieval parameters
    → apply Published/upcoming eligibility and optional filters
    → count matching Events; select one deterministically ordered page
    → render criteria, result metadata, Event links, and page navigation
```

# Design and Coding

## Inspect the inherited solution

Inspect your existing ***Campus Events*** solution before making changes.

Make sure that you staged and commited the previous lesson:

```cmd
git show -s HEAD
```

**Expected output:** should contain "Lesson 02: Implemented and Verified".

Otherwise, build, test, run, stage, and commit:

```cmd
dotnet build
dotnet test
dotnet run --project src/CampusEvents
git add .
git commit -m "Lesson 02: Implemented and Verified"
```

Then, create a new branch:

```cmd
git switch -c lesson-03
```

## Checkpoint A — Add search and Category filtering before pagination

Begin with the existing public Event retrieval. Preserve the Published/upcoming base condition, append optional criteria, and render the matching results. The next checkpoint adds a database-side page limit.

### Bind search and Category criteria from GET

In `CampusEvents/Controllers/EventsController.cs`, first evolve `Index()` to accept nullable `search` and `categoryId` inputs. Add `page` only at the fixed-pagination checkpoint, and introduce `pageSize` in the independent challenge. Begin with GET binding: `/Events?search=Library&categoryId=3` binds the supplied text and Category identifier; omitted query values become null. A GET form uses controls with exactly those `name` attributes.

Start from the public EF Core query and append the optional conditions **before** materialization:

```csharp
IQueryable<Event> query = _context.Events
	.AsNoTracking()
	.Include(eventItem => eventItem.Category)
	.Where(eventItem => eventItem.Status == EventStatus.Published && eventItem.StartDateTime > now);

if (!string.IsNullOrWhiteSpace(search))
{
	var searchTerm = search.Trim();
	query = query.Where(eventItem => eventItem.Title.Contains(searchTerm) || eventItem.Location.Contains(searchTerm));
}

if (categoryId.HasValue)
{
	query = query.Where(eventItem => eventItem.CategoryId == categoryId.Value);
}
```

The `||` belongs **inside** the title/location text condition. The search and Category conditions are joined to the base query as additional `Where` clauses, producing an AND between constraints. `AsNoTracking()` avoids tracking objects in this read-only request; the `Include` supplies each result's Category when the Events are retrieved. Verify that changing search criteria never returns Draft, Cancelled, or past Events.

### Build the initial results model

The composable Event query needs a response model with more than the matching Events.

The page must also render:

```text
Category choices
current search text
current selected Category
```

Use a View Model to combine the matching Events with the search and Category selections required by this page.

A page-level response may combine domain records and request-specific presentation data. An `Event` represents one Event in the application domain. The Index page, however, represents an interaction: it needs many Events, the available Category choices, and the request values that produced the current result set. Those concerns belong to the page response, not to one Event row.

Create:

**File: `CampusEvents/ViewModels/EventSearchViewModel.cs`**

```csharp
using CampusEvents.Models;
using System;
using System.Collections.Generic;

namespace CampusEvents.ViewModels;

public class EventSearchViewModel
{
	public IReadOnlyList<Event> Events { get; set; } = Array.Empty<Event>();

	public IReadOnlyList<Category> Categories { get; set; } = Array.Empty<Category>();

	public string? Search { get; set; }

	public int? CategoryId { get; set; }
}
```

Each property has one page-level responsibility:

- `Events` — the filtered, chronologically ordered Event results;
- `Categories` — the choices for the Category filter;
- `Search` — the current search text;
- `CategoryId` — the currently selected Category ID.

The View Model assembles Event results, Category choices, and active request criteria for rendering one response. Keep persistent Event and Category properties in their domain models and prepare page-specific state in this response model.

Keep the responsibilities distinct:

```text
Event / Category
	→ domain and persistence data

EventSearchViewModel
	→ presentation data for one Index response
```

This separation prevents the persistence model from accumulating UI-only properties such as search text or selected dropdown values. It also gives the View a strongly typed object whose shape matches what that View actually needs.

At this point the application has the right query behavior and the right page-data shape, but they are still disconnected. The controller must now assemble the View Model from the current request and the database results.

Build the project:

```cmd
dotnet build CampusEvents\CampusEvents.csproj
```

The new class should compile. Connect this model to the controller and Razor View as you build the filtered response.

### Assemble the initial filtered response

The controller is the point where the request criteria, EF Core queries, and presentation model come together. It performs the data-access work and constructs the response model; Razor renders the supplied data and user-interface state.

The controller now needs to:

1. compose and materialize the filtered Event query;
2. load the available Categories;
3. place both result data and current filter state into the new View Model.

Open:

**File: `CampusEvents/Controllers/EventsController.cs`**

Add this namespace:

```csharp
using CampusEvents.ViewModels;
```

Then use this `Index()` implementation:

```csharp
public async Task<IActionResult> Index(string? search, int? categoryId)
{
	_logger.LogInformation("Loading Published upcoming CEMS events with public filters.");

	var now = _timeProvider.GetLocalNow().DateTime;

	var query = _context.Events
		.AsNoTracking()
		.Include(eventItem => eventItem.Category)
		.Where(eventItem => eventItem.Status == EventStatus.Published && eventItem.StartDateTime > now);

	if (!string.IsNullOrWhiteSpace(search))
	{
		var searchTerm = search.Trim();

		query = query.Where(eventItem => eventItem.Title.Contains(searchTerm) || eventItem.Location.Contains(searchTerm));
	}

	if (categoryId.HasValue)
	{
		query = query.Where(eventItem => eventItem.CategoryId == categoryId.Value);
	}

	var events = await query
		.OrderBy(eventItem => eventItem.StartDateTime)
		.ThenBy(eventItem => eventItem.Id)
		.ToListAsync();

	var categories = await _context.Categories
		.AsNoTracking()
		.OrderBy(category => category.Name)
		.ToListAsync();

	var viewModel = new EventSearchViewModel
	{
		Events = events, Categories = categories, Search = search, CategoryId = categoryId
	};

	return View(viewModel);
}
```

#### Why Category choices require a separate query

The Event query answers:

```text
Which public Events match the current criteria?
```

The Category query answers:

```text
Which Categories should the filter control offer?
```

Those are separate page-data needs.

The Category query is also read-only, so it uses `AsNoTracking()`. It orders Categories by name so the dropdown is predictable.

The controller then preserves the current request state:

```csharp
Search = search,
CategoryId = categoryId
```

That state will let Razor redisplay the values that produced the current result set. Follow the request/response round trip: query-string values entered by the visitor become action parameters, then View Model properties, then rendered form values in the response.

The controller now returns:

```csharp
return View(viewModel);
```

Update the Index View to accept `EventSearchViewModel` so its model declaration matches the revised controller response.

### Create the first GET form and preserve criteria

The server can now accept criteria, apply them in the database query, and return those criteria in the View Model. Add a GET form that lets visitors submit search and Category criteria as query-string parameters understood by the controller.

Open:

**File: `CampusEvents/Views/Events/Index.cshtml`**

The Lesson 2 model declaration was:

```cshtml
@model System.Collections.Generic.IReadOnlyList<CampusEvents.Models.Event>
```

Replace it with:

```cshtml
@model CampusEvents.ViewModels.EventSearchViewModel
```

Keep the existing page heading:

```cshtml
<h1>Upcoming Campus Events</h1>
```

Then add the search/filter form immediately below it:

```cshtml
<form asp-controller="Events"
		asp-action="Index"
		method="get"
		class="event-filter-form"
		aria-label="Search and filter campus events">

	<div class="event-filter-field">
		<label for="search" class="event-filter-label">Search title or location</label>
		<input id="search"
				name="search"
				type="search"
				class="event-filter-control"
				value="@Model.Search"
				aria-describedby="searchHelp" />
		<div id="searchHelp" class="event-filter-help">
			Matches Event title or location.
		</div>
	</div>

	<div class="event-filter-field">
		<label for="categoryId" class="event-filter-label">Category</label>
		<select id="categoryId" name="categoryId" class="event-filter-control">
			<option value="">All categories</option>

			@foreach (var category in Model.Categories)
			{
				if (Model.CategoryId == category.Id)
				{
					<option value="@category.Id" selected>@category.Name</option>
				}
				else
				{
					<option value="@category.Id">@category.Name</option>
				}
			}
		</select>
	</div>

	<div class="event-filter-actions">
		<button type="submit" class="event-filter-submit">
			Apply filters
		</button>

		<a asp-controller="Events" asp-action="Index" class="event-filter-clear">
			Clear
		</a>
	</div>
</form>
```

```cshtml
	@if (Model.Events.Count == 0)
	{
		<p>There are no upcoming published events to display.</p>
	}
	else
	{
		foreach (var eventItem in Model.Events)
		...
	}
```

#### Razor expressions and C# scope

In Razor, `@` tells the Razor engine to switch from HTML to C#. In your code, `category` is used in two different contexts:

1. Inside a C# code block, no `@` is needed.

```cshtml
@foreach (var category in Model.Categories)
{
    if (Model.CategoryId == category.Id)
    {
        // Already inside C#
    }
}
```

The `@foreach` switches Razor into C# mode. Inside its braces, `category.Id` is already interpreted as C#.

2. Inside HTML, `@` is required to output a C# value.

```cshtml
<option value="@category.Id">@category.Name</option>
```

Here, Razor is processing HTML, so you need `@` to tell it to evaluate `category.Id` and `category.Name` as C# expressions.

The `<option>` element is physically inside the foreach C# block, but Razor automatically switches back to HTML mode when it encounters an HTML element.

The resulting HTML might look like this:

```html
<option value="5">Music</option>
```

The general rule: Use `@` when transitioning from HTML to C#. Once you're already inside C# code, you don't need it again unless you've transitioned back into HTML.

#### Why form field names matter

The form controls use:

```text
name="search"
name="categoryId"
```

Those names match the controller parameters:

```csharp
Index(string? search, int? categoryId)
```

When the browser submits the GET form, the resulting URL can contain values such as:

```text
/Events?search=Workshop&categoryId=1
```

MVC can then bind those named query-string values back to the action parameters.

#### Why filter state remains visible

The text box uses:

```cshtml
value="@Model.Search"
```

The controller stored the submitted text in `Model.Search`, so Razor puts that text back into the control.

For the Category dropdown, Razor compares:

```cshtml
Model.CategoryId == category.Id
```

and renders `selected` on the matching option.

The round trip is:

```text
query string
	↓
Index parameters
	↓
EventSearchViewModel
	↓
rendered form values
```

The page therefore shows the visitor which criteria produced the current results.

### Style the initial form in the established CSS file

#### Add the local styles

Use the existing project stylesheet under `wwwroot`.

Open:

**File: `CampusEvents/wwwroot/css/site.css`**

Append the following rules after the Event catalog and Details styles. The `.event-card`, `.event-card-title`, and `.cems-visually-hidden` classes already belong to the stylesheet and continue to style the results below this form.

```css
.event-filter-form {
	display: grid;
	grid-template-columns:
		minmax(15rem, 2fr)
		minmax(12rem, 1fr)
		minmax(14rem, auto);
	gap: 1rem;
	margin-bottom: 1.5rem;
	align-items: start;
}

.event-filter-field {
	min-width: 0;
}

.event-filter-label {
	display: block;
	margin-bottom: 0.35rem;
	font-weight: 600;
	line-height: 1.5;
}

.event-filter-control {
	width: 100%;
	min-height: 2.5rem;
	padding: 0.55rem 0.65rem;
	border: 1px solid #8c959f;
	border-radius: 0.25rem;
	background: #ffffff;
	color: inherit;
	font: inherit;
}

.event-filter-help {
	margin-top: 0.35rem;
	font-size: 0.875rem;
}

.event-filter-actions {
	display: flex;
	gap: 0.5rem;
	padding-top: calc(1.5em + 0.35rem);
}

.event-filter-submit,
.event-filter-clear {
	white-space: nowrap;
	flex: 1;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	min-height: 2.5rem;
	padding: 0.55rem 0.75rem;
	border: 1px solid #8c959f;
	border-radius: 0.25rem;
	font: inherit;
	text-decoration: none;
	cursor: pointer;
}

.event-filter-submit {
	background: #174ea6;
	border-color: #174ea6;
	color: #ffffff;
}

.event-filter-clear {
	background: #ffffff;
	color: #174ea6;
}

.event-filter-submit:hover {
	background: #123d84;
}

.event-filter-clear:hover {
	background: #f6f8fa;
}

.event-result-count {
	margin: 0 0 1rem;
}

@media (max-width: 48rem) {
	.event-filter-form {
		grid-template-columns: 1fr;
	}

	.event-filter-actions {
		padding-top: 0;
	}
}
```

The search field, Category selector, and action buttons share a three-column grid on wider screens. The `min-width: 0` rule lets grid items shrink without forcing the page wider than the viewport. Each form control inherits the site's text styles, and buttons and links use the same CEMS colors as the shared navigation. The media query stacks the form groups on narrower screens; the shared keyboard-focus rule from Lesson 1 remains in effect.

#### Why wwwroot/css/site.css?

The shared layout you built in Lesson 1 contains this stylesheet reference:

```cshtml
<link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
```

The `~` identifies the application's web root in the URL. ASP.NET Core's Tag Helper resolves the reference to the stylesheet under `wwwroot/css`, and `asp-append-version` adds a content-based version query parameter so browsers can request updated CSS when the file changes. Static file middleware serves the stylesheet to the browser.

The catalog styles from Lesson 2 and these new filter styles live in the same `site.css` file. Razor applies class names such as `event-filter-form` and `event-filter-control` to elements; the CSS rules determine how those elements appear. No View needs its own stylesheet link or client-side script for these GET controls.

#### Accessible and responsive form behavior

The form uses visible `<label>` elements for both controls. The search input also uses `aria-describedby="searchHelp"` to connect its short explanation to the control.

The `.event-filter-form` rule uses CSS Grid for the wider layout. At widths of 48rem or less, the grid switches to one column so the fields and actions stack without horizontal scrolling. Keyboard users can reach the controls in source order, and the shared `:focus-visible` rule identifies the focused link, button, or input. Resize the browser and use Tab to confirm both behaviors.

### Render Events and make no matches understandable

The form can now produce a filtered GET request and reconstruct the submitted criteria from the View Model. The remaining View responsibility is to render the result set itself.

Because the Index View now receives the page-specific View Model, the Event collection is:

```csharp
Model.Events
```

Change the old loop:

```cshtml
@foreach (var eventItem in Model)
```

to:

```cshtml
@foreach (var eventItem in Model.Events)
```

Keep the existing Lesson 2 result markup and Details link.

Then add an explicit no-results branch.

**File: `CampusEvents/Views/Events/Index.cshtml`**

```cshtml
@if (Model.Events.Count == 0)
{
	<p role="status">
		No published upcoming events match your current criteria.
	</p>
}
else
{
	<section aria-labelledby="event-list-heading">
		<h2 id="event-list-heading" class="cems-visually-hidden">
			Published upcoming events
		</h2>

		@foreach (var eventItem in Model.Events)
		{
			<article class="event-card">
				<h3 class="event-card-title">
					<a asp-controller="Events" asp-action="Details" asp-route-id="@eventItem.Id">
						@eventItem.Title
					</a>
				</h3>

				<p><strong>Category:</strong> @eventItem.Category.Name</p>
				<p><strong>Location:</strong> @eventItem.Location</p>
				<p>
					<strong>Starts:</strong>
					<time>@eventItem.StartDateTime.ToString("f")</time>
				</p>
			</article>
		}
	</section>
}
```

A query that returns zero rows can still be completely successful. The request was valid, the database query ran correctly, the controller handled the empty collection correctly, and Razor returned valid HTML. "No matches" is therefore a **domain result**, not an infrastructure failure. The page should communicate that result to the visitor rather than treating it as an exception. The search/filter form stays visible above this branch, so the visitor can immediately change the criteria.

Build and run:

```cmd
dotnet build CampusEvents\CampusEvents.csproj
dotnet run --project CampusEvents\CampusEvents.csproj
```

Open:

```text
/Events
```

With the Lesson 2 seed data, you should still see:

1. Library Research Night
2. Career Networking Lunch

The search/filter form should appear above the results.

### Check the first vertical slice before adding pages

With an approved, freshly seeded Development database, `/Events` should display exactly the **two** Published/upcoming seed Events, `Library Research Night` and `Career Networking Lunch`; the three other seed records are Draft, Cancelled, or in the past. Search for `Library`, search for `Student Center`, and select Categories using IDs read from the **actual** database. Search and Category should intersect, and a valid Category with no matching public Events should leave the form visible with HTTP 200 and the no-results text. Manually inspect the HTML and the GET URL; a direct controller invocation cannot prove Razor rendering or actual MVC binding. The earlier Lesson 2 test that expects `List<Event>` must be adapted to the View Model by the final verification stage.

## Checkpoint B — Implement fixed ten-Event pagination

Now that the filtered catalog works, it needs a bounded retrieval contract. **Temporarily keep the page size fixed at 10**. This separates the pagination mechanism from the later decision to let visitors select a size. The existing `EventSearchViewModel` will evolve, but its `Events` property will still represent Event records, this time only those on the current page.

Before coding, distinguish the decisions:

| Question | Fixed-pagination decision |
| --- | --- |
| How do visitors move? | Previous and Next links on separately rendered HTML pages |
| Which data does the database return? | The next ten ordered matches using `Skip`/`Take` |
| What does `CountAsync()` measure? | All eligible matches after both filters, before `Skip`/`Take` |
| What happens if a page is invalid? | Normalize missing/malformed/nonpositive to 1 and above-range to the filtered last page |

For 27 matching Events, page 2 skips 10 rows and selects rows 11–20. These are **illustrative numbers**, not the two-Event seed dataset. Stable ordering requires `OrderBy(StartDateTime).ThenBy(Id)`; it does not promise a snapshot when other requests change Event data between page visits.

### Add page metadata to the response model

The existing `EventSearchViewModel` already carries the current search criteria and matching Events. Its `Events` property will now contain **only the current page**. Razor also needs to know which page it is displaying and how many results exist across all pages.

Open:

**File: `CampusEvents/ViewModels/EventSearchViewModel.cs`**

Add these properties below `CategoryId`:

```csharp
public int CurrentPage { get; set; } = 1;

public int PageSize { get; set; }

public int TotalCount { get; set; }

public int TotalPages { get; set; }
```

Keep the existing `Events`, `Categories`, `Search`, and `CategoryId` properties. The completed View Model now combines filtered domain data with the request and response information needed by this one page.

### Normalize `page`, count matches, and retrieve ten rows

Open:

**File: `CampusEvents/Controllers/EventsController.cs`**

Add a fixed page-size constant inside `EventsController`, next to its existing fields:

```csharp
private const int PublicPageSize = 10;
```

Then replace the existing `Index()` action with the following implementation:

```csharp
public async Task<IActionResult> Index(string? search, int? categoryId, int? page)
{
	_logger.LogInformation("Loading Published upcoming CEMS events with public filters and paging.");

	var now = _timeProvider.GetLocalNow().DateTime;

	IQueryable<Event> query = _context.Events
		.AsNoTracking()
		.Include(eventItem => eventItem.Category)
		.Where(eventItem => eventItem.Status == EventStatus.Published && eventItem.StartDateTime > now);

	if (!string.IsNullOrWhiteSpace(search))
	{
		var searchTerm = search.Trim();

		query = query.Where(eventItem => eventItem.Title.Contains(searchTerm) || eventItem.Location.Contains(searchTerm));
	}

	if (categoryId.HasValue)
	{
		query = query.Where(eventItem => eventItem.CategoryId == categoryId.Value);
	}

	var totalCount = await query.CountAsync();
	var totalPages = (int) Math.Ceiling(totalCount / (double) PublicPageSize);

	var currentPage = page.GetValueOrDefault(1);

	if (currentPage < 1)
	{
		currentPage = 1;
	}

	if (totalPages > 0 && currentPage > totalPages)
	{
		currentPage = totalPages;
	}

	if (totalPages == 0)
	{
		currentPage = 1;
	}

	var events = await query
		.OrderBy(eventItem => eventItem.StartDateTime)
		.ThenBy(eventItem => eventItem.Id)
		.Skip((currentPage - 1) * PublicPageSize)
		.Take(PublicPageSize)
		.ToListAsync();

	var categories = await _context.Categories
		.AsNoTracking()
		.OrderBy(category => category.Name)
		.ToListAsync();

	var viewModel = new EventSearchViewModel
	{
		Events = events, Categories = categories, Search = search, CategoryId = categoryId,
		CurrentPage = currentPage, PageSize = PublicPageSize, TotalCount = totalCount, TotalPages = totalPages
	};

	return View(viewModel);
}
```

MVC binds `page` from the query string just as it binds `search` and `categoryId`. The parameter is nullable because `/Events` must work without a page number. If the page number is missing or cannot be converted to an integer, the action uses page 1. Zero and negative values also become page 1. A number larger than the available page count selects the last available page.

When no Events match, `TotalCount` and `TotalPages` are both zero. The controller keeps `CurrentPage` at 1, and the existing no-results message remains available to Razor.

Notice the position of `CountAsync()`: the Published/upcoming rule and **both optional filters** are already part of `query`. The count therefore describes the same matching set that the subsequent page query uses. The `OrderBy`, `ThenBy`, `Skip`, and `Take` operations then describe which subset of that set becomes `Model.Events`.

### Add Previous and Next links without losing filters

The controller can now respond to requests for different pages, but a visitor still needs a way to make those requests. Keep the GET search/filter form at the top of the page and add Previous and Next links after the Event list.

Open:

**File: `CampusEvents/Views/Events/Index.cshtml`**

Inside the existing `else` branch, immediately after the Event list's closing `</section>` and before the branch's closing brace, add:

```cshtml
<p class="event-page-status">
	Page @Model.CurrentPage of @Model.TotalPages
</p>

@if (Model.TotalPages > 1)
{
	<nav class="event-pagination" aria-label="Event results pages">
		<div class="event-pagination-actions">
			@if (Model.CurrentPage > 1)
			{
				<a asp-controller="Events"
				   asp-action="Index"
				   asp-route-search="@Model.Search"
				   asp-route-categoryId="@Model.CategoryId"
				   asp-route-page="@(Model.CurrentPage - 1)"
				   class="event-page-link"
				   rel="prev">Previous</a>
			}

			@if (Model.CurrentPage < Model.TotalPages)
			{
				<a asp-controller="Events"
				   asp-action="Index"
				   asp-route-search="@Model.Search"
				   asp-route-categoryId="@Model.CategoryId"
				   asp-route-page="@(Model.CurrentPage + 1)"
				   class="event-page-link"
				   rel="next">Next</a>
			}
		</div>
	</nav>
}
```

The `asp-route-*` attributes tell MVC to generate a new URL containing the current search and Category values alongside the new page number. For example, selecting **Next** from page 1 of the Workshop search produces a request for page 2 **with the Workshop search still active**.

There is no Previous link on page 1 and no Next link on the final page. When the results fit on one page, the page status still shows **Page 1 of 1**, but the navigation links are omitted. If no Events match, the no-results branch shows neither a page status nor navigation links. The `<nav>` element and its accessible label identify the links as navigation for the Event results; ordinary links are also keyboard-operable without JavaScript.

Submitting **Apply filters** sends the values in the GET form, which contains no `page` field. A new search or Category selection therefore starts on page 1. The **Clear** link continues to return to the unfiltered first page.

### Style fixed-page navigation

#### Add the pagination styles

Open:

**File: `CampusEvents/wwwroot/css/site.css`**

Append these styles below the search/filter rules:

```css
.event-pagination {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 0.75rem;
	margin: 1.5rem 0;
}

.event-page-status {
	margin: 1rem 0 0;
	font-weight: 600;
}

.event-pagination-actions {
	display: flex;
	flex-wrap: wrap;
	gap: 0.5rem;
}

.event-page-link {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	min-height: 2.5rem;
	padding: 0.55rem 0.8rem;
	border: 1px solid #8c959f;
	border-radius: 0.25rem;
	background: #ffffff;
	color: #174ea6;
	text-decoration: none;
}

.event-page-link:hover {
	background: #f6f8fa;
}
```

The navigation wraps onto another line when the viewport is narrow. The links keep the site's blue link color and use the global `a:focus-visible` rule, so keyboard users can see which page link is selected. They remain ordinary server-generated navigation links and do not require JavaScript.

Build and run the application again:

```cmd
dotnet build CampusEvents\CampusEvents.csproj
dotnet run --project CampusEvents\CampusEvents.csproj
```

Confirm the two Published upcoming Events on page 1, then prepare a private Development dataset to inspect multiple page boundaries.

### Prepare a private Development dataset for fixed-page verification

To display multiple result pages using controlled data, **temporarily** add the following code to `CampusEvents/Program.cs` inside the existing `if (app.Environment.IsDevelopment())` service scope, immediately after `await DbInitializer.SeedAsync(context, timeProvider);`. Use your own disposable Development database for these practice rows.

```csharp
var academicId = await context.Categories
	.Where(category => category.Name == "Academic")
	.Select(category => category.Id)
	.SingleAsync();

if (!await context.Events.AnyAsync(eventItem => eventItem.Title == "Pagination Practice Workshop 01"))
{
	var createdAt = timeProvider.GetLocalNow().DateTime;
	var practiceEvents = new List<Event>();

	for (var number = 1; number <= 52; number++)
	{
		var start = createdAt.Date.AddDays(20 + (number - 1) / 2).AddHours(14);

		practiceEvents.Add(new Event
		{
			Title = $"Pagination Practice Workshop {number:00}",
			Description = "A Development Event for testing paginated results.",
			Location = "Learning Commons", StartDateTime = start, EndDateTime = start.AddHours(2), Capacity = 30,
			Status = EventStatus.Published, CreatedAt = createdAt, CategoryId = academicId, OrganizerId = "seed-organizer-001"
		});
	}

	context.Events.AddRange(practiceEvents);
	await context.SaveChangesAsync();
}
```

Ensure `using CampusEvents.Models;` is present at the top of `Program.cs`; add it if necessary. The snippet uses an existing `List<Event>` and EF Core. Before running it, verify that the sentinel practice title does not already exist and that the Development database is disposable and owned by you. The sentinel prevents a second insertion when the same Development database is reused.

```cmd
dotnet build CampusEvents\CampusEvents.csproj
dotnet run --project CampusEvents\CampusEvents.csproj
```

With the **original two public Events** and **52 added Published future Events**, an unfiltered catalog has **54** matching results, assuming no other data changes and all example future dates remain eligible. At this fixed-size checkpoint, size 10 has six pages (the last with **four** Events). After the independent challenge, size 25 should have three pages and size 50 should have two (each with **four** on its final page). A text search for `Pagination Practice Workshop` selects **52** practice rows regardless of the two original records; at size 10, the last page has **two** practice rows. Repeated start times let you inspect the `Id` tie-breaker.

### Prove fixed pagination before making it configurable

The GET form submits search and Category criteria, and page-navigation links supply the `page` parameter. Visiting `/Events?page=0`, `/Events?page=-3`, or `/Events?page=abc` should select page 1. An above-range value clamps to the final page of the current matching set. Zero matches must show the existing no-results message, not page navigation. Use the guarded Development practice rows above to inspect Previous/Next across multiple pages while preserving the permanent seed and approved SRS.

Use the controlled Development records to inspect first/last boundaries at size 10. Complete the extension independently, then compare your work with the implementation that follows.

## Independent challenge — Selectable page sizes, First/Last, and result ranges

At Checkpoint B, CEMS always retrieves ten matching Events per page. The controller decides that value with a fixed constant, while the View Model already carries the resulting page size to Razor.

Now extend the feature so the visitor can choose how many Events appear on each page, jump directly to the first or last available page, **and** see the range of results currently visible.

Your completed interface should let the visitor choose:

```text
10 Events per page
25 Events per page
50 Events per page
```

The selected page size becomes part of the GET request state alongside `search`, `categoryId`, and `page`. For example:

```text
/Events?search=Workshop&categoryId=1&page=2&pageSize=25
```

means, conceptually:

```text
show page 2
of the Published/upcoming Events
that match the current search and Category
using 25 Events per page
```

The page should also make the current slice of the matching set visible. For example, a request showing the second group of 25 from 55 matching Events should display:

```text
Showing 26–50 of 55 events.
```

### Attempt the challenge before reading the solution

Complete your initial solution before comparing it with the worked implementation.

First inspect the code you have already built and attempt the change yourself. You have already used every major technique this feature requires: query-string model binding, View Models, GET form state, validation, `CountAsync()`, `Skip()` and `Take()`, Tag Helpers, and conditional Razor rendering.

Before coding, answer these design questions:

1. Which controller, View Model, Razor View, and stylesheet responsibilities change?
2. How should `pageSize` be represented, defaulted, and validated when omitted, malformed, or unsupported?
3. Which total-page, offset, and result-range calculations depend on page size?
4. How will the View render the 10/25/50 options and retain the selected value?
5. Which existing pagination properties support First and Last destinations, and when should each navigation link appear?
6. How will page links preserve search, Category, and page size while a new form submission resets to page 1?
7. How will you check the HTTP 400 response for an unsupported size and the matching range for a valid page?
8. How will direct requests remain safe when they bypass the dropdown's offered choices?

Implement the feature independently, build the application, and exercise several catalog URLs.

Compare your solution with the worked implementation using the request contract, bounded retrieval, preserved state, and visible behavior.

### Acceptance criteria

Your solution should satisfy all of the following:

- `/Events` still defaults to 10 Events per page;
- the form offers only 10, 25, and 50 Events per page;
- the selected page size remains selected after the form is submitted;
- changing search, Category, or page size through the form starts again on page 1 because the form does not submit a `page` field;
- **First** and **Previous** appear only when `CurrentPage > 1`;
- **Next** and **Last** appear only when `CurrentPage < TotalPages`;
- First, Previous, Next, and Last preserve `search`, `categoryId`, and `pageSize`;
- **First** requests page 1 and **Last** requests `TotalPages` without requiring new controller actions;
- the **Clear** link removes the search and Category filters while preserving the selected page size;
- explicitly supplied unsupported or malformed page-size values return **400 Bad Request**;
- `CountAsync()` still counts the complete matching set before page retrieval;
- `Skip()` and `Take()` use the validated page size;
- the result range reflects the current page and page size;
- zero-result pages continue to show the existing no-results message without a result range or pagination links.

This exercise intentionally treats `pageSize` more strictly than the `page` parameter implemented earlier. The earlier page-number normalization remains unchanged. Here, the page-size contract is deliberately narrow because the value directly controls how many rows one request asks the database to materialize and how much result data the response may contain.

### Extend the challenge — Observe HTTP 400 before improving its presentation

As you validate explicit `pageSize`, start with a minimal rejection branch in `Index()`. Define the allowed size set and nullable `pageSize` argument yourself. Before the filter query executes, determine whether the original request supplied `pageSize`. If supplied but missing a bindable allowed integer, return:

```csharp
var pageSizeWasSupplied = Request.Query.ContainsKey(nameof(pageSize));

if (pageSizeWasSupplied && (!pageSize.HasValue || !AllowedPageSizes.Contains(pageSize.Value)))
{
	return BadRequest();
}
```

Try `/Events?pageSize=1` and `/Events?pageSize=abc` in the browser; inspect the **document request's HTTP status** in Developer Tools → Network. Both must report 400. A plain `BadRequest()` result has no Razor error View, so its body can be empty. Compare that with `/Events?search=unlikely-no-match`: a valid search returning no Events must be HTTP 200 and render a no-results message.

**Second independent step:** devise a shared `ErrorPageViewModel` and a `Views/Shared/BadRequest.cshtml` with a useful message and server-generated return link. Replace the plain 400 with that View **without changing the HTTP status**. Inspect the HTTP 400 document status and its explanatory rendered HTML in the browser. The worked solution follows only after this attempt.

## Worked implementation — compare only after attempting the challenge

Compare your implementation with these complete replacement files after finishing the independent challenge. `EventsController`, both View Models, the Index and BadRequest Views, and the final CSS preserve the established application contract: public eligibility, validated page sizes, page normalization, and existing Details behavior.

### Final `EventSearchViewModel`: add the allowed choices

Retain the Checkpoint B page metadata and add `PageSizeOptions` for Razor's dropdown. The View Model exposes these selectable options to the form. The complete file should be:

**File: `CampusEvents/ViewModels/EventSearchViewModel.cs`**

```csharp
using CampusEvents.Models;
using System;
using System.Collections.Generic;

namespace CampusEvents.ViewModels;

public class EventSearchViewModel
{
	public IReadOnlyList<Event> Events { get; set; } = Array.Empty<Event>();

	public IReadOnlyList<Category> Categories { get; set; } = Array.Empty<Category>();

	public string? Search { get; set; }

	public int? CategoryId { get; set; }

	public int CurrentPage { get; set; } = 1;

	public int PageSize { get; set; }

	public IReadOnlyList<int> PageSizeOptions { get; set; } = Array.Empty<int>();

	public int TotalCount { get; set; }

	public int TotalPages { get; set; }
}
```

### Build a reusable HTML 400 response after observing the bare 400

Create a reusable response model and Razor View that explain invalid page-size requests and provide a safe return to the catalog.

**File: `CampusEvents/ViewModels/ErrorPageViewModel.cs`**

```csharp
namespace CampusEvents.ViewModels;

public class ErrorPageViewModel
{
	public string Title { get; init; } = "Invalid Request";

	public string Message { get; init; } = "The request could not be processed.";

	public string? ReturnUrl { get; init; }

	public string ReturnLabel { get; init; } = "Return";
}
```

**File: `CampusEvents/Views/Shared/BadRequest.cshtml`**

```cshtml
@model CampusEvents.ViewModels.ErrorPageViewModel

@{
	ViewData["Title"] = Model.Title;
}

<h1>@Model.Title</h1>

<p role="alert">@Model.Message</p>

@if (!string.IsNullOrWhiteSpace(Model.ReturnUrl))
{
	<a href="@Model.ReturnUrl">@Model.ReturnLabel</a>
}
```

The controller must detect the **presence** of `pageSize` in the original query string and reject a present unsupported or unbindable value. Supplying `pageSize=abc` must not silently fall back to 10. Set `Response.StatusCode` to 400 before returning the shared View so the document status agrees with the error message. The return URL comes from server-generated `Url.Action("Index", "Events")`, not a client-supplied arbitrary destination.

### Final `EventsController`: valid size, bounded search, and unchanged Details

Replace the fixed-page `Index()` in `CampusEvents/Controllers/EventsController.cs` with the implementation below, keeping the existing constructor dependencies and especially `Details(int id)`. This controller combines filtering, counting, normalization, bounded retrieval, and the existing public Details behavior.

**File: `CampusEvents/Controllers/EventsController.cs`**

```csharp
using CampusEvents.Data;
using CampusEvents.Models;
using CampusEvents.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace CampusEvents.Controllers;

public class EventsController : Controller
{
	private readonly ILogger<EventsController> _logger;
	private readonly ApplicationDbContext _context;
	private readonly TimeProvider _timeProvider;
	private const int DefaultPageSize = 10;

	private static readonly int[] AllowedPageSizes =
	{
		10, 25, 50
	};

	public EventsController(ILogger<EventsController> logger, ApplicationDbContext context, TimeProvider timeProvider)
	{
		_logger = logger;
		_context = context;
		_timeProvider = timeProvider;
	}

	public async Task<IActionResult> Index(string? search, int? categoryId, int? page, int? pageSize)
	{
		_logger.LogInformation("Loading Published upcoming CEMS events with public filters and paging.");

		var pageSizeWasSupplied = Request.Query.ContainsKey(nameof(pageSize));

		if (pageSizeWasSupplied && (!pageSize.HasValue || !AllowedPageSizes.Contains(pageSize.Value)))
		{
			var errorViewModel = new ErrorPageViewModel
			{
				Title = "Invalid Page Size", Message = "Choose a page size of 10, 25, or 50.",
				ReturnUrl = Url.Action("Index", "Events"), ReturnLabel = "Return to Events"
			};

			Response.StatusCode = StatusCodes.Status400BadRequest;

			return View("BadRequest", errorViewModel);
		}

		var currentPageSize = pageSize.GetValueOrDefault(DefaultPageSize);
		var now = _timeProvider.GetLocalNow().DateTime;

		IQueryable<Event> query = _context.Events
			.AsNoTracking()
			.Include(eventItem => eventItem.Category)
			.Where(eventItem => eventItem.Status == EventStatus.Published && eventItem.StartDateTime > now);

		if (!string.IsNullOrWhiteSpace(search))
		{
			var searchTerm = search.Trim(); // Match normalized text; retain the original value for the form.

			query = query.Where(eventItem => eventItem.Title.Contains(searchTerm) || eventItem.Location.Contains(searchTerm));
		}

		if (categoryId.HasValue)
		{
			query = query.Where(eventItem => eventItem.CategoryId == categoryId.Value);
		}

		var totalCount = await query.CountAsync();
		var totalPages = (int)Math.Ceiling(totalCount / (double)currentPageSize);

		var currentPage = page.GetValueOrDefault(1);

		if (currentPage < 1)
		{
			currentPage = 1;
		}

		if (totalPages > 0 && currentPage > totalPages)
		{
			currentPage = totalPages;
		}

		if (totalPages == 0)
		{
			currentPage = 1;
		}

		// EF Core Skip accepts an Int32 offset; calculate in a wider type before converting.
		var offset = ((long)currentPage - 1) * currentPageSize;

		if (offset > int.MaxValue)
		{
			Response.StatusCode = StatusCodes.Status400BadRequest;
			return View("BadRequest", new ErrorPageViewModel
			{
				Title = "Page Range Unavailable",
				Message = "Choose an earlier page in the event catalog.", ReturnUrl = Url.Action("Index", "Events"),
				ReturnLabel = "Return to Events"
			});
		}

		var events = await query
			.OrderBy(eventItem => eventItem.StartDateTime)
			.ThenBy(eventItem => eventItem.Id)
			.Skip((int)offset)
			.Take(currentPageSize)
			.ToListAsync();

		var categories = await _context.Categories
			.AsNoTracking()
			.OrderBy(category => category.Name)
			.ToListAsync();

		var viewModel = new EventSearchViewModel
		{
			Events = events, Categories = categories, Search = search, CategoryId = categoryId,
			CurrentPage = currentPage, PageSize = currentPageSize, PageSizeOptions = AllowedPageSizes, TotalCount = totalCount,
			TotalPages = totalPages
		};

		return View(viewModel);
	}

	public async Task<IActionResult> Details(int id)
	{
		var eventItem = await _context.Events
			.AsNoTracking()
			.Include(item => item.Category)
			.Include(item => item.Organizer)
			.SingleOrDefaultAsync(item => item.Id == id && item.Status == EventStatus.Published);

		if (eventItem is null)
		{
			return EventNotFound();
		}

		return View(eventItem);
	}

	private ViewResult EventNotFound()
	{
		Response.StatusCode = StatusCodes.Status404NotFound;
		return View("EventNotFound");
	}
}
```

Use `EventSearchViewModel` as the shared controller-to-Razor response contract. Build the application after replacing the controller and View.

### Final `Index.cshtml`: form, range, links, and empty state

Replace the intermediate `CampusEvents/Views/Events/Index.cshtml` with the complete strongly typed View below. The form uses `method="get"` and submits search, Category, and page size but **not** the existing page. This is why applying a new query naturally restarts at page 1. The Clear link retains page size but discards both filters.

The View derives the visible range from the validated page state and actual number of returned rows. It omits result-range text and pagination navigation for zero matches; it also omits pagination links when only one page exists. Tag Helpers construct Details links and First/Previous/Next/Last URLs with current criteria. When two Events have the same start time, the controller's stable secondary ordering is retained independently of how Razor renders them.

**File: `CampusEvents/Views/Events/Index.cshtml`**

```cshtml
@model CampusEvents.ViewModels.EventSearchViewModel

@{
	ViewData["Title"] = "Events";
}

<h1>Upcoming Campus Events</h1>

<form asp-controller="Events"
		asp-action="Index"
		method="get"
		class="event-filter-form"
		aria-label="Search and filter campus events">

	<div class="event-filter-field">
		<label for="search" class="event-filter-label">Search title or location</label>
		<input id="search"
				 name="search"
				 type="search"
				 class="event-filter-control"
				 value="@Model.Search"
				 aria-describedby="searchHelp" />
		<div id="searchHelp" class="event-filter-help">
			Matches Event title or location.
		</div>
	</div>

	<div class="event-filter-field">
		<label for="categoryId" class="event-filter-label">Category</label>
		<select id="categoryId" name="categoryId" class="event-filter-control">
			<option value="">All categories</option>

			@foreach (var category in Model.Categories)
			{
				if (Model.CategoryId == category.Id)
				{
					<option value="@category.Id" selected>@category.Name</option>
				}
				else
				{
					<option value="@category.Id">@category.Name</option>
				}
			}
		</select>
	</div>

	<div class="event-filter-field">
		<label for="pageSize" class="event-filter-label">Events per page</label>
		<select id="pageSize" name="pageSize" class="event-filter-control">
			@foreach (var pageSizeOption in Model.PageSizeOptions)
			{
				if (Model.PageSize == pageSizeOption)
				{
					<option value="@pageSizeOption" selected>@pageSizeOption</option>
				}
				else
				{
					<option value="@pageSizeOption">@pageSizeOption</option>
				}
			}
		</select>
	</div>

	<div class="event-filter-actions">
		<button type="submit" class="event-filter-submit">
			Apply filters
		</button>

		<a asp-controller="Events" asp-action="Index" asp-route-pageSize="@Model.PageSize" class="event-filter-clear">
			Clear
		</a>
	</div>
</form>

@if (Model.TotalCount > 0)
{
	var firstResult = (Model.CurrentPage - 1) * Model.PageSize + 1;
	var lastResult = firstResult + Model.Events.Count - 1;

	<p class="event-result-count">
		Showing @firstResult–@lastResult of @Model.TotalCount events.
	</p>
}

@if (Model.Events.Count == 0)
{
	<p role="status">
		No published upcoming events match your current criteria.
	</p>
}
else
{
	<section aria-labelledby="event-list-heading">
		<h2 id="event-list-heading" class="cems-visually-hidden">
			Published upcoming events
		</h2>

		@foreach (var eventItem in Model.Events)
		{
			<article class="event-card">
				<h3 class="event-card-title">
					<a asp-controller="Events" asp-action="Details" asp-route-id="@eventItem.Id">
						@eventItem.Title
					</a>
				</h3>

				<p><strong>Category:</strong> @eventItem.Category.Name</p>
				<p><strong>Location:</strong> @eventItem.Location</p>
				<p>
					<strong>Starts:</strong>
					<time>@eventItem.StartDateTime.ToString("f")</time>
				</p>
			</article>
		}
	</section>

	<p class="event-page-status">
		Page @Model.CurrentPage of @Model.TotalPages
	</p>

	@if (Model.TotalPages > 1)
	{
		<nav class="event-pagination" aria-label="Event results pages">
			<div class="event-pagination-actions">
				@if (Model.CurrentPage > 1)
				{
					<a asp-controller="Events"
					   asp-action="Index"
					   asp-route-search="@Model.Search"
					   asp-route-categoryId="@Model.CategoryId"
					   asp-route-pageSize="@Model.PageSize"
					   asp-route-page="1"
					   class="event-page-link">First</a>

					<a asp-controller="Events"
					   asp-action="Index"
					   asp-route-search="@Model.Search"
					   asp-route-categoryId="@Model.CategoryId"
					   asp-route-pageSize="@Model.PageSize"
					   asp-route-page="@(Model.CurrentPage - 1)"
					   class="event-page-link"
					   rel="prev">Previous</a>
				}

				@if (Model.CurrentPage < Model.TotalPages)
				{
					<a asp-controller="Events"
					   asp-action="Index"
					   asp-route-search="@Model.Search"
					   asp-route-categoryId="@Model.CategoryId"
					   asp-route-pageSize="@Model.PageSize"
					   asp-route-page="@(Model.CurrentPage + 1)"
					   class="event-page-link"
					   rel="next">Next</a>

					<a asp-controller="Events"
					   asp-action="Index"
					   asp-route-search="@Model.Search"
					   asp-route-categoryId="@Model.CategoryId"
					   asp-route-pageSize="@Model.PageSize"
					   asp-route-page="@Model.TotalPages"
					   class="event-page-link">Last</a>
				}
			</div>
		</nav>
	}
}
```

The `<label for="...">` attributes match field IDs. Native controls and links are keyboard-operable. The pagination `<nav>` has an accessible name, and the result page status states the visitor's position. Razor encodes text values in HTML output. Do not use client-side hiding as a substitute for server-side public eligibility checks.

### Final `site.css`: replace provisional rules with the final layout

Replace the **Checkpoint A/B Lesson 3 rules** in `CampusEvents/wwwroot/css/site.css` with this final Lesson 3 stylesheet block; retain the original Lesson 1–2 layout, `.event-card`, `.event-card-title`, `.cems-visually-hidden`, and Details rules. Do not retain duplicate provisional selectors or introduce an external CSS framework. The form uses four columns where there is room, two at intermediate sizes, and one at narrow widths; controls wrap instead of overflowing their container.

**File: `CampusEvents/wwwroot/css/site.css` — Lesson 3 additions**

```css
.event-filter-form {
	display: grid;
	grid-template-columns:
		minmax(15rem, 2fr)
		minmax(12rem, 1fr)
		minmax(9rem, auto)
		minmax(14rem, auto);
	gap: 1rem;
	margin-bottom: 1.5rem;
	align-items: start;
}

.event-filter-field {
	min-width: 0;
}

.event-filter-label {
	display: block;
	margin-bottom: 0.35rem;
	font-weight: 600;
	line-height: 1.5;
}

.event-filter-control {
	width: 100%;
	min-height: 2.5rem;
	padding: 0.55rem 0.65rem;
	border: 1px solid #8c959f;
	border-radius: 0.25rem;
	background: #ffffff;
	color: inherit;
	font: inherit;
}

.event-filter-help {
	margin-top: 0.35rem;
	font-size: 0.875rem;
}

.event-filter-actions {
	display: flex;
	gap: 0.5rem;
	padding-top: calc(1.5em + 0.35rem);
}

.event-filter-submit,
.event-filter-clear {
	flex: 1;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	min-height: 2.5rem;
	padding: 0.55rem 0.75rem;
	border: 1px solid #8c959f;
	border-radius: 0.25rem;
	font: inherit;
	text-decoration: none;
	cursor: pointer;
}

.event-filter-submit {
	background: #174ea6;
	border-color: #174ea6;
	color: #ffffff;
}

.event-filter-clear {
	background: #ffffff;
	color: #174ea6;
}

.event-filter-submit:hover {
	background: #123d84;
}

.event-filter-clear:hover {
	background: #f6f8fa;
}

.event-result-count {
	margin: 0 0 1rem;
}

@media (max-width: 64rem) {
	.event-filter-form {
		grid-template-columns: repeat(2, minmax(0, 1fr));
	}

	.event-filter-actions {
		padding-top: 0;
	}
}

@media (max-width: 48rem) {
	.event-filter-form {
		grid-template-columns: 1fr;
	}

	.event-filter-actions {
		padding-top: 0;
	}
}

.event-pagination {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 0.75rem;
	margin: 1.5rem 0;
}

.event-page-status {
	margin: 1rem 0 0;
	font-weight: 600;
}

.event-pagination-actions {
	display: flex;
	flex-wrap: wrap;
	gap: 0.5rem;
}

.event-page-link {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	min-height: 2.5rem;
	padding: 0.55rem 0.8rem;
	border: 1px solid #8c959f;
	border-radius: 0.25rem;
	background: #ffffff;
	color: #174ea6;
	text-decoration: none;
}

.event-page-link:hover {
	background: #f6f8fa;
}
```

Confirm visually that `/css/site.css` loads, focus indications remain visible, and wide and narrow pages work without horizontal scrolling. Inspect the browser output at wide and narrow viewport sizes, including keyboard focus and the loaded stylesheet.

## Trace the completed request and review the change

Follow one concrete GET through the system:

```text
GET /Events?search=Library&categoryId=<actual Academic ID>&page=1&pageSize=10
	↓ bind and validate client-controlled values
Published + future-start query
	↓ add title/location search and Category conditions
CountAsync() → all matching public rows
	↓ normalize requested page and order by StartDateTime, Id
Skip / Take / ToListAsync() → only the selected page
	↓ separately load Category choices
EventSearchViewModel → Razor form, results, status, page links
	↓ HTML and HTTP 200
```

Replace `<actual Academic ID>` in that illustrative URL with the real numeric value when making a request. In the five-Event seed set, `Library Research Night` is the one public Academic Event matching `Library`; the Cancelled `Outdoor Movie Night` cannot appear even if its text matches a search.

Make focused commits for stable changes. Run `git diff --check` and build after significant implementation steps.

# Discussion

## Read-only retrieval contracts and application state

A visitor submits `GET /Events?search=Library&categoryId=2&pageSize=25`. MVC binds the parameters, the controller selects the matching public Events, and `EventSearchViewModel` carries the result and active selections into Razor. Selecting Next produces another GET containing the same criteria and a destination page; submitting new filter criteria begins at page 1. Because GET describes retrieval, the URL can be inspected, bookmarked, and shared without writing records.

| State | Example | Responsibility |
| --- | --- | --- |
| Persisted domain data | Event status, title, start time, Category relationship | Database records across requests |
| Request state | `search`, `categoryId`, `page`, `pageSize` | Client-supplied criteria interpreted for each GET |
| Presentation state | Category options, matching total, result range | Server-produced values that describe the rendered response |

A visitor can edit URL values directly. Apply visibility and input rules on the server, independently of which options the form displays.

## Eligibility invariants and composable predicates

The public eligibility invariant is `Published AND StartDateTime > now`, using the application's configured local clock. Search and Category constraints narrow that eligible set:

```text
Eligible = Published AND StartDateTime > now
Matching = Eligible AND (optional title OR location match) AND optional Category
Page = deterministic ordered slice of Matching
```

Compare `Eligible AND (title match OR location match)` with `Eligible OR title match`. The second form could expose a matching Draft, Cancelled, or past Event. Compose mandatory eligibility first and add optional `Where` conditions before counting or retrieving. This pattern preserves a policy as retrieval features evolve.

## Query composition, execution, and bounded retrieval

In a database-backed application, constructing a query and fetching the results are different phases. An `IQueryable<Event>` is a description that can be extended with predicates and ordering. `CountAsync()` executes a database count; `ToListAsync()` executes retrieval and materializes objects. Apply matching predicates, stable ordering, `Skip`, and `Take` **before** retrieving rows into application memory.

```text
base eligibility + optional predicates
          ├── CountAsync()                         → full matching total
          └── OrderBy / ThenBy / Skip / Take
                                      └── ToListAsync() → one bounded page
```

Filtering in SQL reduces transferred rows and application memory use. Apply the public visibility predicate within the query and limit each response to at most 50 Event rows. For additional count, sort, and search cost analysis, see [Database search and query-performance alternatives](#database-search-and-query-performance-alternatives).

**Search-scope trade-off:** `FR-SRH-005` requires matching at least title or location, which is the chosen implementation. Expanding into Description changes both the result contract and query cost. [Database search and query-performance alternatives](#database-search-and-query-performance-alternatives) compares indexing, substring and full-text approaches.

## Counting, deterministic ordering, and page boundaries

A matching count and a page answer different questions: `CountAsync()` determines the size of the fully filtered set, while `Skip()` and `Take()` retrieve a bounded portion. For 27 matches with page size 10, there are three pages; page 2 starts at offset 10, and page 3 holds seven Events. Compute `(page - 1) * pageSize` using a wide intermediate value. Check that the offset fits the database API’s supported integer range; return the controlled **Page Range Unavailable** response when it exceeds that range. The stitched Lessons 1–8 `EventsController.Index` now retains this `long` intermediate and the checked conversion to `Skip(int)`, matching the worked implementation. With `CountAsync()` returning an `int`, a validated page size, and an in-range normalized page, that overflow branch is not expected to be reachable in the current SQL Server contract; the guard is deliberately retained as defensive parity, **not** evidence of a previously exploitable overflow.

Ordering by `StartDateTime` alone leaves ties ambiguous. `ThenBy(Id)` gives each Event a deterministic position in an unchanged dataset. Insertions, deletions, and updates between requests can still shift offset page boundaries. Offset queries support direct numbered destinations and First/Last links. Keyset continuation advances after an ordering key and supports a different navigation contract; compare these in [Pagination strategies and result consistency](#pagination-strategies-and-result-consistency).

## Input validation, normalization, and HTTP semantics

Retrieval inputs are untrusted even if a browser uses a dropdown. CEMS intentionally gives two inputs different contracts:

- `page` is forgiving as required by SRS §11: omitted, malformed, zero, or negative values select page 1; an above-range page selects the last available page; zero matches keep `CurrentPage = 1` and `TotalPages = 0`.
- `pageSize` is limited to 10, 25, or 50: omission means 10; an explicitly supplied unsupported, zero, negative, or malformed value produces HTTP **400 Bad Request**. This prevents bypassing the dropdown to request an unbounded result set.

The controller trims search text before comparing titles and locations while retaining the supplied `search` value in the View Model for the input field. This keeps the visitor’s entry visible while matching against a normalized term.

An empty successful query remains HTTP **200 OK** and renders a no-results message. An invalid page-size request returns HTTP 400 with a Razor page explaining valid choices. The HTTP status and the rendered HTML communicate the outcome to clients and visitors respectively. Other applications might choose stricter page-number rejection, but that would conflict with the approved CEMS §11 policy.

For malformed nullable numbers, MVC can bind a `null` value and record model-state errors. Inspect whether `pageSize` was **present** to distinguish omission from invalid explicit input. The HTTP integration checks exercise binding, response status, and error-page content.

## View-specific models, preserved state, and accessible interaction

An Event entity represents one persisted record. A catalog response combines matching Events, current inputs, available Categories, result totals, page-size choices, and navigation metadata. `EventSearchViewModel` combines these concerns without adding temporary UI data to the Event table.

The controller queries the fully filtered Events and the available Categories independently. Event filtering determines the current public result page; the Category query supplies all filter choices even when the current results are empty or belong to one Category. The View Model combines these different datasets into one response without restricting filter choices to the current page. Razor renders that response. A GET form resets page selection by omitting `page`; pagination links preserve `search`, `categoryId`, `pageSize`, and the destination page. For example, applying new criteria from `?page=3&pageSize=25` begins at page 1, while selecting Next from `?search=Library&page=1&pageSize=25` retains `search=Library` and moves to page 2.

Semantic labels, native controls, keyboard-accessible links, visible focus, current-page information, and a meaningful empty state keep the workflow understandable. Inspect the rendered layout at narrow and wide viewport widths.

# Verification and Validation

Verify the catalog with isolated database-backed controller tests, HTTP integration tests, and browser scenarios. Check query results and selected action responses directly, then exercise MVC binding and rendered HTML over HTTP and inspect keyboard and responsive behavior in the browser.

## Automated

| Test / case | Behavior or boundary | Expected assertion / evidence |
| --- | --- | --- |
| Retained/revised — public Index and Details (2 expected cases) | Published/upcoming order and Details 404 | Existing eligibility and missing-resource behavior remain correct. |
| New — combined text and Category search (1 expected case) | Optional predicates intersect without changing public eligibility | Only matching eligible Events returned and requested criteria retained. |
| New — permitted page sizes (3 expected cases) | 10, 25, and 50 results per page | Expected totals, bounds, current page, and deterministic order for each size. |
| New — invalid page sizes (3 expected cases) | Unsupported, malformed, or nonpositive explicit page size | Controller status code 400 and a populated error View Model; inspect rendered HTML in browser checks. |
| New — filtered page-number normalization (4 expected cases) | Missing, zero, negative, and above-range pages | Page selection uses the filtered total and a valid bounded result. |
| New — zero matches (1 expected case) | No public Events match the request | Empty list, zero total pages, valid no-results response. |
| New — HTTP binding (3 expected cases) | Malformed `page` and `pageSize`, including error rendering | Real HTTP page normalization and 400 response with informative HTML. |

**Authored test inventory:** Seven direct-controller methods define fourteen individual cases, including parameterized page-number cases. The HTTP test class adds three request-level cases.

### Preserve the isolated SQL server fixture and fixed clock

Keep `CampusEvents.Tests/IsolatedSqlServerDatabase.cs` from the revised Lesson 2 unchanged. On Windows it uses `(localdb)\MSSQLLocalDB` by default; each test creates a **new GUID-named SQL Server database**, applies the real EF Core migrations, and drops only that database. Each fixture creates and drops only its own database. `CEMS_TEST_SQLSERVER` is an optional override for another authorized SQL Server endpoint and must target `master`. Do not use the application's Development `DefaultConnection` or point the fixture at production data. LocalDB is Windows-only; macOS/Linux runners need an authorized SQL Server connection. Use fixed-clock fixtures with test-owned data.

Maintain the established class and namespace in `CampusEvents.Tests/EventsControllerTests.cs`. Update the Index assertion to `EventSearchViewModel`, retain the Details 404 check, and add filtering, permitted-size pagination, invalid-size rejection, and zero-result normalization. It uses `FakeTimeProvider` with a fresh explicit UTC−04 test zone per test and the per-test SQL Server database fixture. The `IUrlHelper` helper provides the return link for direct action checks. HTTP-level scenarios exercise routing and model binding.

**File: `CampusEvents.Tests/EventsControllerTests.cs`**

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
		var zone = TimeZoneInfo.CreateCustomTimeZone(
			"CemsFixedUtcMinus4", TimeSpan.FromHours(-4), "CEMS fixed UTC-04 test zone", "CEMS fixed UTC-04 test zone");
		clock.SetLocalTimeZone(zone);
		return clock;
	}

	private static Event MakeEvent(string title, EventStatus status, DateTime start,
		int categoryId, string organizerId, string location = "Campus Student Center")
	{
		return new Event
		{
			Title = title, Description = "CEMS relational regression example",
			Location = location, StartDateTime = start, EndDateTime = start.AddHours(2), Capacity = 50,
			Status = status, CreatedAt = start.AddDays(-7), CategoryId = categoryId, OrganizerId = organizerId
		};
	}

	private static EventsController CreateController(
		ApplicationDbContext context, FakeTimeProvider clock, string queryString = "")
	{
		var httpContext = new DefaultHttpContext();
		httpContext.Request.QueryString = new QueryString(queryString);

		var controller = new EventsController(NullLogger<EventsController>.Instance, context, clock)
		{
			ControllerContext = new ControllerContext
			{
				HttpContext = httpContext
			}
		};

		// Provide a return URL for the error View.
		controller.Url = new EventsIndexUrlHelper(controller.ControllerContext);
		return controller;
	}

	// Supply the return link for direct controller action tests.
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

		Assert.Collection(model.Events, item => Assert.Equal("Earlier Seminar", item.Title),
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

		Assert.Collection(model.Events, item => Assert.Equal("Library Workshop", item.Title),
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
				arrange.Events.Add(MakeEvent( $"Campus Workshop {number:00}", EventStatus.Published,
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
	[InlineData(null, 1)]
	[InlineData(0, 1)]
	[InlineData(-3, 1)]
	[InlineData(999, 2)]
	public async Task Index_PageNumber_NormalizesAgainstFilteredMatches(int? requestedPage, int expectedPage)
	{
		var clock = FixedClock();
		var now = clock.GetLocalNow().DateTime;
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		int academicId;

		await using (var arrange = database.CreateContext())
		{
			var category = new Category { Name = "Academic" };
			var organizer = new ApplicationUser { Id = "page-boundary-organizer", DisplayName = "Campus Office" };
			arrange.Categories.Add(category);
			arrange.Users.Add(organizer);
			await arrange.SaveChangesAsync();
			academicId = category.Id;

			for (var number = 1; number <= 11; number++)
			{
				arrange.Events.Add(MakeEvent($"Academic Workshop {number:00}", EventStatus.Published,
					now.AddDays(number), category.Id, organizer.Id));
			}
			arrange.Events.Add(MakeEvent("Social Workshop", EventStatus.Published,
				now.AddDays(1), category.Id, organizer.Id, "Remote Campus"));
			await arrange.SaveChangesAsync();
		}

		await using var verify = database.CreateContext();
		var controller = CreateController(verify, clock, $"?search=Academic&categoryId={academicId}");
		var view = Assert.IsType<ViewResult>(await controller.Index("Academic", academicId, requestedPage, 10));
		var model = Assert.IsType<EventSearchViewModel>(view.Model);
		Assert.Equal(11, model.TotalCount);
		Assert.Equal(2, model.TotalPages);
		Assert.Equal(expectedPage, model.CurrentPage);
		Assert.Equal(expectedPage == 2 ? 1 : 10, model.Events.Count);
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

	[Fact]
	public async Task Index_NoPublicMatches_NormalizesPageAndReturnsEmptyState()
	{
		var clock = FixedClock();
		var now = clock.GetLocalNow().DateTime;
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();
		int academicId;

		await using (var arrange = database.CreateContext())
		{
			var academic = new Category { Name = "Academic" };
			var organizer = new ApplicationUser
			{
				Id = "zero-results-organizer", DisplayName = "CEMS Test Organizer"
			};
			arrange.Categories.Add(academic);
			arrange.Users.Add(organizer);
			await arrange.SaveChangesAsync();
			academicId = academic.Id;

			arrange.Events.AddRange(MakeEvent("Secret Research", EventStatus.Draft,
					now.AddDays(3), academic.Id, organizer.Id), MakeEvent("Public Seminar", EventStatus.Published,
					now.AddDays(4), academic.Id, organizer.Id));
			await arrange.SaveChangesAsync();
		}

		await using var verify = database.CreateContext();
		var controller = CreateController(verify, clock, $"?search=Secret&categoryId={academicId}&page=999&pageSize=10");
		var view = Assert.IsType<ViewResult>(await controller.Index("Secret", academicId, 999, 10));
		var model = Assert.IsType<EventSearchViewModel>(view.Model);

		Assert.Empty(model.Events);
		Assert.Equal(0, model.TotalCount);
		Assert.Equal(0, model.TotalPages);
		Assert.Equal(1, model.CurrentPage);
		Assert.Equal(10, model.PageSize);
		Assert.Equal(StatusCodes.Status200OK, controller.Response.StatusCode);
	}

}
```

The seven direct-controller methods cover fourteen cases: the original public Index case, original Details case, combined search-and-Category case, three allowed-size cases, three invalid-size cases, four filtered page-number cases, and one no-results case. Compare the inventory with the runner’s discovered, passed, failed, and skipped counts. The combined-filter fixture includes a matching location even when the title does not match; it also includes Draft, Cancelled, past, and wrong-Category records. The page fixture gives pairs equal start times to exercise ID tie-breaking. The no-results fixture checks zero matches without inventing a page zero.

### Check malformed retrieval input through real HTTP requests

The MVC pipeline binds URL query strings before invoking `EventsController`. Verify the behavior using an application test host, the isolated SQL Server fixture, and the fixed clock. Install the ASP.NET Core MVC testing package in the test project:

```cmd
dotnet add package Microsoft.AspNetCore.Mvc.Testing --project tests/CampusEvents.Tests
```

Expose the application's existing entry point to the test assembly by adding this declaration **after the top-level statements** in `CampusEvents/Program.cs`:

```csharp
public partial class Program { }
```

The standard project layout places `CampusEvents` and `CampusEvents.Tests` under the same solution directory. Run tests from this layout so the content-root helper can find `CampusEvents.csproj` by walking upward from the test output directory. For custom build-output paths, configure an explicit content root that points to the web project.

**File: `CampusEvents.Tests/EventsHttpTests.cs`**

```csharp
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
```

The test host uses the fixture-owned database connection and a fixed clock. Its malformed-page request checks the rendered first page of an eleven-Event dataset; invalid page-size requests check HTTP 400, the explanatory HTML, and a navigable return link. Run the suite with the inherited regression tests.

### Execute and interpret the regression suite

From the solution root, with Windows LocalDB installed (no variables required), or with the optional `CEMS_TEST_SQLSERVER` override set to an authorized `master` connection:

```cmd
dotnet --version
dotnet build
dotnet test CampusEvents.Tests\CampusEvents.Tests.csproj
```

Record the test run date, endpoint type, discovered tests, passed/failed/skipped counts, and per-test database cleanup results without credentials. The authored inventory comprises fourteen direct-controller cases and three HTTP cases. Record unavailable test infrastructure and database setup failures under their actual outcome. If neither LocalDB nor another authorized SQL Server endpoint is available, record `NOT EXECUTED — TEST ENVIRONMENT UNAVAILABLE`. Inspect failed assertions and relevant generated SQL before editing application code. Run the retained Lesson 2 tests alongside the new cases and investigate regressions.

Direct-controller tests check query results, View Model state, and action-selected status codes. HTTP integration tests exercise actual query binding, response status, and rendered navigation. Browser scenarios assess keyboard interaction and responsive layout.

## Manual

### Verify the fresh five-event baseline

For each scenario, record the request URL, document response status, and visible result.

**Prerequisite:** Use a personally controlled Development database, initialized once from Lesson 2 while the seeded future dates still qualify. Confirm all five records and their real timestamps before assuming the examples below. Do not reset or modify another person's database. The table uses the exact Lesson 2 seed; a database created long ago may have fewer currently upcoming Events.

| Scenario | Prerequisites, inputs, and steps | Expected result and evidence |
| --- | --- | --- |
| `/Events` | Open `/Events`. | `Library Research Night` and `Career Networking Lunch`, ordered by start; result range `Showing 1–2 of 2 events.`; size 10. |
| Search `Library` with All categories | Enter `Library`, select All categories, and submit Apply filters. | `Library Research Night` only. |
| Search `Student Center` | Enter `Student Center`. | `Career Networking Lunch` only; Draft `Welcome Mixer Planning Draft` remains hidden. |
| Category `Academic`, no search | Select Category `Academic` with an empty search and submit Apply filters. | `Library Research Night` only. |
| Category `Social`, no search | Select Category `Social` with an empty search and submit Apply filters. | No-results message; no pagination links. |
| Search `Movie` | Enter `Movie`. | No results; Cancelled `Outdoor Movie Night` remains hidden. |
| Search `Library` and choose Category `Career` | Enter `Library`, choose Category `Career`, and submit Apply filters. | No results; optional predicates intersect. |
| Search `Library` and choose Category `Academic` | Enter `Library`, choose Category `Academic`, and submit Apply filters. | One public result; the selected text and Category remain visible. |
| Select `Library Research Night` title | Select `Library Research Night` from the catalog. | Existing Details page opens; confirm stored description, location, Category, and owner fields as available. |
| Request nonexistent or Draft Details ID | Open the Details URL for a nonexistent or Draft Event ID. | Existing named not-found presentation with HTTP 404 (do not confuse this with no-match catalog HTTP 200). |

Inspect URL query parameters after using Apply filters; the form should preserve text and selected Category. Confirm no `page` parameter is submitted from the filter form. For browser-dependent status checks, use Developer Tools → Network and inspect the document response, not only the text on the page.

### Use the private Development pagination dataset

Reuse the temporary, guarded Development-only 52-Event practice dataset introduced in **Checkpoint B**. If you have not added it, follow that earlier section's guard and ownership instructions before running these checks. With two freshly eligible seed Events plus 52 eligible practice records (and no other changes), the count is 54. Keep all practice rows until the full navigation and error tests below are complete; remove them only through the restricted cleanup procedure that follows.

### Check navigation, request boundaries, and rendering

Use the disposable Development dataset and record each scenario’s URL, HTTP status, and visible result.

Use a real browser request for `/Events?pageSize=abc` and check that the response is HTTP 400 with the explanatory HTML error page. Also request `/Events?page=abc&pageSize=25`, confirm that it renders page 1, and repeat with `page=-3`, an above-range page, and a category-filtered result set. These HTTP requests exercise model binding and rendered response behavior alongside the direct-controller tests.


| Scenario | Prerequisites, inputs, and steps | Expected result and evidence |
| --- | --- | --- |
| `/Events?page=1&pageSize=10` | Open `/Events?page=1&pageSize=10`. | 10 results, `Page 1 of 6`, Next and Last but no First/Previous. |
| `/Events?page=6&pageSize=10` | Open `/Events?page=6&pageSize=10`. | 4 results, `Showing 51–54 of 54 events.`, First and Previous but no Next/Last. |
| `/Events?page=2&pageSize=25` | Open `/Events?page=2&pageSize=25`. | 25 results, `Showing 26–50 of 54 events.`, all four links. |
| `/Events?page=3&pageSize=25` | Open `/Events?page=3&pageSize=25`. | 4 results, `Showing 51–54 of 54 events.`. |
| `/Events?page=2&pageSize=50` | Open `/Events?page=2&pageSize=50`. | 4 results on the final page. |
| `/Events?page=0`, `?page=-3`, `?page=abc` | Open `/Events?page=0`, `?page=-3`, `?page=abc`. | Normalize to the first page of the matching set. |
| `/Events?page=999` | Open `/Events?page=999`. | Normalize to last available page for the active filter set. |
| `/Events?pageSize=10`, `25`, or `50` | Open `/Events?pageSize=10`, `25`, or `50`. | Accepted with selected option visible. |
| `/Events?pageSize=0`, `-10`, `37`, `abc`, or `1000000` | Open `/Events?pageSize=0`, `-10`, `37`, `abc`, or `1000000`. | HTTP 400; shared Invalid Page Size HTML and a Return to Events link. |
| `/Events?search=unmatched-value` | Open `/Events?search=unmatched-value`. | HTTP 200; no-results message, no range or pagination links. |

Search for `Pagination Practice Workshop`, choose `Academic` and size 25, and go to Next then Last; verify all links preserve search, Category, and size. Use the numeric Academic ID actually stored in your database rather than assuming it is 1. From a later page, change the search to `Library` and submit Apply filters: the resulting view should begin at page 1. Clear should discard search and Category while retaining size 25 and starting at page 1. Examine a filtered `?page=999` and ensure it clamps to the **filtered** last page, not the unfiltered total.

Test keyboard navigation through search, Category, page size, Apply filters, Clear, Details, and page links. Confirm controls have labels, focus is visible, and the current page indicator makes sense. At wide and narrow browser widths check wrapping, touch-target size, heading order, readable error text, and absence of normal-content horizontal scrolling. Verify `/css/site.css` loads successfully. Use the project-owned stylesheet and server-rendered controls throughout the workflow.

### Remove temporary practice records safely

Remove the temporary insertion code from `Program.cs`. In the **same private Development database only**, inspect the intended 52 practice records before deletion. For example, temporarily run this restricted cleanup once inside the Development-only scope after the ordinary initializer, then remove the cleanup snippet immediately:

```csharp
var practiceEventsToRemove = await context.Events
	.Where(eventItem => eventItem.Title.StartsWith("Pagination Practice Workshop ") &&
		eventItem.Description == "A Development Event for testing paginated results." &&
		eventItem.Location == "Learning Commons" && eventItem.OrganizerId == "seed-organizer-001")
	.ToListAsync();

if (practiceEventsToRemove.Count != 52)
{
	throw new InvalidOperationException("Expected exactly 52 practice Events; inspect Development data before cleanup.");
}

context.Events.RemoveRange(practiceEventsToRemove);
await context.SaveChangesAsync();
```

Never execute this cleanup against the isolated SQL Server test fixture, a shared database, or any unrelated data. If count or ownership does not match, stop and inspect rather than weakening the guard. Keep the migration history intact. With a freshly initialized original dataset, the catalog returns to two Published upcoming Events and `Showing 1–2 of 2 events.`; the page size defaults to 10 if omitted.

## SRS Traceability Matrix Update with Evidence

Continue the existing working record from Lesson 2, `docs/lesson-02-traceability.md`, and create `docs/lesson-03-traceability.md` for the new slice. Maintain the approved SRS and record lesson-slice progress in the working traceability file. For each entry, capture the implementation locations, actual check performed, environment/clock or dataset, observed result, date, and commit reference. Distinguish `IMPLEMENTED—UNVERIFIED`, `VERIFIED FOR LESSON SLICE`, `PARTIAL`, `DEFERRED`, `FAILED`, and `NOT EXECUTED`.

```md
# CEMS Lesson 3 — Working Traceability Evidence

| Requirement / Reference | Implementation / Artifact | Verification / Evidence | Status | Notes |
| --- | --- | --- | --- | --- |
| UC-001 | Events Index and retained Details | Browser catalog and Details scenarios: Not executed | PARTIAL | Registration availability and Cancelled Details remain open |
| FR-SRH-001–002 | EventsController.Index | SQL Server eligibility and ordering regression: Not executed | IMPLEMENTED—UNVERIFIED | Retained baseline; record regression output |
| FR-SRH-003 | Details action and View | Details 404 test and link walk: Not executed | IMPLEMENTED—UNVERIFIED | Retained behavior requires observed evidence |
| FR-SRH-004 / FR-SRH-008 | Details scope | Availability and Cancelled direct-link behavior pending | PARTIAL | Complete availability calculations and previously Published Cancelled-event Details visibility |
| FR-SRH-005–006 | Index predicates and GET form | Combined-filter test and browser criteria: Not executed | IMPLEMENTED—UNVERIFIED | Confirm intersection and public eligibility |
| FR-SRH-007 | Index empty state | Zero-match assertion and HTTP 200 browser check: Not executed | IMPLEMENTED—UNVERIFIED | Record response and visible message |
| FR-SRH-009 | Count, bounds, and navigation | Page cases and navigation checks: Not executed | IMPLEMENTED—UNVERIFIED | Record range, links, and carried criteria |
| SRS §11 | Page normalization | Boundary tests and malformed GET: Not executed | IMPLEMENTED—UNVERIFIED | Verify first, last, and empty page policies |
| NFR-PER-002 | Database query | Query inspection and bounded-row assertions: Not executed | PARTIAL | No measured performance evidence yet |
| NFR-ACC-001–003 / NFR-COM-002 | Razor and site.css | Keyboard and narrow-viewport inspection: Not executed | PARTIAL | Record actual accessibility and responsive observations |
| AC-001 / AC-010 | Catalog and automated checks | Full acceptance evidence: Not available | PARTIAL | Final-system acceptance remains open |

**Evidence context:** Run `dotnet build` and `dotnet test`, record the test results, and compare the manual inspector output with the expected scenarios. Both are necessary to evidence that requirements have been implemented and verified.

Confirm that your project builds, passes all tests, and runs without errors.

Stage and commit:

```cmd
git add .
git commit -m "Lesson 03: Implemented and Verified"
```

**NOTE:** Never stage or commit source code that does not build, fails a test, or runs with errors.

# Guided Exercises

## Exercise: diagram the retrieval contract

**Purpose and expected outcome:** Apply the separation between request, persisted data, and response state to the Lesson 3 Index workflow.

**Prerequisites and SRS ties:** Start from the completed public catalog and its View Model; no code change is required. Related SRS ties: FR-SRH-005–009: composable public retrieval, filtering, and pagination.

**Task:** Draw a diagram of one GET with search, Category, page, and page size. Identify where each value enters, where the eligibility predicate remains enforced, which database operations execute, and where the result range and page links are derived. Explain why a browser-only filter would not fulfill the same responsibility.

**Hints or checkpoints:** Follow input binding to composed SQL predicates, matching count, bounded rows, and the View Model links.

**Deliverable:** A labeled diagram and a short explanation that distinguishes matching count from page rows and identifies all untrusted inputs. Check the diagram against the controller and a captured browser URL.

**Completion and verification:** Check all state sources against the action and a captured GET URL, including the untrusted page and size values.

**Challenge (optional):** Identify what would have to change if filters were stored in a saved-search feature.

## Exercise: diagnose an unstable page boundary

**Purpose and expected outcome:** Use deterministic ordering and request-state reasoning to investigate pagination bugs.

**Prerequisites and SRS ties:** Use controlled Development data containing at least two equal-start Events; do not modify a fixture-owned SQL Server test database manually. Related SRS ties: FR-SRH-002 and FR-SRH-009: stable ordering and retained pagination state.

**Task:** Temporarily remove the `ThenBy(Id)` expression on a separate branch or uncommitted experiment. Compare page boundaries and explain the possible failure even if your small dataset happens to return the same order. Then inspect a deliberately defective Next link that omits `categoryId` or `pageSize`; identify the changed request contract. Restore both correct implementations.

**Hints or checkpoints:** Use equal-start records to reason about unstable ordering even if one small run appears stable; compare links before and after removing a query key.

**Deliverable:** A brief defect report with the offending query or link, a predicted or observed failure, a test approach, and the corrected code. Verify the restored code with repeatable data and URL inspection.

**Completion and verification:** Restore the tie-breaker and query keys, then confirm stable page boundaries with repeatable data.

**Challenge (optional):** Compare how concurrent inserts affect offset versus keyset pagination.

## Exercise: add an independent regression boundary

**Purpose and expected outcome:** Distinguish an action-level assertion from an HTTP-interface contract.

**Prerequisites and SRS ties:** Start from the ten-case suite and Windows LocalDB (or an authorized SQL Server override); if SQL Server is unavailable, write the test without claiming execution. Related SRS ties: SRS §11 and AC-010: invalid-page normalization and test coverage.

**Task:** Add a separate case for an omitted `pageSize` default or a malformed page number, with controlled Events and a fixed clock. Make a prediction for `TotalCount`, `CurrentPage`, `PageSize`, and returned rows. Explain which browser observation would still be needed to prove request binding and HTML.

**Hints or checkpoints:** Keep the same fixture-owned, GUID-named database and fixed clock, and assert model state separately from browser binding behavior.

**Deliverable:** A focused test, expected and recorded actual assertions, and an evidence note describing execution status.

**Completion and verification:** Record test discovery and execution, then compare direct controller assertions with HTTP response and rendered-HTML checks.

**Challenge (optional):** Add an integration test that exercises real GET binding while retaining fixture-owned database cleanup.

## Exercise: evaluate a pagination alternative

**Purpose and expected outcome:** Practice selecting an architectural trade-off instead of changing technology by default.

**Prerequisites and SRS ties:** Assume the public catalog grows to a very large number of Events while preserving chronological order and a unique tie-breaker. Related SRS ties: FR-SRH-009 and NFR-PER-002: bounded catalog navigation and server-side resource use.

**Task:** Compare the existing offset approach with a cursor/keyset approach in a decision table covering direct page access, Last navigation, query cost, behavior under concurrent inserts, URL design, and implementation complexity. Make a recommendation for CEMS today and one for a larger activity feed.

**Hints or checkpoints:** Keyset navigation trades arbitrary page numbers and Last links for stable cursors; compare against the documented UX, not just query speed.

**Deliverable:** A concise decision record with explicit assumptions, at least one counterargument, and a recommended next experiment. No production refactor is required.

**Completion and verification:** Justify both recommended choices using explicit UX, concurrency, and resource assumptions.

**Challenge (optional):** Propose measurable performance evidence needed before changing the public query design.

# Troubleshooting Common Problems

| Symptom | Likely cause / diagnosis | Correction / verification |
| --- | --- | --- |
| Form submission ignores search or Category | Compare `name="search"` and `name="categoryId"` to action parameter names; inspect URL | Match names, use GET, and keep filter values in the View Model |
| Draft, Cancelled, or past records appear | Inspect predicate grouping, especially accidental `OR` outside the text condition | Start with the Published/upcoming base query, then append narrowing conditions |
| One filter erases the other | Check whether a branch rebuilds `query` from `_context.Events` | Reassign each optional `Where` onto the same composed query |
| Filtering unexpectedly happens in memory | Find an early `ToListAsync()` or `AsEnumerable()` | Keep translatable filters, order, and bounds in the SQL query |
| Text search differs by letter case | Inspect SQL Server column/database collation and actual SQL | Document and test collation behavior; do not assume `.Contains` is universally case-insensitive |
| Dropdown empty or selection lost | Check separate Category query, selected `CategoryId`, and View Model type | Load all Categories for options and reflect request state in Razor |
| Razor error says the model type is incompatible | Index View or existing regression still expects `List<Event>` | Update Index View to `EventSearchViewModel` and use `.Events` in assertions |
| Total count equals only the page size | Locate `CountAsync()` relative to `Skip`/`Take` | Count the filtered matching set **before** page selection |
| Page results shuffle among equal start times | Check for missing `ThenBy(Id)` | Include a unique tie-breaker and test equal timestamps |
| Next or Last loses criteria | Inspect the generated anchor URL in browser tools | Include `search`, `categoryId`, `pageSize`, and destination `page` |
| Changing a filter keeps the previous page | Inspect GET form for `name="page"` | Remove page from the form; only navigation links set the page |
| Invalid `pageSize` becomes default or returns 200 | Check query-key presence, nullable binding, response status | Reject explicitly invalid sizes and preserve HTTP 400 for the shared View |
| Invalid `page=abc` appears not to fail | Check SRS §11 and controller normalization | This fallback is required for `page`; do not confuse it with strict `pageSize` handling |
| No matches produce a blank page or links | Check the `Events.Count == 0` branch and `TotalPages` | Render a no-results message and omit navigation and range text |
| Styling overflows or focus is invisible | Verify stylesheet request, responsive rules, and browser inspection | Load project CSS, preserve visible focus, and test keyboard/narrow viewport |
| SQL Server regression fixture refuses to run | Check LocalDB availability, the optional `master` connection override, database permissions, and migration errors | Fix the endpoint/configuration; never use an existing application database as a test target |
| Practice rows persist after the lesson | Check `Program.cs` changes and inspect the Development-only data | Restore startup code and use the restricted, exact-count cleanup procedure |

# Checking Your Understanding

<details><summary>Why should a read-only search use request state instead of changing stored Event data?</summary>

A search selects a representation of existing records. Putting the criteria in the GET request makes the result reproducible without creating domain fields or modifying the database. The server still validates those criteria.

</details>

<details><summary>What invariant prevents a text match from exposing a Draft Event?</summary>

The query starts with `Status == Published` and a future start time. The optional title/location and Category conditions are appended with AND semantics; they never replace the eligibility rule.

</details>

<details><summary>What is the difference between a query description, a matching count, and a materialized page?</summary>

The composed query describes database work. A count executes a database operation returning the size of the full matching set. A materialized page is the bounded ordered subset retrieved into memory after `Skip` and `Take`.

</details>

<details><summary>Why can ordering by start time alone give unpredictable pagination?</summary>

Several Events may share the same start time, leaving their relative order unspecified. A unique secondary key creates a deterministic sequence for an unchanged dataset, although concurrent data changes can still move offset boundaries.

</details>

<details><summary>Is infinite scrolling the same as cursor pagination?</summary>

No. Infinite scrolling is an interface pattern for revealing more content; cursor/keyset pagination is a retrieval method. Numbered links or scrolling can in principle use different retrieval methods.

</details>

<details><summary>How should `/Events?page=abc&pageSize=25` differ from `/Events?page=2&pageSize=37`?</summary>

The first uses the SRS's forgiving page-number rule and displays page 1 at size 25. The second supplies an unsupported page size, so the controller returns HTTP 400 with the Invalid Page Size View.

</details>

<details><summary>Why does checking `pageSize` only against a dropdown not validate it?</summary>

A visitor can construct a URL manually or change form submissions. The server must distinguish an omitted value from an explicitly supplied malformed or unsupported value and enforce the permitted set before querying.

</details>

<details><summary>What information belongs in the results View Model instead of the Event entity?</summary>

Active search and Category criteria, list of Category choices, page-size options, current page, total match count, and total page count are properties of the response, not the persisted Event domain object.

</details>

<details><summary>Why does a new form submission reset to page 1 while Next retains the current criteria?</summary>

The GET form omits the page field; its next request defaults to page 1. The Next link includes a destination page and copies the active search, Category, and validated page size into its URL.

</details>

<details><summary>How do you distinguish a valid empty result from an invalid request?</summary>

A valid query with no matches returns HTTP 200 with a clear no-results message and an empty pagination area. An explicitly invalid page-size parameter is HTTP 400 with an explanatory View.

</details>

<details><summary>Which behaviors require an HTTP-level check in addition to a direct controller regression test?</summary>

Controller-level checks establish action results and response models. HTTP and browser checks establish binding, routing, generated links, Razor output, keyboard operation, and responsive layout.

</details>

<details><summary>What information makes a repository checkpoint reproducible?</summary>

Reviewed changes, intended staged files, a descriptive commit reference, environment and fixed-data assumptions, actual test outcomes, and an evidence record that notes unexecuted checks and excludes secrets.

</details>

# Summary

You **analyzed read-only retrieval contracts** by distinguishing persisted domain records, request criteria, and response state. You **designed composable query constraints** that preserve an eligibility invariant while independently supplied conditions narrow a result. These concepts help prevent accidental disclosure and keep business rules separate from presentation concerns.

You **evaluated query execution and resource boundaries** by locating the point where a query runs and data becomes materialized. You also **designed reliable pagination** through total-result counting, bounded selection, deterministic ordering with a tie-breaker, and recognition of how concurrent changes can still affect offset pages. Comparing cursor and offset retrieval against numbered and incremental navigation strengthened your ability to choose techniques by trade-off rather than habit.

You **specified and validated input contracts** by separating omission, malformed values, valid ranges, fallback rules, and explicit rejection. You **modeled response-specific state** without changing persistent entities, and connected client forms to navigational links that preserve or reset state deliberately. You also considered accessible and responsive interaction as observable behavior.

You **used controller, HTTP, and browser checks** to examine retrieval logic, request binding, rendered responses, and accessible interaction. Finally, you **applied traceable incremental development** through reviewable changes, environment safeguards, recorded evidence, and version-control practices. Each skill transfers to other languages, frameworks, persistence layers, and deployment environments.

# Checkpoint

**Inherited baseline:** The `net10.0` CEMS MVC application already had a SQL Server-backed `Event`, `Category`, and organizer model, an `InitialCemsCatalog` migration, a guarded Development initializer, dependency-injected `TimeProvider`, a public `/Events` list of all Published upcoming Events in chronological order, an Event Details flow with controlled 404 behavior, a shared Razor layout and stylesheet, and SQL Server-backed catalog tests.

**New in this lesson:**

- The public `EventsController.Index()` now receives optional `search`, `categoryId`, `page`, and `pageSize` inputs, composes title/location and Category filters onto the unchanged public eligibility predicate, obtains a complete matching count, normalizes requested pages, and returns an ordered, database-bounded page. `EventSearchViewModel` contains page results, all Category choices, current criteria, available 10/25/50 page sizes, and pagination metadata. The results View adds a GET form, retained filter controls, result ranges, an explicit empty state, and First/Previous/Next/Last navigation with preserved state.
- An `ErrorPageViewModel` and shared `BadRequest` Razor View provide an explanatory HTTP 400 response for invalid explicit page sizes. Project CSS adds responsive form and pagination styling; the existing Details action remains unchanged. The authored regression suite contains fourteen controller cases and three HTTP cases using fixture-owned SQL Server databases; the repository includes the HTTP-test dependencies and a Lesson 3 traceability record.

**Resulting baseline for next lesson:** CEMS offers a server-rendered, publicly accessible searchable, Category-filterable, paginated catalog with a bounded user-selectable page size and an existing Details workflow. Domain classes, database schema, and Development seed definitions have not changed. The application is ready to add identity and authorization to protected requests while retaining its public retrieval contract. Track registration availability and the previously Published Cancelled-event direct-link policy in the SRS traceability record.

# Next Lesson Preview

Lesson 4 introduces **Identity, Authentication, Roles, and Protected Access**. You will evolve the existing application so users can register accounts, log in, maintain an authenticated session, log out, and reach server-protected functionality. The public catalog's GET request state, database predicates, result models, navigation, and regression baseline continue to work for anonymous users while the request pipeline gains reliable identity and authorization checks for later Participant, Organizer, and Administrator workflows.

# Appendix

These design alternatives extend the retrieval and interface principles practiced in the CEMS catalog.

## Pagination strategies and result consistency

### Offset pages and keyset continuation

CEMS retrieves a page by applying `Skip((page - 1) * pageSize)` and `Take(pageSize)` after sorting by `(StartDateTime, Id)`. This **offset** approach supports a visitor requesting page 4 directly, moving to the last page using a matching count, and sharing a conventional page-number URL. Its cost can grow with the number of skipped database rows: even though only a few records are returned, the database may need to find and traverse the preceding ordered records.

**Keyset pagination** instead remembers the ordering keys of the last displayed record. For ascending `(StartDateTime, Id)`, a conceptual next-page filter is:

```text
Event.StartDateTime > lastStart
OR (Event.StartDateTime = lastStart AND Event.Id > lastId)
```

The filter is applied after the same public eligibility and optional search/Category predicates. The query uses the same ordering and page limit. In a suitably indexed dataset, this can avoid walking a deep offset. But it does not naturally jump to arbitrary numbered pages or reveal a meaningful final page without additional work. A cursor can encode the last ordered keys; production designs must also consider validation, tampering, and whether the cursor should bind to the current filters.

| Decision dimension | Offset paging used in CEMS | Keyset continuation |
| --- | --- | --- |
| Direct numbered URL | Natural (`page=4`) | Requires an established cursor chain or extra lookup |
| First/Last links | Straightforward with a matching count | First is easy; Last requires a distinct reverse-order strategy |
| Deep retrieval | Large offsets can cost more | Often efficient when an ordered index supports continuation |
| Concurrent inserts/deletes | Records may shift between numbered pages | Continuation avoids some earlier-page shifts but does not create a snapshot |
| Implementation complexity | Count and page arithmetic | Multi-key comparisons and cursor lifecycle |

For example, sort these Events by `(StartDateTime, Id)`:

| Event | Start | ID |
| --- | --- | --- |
| A | 09:00 | 11 |
| B | 09:00 | 12 |
| C | 10:00 | 13 |
| D | 11:00 | 14 |

With page size 2, offset page 2 returns C and D. A keyset request after B instead selects rows satisfying `(StartDateTime > 09:00) OR (StartDateTime = 09:00 AND Id > 12)`, then takes two. An Event inserted at 08:30 before the second offset request may shift offset page 2 to include B again; the same insertion does not shift a forward continuation after B.

### Ordering is not a snapshot

Suppose two Events begin at 10:00. Ordering only by start time leaves their relative positions unspecified; including unique `Id` makes the result deterministic for a fixed dataset. Yet if another matching Event is inserted before the next requested offset, a visitor can see a duplicate across pages or miss a record. Keyset continuation can mitigate this kind of displacement for forward traversal, but edits to sorting fields and visibility eligibility can still change what is returned. A stronger cross-request snapshot contract would need additional data-version or transaction design and has different operational costs.

## Database search and query-performance alternatives

### Matching rules and database collation

The implemented predicate searches title or location using database-translatable substring matching, with the public visibility rule always applied first. Exact matching compares complete values; prefix matching tests whether a string begins with a term; substring matching finds the term anywhere in the field. They answer different user questions and may use database indexes differently. Case and accent behavior can depend on SQL Server collation: a C# assumption about letter case does not establish database matching semantics. Compare actual requests and generated SQL when investigating unexpected results.

Including `Description` would increase the searchable surface beyond the minimum SRS title/location requirement. That can be useful for discovery, but longer free-form fields may change relevance and performance expectations. Full-text search can offer token-aware retrieval and relevance options that simple substring operations do not, at the cost of additional schema, indexing, and maintenance decisions. Select this strategy when language-aware matching, indexed search, and ranking justify the additional database infrastructure.

| Search mode | Matching example for `Library` | Typical implication |
| --- | --- | --- |
| Exact | Title equals `Library` | Narrow equality comparison |
| Prefix | Title begins with `Library` | Often compatible with an ordered index, depending on collation and expression |
| Substring | Title contains `Library` anywhere | May require scanning more values, especially with a leading wildcard |
| Full-text | Search indexed words and language-aware terms | Requires a full-text index and explicit matching semantics |

### Returned rows versus total query work

`Take(10)` bounds the number of Event objects returned. Evaluate count, sort, and text-search costs independently. The matching count may examine all eligible matches, while ordering, joins, and substring matching can require work that is not visible in the final ten rows. Performance investigation should start with representative data volumes, captured SQL, execution plans, useful indexes, and measurements under known conditions. Use representative measurements when evaluating whether additional indexing or search strategies are warranted.

## Pagination interface and navigation alternatives

A pagination *interface* controls how a visitor navigates results; a pagination *query strategy* selects data. These can be combined in different ways. Conventional numbered pages provide bookmarkable destinations and clear progress through a finite result set. Previous/Next controls provide a simpler sequential path. First/Last links add shortcuts when total pages are known. A **Load more** control appends another batch to the current screen, while infinite scrolling requests more content as the user advances.

| Interface | Strength | Consideration |
| --- | --- | --- |
| Numbered pages with First/Last | Direct destinations and explicit position | Requires page counts and understandable boundaries |
| Previous/Next only | Small navigation surface | Harder to jump directly to a distant result |
| Load more | Retains previous cards in view | Accumulating cards and history/state restoration need design |
| Infinite scrolling | Fluid continuous exploration | Keyboard use, footer reachability, announcements, and resuming position need care |

A server-rendered page can use offset or keyset queries. JavaScript can append server-rendered HTML fragments while retaining server-managed page rendering. Likewise, infinite scrolling does not mandate cursor queries. For CEMS, GET forms and ordinary links fit the stated server-rendered architecture, accessible navigation, and the requirement to preserve selected criteria in shareable URLs. Any alternative should continue to communicate loading, empty states, current selection, and navigation results to assistive technology.

Consider a visitor searching for `Library`: numbered pages offer a direct URL to page 3; Previous/Next offers sequential exploration; Load More retains the first batch on screen; infinite scrolling needs a deliberate mechanism for returning to a prior position. Compare keyboard navigation and browser history for each experience before selecting an interface.

## Response-model and retrieval-architecture alternatives

### Entity data versus projected read models

CEMS uses an `EventSearchViewModel` to represent a whole catalog response: Events, Category options, criteria, totals, page choices, and navigation state. Those page-wide fields do not belong on a persisted Event entity. One implementation can nevertheless pass Event entities as the rows inside that View Model, as the current lesson does. An alternative is to project database query results into small read models containing only fields displayed by a card. Such projections can reduce transferred columns and presentation coupling, but require explicit mappings and care when later page requirements change.

A catalog response can be assembled through either of these flows:

```text
Controller → compose EF query → EventSearchViewModel → Razor
Controller → catalog query service → EventSearchViewModel → Razor
```

In the second design, the query service owns reusable retrieval rules while the controller remains responsible for HTTP inputs and responses.

### Controller composition versus a query service

A small controller can validate inputs, compose a bounded query, build the View Model, and return a View while remaining understandable. As retrieval behavior becomes shared by additional endpoints or more complex, a dedicated query service can centralize eligibility predicates and page-result construction. The trade-off is an extra abstraction, service registration, and a contract that itself needs tests. Choose a boundary because it clarifies responsibilities or removes meaningful duplication, not merely because an additional layer is available.

These two choices are independent: a controller can project read models directly, and a query service can still return entities if that is appropriate. In either organization the public eligibility invariant, deterministic ordering, and untrusted-input policy remain intact.
