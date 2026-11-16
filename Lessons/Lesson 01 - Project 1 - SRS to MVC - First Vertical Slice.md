# **Project — Campus Events**
## **Lesson 1 — SRS to MVC: First Vertical Slice**

Revision: 2026.10.25.10

# Table of Contents

- [Review of Last Lesson](#review-of-last-lesson)
- [Learning Objectives](#learning-objectives)
- [SRS Requirements and Use Cases](#srs-requirements-and-use-cases)
- [Guided Feature Preview](#guided-feature-preview)
- [Guided Design and Coding](#guided-design-and-coding)
	- [Install Visual Studio Code and C# extensions](#install-visual-studio-code-and-c-extensions)
	- [Check the installed .NET SDK](#check-the-installed-net-sdk)
	- [Create the MVC project](#create-the-mvc-project)
	- [Tour the generated solution files](#tour-the-generated-solution-files)
	- [Remove unused frontend frameworks](#remove-unused-frontend-frameworks)
	- [Implement the first Events vertical slice](#implement-the-first-events-vertical-slice)
		- [Create the Events controller without a model](#create-the-events-controller-without-a-model)
		- [Render a static Events view](#render-a-static-events-view)
		- [Move the temporary title into the controller](#move-the-temporary-title-into-the-controller)
		- [Add Events link to the shared navigation](#add-events-link-to-the-shared-navigation)
		- [Trace the request and make a bounded change](#trace-the-request-and-make-a-bounded-change)
	- [Document and commit the working MVC slice](#document-and-commit-the-working-mvc-slice)
- [Guided Discussion](#guided-discussion)
	- [Server-rendered pages and the HTTP contract](#server-rendered-pages-and-the-http-contract)
	- [Application startup and composition root](#application-startup-and-composition-root)
		- [Service registration and dependency injection](#service-registration-and-dependency-injection)
		- [Middleware and the request pipeline](#middleware-and-the-request-pipeline)
		- [Route-to-endpoint mapping](#route-to-endpoint-mapping)
	- [Routing, endpoints, and rendering](#routing-endpoints-and-rendering)
	- [Semantic HTML, styling, and accessibility](#semantic-html-styling-and-accessibility)
	- [Verification, validation, and reproducible evidence](#verification-validation-and-reproducible-evidence)
- [Guided Verification and Validation](#guided-verification-and-validation)
	- [Automated](#automated)
		- [Test — `Index_ReturnsViewWithExpectedTemporaryTitle`](#test--index_returnsviewwithexpectedtemporarytitle)
	- [Manual](#manual)
	- [SRS Traceability Matrix Update with Evidence](#srs-traceability-matrix-update-with-evidence)
- [Guided Exercises](#guided-exercises)
	- [Exercise: independently trace another page](#exercise-independently-trace-another-page)
	- [Exercise: diagnose a contract failure](#exercise-diagnose-a-contract-failure)
	- [Exercise: challenge a test assertion](#exercise-challenge-a-test-assertion)
- [Troubleshooting Common Problems](#troubleshooting-common-problems)
- [Checking Your Understanding](#checking-your-understanding)
- [Summary](#summary)
- [Checkpoint](#checkpoint)
- [Next Lesson Preview](#next-lesson-preview)
- [Appendix](#appendix)
	- [From requirements to design decisions](#from-requirements-to-design-decisions)
	- [Rendering and navigation strategies](#rendering-and-navigation-strategies)
		- [Where page markup is produced](#where-page-markup-is-produced)
		- [When HTML is generated and refreshed](#when-html-is-generated-and-refreshed)
		- [How visitors move between pages](#how-visitors-move-between-pages)
		- [How interactivity reaches the browser](#how-interactivity-reaches-the-browser)
		- [Choosing compatible techniques](#choosing-compatible-techniques)

# Review of Last Lesson

The starting point is the approved **Campus Events Management System (CEMS) Software Requirements Specification (SRS)** from Lesson 0. That specification describes the intended product, its user roles, business rules, system qualities, and imposed technology constraints. It establishes *what the finished application must do* without prescribing its complete design or implementation.

You will turn that requirements baseline into the first executable architectural slice from scratch. You will build an executable request/response path that connects browser navigation, controller-supplied data, Razor rendering, and HTML output.

# Learning Objectives

By the end of the lesson, you should be able to:

1. **Classify** product requirements, imposed design constraints, and discretionary implementation decisions, and explain how each influences design.
2. **Trace** a browser request through routing, application logic, presentation, and the HTTP response while distinguishing the responsibilities of each stage.
3. **Explain and apply** separation of concerns and dependency injection to keep request handling, presentation, and supporting services independently understandable.
4. **Design and evaluate** a small end-to-end software slice whose input, processing path, output, and component contracts are observable.
5. **Compare** automated component checks with manual end-to-end observation, and identify what each can and cannot establish.
6. **Investigate** failures systematically using observable symptoms, request traces, diagnostics, and controlled changes.
7. **Apply** reproducible project organization, safe configuration management, and version-control practices to make development states secure, reviewable, and traceable.
8. **Evaluate** a simple interface using semantic structure, accessible interaction, and responsive presentation principles.

These objectives concern engineering skills you can apply regardless of language, framework, or platform. The specific tools used below provide a concrete environment in which to practice them.

# SRS Requirements and Use Cases

CEMS is a web application for campus Event discovery, registration, Organizer management, and Administration. The SRS is authoritative for required behavior; code examples presented here are only one way to implement it. For additional context on how the SRS requirements inform this and future lessons, see [Appendix: From requirements to design decisions](#from-requirements-to-design-decisions) in the appendix.

| SRS reference | Required behavior / constraint | Lesson contribution |
| --- | --- | --- |
| **SRS §3.1–3.2** | Use ASP.NET Core, C#, MVC, Razor, server-rendered pages, HTML/CSS without a CSS framework, Git/GitHub, and automated verification while retaining architectural boundaries | Establish the MVC solution, project-owned frontend, version-control workflow, and introductory test |
| **SRS §9.2** | Standard HTTP/HTTPS browser interaction with server-rendered HTML | Establish a request to `/Events` that returns an HTML page |

**Lesson-level acceptance contract:** A browser request to `/Events` should reach the expected controller action; the action should supply the current temporary title to a Razor View; the server should return readable HTML with working navigation and styling; and an automated test should check the action's narrow result contract.

# Feature Preview

Starting from the approved CEMS SRS, build a server-rendered application with shared navigation and a working Events page. This is the target browser experience for the completed vertical slice.

A Visitor opens the application’s root URL and sees the Home page with a shared header, navigation, and footer. Selecting **Privacy** opens the Privacy page. Selecting **Events** requests `/Events` and displays the **Campus Events** heading and a temporary event preview with **Student Research Showcase** as its title and **Student Center** as its location. The pages retain readable styling and keyboard-accessible navigation at desktop and narrow widths.

![Target CEMS Events page with shared navigation and a temporary Student Research Showcase preview.](images/lesson-01/events-controller-changed-title.png)

The request and response follow this path:

```text
Visitor selects Events in the browser
    ↓ HTTP GET /Events
Server chooses a request handler and prepares a temporary title
    ↓ Render the Events template within the shared page layout
Browser receives HTML, displays the title, and loads the CSS
```

# Design and Coding

## Install Visual Studio Code and C# extensions

The first step is to prepare your development environment and create a standard ASP.NET Core MVC project. If VS Code is not installed, download it from the official [Visual Studio Code website](https://code.visualstudio.com/Download) and complete the installation.

1. Open VS Code and select **Extensions** from the Activity Bar (or press **Ctrl+Shift+X**).
2. Search for and install **C# Dev Kit** by Microsoft. Installing **C# Dev Kit** normally installs other required extensions automatically.
3. Install the **.NET 10 SDK** from the official [.NET download page](https://dotnet.microsoft.com/en-us/download/dotnet). The editor extensions provide C# tooling.
4. Restart your computer to ensure the installation and environment changes take effect.

## Check the installed .NET SDK

Open a terminal and run:

```cmd
dotnet --version
```

This displays the installed SDK version (for example, `10.0.100`). Use the **.NET 10 SDK** for the commands and project examples throughout this project.

If the command fails, follow these steps:

1. Visit Microsoft's official [.NET download page](https://dotnet.microsoft.com/en-us/download/dotnet).
2. Select the **.NET 10 SDK** for your operating system.
3. Run the installer and complete the installation.
4. Restart your computer to ensure the installation and environment changes take effect.
5. Open terminal and run `dotnet --version` again to verify the installation.

Then verify that the ASP.NET Core MVC template is available:

```cmd
dotnet new list
```

Once these commands work, your development environment is ready to create the CEMS project.

## Create the MVC project

1. Create an empty `CampusEvents` folder, or clone an empty `CampusEvents` GitHub repository. Treat that folder as the **solution root** for the remaining terminal commands. When cloning, use the repository’s actual HTTPS URL in place of the example below.

```cmd
git clone https://github.com/<username>/CampusEvents.git
```

2. Open the **`CampusEvents` solution-root folder** in VS Code and open its integrated terminal. Check that the terminal is located in that folder before creating projects.
3. Initialize the repository. **NOTE:** If you cloned the GitHub repository, skip this step:

```cmd
git init
```

4. Create the solution and application/test projects from the solution root:


```cmd
git switch -c lesson-01
dotnet new gitignore
dotnet new sln
dotnet new mvc -o src/CampusEvents
dotnet new xunit -o tests/CampusEvents.Tests
dotnet sln add src/CampusEvents
dotnet sln add tests/CampusEvents.Tests
dotnet add tests/CampusEvents.Tests reference src/CampusEvents
```

This establishes the names that all the lessons will use:

- **Solution:** `CampusEvents`
- **Main project:** `src/CampusEvents`
- **Test project:** `tests/CampusEvents.Tests`
- **Root namespaces:** `CampusEvents` and `CampusEvents.Tests`

5. Build and test the generated projects:

```cmd
dotnet build
dotnet test
```

A successful build establishes a baseline for diagnosing subsequent changes.

6. Start the application by typing:

```cmd
dotnet run --project src/CampusEvents
```

in the terminal. Open the HTTP(S) URL printed in the console; the port number may differ between runs. As an alternative, use **Run > Start Debugging (F5)** in VS Code after configuring a C# launch profile for `CampusEvents`. Select that profile to start the debugger.

The generated MVC template now runs through the development server. You can inspect its initial Home page and navigation before customizing the CEMS interface.

![CEMS Home page first run before frontend framework removal.](images/lesson-01/events-first-run.png)

Leave the web browser window open. Stop the server by pressing **Ctrl+C** in the terminal or by pressing the stop button (□) in the debug controls.

## Tour the generated solution files

Inspect the files that establish request handling, View rendering, project configuration, and browser assets.

```text
CampusEvents/
├── src/
│		└── CampusEvents/
│				├── Controllers/
│				│		└── HomeController.cs
│				├── Models/
│				│		└── ErrorViewModel.cs
│				├── Views/
│				│		├── Home/
│				│		│		└── Index.cshtml
│				│		├── Shared/
│				│		│		└── _Layout.cshtml
│				│		└── _ViewImports.cshtml
│				├── wwwroot/
│				│		└── css/
│				│				└── site.css
│				├── appsettings.Development.json
│				├── appsettings.json
│				├── CampusEvents.csproj
│				└── Program.cs
├── tests/
│		└──  CampusEvents.Tests/
│				├── CampusEvents.Tests.csproj
│				└── UnitTest1.cs
├── .gitignore
└── CampusEvents.slnx
```

The application and test project are separate projects within one solution.

#### File: `Program.cs`

This is the application startup file and *composition root*. It registers services, builds the app, configures the request pipeline (middleware), and maps routes to endpoints.

#### Folder: `Controllers/`

Controllers contain action methods that can respond to routed requests.

#### Folder: `Models/`

This folder contains the data models used by the application.

#### Folder: `Views/`

Views are Razor templates that render HTML on the server.

#### File: `Views/_ViewStart.cshtml`

Selects the shared layout for Views.

#### File: `Views/_ViewImports.cshtml`

Enables common Razor imports and the MVC Tag Helpers used by links.

#### File: `Views/Shared/_Layout.cshtml`

The shared layout contains page structure used across multiple Views, including site navigation.

#### Folder: `wwwroot/`

This folder contains static files such as CSS, JavaScript, and images.

#### Files: `appsettings.json` and `appsettings.Development.json`

These files hold configuration. Normal non-secret configuration can live here, but passwords, API keys, tokens, and production credentials should not be committed to source control.

## Remove unused frontend frameworks

CEMS uses semantic HTML, a shared Razor layout, and `wwwroot/css/site.css`. Remove the generated frontend packages so the application uses the project-owned stylesheet and semantic HTML:

```cmd
rmdir /s /q src\CampusEvents\wwwroot\lib\bootstrap
rmdir /s /q src\CampusEvents\wwwroot\lib\jquery
rmdir /s /q src\CampusEvents\wwwroot\lib\jquery-validation
rmdir /s /q src\CampusEvents\wwwroot\lib\jquery-validation-unobtrusive
del /q src\CampusEvents\wwwroot\js\site.js
del /q src\CampusEvents\Views\Shared\_ValidationScriptsPartial.cshtml
```

#### File: `src/CampusEvents/wwwroot/css/site.css`

Replace everything with:

```css
:root {
	font-family: system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
	line-height: 1.5;
	color: #1f2328;
	background: #ffffff;
}

* {
	box-sizing: border-box;
}

body {
	min-height: 100vh;
	margin: 0;
	display: flex;
	flex-direction: column;
}

a {
	color: #174ea6;
}

a:focus-visible,
button:focus-visible,
input:focus-visible,
select:focus-visible,
textarea:focus-visible {
	outline: 3px solid #7aa7e8;
	outline-offset: 2px;
}

.site-shell {
	width: min(72rem, calc(100% - 2rem));
	margin-inline: auto;
}

.site-header {
	border-bottom: 1px solid #d0d7de;
	background: #f6f8fa;
}

.site-header-inner {
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	gap: 1rem;
	padding-block: 0.85rem;
}

.site-brand {
	color: #1f2328;
	font-weight: 700;
	text-decoration: none;
}

.site-nav {
	flex: 1;
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	justify-content: space-between;
	gap: 1rem;
}

.site-nav-list {
	display: flex;
	flex-wrap: wrap;
	gap: 0.8rem;
	margin: 0;
	padding: 0;
	list-style: none;
}

.site-nav-link {
	display: inline-block;
	padding: 0.35rem 0;
}

.page-content {
	flex: 1;
	padding-block: 1.5rem 2.5rem;
}

.site-footer {
	border-top: 1px solid #d0d7de;
	padding-block: 1rem;
	background: #f6f8fa;
}

@media (max-width: 48rem) {
	.site-header-inner,
	.site-nav {
		align-items: flex-start;
	}

	.site-nav {
		flex-direction: column;
	}
}
```

The CSS provides readable defaults, maximum content width, a shared header and footer, visible keyboard focus, and navigation that can wrap or stack. CEMS uses its own CSS class names and stylesheet.

#### File: `src/CampusEvents/Views/Shared/_Layout.cshtml`

Replace everything with:

```cshtml
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
			<a class="site-brand" asp-controller="Home" asp-action="Index">CampusEvents</a>

			<nav class="site-nav" aria-label="Primary navigation">
				<ul class="site-nav-list">
					<li><a class="site-nav-link" asp-controller="Home" asp-action="Index">Home</a></li>
					<li><a class="site-nav-link" asp-controller="Home" asp-action="Privacy">Privacy</a></li>
				</ul>
			</nav>
		</div>
	</header>

	<main class="site-shell page-content">
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

`asp-append-version="true"` gives the stylesheet URL a content-based version token (e.g., `/css/site.css?v=abc123`). When the file changes, the version changes too (e.g., `/css/site.css?v=xyz789`), telling the browser to download the updated file instead of using an outdated cached copy (i.e., *cache busting*).

The initial navigation contains **Home** and **Privacy**, both backed by the template's existing controller. The `asp-controller` and `asp-action` Tag Helpers generate ordinary `href` hyperlinks pointing to the chosen endpoints `Home/Index` and `Home/Privacy`.

`@RenderBody()` inserts the requested page's View into the main region. Notice that the same navigation and stylesheet surround every page, while the individual View supplies only its page content.

#### File: `src/CampusEvents/Views/Home/Index.cshtml`

Replace everything with: 

```cshtml
@{
	ViewData["Title"] = "Home";
}

<h1>Campus Events Management System</h1>

<p>Welcome to the Campus Events Management System. Browse the Events preview to explore campus activities.</p>
```

#### File: `src/CampusEvents/Views/Home/Privacy.cshtml`

Replace everything with: 

```cshtml
@{
	ViewData["Title"] = "Privacy";
}

<h1>Privacy</h1>

<p>For questions about campus privacy practices and personal information, consult your institution’s official privacy guidance.</p>
```

#### File: `src/CampusEvents/Views/Shared/Error.cshtml`

Replace: 

```cshtml
@model ErrorViewModel

@{
	ViewData["Title"] = "Error";
}

<h1>Something went wrong</h1>

<p>We could not complete your request.</p>

@if (Model.ShowRequestId)
{
	<p><strong>Request ID:</strong> <code>@Model.RequestId</code></p>
}
```

Build, and run it:

```cmd
dotnet build
dotnet run --project src/CampusEvents
```

Open the HTTP(S) address printed by the application. Check Home and Privacy, including navigation, headings, stylesheet, and narrow-width layout.

![CEMS Home page with project-owned styling and shared Home and Privacy navigation.](images/lesson-01/home-shared-shell.png)

## Implement the first Events vertical slice

### Create the Events controller without a model

#### File: `src/CampusEvents/Controllers/EventsController.cs`

Create:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CampusEvents.Controllers;

public class EventsController : Controller
{
	private readonly ILogger<EventsController> _logger;

	public EventsController(ILogger<EventsController> logger)
	{
		_logger = logger;
	}

	public IActionResult Index()
	{
		_logger.LogInformation("Handling request for the CEMS Events page.");
		return View();
	}
}
```

The controller suffix and inheritance from `Controller` allow MVC to discover and use the class according to ordinary conventions. The public `Index()` method is an **action**, `IActionResult` represents a returned result, and `View()` asks MVC to render the conventional View without supplying a model yet.

The Events controller receives a logger via constructor dependency injection. The constructor requests an `ILogger<EventsController>`. The framework's dependency-injection system provides it when constructing the controller. The runtime's application startup registers the services and assembles the request pipeline. The application configures the logging provider, while the controller uses the injected logger through a stable interface. `LogInformation(...)` helps establish that the action actually ran. 

### Render a static Events view

#### File: `src/CampusEvents/Views/Events/Index.cshtml`

Create `Events` folder, then `Index.cshtml`:

```cshtml
@{
	ViewData["Title"] = "Events";
}

<h1>Campus Events</h1>

<p>Explore campus activities and discover where events take place.</p>

<section>
	<h2>Temporary Event Preview</h2>
	<p>Campus Welcome Mixer</p>
	<p>Student Center</p>
</section>
```

Build and run it:

```cmd
dotnet build
dotnet run --project src/CampusEvents
```

Open `/Events` directly by adding that path to the application URL printed in the terminal. The page should show **Campus Events**, **Temporary Event Preview**, **Campus Welcome Mixer**, and **Student Center**.

![Static CEMS Events page showing Campus Welcome Mixer before controller-supplied data.](images/lesson-01/events-static-view.png)

The informational controller log should appear in the terminal. The log identifies the controller action that handled the request; the browser receives the resulting HTML.

![Informational controller log in the terminal.](images/lesson-01/events-static-view-log.png)

### Move the temporary title into the controller

Now deliberately establish a simple **controller/View model contract**: the controller supplies a `string`; the View declares it expects `string`; the View renders the supplied value.

#### File: `src/CampusEvents/Controllers/EventsController.cs`

Replace only the `Index()` method:

```csharp
public IActionResult Index()
{
	_logger.LogInformation("Handling request for the CEMS Events page.");

	var featuredEventTitle = "Campus Sustainability Fair";

	return View(model: featuredEventTitle);
}
```

#### File: `src/CampusEvents/Views/Events/Index.cshtml`

Replace everything with:

```cshtml
@model string

@{
	ViewData["Title"] = "Events";
}

<h1>Campus Events</h1>

<p>Explore campus activities and discover where events take place.</p>

<section>
	<h2>Temporary Event Preview</h2>
	<p><strong>@Model</strong></p>
	<p>Student Center</p>
</section>
```

These similarly spelled tokens do **different jobs**:

| Syntax | Meaning |
| --- | --- |
| `model:` in `View(model: featuredEventTitle)` | C# named argument supplying a **value** to the View result |
| `@model string` | Razor directive declaring the View's expected **type** |
| `@Model` | Razor property exposing the supplied **value** during rendering |

> **Naming note:** `model:`, `@model`, and `@Model` are three different things. Think of this as a small ASP.NET naming hangover — as if the framework designers had one drink too many before choosing three nearly identical labels — or, perhaps more charitably, as the result of naming important framework mechanisms before anyone had enough coffee.

Follow the **value** from start to finish:

```text
featuredEventTitle = "Campus Sustainability Fair"
    ↓ return View(model: featuredEventTitle) passes a string
Views/Events/Index.cshtml @model directive declares the model type as a string
    ↓ @Model property receives the supplied string
Razor inserts the text into server-generated HTML
```

Build and run it:

```cmd
dotnet build
dotnet run --project src/CampusEvents
```

The displayed title should now be **"Campus Sustainability Fair"** in bold.

![CEMS Events page displaying Campus Sustainability Fair supplied by the controller.](images/lesson-01/events-controller-title.png)

### Add Events link to the shared navigation

Typing `/Events` now works, but visitors should also be able to discover the page through the shared layout.

#### File: `src/CampusEvents/Views/Shared/_Layout.cshtml`

Insert this item **between Home and Privacy** inside `<ul class="site-nav-list">`:

```cshtml
<li><a class="site-nav-link" asp-controller="Events" asp-action="Index">Events</a></li>
```

Rebuild and click on **Events**. The Events page should display **"Campus Sustainability Fair"** and share the same header, footer, and stylesheet as the other pages.

### Trace the request and make a bounded change

Use the application log, debugger breakpoint, or browser Network panel to observe different parts of the same request:

- **Server log:** Shows *"Handling request for the CEMS Events page."* in the terminal.
- **Breakpoint:** Establishes that `EventsController.Index()` executed and exposes the local `featuredEventTitle` value.
- **Document response:** Browser developer tools → **Network** panel shows the requested URL, HTTP status, response type, and rendered HTML.
- **Browser page:** Shows whether the expected text and styling are actually visible and the Events link works.

The logger identifies the selected action, the breakpoint exposes its local values, and the Network panel shows the HTTP exchange.

Now, change the title from **"Campus Sustainability Fair"** to **"Student Research Showcase"**. Rebuild and reload `/Events`. Notice that even though the View's markup stayed the same the displayed title changed when the supplied data changed. This is the architectural reason for moving the title out of the View before introducing more complex Event information.

![CEMS Events page displaying Student Research Showcase supplied by the controller.](images/lesson-01/events-controller-changed-title.png)

**Expected contract:** The focused regression assertion uses the final title `Student Research Showcase`; keep the controller and test expectations aligned.

**Expected observable result:** The Events link should appear between Home and Privacy on all three pages, and `/Events` should render **"Student Research Showcase"** without editing the View again. At a narrow viewport, the navigation should wrap or stack while preserving access to the Events link.

![Narrow-width CEMS Events page with responsive navigation and Student Research Showcase preview.](images/lesson-01/events-final-mobile.png)

**Checkpoint:** `/Events` connects routing, a controller-supplied string, Razor rendering, shared navigation, and CSS in one inspectable request/response slice.

## Document and commit the working MVC slice

Create a root **`README.md`** with the application purpose, prerequisites, commands, and currently available pages:

````md
# Campus Events Management System (CEMS)

Course Project #1 — server-rendered ASP.NET Core MVC application.

## Prerequisites

- .NET 10.x SDK

## Run from the solution folder

```cmd
dotnet build
dotnet test
dotnet run --project src/CampusEvents
```

If you want to use HTTPS run:

```cmd
dotnet dev-certs https --trust
```

once, from then on only this:

```cmd
dotnet run --project src/CampusEvents --launch-profile https
```

If dependency restore reports a vulnerability, inspect the package advisory. Update the affected direct dependency to a compatible patched version, then rebuild and retest.

```cmd
dotnet package update --vulnerable --project src/CampusEvents
```

Open the URL printed by the application and select Events.

## Current scope

The application serves Home, Privacy, and Events pages through MVC, with shared navigation, project-owned CSS, and a controller-provided event title. It also includes an xUnit test project.
````

# Discussion

The working CEMS request-to-page slice provides a concrete example of the boundaries between request routing, controller logic, rendering, shared structure, and browser behavior.

## Server-rendered pages and the HTTP contract

A browser sends an **HTTP request** containing a method, URL, protocol version, headers, and sometimes a message body. A server replies with a protocol version, status code, response headers, and an optional message body.

For example, an HTTP request may contain:

```http
GET /Events HTTP/1.1
Host: www.cems.com
```

For CEMS pages, a successful response contains server-generated HTML in its message body:

```http
HTTP/1.1 200 OK
Content-Type: text/html; charset=utf-8

<!DOCTYPE html>
<html lang="en">
...
</html>
```

CEMS uses **server-side rendering (SSR)**: MVC and Razor generate HTML on the server. As a **multi-page application (MPA)**, CEMS ordinarily loads a new HTML document when the browser follows a page link. The browser also requests referenced stylesheets and other static resources. By comparison, **client-side rendering (CSR)** uses browser JavaScript to construct or update the page. The [Appendix: Rendering and navigation strategies](#rendering-and-navigation-strategies) section in the appendix compares these strategies and their possible combinations.

For the first Events page, the intended flow is:

```text
Browser GET /Events
	↓
HTTP request enters server pipeline
	↓
Router selects a controller action
	↓
Controller action prepares a temporary title
	↓
Razor View combines markup and the title
	↓
Server returns an HTML response
	↓
Browser renders the page and requests its CSS
```

The server executes C# and Razor and sends the resulting HTML to the browser, which renders the document and follows its hyperlinks.


## Application startup and composition root

#### File: `src/CampusEvents/Program.cs`

Open `Program.cs`. The exact generated file can vary slightly across .NET versions, but the Lesson 1 baseline is equivalent to the following:

```csharp
// Services registration

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

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

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

`Program.cs` is more or less divided into three sections:

1. Services registration
2. Middleware configuration
3. Route-to-endpoint mapping

A useful first mental model is:

```text
startup
│
├─ register services
│		↓
│	dependency-injection container knows what can be provided
│
├─ configure the request pipeline
│		↓
│	ordered middleware processes requests
│
└─ map routes to endpoints
		↓
	 requests can reach controller actions
```

### Service registration and dependency injection

A component should declare the services it needs rather than construct all supporting infrastructure itself. **Dependency injection** lets the application supply those dependencies. This supports clearer responsibilities and often makes isolated testing easier.

ASP.NET Core uses a built-in **dependency injection container**. Applications register services during startup, and framework components can receive those services when they are created. 

```csharp
var builder = WebApplication.CreateBuilder(args);
```

automatically registers ASP.NET Core's default logging services in the built-in DI container, including:

- `ILogger<T>`: Used for logging within controllers and services.
- `ILoggerFactory`: Used to create logger instances.
- `ILoggerProvider`: The interface implemented by logging providers.

ASP.NET Core registers logging services in the application host and resolves `ILogger<T>` for controllers through dependency injection.

When a controller declares `ILogger<EventsController>` in its constructor, the framework can supply a compatible logger; that way the controller does not construct a logging provider or require an explicit logger registration of its own. 

The following statement registers the services required for MVC controllers and Razor views:

```csharp
builder.Services.AddControllersWithViews();
```

Think of startup as creating a registry of available dependencies. A component declares what it needs; the application supplies it. This separation makes it easier to reason about components.

### Middleware and the request pipeline

These statements participate in configuring how requests move through the application:

```csharp
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
```

Middleware components form an ordered request-processing pipeline. Each component can handle a request or pass it onward; components that pass control forward can process the response as it returns. The order in `Program.cs` determines which components run before an endpoint and which can act on the response.

Conceptually:

```text
Incoming request
	↓
Middleware pipeline
		↓
	Exception handler
		↓
	HSTS
		↓
	HTTPS redirection
		↓
	Static-file handling (/css/site.css is served here)
		↓
	Routing
		↓
	Authorization evaluates endpoint requirements
		↓
Selected endpoint (action → Razor View → HTML response)
	↓
Response travels back through the pipeline
```

The configured middleware components have these responsibilities:

- `UseExceptionHandler("/Home/Error")`, handles unhandled exceptions from later middleware by re-executing the request at `/Home/Error`.
- `UseHsts()`, adds the HTTP Strict Transport Security (HSTS) header to eligible HTTPS responses, directing browsers to use HTTPS on subsequent visits.
- `UseHttpsRedirection()` redirects incoming HTTP requests to their HTTPS equivalents, helping ensure that communication between the browser and server is encrypted.
- `UseStaticFiles()` or `MapStaticAssets()` enables the application to serve static files, such as CSS, JavaScript, and images, directly from the `wwwroot` directory without requiring controller actions to handle those requests.
- `UseRouting()` matches the incoming request to an application endpoint, such as an MVC controller action, based on the configured routing rules.
- `UseAuthorization()` runs after routing so it can evaluate any authorization requirements associated with the selected endpoint.

Serve static stylesheets through static-file middleware, and place authorization after routing so it can evaluate the selected endpoint’s policies. Preserve this ordering when modifying the pipeline.

### Route-to-endpoint mapping

This mapping is central to the lesson:

```csharp
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
```

The route pattern has three pieces:

```text
controller / action / optional id
```

It also provides defaults:

- controller: `Home`
- action: `Index`

With these defaults, `/` expands to `/Home/Index` and selects `HomeController.Index()`, while `/Home/Privacy` selects `HomeController.Privacy()`. The controller and action segments select a request handler; MVC then locates the Razor View returned by that action.

When an action method returns `View()`, the View-location convention normally finds the Razor file based on controller and action names. For the existing Home page:

```text
Browser GET /
    ↓ conventional route: Home / Index
HomeController.Index()
    ↓ returns View()
Views/Home/Index.cshtml
    ↓ rendered inside Views/Shared/_Layout.cshtml
HTML document
    ↓
Browser → fetches wwwroot/css/site.css
```

## Routing, endpoints, and rendering

Design URLs around the resource or user action they represent, then assign request handling and page rendering to distinct components. The `/Events` implementation illustrates this separation: a controller prepares page data, a View renders content, the shared layout supplies common structure, and CSS controls presentation.

When designing another page, decide which URL identifies the resource, which action handles the request, and which View presents the result. Shared navigation links should resolve to application routes rather than to the physical `.cshtml` source paths. For a deeper comparison of server and browser rendering, navigation mechanisms, and related delivery strategies, see [Appendix: Rendering and navigation strategies](#rendering-and-navigation-strategies).

## Semantic HTML, styling, and accessibility

Use structural HTML to express document meaning and CSS to express presentation. A page should have a meaningful top-level heading, identifiable navigation, a main-content region, readable links, and visible keyboard focus. A reusable layout prevents each page from duplicating the shared structure.

Responsive design is not merely shrinking text. Navigation must remain discoverable, content must retain usable spacing, and normal pages should not require horizontal scrolling at common mobile widths. When validating the interface, inspect the rendered layout, keyboard focus, and navigation at desktop and mobile widths.

## Verification, validation, and reproducible evidence

**Verification** asks whether implementation conforms to a defined technical contract. **Validation** asks whether the resulting system behavior meets the intended need. A common way to express it is:

> "Verification checks if you made the product right, and validation checks if you made the right product."
 
Use action-level tests to check controller results and browser scenarios to check routing, HTML rendering, and accessible navigation.

# Verification and Validation

## Automated

| Test / case | Behavior or boundary | Expected assertion / evidence |
| --- | --- | --- |
| New — `Index_Returns​View​With​Expected​Temporary​Title` (1 expected case) | Direct controller View contract for the temporary Events title | `ViewResult` with exactly `Student Research Showcase`. |

### Test — `Index_ReturnsViewWithExpectedTemporaryTitle`

A `[Fact]` method defines one non-parameterized test case. The inventory below describes the controller test authored for this lesson.

**Purpose:** Verify that invoking the Events action directly returns a View result carrying the expected temporary title.

The xUnit project template may contain a generated placeholder test. Delete or rename it:

```cmd
ren tests\CampusEvents.Tests\UnitTest1.cs tests\CampusEvents.Tests\EventsControllerTests.cs
```

#### File: `tests/CampusEvents.Tests/EventsControllerTests.cs`

Create:

```csharp
using CampusEvents.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusEvents.Tests;

public class EventsControllerTests
{
	[Fact]
	public void Index_ReturnsViewWithExpectedTemporaryTitle()
	{
		// Arrange: provide a simple logger without starting a web server.
		var controller = new EventsController(NullLogger<EventsController>.Instance);

		// Act: invoke the controller action directly.
		var result = controller.Index();

		// Assert: inspect the action result and its model.
		var view = Assert.IsType<ViewResult>(result);
		var title = Assert.IsType<string>(view.Model);
		Assert.Equal("Student Research Showcase", title);
	}
}
```

This test follows the **Arrange–Act–Assert (AAA)** pattern, a widely used convention in software testing that organizes a test into three steps:

1. **Arrange:** Prepare the objects and dependencies needed for the test. Here, create the controller with a no-op logger (a logger that discards messages).
2. **Act:** Execute the behavior being tested by invoking the controller's action method.
3. **Assert:** Verify that the action produces the expected result type and exact string model.

The no-op logger keeps the test simple and isolated, eliminating the need for a mocking package or a running web server.

From the solution root, inspect the discovered tests before running the suite:

```cmd
dotnet test --list-tests
dotnet test
```

**Expected outcome:** The test runner discovers the test and reports a pass when the controller returns a `ViewResult` with the model `Student Research Showcase`.

## Manual

Perform these checks. Record date and actual result for each.

| Scenario | Prerequisites, inputs, and steps | Expected result and evidence |
| --- | --- | --- |
| Root page | Start MVC application; request `/` | Home content and shared header, navigation, and footer render |
| Events route | Select **Events** or request `/Events` directly | Events View renders `Student Research Showcase` in the temporary preview |
| HTML contract | Open browser DevTools → Network; reload `/Events`; inspect document | Successful HTML response, not Razor source code |
| Static CSS | Inspect the `/css/site.css` request and rendered page | Stylesheet loads; content width, header, and footer styles apply |
| Shared navigation | Follow Home → Events → Privacy → Home links | Each link reaches its intended page |
| Keyboard access | Press **Tab** and **Shift+Tab** through the links, then **Enter** on Events | Links are operable without a mouse and focus remains visible |
| Narrow viewport | Inspect website at a typical mobile width (in *Developer Tools* select *Toggle device toolbar* or press CTRL+SHIFT+M) | Normal page content remains readable without horizontal scrolling |
| Invalid action | Request `/Events/NoSuchAction` | Normal routing does not find an action; a controlled 404 is expected |
| Diagnostic trace | Trigger `/Events`; inspect log or breakpoint in `Index()` | Controller action is reached and the title value can be observed |

For browser-side accessibility checks, inspect the rendered `<nav aria-label="Primary navigation">`, `<main>`, and heading structure. For diagnostics, place a breakpoint in `EventsController.Index()` or inspect the information-level log.

After correcting a failed scenario, repeat the corresponding check and record its actual outcome.

## SRS Traceability Matrix Update with Evidence

Maintain the traceability matrix at the end of the SRS. It connects the implemented solution and tests to SRS references and observed verification results.

The statuses presented below are provisional; enter observed results and revise each status after running the checks.

| Requirement / Reference | Implementation / Artifact | Verification / Evidence | Status | Notes |
| --- | --- | --- | --- | --- |
| SRS §3.1‑3.2 | `Program.cs`, MVC project, `wwwroot/css/site.css`, `tests/CampusEvents.Tests`, Git repository | Not executed; record build, test, and staged-diff results | **PARTIAL** | MVC architecture contributes to the broader design constraints; record remaining SRS work |
| SRS §9.2 | `GET /Events` → controller → Razor → HTML | Not executed; record response status and `Content-Type` from browser Network tools | **IMPLEMENTED—UNVERIFIED** | Update to `VERIFIED` when the relevant HTML response has been observed |

**Evidence context:** Run `dotnet build` and `dotnet test`, record the test results, and compare the manual inspector output with the expected scenarios. Both are necessary to evidence that requirements have been implemented and verified.

Confirm that your project builds, passes all tests, and runs without errors.

Stage and commit:

```cmd
git add .
git commit -m "Lesson 01: Implemented and Verified"
```

**NOTE:** Never stage or commit source code that does not build, fails a test, or runs with errors.

# Exercises

## Exercise: independently trace another page

**Purpose and expected outcome:** Explain a second browser request from URL through routing, request handling, template discovery, and HTTP response. Your trace should identify each boundary using actual project files and an observable response.

**Task:** Trace `/Home/Privacy` from the browser URL through its controller action, View-location convention, shared layout, and generated HTML. Inspect the document response in the browser Network panel.

**Challenge:** Compare this response with the HTTP behavior expected from a redirect action.

## Exercise: diagnose a contract failure

**Purpose and expected outcome:** Isolate a controller/View contract mismatch by moving from a browser symptom to the code boundary responsible.

**Task:** Temporarily replace `@model string` with an incompatible type `@model int` in `src/CampusEvents/Views/Events/Index.cshtml`. Request `/Events`, inspect the Development error and logs, then restore the original declaration.

**Challenge:** Design a request-level check for the observed View-rendering failure.

## Exercise: challenge a test assertion

**Purpose and expected outcome:** Interpret failing assertions and show how controlled test inputs support repeatable evidence.

**Task:** Temporarily change the expected title in `tests/CampusEvents.Tests/EventsControllerTests.cs` to an incorrect value. Run `dotnet test`, inspect the assertion failure, restore `Student Research Showcase`, and run the test again.

**Challenge:** Suppose that the title is officially updated. Change the test so it does not fail.

# Troubleshooting Common Problems

Investigate from the observable symptom toward the relevant boundary. Check the actual URL, terminal output, route, file location, View model type, and staged changes before making broad edits.

| Symptom | Likely cause / diagnosis | Correction / verification |
| --- | --- | --- |
| `/Events` returns 404 | Controller/action name or conventional route mismatch | Confirm `EventsController.Index()` and default route; request `/Events` again |
| View-not-found error | Missing or misnamed `Views/Events/Index.cshtml` | Correct folder and filename capitalization; rebuild and reload |
| View model type exception | Action passes `string`, View declares incompatible `@model` | Restore `@model string`; verify with browser and test |
| Literal/missing title | Confusion among `model:`, `@model`, and `@Model` | Trace value from `View(model: ...)` into `@Model`; reload |
| Events navigation does not work | Tag Helpers disabled or wrong `asp-controller` / `asp-action` | Inspect `_ViewImports.cshtml` and rendered `href`; follow link |
| Page has no CEMS styling | Incorrect stylesheet URL or static-file middleware | Inspect `/css/site.css` in Network and `UseStaticFiles()` in `Program.cs`; reload |
| Port differs from an example | Local development endpoints are dynamically configured | Use the URL printed by the running server |
| Solution/project not found | Terminal is in inner project or incorrect directory | Change directory to solution root; add `--project src/CampusEvents` to `dotnet` command |
| Test is not discovered | Missing test package, incorrect test attributes, or test project configuration | Confirm `[Fact]` on the test method, inspect the test project packages, and rerun `dotnet test` |
| Test fails compilation | Missing project reference or C# compilation errors | Check `.csproj` and run `dotnet add tests/CampusEvents.Tests reference src/CampusEvents` if project reference is missing |
| Test fails an assertion | Controller-supplied title differs from the expected string | Restore `Student Research Showcase` and rerun the affected test |
| Logger output is absent | Log filtering or the action was not executed | Inspect the information-level logging statement and configured log level; request `/Events` again |
| Git tracks generated files such as `obj` and `bin` folders | Missing ignore pattern or files tracked before an ignore rule was added | Inspect `.gitignore`, correct the patterns, and remove unwanted tracked artifacts from the index; keep secrets out of commits |
| Push fails or remote already exists | Remote not set (started solution from an empty folder and not from a GitHub repository clone) | Create CampusEvents repository in GitHub, then run `git remote add origin <github-repo-url>`; change `add` to `set-url` if remote already exists |

Avoid “fixes” that alter unrelated application behavior. After correcting a failure, rerun the smallest affected check and any existing regression tests.

# Checking Your Understanding

<details><summary>How do product requirements, design constraints, and implementation choices differ?</summary>

Product requirements specify observable behavior; constraints limit permissible designs; implementation choices select an approach within those limits.

</details>

<details><summary>Why is routing to an action different from locating a View?</summary>

Routing maps an HTTP request to code that can handle it. View-location conventions locate a rendering template **after** an action requests a View result. Route selection can succeed even when its View is missing.

</details>

<details><summary>What does the browser receive after a server-rendered request?</summary>

It receives an HTTP response containing HTML generated by the server; the browser renders that HTML and associated styles.

</details>

<details><summary>What distinction exists between `model:`, `@model`, and `@Model`?</summary>

`model:` is a named C# argument that supplies a value to `View(...)`; `@model` declares the Razor View's expected type; `@Model` reads the supplied value during server rendering.

</details>

<details><summary>How does dependency injection improve the controller's design?</summary>

The controller declares required supporting services without constructing their infrastructure. This separates responsibilities and allows a test to substitute an appropriate dependency, such as a no-op logger.

</details>

<details><summary>Which aspects of the solution should be tested automatically versus manually?</summary>

Automate repetitive, deterministic checks that are costly to perform manually, such as logic, integrations, edge cases, and regressions. Manually assess aspects requiring human judgment, such as usability, visual quality, accessibility, and intuitive workflows.

</details>

<details><summary>When is a project ready to be committed to Git?</summary>

When the project builds successfully, all tests pass, and the application runs without errors.

</details>

<details><summary>How do automated tests support software verification and requirements traceability?</summary>

Automated tests use assertions to compare actual results with expected results. For example, a test might verify that requesting a nonexistent ID returns an HTTP 404 response. Tests pass when expectations are met and fail otherwise. They can be rerun as **regression tests** to detect previously working functionality that breaks after code changes. Linking test cases and their results to requirements in a **Requirements Traceability Matrix (RTM)** helps track which requirements have been implemented, tested, and successfully verified.

</details>

# Summary

You learned how to translate software requirements into an architectural design while distinguishing what an application must do from how you choose to implement it. You traced how an HTTP request travels from the browser through routing and controller execution to a view that generates the response. You also explored the distinct responsibilities of the composition root, controllers, and views, including service registration, dependency injection, endpoint routing, request handling, model binding, validation, and presentation. Finally, you learned how dependency injection allows components to use services without having to construct or manage them.

You also learned how automated tests check specific behaviors, while manual testing helps identify problems that automated tests may miss. You practiced finding and fixing errors and explored how clear HTML structure, keyboard navigation, and responsive design improve the user experience.

Finally, you learned how to organize project files, manage configuration safely, commit changes regularly, and document evidence to make software easier to develop, test, and maintain. These practices are useful across different programming languages, frameworks, and development environments.

# Checkpoint

**Inherited baseline:** Lesson 0 established the approved CEMS SRS, including actors, system behavior, business rules, quality requirements, and architectural constraints.

**New in this lesson:**

- **Application:** A running MVC application renders Home, Privacy, and Events through conventional routing, shared layout, and project-owned CSS. `EventsController.Index()` supplies `Student Research Showcase` to a Razor View.
- **Testing and traceability:** An xUnit test project includes the Events controller contract test, alongside a maintained SRS traceability record.
- **Repository:** A solution containing the application and test projects, a `.gitignore`, a setup README, and reviewed Git commits establishes a reproducible development checkpoint.

# Next Lesson Preview

Lesson 2, **Relational Data and the Public Event Catalog**, introduces relational entities, Entity Framework Core migrations, Development seed data, and queries for Published upcoming Events. You will display persisted Event records in a public catalog and a Details page.

# Appendix

## From requirements to design decisions

A **functional requirement** describes a behavior users or systems need. A **non-functional requirement** describes a quality or condition under which behavior must work. A **design constraint** restricts the available technologies or architectural choices. An **implementation decision** chooses a specific approach within those constraints.

For example, *“visitors can browse Published upcoming Events”* is required behavior, and *“use server-rendered MVC and Razor”* is a design constraint. Integrating additional frameworks or libraries or employing architectural or design patterns, as well as, selecting which folder structure, file name and organization, variable naming conventions to follow — as long as they are not specified in the SRS — are considered implementation decisions. These categories help you identify which choices must satisfy the approved specification and which choices you can justify through engineering judgment.

Translate each SRS requirement into a design by identifying the actor, triggering input, observable result, relevant constraints, and evidence needed to decide whether the behavior works. Keep that connection with the SRS visible as the project expands so feature work does not drift from the approved product scope.

## Rendering and Navigation Strategies

CEMS uses **server-side rendering (SSR)** and **full-page navigation**. When a user clicks **Events**, the browser requests `/Events`, the server generates HTML, and the browser displays the page.

Other strategies and related techniques are available depending on the application's requirements.

### Rendering Strategies

Rendering determines where and how page content is generated.

| Strategy | Short Description / Related Techniques | Advantages / When to Use | Disadvantages / When to Avoid |
|---|---|---|---|
| **Server-Side Rendering (SSR)** | The server generates HTML and sends it to the browser. **Streaming SSR** sends HTML in parts as they become ready. | Good for data-driven pages, traditional web applications, and straightforward navigation. Streaming can improve perceived loading speed. | Requires server processing for each uncached request. |
| **Client-Side Rendering (CSR)** | JavaScript generates page content in the browser, often using frameworks such as React or Angular. | Good for highly interactive applications with frequent screen updates. | Depends heavily on JavaScript; initial loading and state management can be more complex. |
| **Static-Site Generation (SSG)** | HTML pages are generated before users request them. **Incremental regeneration** can update generated pages periodically or when triggered. | Fast and efficient for content that rarely changes. | Less suitable for personalized or frequently changing content. |
| **Hybrid Rendering** | Combines server-generated HTML with browser-based interactions. Related techniques include **hydration**, **islands architecture**, **selective hydration**, **resumability**, and **progressive enhancement**. | Useful when pages need both readily available content and interactive features. Islands limit JavaScript to selected components. | Adds complexity because rendering, interactivity, and state must be coordinated. |

### Navigation Strategies

Navigation determines how the browser updates the displayed content when users follow links or perform actions.

| Strategy | Short Description / Related Techniques | Advantages / When to Use | Disadvantages / When to Avoid |
|---|---|---|---|
| **Full-Page Navigation** | The browser requests and loads a complete HTML document using ordinary links or forms. | Simple, reliable, accessible, and well suited to traditional websites. | Reloads the document during navigation. |
| **Client-Side Navigation** | JavaScript changes the displayed page without reloading the entire document. **Client-side routing**, **prefetching**, and **view transitions** are related techniques. | Useful for highly interactive applications that need smooth transitions and persistent browser state. | Requires additional code to manage navigation history, accessibility, and errors. |
| **Partial-Page Updates** | Only a selected section of the page is updated. **HTML-over-the-wire** sends server-generated HTML fragments; other approaches use JSON and JavaScript. | Efficient for filters, search results, and other localized updates. | Requires additional logic to update content and maintain accessibility. |

### Choosing a Strategy

Choose the simplest approach that meets the application's requirements. Strategies can be combined; for example, a server-rendered page can use JavaScript to update search results without reloading the entire page.

**For CEMS:** Server-side rendering and full-page navigation provide a straightforward foundation. Interactive features can be added later when needed.