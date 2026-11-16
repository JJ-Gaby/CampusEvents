# **Project: Campus Events**
## **Lesson 2 — Relational Data and the Public Event Catalog**

Revision: 2026.11.09.1

# Table of Contents

- [Review of Last Lesson](#review-of-last-lesson)
- [Learning Objectives](#learning-objectives)
- [SRS Requirements and Use Cases](#srs-requirements-and-use-cases)
- [Feature Preview](#feature-preview)
- [Design and Coding](#design-and-coding)
	- [Trace the existing data path](#trace-the-existing-data-path)
	- [Modeling CEMS data](#modeling-cems-data)
		- [Add `EventStatus`](#add-eventstatus)
		- [Add `ApplicationUser`](#add-applicationuser)
		- [Add `Category`](#add-category)
		- [Add the first version of `Event`](#add-the-first-version-of-event)
	- [Configure EF Core and the database management system](#configure-ef-core-and-the-database-management-system)
		- [Install the EF Core packages](#install-the-ef-core-packages)
		- [Database management system (DBMS)](#database-management-system-dbms)
		- [Create `ApplicationDbContext`](#create-applicationdbcontext)
		- [Configure `DefaultConnection`](#configure-defaultconnection)
		- [Register the `DbContext` in `Program.cs`](#register-the-dbcontext-in-programcs)
	- [Create and inspect the initial migration](#create-and-inspect-the-initial-migration)
		- [Check the EF CLI](#check-the-ef-cli)
		- [Create `InitialCemsCatalog`](#create-initialcemscatalog)
		- [Inspect the migration](#inspect-the-migration)
		- [Apply the migration](#apply-the-migration)
		- [Inspect the database](#inspect-the-database)
	- [Add Development seed data](#add-development-seed-data)
		- [Create the initializer](#create-the-initializer)
		- [Run the initializer only in Development](#run-the-initializer-only-in-development)
	- [Query Published upcoming Events](#query-published-upcoming-events)
		- [Replace the temporary Event value with a database query](#replace-the-temporary-event-value-with-a-database-query)
		- [Read the composed query from top to bottom](#read-the-composed-query-from-top-to-bottom)
		- [Execute and materialize the catalog query](#execute-and-materialize-the-catalog-query)
	- [Render persisted Events](#render-persisted-events)
		- [Style the catalog in `site.css`](#style-the-catalog-in-sitecss)
		- [Observe query composition and execution](#observe-query-composition-and-execution)
	- [Add an Event detail page](#add-an-event-detail-page)
		- [Add the controller action](#add-the-controller-action)
		- [Create the Razor view](#create-the-razor-view)
		- [Capacity and registration availability](#capacity-and-registration-availability)
	- [Link the catalog to Event Details](#link-the-catalog-to-event-details)
	- [Handle missing Events](#handle-missing-events)
- [Verification and Validation](#verification-and-validation)
	- [Automated](#automated)
		- [Prepare an isolated SQL server test database](#prepare-an-isolated-sql-server-test-database)
		- [Implement focused catalog and Details regression tests](#implement-focused-catalog-and-details-regression-tests)
		- [Run the suite and interpret results](#run-the-suite-and-interpret-results)
		- [Extend the regression coverage](#extend-the-regression-coverage)
	- [Manual](#manual)
	- [SRS Traceability Matrix Update with Evidence](#srs-traceability-matrix-update-with-evidence)
- [Discussion](#discussion)
	- [Persistence and separation of responsibilities](#persistence-and-separation-of-responsibilities)
	- [Relational identity and data integrity](#relational-identity-and-data-integrity)
	- [Entity models, migrations, and persisted schema](#entity-models-migrations-and-persisted-schema)
	- [Public eligibility, query composition, and execution](#public-eligibility-query-composition-and-execution)
	- [Time-dependent behavior and repeatability](#time-dependent-behavior-and-repeatability)
	- [Resource representation, HTTP behavior, and accessibility](#resource-representation-http-behavior-and-accessibility)
- [Exercises](#exercises)
	- [Exercise: diagram the persistence boundary](#exercise-diagram-the-persistence-boundary)
	- [Exercise: inspect a query without broadening access](#exercise-inspect-a-query-without-broadening-access)
	- [Exercise: extend the Development seed dataset safely](#exercise-extend-the-development-seed-dataset-safely)
	- [Exercise: challenge an automated result](#exercise-challenge-an-automated-result)
- [Troubleshooting Common Problems](#troubleshooting-common-problems)
	- [Diagnostic walkthroughs](#diagnostic-walkthroughs)
		- [`UseSqlServer` or `UseSqlite` is not recognized](#usesqlserver-or-usesqlite-is-not-recognized)
		- [`dotnet ef` is not recognized](#dotnet-ef-is-not-recognized)
		- [The SQL server connection fails](#the-sql-server-connection-fails)
		- [An EF command cannot find `DefaultConnection`](#an-ef-command-cannot-find-defaultconnection)
		- [EF Core SQL does not appear in the application output](#ef-core-sql-does-not-appear-in-the-application-output)
		- [The migration is empty](#the-migration-is-empty)
		- [Seed insertion fails](#seed-insertion-fails)
		- [`/Events` shows Draft or Cancelled Events](#events-shows-draft-or-cancelled-events)
		- [Category data fails in the catalog](#category-data-fails-in-the-catalog)
		- [Organizer data fails on the Event page](#organizer-data-fails-on-the-event-page)
		- [The Razor model does not match the controller](#the-razor-model-does-not-match-the-controller)
		- [Your new seed Event does not appear after editing `DbInitializer`](#your-new-seed-event-does-not-appear-after-editing-dbinitializer)
		- [An isolated SQL server test cannot start](#an-isolated-sql-server-test-cannot-start)
		- [The catalog or Details page has no CEMS styling](#the-catalog-or-details-page-has-no-cems-styling)
- [Checking Your Understanding](#checking-your-understanding)
- [Summary](#summary)
- [Checkpoint](#checkpoint)
- [Next Lesson Preview](#next-lesson-preview)
- [Appendix](#appendix)
	- [Relational persistence technologies and schema evolution](#relational-persistence-technologies-and-schema-evolution)
		- [SQL Server and SQLite as relational providers](#sql-server-and-sqlite-as-relational-providers)
		- [Schema changes across environments](#schema-changes-across-environments)
	- [Configuration and operational resource controls](#configuration-and-operational-resource-controls)
		- [Configuration precedence and secret handling](#configuration-precedence-and-secret-handling)
		- [Operational resource bounds and design alternatives](#operational-resource-bounds-and-design-alternatives)
		- [Isolated test databases and connection pooling](#isolated-test-databases-and-connection-pooling)
	- [Retrieval and persistence design alternatives](#retrieval-and-persistence-design-alternatives)
		- [Entity materialization versus projection](#entity-materialization-versus-projection)
		- [Local time, UTC, and offset-aware values](#local-time-utc-and-offset-aware-values)
		- [Offset and keyset retrieval](#offset-and-keyset-retrieval)

# Review of Last Lesson

Lesson 1 translated an approved requirements baseline into an observable request/response slice. You practiced separating routing, request handling, data passed to a view, and rendering; used dependency injection, semantic HTML, and project-owned CSS; and established a repository, a small test contract, and a working traceability record.

The existing `CampusEvents` application serves `/Events` through `EventsController.Index()` and a Razor View expecting one temporary `string` (`Student Research Showcase`). The sibling `CampusEvents.Tests` project checks that narrow controller contract. This lesson keeps the same application and projects while replacing the temporary value with relationally persisted Event records, a public catalog, and a Details page.

# Learning Objectives

By the end of this lesson, you should be able to:

1. **Design and justify** a small relational model using identities, one-to-many relationships, and referential-integrity rules.
2. **Compare** entity representations, declarative schema changes, and actual persisted database structures, and evaluate how to evolve them safely.
3. **Analyze** the boundary between query composition, execution, and materialization, including filtering, eager loading, deterministic ordering, and asynchronous I/O.
4. **Evaluate** which data belongs in a public result and distinguish filtering for eligibility from explicit limits on resource consumption.
5. **Design and assess** server-rendered resource views that preserve data/presentation separation, meaningful HTTP results, semantic structure, and responsive accessibility.
6. **Develop and interpret** reproducible automated and manual checks using controlled input data, fixed time, isolated environments, and truthful evidence records.
7. **Apply** safe configuration and incremental version-control practices so application and database changes remain reviewable and reproducible.

These abilities apply across relational databases, programming languages, web stacks, testing frameworks, and deployment environments.

# SRS Requirements and Use Cases

The approved **CEMS SRS v1.3.3** defines the required product behavior. Use its stable requirement identifiers to connect the relational implementation with the public catalog and its acceptance conditions.

| SRS reference | Required behavior / constraint | Lesson contribution |
| --- | --- | --- |
| **SRS §3.1–3.2; §4.1; §8.1–8.3** | Server-rendered MVC, relational persistence, required Event data, relationships, and durable integrity | Add SQL Server-backed entities, foreign keys, check constraints, migration, and Development seeds. |
| **UC-001; FR-SRH-001; BR-EVT-005** | Visitors browse Published upcoming Events | Query and render persisted eligible public Events. |
| **FR-SRH-002** | Present upcoming Events chronologically | Order by `StartDateTime`, then by unique `Id` to make ties deterministic. |
| **FR-SRH-003** | Open a published Event's Details page | Add Published Details query, Category/Organizer data, and a controlled 404 response. |
| **FR-SRH-004** | Show title, description, Category, location, schedule, Organizer, capacity, and current registration availability | Render persisted detail fields and configured capacity. |
| **FR-SRH-008** | Previously published Cancelled Events remain visible by direct link with their cancelled state | Document the distinct direct-link visibility rule in the SRS traceability record. |
| **BR-EVT-001, BR-EVT-003, BR-EVT-004** | Start precedes end; Capacity is positive; referenced Category exists | SQL Server check constraints and Category foreign key enforce these stored-data invariants. |
| **BR-CAT-001, BR-CAT-002** | Case-insensitive Category-name uniqueness; protect referenced Categories from deletion | Configure `DeleteBehavior.Restrict` for referenced Category rows. |
| **NFR-SEC-004–005; NFR-ACC-002–003; NFR-COM-002** | Protect secrets; avoid exposed diagnostics; enable semantic, keyboard-operable, responsive pages | Use protected Development configuration, controlled 404 handling, accessible HTML, and responsive CSS. |
| **NFR-PER-001–002** | Meet classroom performance expectations and perform search/filter/page selection in database | Compose the basic eligible-Events query before materialization. |
| **AC-001, AC-005, AC-008, AC-009, AC-010, AC-011, AC-013** | Final browse workflow, integrity, persistence, failures, tests, quality, setup | Develop reproducible setup, relational persistence, automated checks, and browser verification procedures. |

**Lesson-level acceptance contract:** A migrated SQL Server database stores related Event, Category, and organizer-reference rows; the public catalog retrieves only Published Events with a future start, sorted chronologically; a Published Event has a server-rendered Details page; an absent or non-Published Details resource produces an HTML 404; and reproducible checks can be executed against an instructor-approved isolated SQL Server instance. 


# Feature Preview

Lesson 1 already serves `/Events` with a temporary title and a static preview. After this lesson, a Visitor opens the same route and receives a database-backed list of Published Events that start after the application's current local time. Events appear chronologically; selecting a catalog link opens a Details page with its Category, Organizer, schedule, location, description, and configured capacity. The public list omits Draft, Cancelled, and already-started Events. A missing or non-public Details request returns a controlled 404 response.

With the representative Development seed data and while both eligible starts remain in the future, the target catalog displays **Library Research Night** and then **Career Networking Lunch**. Its content comes from persisted records rather than the temporary Lesson 1 string. The database schema and seeded records survive an ordinary application restart.

![Target database-backed Events catalog with two chronological public Events.](images/lesson-02/persisted-event-catalog.png)

![Target Published Event Details page with organizer and schedule.](images/lesson-02/published-event-details.png)

**Expected request path:** Browser GET `/Events` → controller builds a filtered, ordered database query → SQL Server returns qualifying Event records → Razor renders the catalog HTML. A Details link requests `/Events/Details/{id}` and either returns the selected public representation or HTTP 404. 

# Design and Coding

## Inspect the inherited solution

Inspect your existing ***Campus Events*** solution before making changes.

Make sure that you staged and commited the previous lesson:

```cmd
git show -s HEAD
```

**Expected output:** should contain "Lesson 01: Implemented and Verified".

Otherwise, build, test, run, stage, and commit:

```cmd
dotnet build
dotnet test
dotnet run --project src/CampusEvents
git add .
git commit -m "Lesson 01: Implemented and Verified"
```

Then, create a new branch:

```cmd
git switch -c lesson-02
```

## Trace the existing data path

Start at the existing `EventsController.Index()` and `Views/Events/Index.cshtml`. Note that `View(string)` currently crosses the controller/View boundary. Sketch the revised contract: a controller queries eligible persisted Events, passes a collection, and the View renders the records. A running route and stylesheet already exist; this lesson evolves that slice rather than scaffolding it again.

## Modeling CEMS data


Before EF Core can persist CEMS data, the application needs a model that expresses the concepts it cares about. In this lesson those concepts are an Event, its Category, its lifecycle status, and the user reference that owns or organizes it.

An **entity class** is a C# type that EF Core can map to relational data. The class expresses the application's object-oriented view of the concept; EF Core builds mapping metadata that connects these properties and relationships to tables, columns, keys, and constraints.

Scalar properties map to stored values, while navigation properties represent relationships among entities. The purpose of the model is to let C# code work with meaningful application concepts while EF Core handles the relational mapping.

### Add `EventStatus`


**Working directory for all terminal commands in this lesson:** the **outer `CampusEvents` solution root**, which contains `src/CampusEvents/` and `tests/CampusEvents.Tests/`. File references beginning with `src/CampusEvents/` refer to the MVC application. Use the approved .NET 10.0.x SDK and verify that both projects target `net10.0`. Run the prerequisite solution build and first test from Lesson 1 in your own environment before continuing. 

CEMS currently recognizes exactly three Event states:

- `Draft`
- `Published`
- `Cancelled`

Open a new file:

**File: `src/CampusEvents/Models/EventStatus.cs`**

```csharp
namespace CampusEvents.Models;

public enum EventStatus
{
	Draft,
	Published,
	Cancelled
}
```

An enum makes those states explicit in C#. `EventStatus.Published` communicates much more clearly than a numeric value such as `1`.

### Add `ApplicationUser`


Now create the small user model that provides Event ownership.

**File: `src/CampusEvents/Models/ApplicationUser.cs`**

```csharp
namespace CampusEvents.Models;

public class ApplicationUser
{
	public string Id { get; set; } = string.Empty;

	public string DisplayName { get; set; } = string.Empty;

	public ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
}
```

The `Id` uses a `string`, which matches the default key shape used by ASP.NET Core Identity. The model stores an organizer reference through a stable string key. It defines the ownership relationship used by the Event catalog and Details page.

`DisplayName` supplies the human-readable organizer name displayed with an Event.

`OrganizedEvents` is the collection navigation property for the one-to-many relationship between an `ApplicationUser` and the Events they organize. Initializing it with an empty `List<Event>` keeps the collection ready to use and avoids null checks when Events are added or loaded through Entity Framework Core.

Complete the `Event` class below to resolve the navigation-property type.

### Add `Category`


Create:

**File: `src/CampusEvents/Models/Category.cs`**

```csharp
namespace CampusEvents.Models;

public class Category
{
	public int Id { get; set; }

	public string Name { get; set; } = string.Empty;

	public ICollection<Event> Events { get; set; } = new List<Event>();
}
```

`Id` is the Category primary key. `Name` is the label users will see. `Events` is the one-to-many navigation that represents all Events assigned to the Category.

The collection is ***not*** one giant column in the Categories table. It represents a relationship that EF Core understands.

The one-to-many relationship will eventually have this shape:

```text
Categories
----------
Id = 1, Name = Academic

Events
------
Id = 10, CategoryId = 1, ...
Id = 11, CategoryId = 1, ...
```

The database expresses the relationship by storing `CategoryId` on each Event row. The `Category.Events` collection is the C# view of that same relationship from the Category side.

**NOTE**: The editor will mark `Event` as undefined for `Category` until you create the `Event` model (below).

### Add the first version of `Event`


Create:

**File: `src/CampusEvents/Models/Event.cs`**

```csharp
namespace CampusEvents.Models;

public class Event
{
	public int Id { get; set; }

	public string Title { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public string Location { get; set; } = string.Empty;

	public DateTime StartDateTime { get; set; }

	public DateTime EndDateTime { get; set; }

	public int Capacity { get; set; }

	public EventStatus Status { get; set; }

	public DateTime CreatedAt { get; set; }

	public int CategoryId { get; set; }

	public Category Category { get; set; } = null!;

	public string OrganizerId { get; set; } = string.Empty;

	public ApplicationUser Organizer { get; set; } = null!;
}
```

This model contains the Event information needed at this stage: identity, title, description, location, start and end date/time, capacity, status, creation time, category relationship, category, organizer relationship, and organizer.

Build the solution from the outer solution root before adding EF Core:

```cmd
dotnet build
```

A successful build confirms that these entity definitions compile. Next, map their relationships to the relational database.

## Configure EF Core and the database management system


**Entity Framework Core (EF Core)** maps CEMS entities to SQL tables, translates database-oriented LINQ operations into SQL, manages persistence, and materializes results as application objects.

A useful mental model is:

```text
CEMS entity objects and LINQ
		↓
ApplicationDbContext
		↓
EF Core model + query/change services
		↓
DBMS provider (SQL Server)
		↓
Database Management System (DBMS)
```

Each part has a distinct role:

- the **entity classes** describe CEMS data in C#
- `ApplicationDbContext` is CEMS's EF Core entry point and contains the model configuration
- **EF Core** uses that model to translate supported LINQ expressions into SQL
- the **DBMS provider** knows how EF Core should communicate with DBMS
- **DBMS** is the relational database engine that actually stores the records

These elements form a sequence: define entity structure, configure relational mappings, generate the migration, and apply the migration to create the database schema.

### Install the EF Core packages


CEMS uses the instructor-approved **.NET 10.0.x SDK**, and both existing project files target **`net10.0`**. Confirm `<TargetFramework>net10.0</TargetFramework>` in `src/CampusEvents/CampusEvents.csproj` and `tests/CampusEvents.Tests/CampusEvents.Tests.csproj`. Use EF Core **10.0.13** consistently for SQL Server and design-time support. The commands below run from the **outer solution root**:

```cmd
dotnet package add Microsoft.EntityFrameworkCore.SqlServer --project src\CampusEvents
dotnet package add Microsoft.EntityFrameworkCore.Design --project src\CampusEvents
dotnet package update --vulnerable --project src\CampusEvents
dotnet build
```

The SQL Server provider runs CEMS queries against SQL Server. The EF Core design package supports migration generation. The matching `dotnet-ef` 10.0.13 tool is installed in the EF CLI setup. Continue using the existing xUnit project, which references the application project.

### Database management system (DBMS)


CEMS uses **SQL Server** as its canonical relational provider. Windows students may use SQL Server Express LocalDB for Development; LocalDB is **Windows-only**. Students on macOS or Linux use an instructor-provisioned SQL Server endpoint reachable over the network. On supported x86-64 Linux hosts, an instructor may instead approve a dedicated Linux SQL Server installation/container. A macOS student, including one with an ARM-based computer, does not need to run a local SQL Server image.

Use an instructor-approved SQL Server endpoint for the Development application. On Windows, SQL Server Express LocalDB provides a local option. On other platforms, connect to the authorized SQL Server endpoint.


#### Install SQL server express


On Windows, inspect the Development LocalDB instance if installed:

```cmd
sqllocaldb info MSSQLLocalDB
```

If this is unavailable, install the LocalDB feature using the instructor-approved [SQL Server Express installer](https://www.microsoft.com/en-us/sql-server/sql-server-downloads). Verify the instance before applying a migration. Using the same LocalDB instance for Development and tests is acceptable: the fixture creates a different GUID-named database for each test and never drops `CampusEventsDb` or any other database it did not create. For macOS or Linux, obtain authorized SQL Server connection settings from the instructor instead of attempting `sqllocaldb`.


### Create `ApplicationDbContext`


`ApplicationDbContext` is where CEMS tells EF Core which entity sets belong to the current persistence model and where explicit mapping rules live. Think of the context as the application's EF Core working boundary.

Its `DbSet<T>` properties provide typed entry points for querying and saving entities. The context also supplies explicit relationship and constraint configuration.

Create the `Data` folder, then create:

**File: `src/CampusEvents/Data/ApplicationDbContext.cs`**

```csharp
using CampusEvents.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Data;

public class ApplicationDbContext : DbContext
{
	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
	{
	}

	public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

	public DbSet<Category> Categories => Set<Category>();

	public DbSet<Event> Events => Set<Event>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<ApplicationUser>(entity =>
		{
			entity.ToTable("AspNetUsers");

			entity.HasKey(user => user.Id);

			entity.Property(user => user.Id)
				.HasMaxLength(450)
				.ValueGeneratedNever();

			entity.Property(user => user.DisplayName)
				.IsRequired();
		});

		modelBuilder.Entity<Category>(entity =>
		{
			entity.Property(category => category.Name)
				.IsRequired();
		});

		modelBuilder.Entity<Event>(entity =>
		{
			entity.Property(eventItem => eventItem.Title)
				.IsRequired();

			entity.Property(eventItem => eventItem.Description)
				.IsRequired();

			entity.Property(eventItem => eventItem.Location)
				.IsRequired();

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

There are several important pieces here. Read this class as **model configuration**, not as a hand-written SQL schema. EF Core combines conventions with the explicit rules in `OnModelCreating(...)` to build its model of CEMS persistence.

Some things are conventional, for example, an `Id` property is naturally treated as a key in common EF Core models. Other decisions are explicit here because CEMS wants a particular database shape or behavior, such as the `AspNetUsers` table name, the organizer string-key configuration, restricted deletes, and the Event check constraints.


#### DbSet entry points


These properties give EF Core query/save entry points for the current entities:

```csharp
public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
public DbSet<Category> Categories => Set<Category>();
public DbSet<Event> Events => Set<Event>();
```

Each entity set can serve as a starting point for relational queries:

```csharp
_context.Users
_context.Categories
_context.Events
```

These entity sets support query composition through `Where(...)`, `OrderBy(...)`, and `Include(...)`, followed by execution when the result is needed.


#### Event → Category


This relationship:

```csharp
entity.HasOne(eventItem => eventItem.Category)
	.WithMany(category => category.Events)
	.HasForeignKey(eventItem => eventItem.CategoryId)
	.OnDelete(DeleteBehavior.Restrict);
```

means many Events can refer to one Category through `CategoryId`.


#### Event → Organizer


This relationship:

```csharp
entity.HasOne(eventItem => eventItem.Organizer)
	.WithMany(user => user.OrganizedEvents)
	.HasForeignKey(eventItem => eventItem.OrganizerId)
	.OnDelete(DeleteBehavior.Restrict);
```

means many Events can be organized by one user reference through `OrganizerId`.

The navigation pairs make the relationships convenient to traverse in C#. The foreign-key columns are what preserve the actual references in relational storage.

A **foreign key** protects referential integrity at the database level. Even if normal application code supplies valid IDs, the database constraint prevents an `Event` from being stored with a `CategoryId` or `OrganizerId` that does not correspond to an existing related row. This protects the data from bugs, scripts, manual database changes, or other code paths that might bypass normal application logic.

`DeleteBehavior.Restrict` prevents a referenced category or organizer row from being silently cascade-deleted along with Events. A **cascade delete** means that deleting one database row automatically causes related dependent rows to be deleted as well. Using `Restrict` requires those relationships to be handled explicitly instead of allowing a delete to remove related data automatically.


#### Database check constraints


The Event table includes:

```text
[StartDateTime] < [EndDateTime]
```

and:

```text
[Capacity] > 0
```

These constraints support the Event timing and positive-capacity business rules in persisted data.

The `AspNetUsers` table name and string-based organizer key establish a consistent relational identity for Event ownership.

### Configure `DefaultConnection`


Open **File: `src/CampusEvents/appsettings.Development.json`** and enable EF SQL command logging. Store credentials and connection details outside committed configuration:

```json
{
	"Logging": {
		"LogLevel": {
			"Default": "Information",
			"Microsoft.AspNetCore": "Warning",
			"Microsoft.EntityFrameworkCore.Database.Command": "Information"
		}
	}
}
```

For Windows Development, an example non-secret connection string is `Server=(localdb)\MSSQLLocalDB;Database=CampusEventsDb;Trusted_Connection=True;MultipleActiveResultSets=true`. For macOS/Linux, use the instructor-provided SQL Server Development endpoint, encryption and certificate settings, and credentials. The application reads the same `ConnectionStrings:DefaultConnection` key on every platform. **The dedicated SQL Server test connection is never stored under that key.**


#### Connection strings that contain sensitive information


Connection strings can include usernames, passwords, tokens, or host details. Keep them out of tracked `appsettings` files and code examples that may be committed. Even a local trusted-connection example is best configured through Development secrets to establish a consistent practice. A Development connection and a disposable test-instance connection serve different purposes and must not be interchangeable.


#### Secret manager


From the **outer solution root**, initialize the existing MVC project's Development secrets and supply the approved Development connection value:

```cmd
dotnet user-secrets init --project src\CampusEvents
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=CampusEventsDb;Trusted_Connection=True" --project src\CampusEvents
dotnet user-secrets list --project src\CampusEvents
```

The value shown is a Windows LocalDB Development example only. Replace it with the instructor-approved Development SQL Server endpoint on another platform. User Secrets are unencrypted local Development storage, not a production secret manager. Store the Development connection under `DefaultConnection` and keep credentials in the appropriate local configuration source.


#### System environment variables


Environment variables can also configure the application. Windows terminal example (current terminal process only):

```cmd
set "ConnectionStrings__DefaultConnection=Server=(localdb)\MSSQLLocalDB;Database=CampusEventsDb"
```

ASP.NET Core uses `__` as a configuration-key path separator, allowing the application to continue reading:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
```

Use secure instructor-provided secret injection for sensitive Development or test connections. Avoid printing connection strings or all environment variables to a shared log. For deployed applications, use the hosting platform's approved secret-management facility.


#### Configuration precedence in Development

Application configuration providers may override one another. Use User Secrets for the documented Development connection and environment variables or an approved hosting secret store where appropriate. Inspect the effective configuration source without printing credentials. Configuration providers have an explicit precedence order; compare the provider choices in the Appendix.

### Register the `DbContext` in `Program.cs`


ASP.NET Core dependency injection supplies controllers with a configured `ApplicationDbContext`, using the application's registered SQL Server connection.

The registration establishes this dependency path:

```text
code asks for `ApplicationDbContext`
		↓
ASP.NET Core dependency injection
		↓
uses `DefaultConnection` configuration value as `connectionString`
		↓
configures EF Core with `UseSqlServer(connectionString)`
		↓
constructs `ApplicationDbContext`
```

Controllers receive `ApplicationDbContext` through constructor injection, keeping database configuration in the application startup.

Open:

**File: `src/CampusEvents/Program.cs`**

At this point, before seed logic is added, use:

```csharp
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

app.Run();
```

The `AddDbContext` registration supplies database access through dependency injection, and `TimeProvider.System` supplies a shared application clock. Controllers receive these dependencies through their constructors rather than configuring SQL Server or creating clocks themselves.

CEMS currently stores Event dates and times as offset-free, application-local `DateTime` values. Capture one clock reading when making a time-sensitive decision:

```csharp
DateTime localNow = timeProvider.GetLocalNow().DateTime;
```

`GetLocalNow()` returns a `DateTimeOffset`. Its `.DateTime` keeps the provider-local **wall-clock time** and discards the offset (`DateTimeKind.Unspecified`), matching the existing SQL Server `datetime2` values. The application host must use the agreed campus/application time zone; a remote database host does not automatically change that interpretation. **For this classroom slice the particular time zone is an operational assumption, not yet an enforced application setting.** Offsets discarded by `.DateTime` cannot later be reconstructed from stored values; browser `datetime-local` submits an offset-free wall time. Lesson 9 will require an institution-approved zone/DST policy, explicit environment checks, and a migration analysis before changing the representation. Do not infer the zone from a developer laptop or test machine. Configure the application host to the course-approved campus time zone, then inspect `TimeZoneInfo.Local.Id` and the value of `TimeProvider.System.GetLocalNow()` to confirm the host’s effective setting. Create seed timestamps and fixed test timestamps under the same documented convention. Capture one reading per request so all time-dependent decisions share a single cutoff.

The conventional route remains unchanged:

```text
{controller=Home}/{action=Index}/{id?}
```

Build it:

```cmd
dotnet build
```

Do not create a migration until this model and configuration compile. Inspect the Git diff before staging: generated files and migration artifacts belong in the repository; secrets and any locally created database files do not.

## Create and inspect the initial migration


At this point, CEMS has C# entity classes and EF Core model configuration, but that does **not** mean DBMS already contains matching tables. The application model and the physical database schema are separate artifacts.

EF Core migrations provide a versioned bridge between them.

Keep these three states distinct:

```text
1. EF Core model
	 C# entities + ApplicationDbContext configuration

2. Migration
	 versioned instructions describing a schema change

3. Database schema
	 the tables, columns, keys, and constraints that actually exist in DBMS
```

A migration records how EF Core believes the relational schema should change to match the model at that point in the project. Creating a migration generates project files that describe the schema operations. Applying the migration executes the pending schema changes against the configured database.

The workflow is therefore:

```text
change/build EF Core model
		↓
dotnet ef migrations add ...
		↓
inspect generated migration
		↓
dotnet ef database update ...
		↓
DBMS schema changes
```

Editing an entity class changes the application model. Generating a migration records intended schema changes; applying the migration updates the database schema.

### Check the EF CLI


**Working directory:** the outer `CampusEvents` solution root. Keep the command-line migration tool in a solution-local tool manifest so its version is reproducible:

```cmd
dotnet new tool-manifest
dotnet tool install dotnet-ef
dotnet ef --version
```

If the repository already contains a tool manifest, use `dotnet tool restore` rather than creating another. If `dotnet-ef` is already present in that manifest, verify that it resolves to version `10.0.13`. Both the application EF packages and the EF command-line tool use the same EF Core `10.0.13` version. Commit the generated `.config/dotnet-tools.json` manifest, not machine-specific global tool configuration.

### Create `InitialCemsCatalog`


Run:

```cmd
dotnet ef migrations add InitialCemsCatalog --project src\CampusEvents --startup-project src\CampusEvents --output-dir Migrations -- --environment Development
```

The arguments after `--` are forwarded to the ASP.NET Core startup application. That causes Development configuration to load while EF Core creates the design-time `ApplicationDbContext`.

EF Core creates a `Migrations` folder whose generated migration files include the logical name `InitialCemsCatalog`.

Treat generated migrations as schema-change history, not as disposable output. The migration should correspond to the model change you intended. That is why the next step is inspection rather than immediately updating the database.

### Inspect the migration


Open the generated migration. Inspect its tables, relationships, and constraints:

- `AspNetUsers`
- `Categories`
- `Events`
- primary keys
- `Events.CategoryId`
- `Events.OrganizerId`
- foreign keys to Categories and `AspNetUsers`
- `CK_Events_StartBeforeEnd`
- `CK_Events_CapacityPositive`

A portion of the generated Events-table code should express ideas similar to this:

```csharp
migrationBuilder.CreateTable(
	name: "Events",
	columns: table => new
	{
		Id = table.Column<int>(nullable: false),
		Title = table.Column<string>(nullable: false),
		Description = table.Column<string>(nullable: false),
		Location = table.Column<string>(nullable: false),
		StartDateTime = table.Column<DateTime>(nullable: false),
		EndDateTime = table.Column<DateTime>(nullable: false),
		Capacity = table.Column<int>(nullable: false),
		Status = table.Column<int>(nullable: false),
		CreatedAt = table.Column<DateTime>(nullable: false),
		CategoryId = table.Column<int>(nullable: false),
		OrganizerId = table.Column<string>(nullable: false)
	},
	constraints: table =>
	{
		table.PrimaryKey("PK_Events", eventItem => eventItem.Id);
		table.CheckConstraint(
			"CK_Events_CapacityPositive",
			"[Capacity] > 0");
		table.CheckConstraint(
			"CK_Events_StartBeforeEnd",
			"[StartDateTime] < [EndDateTime]");
	});
```

Exact DBMS annotations and generated type details may vary with the EF Core version used. Inspect the generated migration rather than replacing it with this abbreviated teaching fragment.

When you inspect the migration, translate it back into the concepts you modeled:

```text
entity identity					→ primary key
Event.CategoryId			 	→ foreign key
Event.OrganizerId				→ foreign key
start-before-end rule		→ check constraint
positive-capacity rule 	→ check constraint
```

Inspect the generated migration to confirm its operations represent the intended CEMS schema.

### Apply the migration


Creating `InitialCemsCatalog` changed the project by adding migration files. The database itself still needs the schema update. `database update` is the step that applies the pending migration to `CampusEventsDb`.

Apply the migration to the configured **Development** SQL Server database but first drop any existing database:

```cmd
dotnet ef database drop --project src\CampusEvents --startup-project src\CampusEvents -- --environment Development
```

```cmd
dotnet ef database update --project src\CampusEvents --startup-project src\CampusEvents -- --environment Development
```

Inspect the destination shown in the Development connection before running this command. A reset is a separate instructor-approved operation only for an explicitly disposable Development database; it is never a routine schema-setup command.

### Inspect the database


Now inspect `CampusEventsDb` with Visual Studio SQL Server Object Explorer or some other SQL Server inspection tool. The VS Code equivalent is:

**SQL Server Extension**<br>
**Publisher**: Microsoft<br>
**Extension ID**: ms-mssql.mssql<br>

It includes an actual Object Explorer where you can inspect servers, databases, tables, views, stored procedures, functions, and other database objects. It also supports table/schema designers, editing table data, query execution, execution plans, schema compare, backup/restore, and database object search.

Install it from a terminal:

```cmd
code --install-extension ms-mssql.mssql
```

or install it from the `Extensions (CTRL+SHIFT+X)` view in VS Code.

After installation, you'll see the MSSQL icon in the VS Code Activity Bar. From there you can add a SQL Server connection and expand it much like:

```text
SQL Server
└── localhost
	└── MyDatabase
		├── Tables
		│	 ├── dbo.Users
		│	 └── dbo.Orders
		├── Views
		├── Stored Procedures
		├── Functions
		└── Security
```

The applied migration establishes the schema. Populate it with representative Development records.

## Add Development seed data


Create a **Development-only dataset** containing Published, Draft, Cancelled, upcoming, and past Events. These records make the public visibility rule observable and provide controlled examples for comparing eligible and excluded rows.

The initializer creates a predictable local dataset for checking public catalog eligibility and Event relationships.

The first seed set includes:

- three Categories
- one organizer reference
- two Published upcoming Events
- one Draft upcoming Event
- one Cancelled upcoming Event
- one Published past Event

This lets you prove that the public query is selecting the correct rows instead of simply rendering every Event.

### Create the initializer


Create:

**File: `src/CampusEvents/Data/DbInitializer.cs`**

```csharp
using CampusEvents.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Data;

public static class DbInitializer
{
	private const string OrganizerId = "seed-organizer-001";

	public static async Task SeedAsync(ApplicationDbContext context, TimeProvider timeProvider)
	{
		// Seed independently: an unrelated Event must not suppress missing classroom records.
		var organizer = await context.Users.SingleOrDefaultAsync(user => user.Id == OrganizerId);
		if (organizer is null)
		{
			organizer = new ApplicationUser { Id = OrganizerId, DisplayName = "Campus Events Office" };
			context.Users.Add(organizer);
		}

		async Task<Category> EnsureCategoryAsync(string name)
		{
			var existing = await context.Categories.SingleOrDefaultAsync(category => category.Name == name);
			if (existing is not null) return existing;
			var category = new Category { Name = name };
			context.Categories.Add(category);
			return category;
		}

		var academic = await EnsureCategoryAsync("Academic");
		var social = await EnsureCategoryAsync("Social");
		var career = await EnsureCategoryAsync("Career");
		await context.SaveChangesAsync();

		var createdAt = timeProvider.GetLocalNow().DateTime;
		var today = createdAt.Date;

		var libraryStart = today.AddDays(30).AddHours(18);
		var careerStart = today.AddDays(50).AddHours(12);
		var draftStart = today.AddDays(70).AddHours(15);
		var cancelledStart = today.AddDays(90).AddHours(20);
		var pastStart = today.AddDays(-10).AddHours(13);

		var events = new[]
		{
			new Event
			{
				Title = "Library Research Night",
				Description = "An evening workshop on research databases and source evaluation.",
				Location = "Main Library, Room 204",
				StartDateTime = libraryStart,
				EndDateTime = libraryStart.AddHours(2),
				Capacity = 60,
				Status = EventStatus.Published,
				CreatedAt = createdAt,
				CategoryId = academic.Id,
				OrganizerId = organizer.Id
			},
			new Event
			{
				Title = "Career Networking Lunch",
				Description = "Meet local employers and practice short professional introductions.",
				Location = "Student Center Ballroom",
				StartDateTime = careerStart,
				EndDateTime = careerStart.AddHours(2),
				Capacity = 120,
				Status = EventStatus.Published,
				CreatedAt = createdAt,
				CategoryId = career.Id,
				OrganizerId = organizer.Id
			},
			new Event
			{
				Title = "Welcome Mixer Planning Draft",
				Description = "A draft event used to demonstrate that Draft Events are not public.",
				Location = "Student Center Patio",
				StartDateTime = draftStart,
				EndDateTime = draftStart.AddHours(2),
				Capacity = 80,
				Status = EventStatus.Draft,
				CreatedAt = createdAt,
				CategoryId = social.Id,
				OrganizerId = organizer.Id
			},
			new Event
			{
				Title = "Outdoor Movie Night",
				Description = "A cancelled event retained in the database for lifecycle examples.",
				Location = "Campus Green",
				StartDateTime = cancelledStart,
				EndDateTime = cancelledStart.AddHours(3),
				Capacity = 200,
				Status = EventStatus.Cancelled,
				CreatedAt = createdAt,
				CategoryId = social.Id,
				OrganizerId = organizer.Id
			},
			new Event
			{
				Title = "Alumni Panel Archive",
				Description = "A past Published event used to demonstrate upcoming-event filtering.",
				Location = "Business Building Auditorium",
				StartDateTime = pastStart,
				EndDateTime = pastStart.AddHours(2),
				Capacity = 100,
				Status = EventStatus.Published,
				CreatedAt = createdAt,
				CategoryId = career.Id,
				OrganizerId = organizer.Id
			}
		};

		// These titles are stable identifiers for the *Development sample dataset*, not
		// uniqueness requirements on Event titles created by real users.
		var existingTitles = (await context.Events.AsNoTracking()
			.Where(item => item.OrganizerId == OrganizerId)
			.Select(item => item.Title)
			.ToListAsync()).ToHashSet(StringComparer.Ordinal);
		var missingEvents = events.Where(item => !existingTitles.Contains(item.Title)).ToList();
		if (missingEvents.Count > 0)
		{
			context.Events.AddRange(missingEvents);
			await context.SaveChangesAsync();
		}
	}
}
```

The initializer first finds or creates the fixed Development Organizer by ID, then independently finds or creates the three Categories. Finally, it checks the five classroom Event titles **for that seed Organizer** and inserts only missing sample Events. Unlike the original all-or-nothing `Events.AnyAsync()` guard, a preexisting unrelated Event no longer suppresses the sample dataset.

This is intentionally **Development-only**, repeatable setup—not an application-wide Event-title uniqueness rule. Do not run concurrent initializers against the same database; durable category uniqueness is introduced later, and seed-row identities still require a controlled Development process. Existing sample Events are **not** overwritten on restart. To regenerate their dates, explicitly arrange a new disposable database instead of resetting another person's work.

**Automated regression for seed resilience:** Add `DevelopmentSeedTests.SeedAsync_IsIdempotentWithEmptyAndPartialDevelopmentData` from the revised test project. Its three parameterized cases call the initializer twice against freshly migrated disposable SQL Server databases and compare category/seed-owner/other-owner Event counts and retained sample values. This is authored test coverage; record an actual result only after execution.

**Partial-data verification:** In three separate disposable databases, (1) seed an empty database twice and verify five distinct sample Events, (2) create an unrelated Event and the Academic Category first and verify the remaining sample records appear without duplicates, and (3) pre-create one sample Event under the seed Organizer and verify only the other four are added. Re-query the Categories, Event owner IDs, and record counts after each run.

It saves the organizer and Categories before the Events because the integer Category IDs are generated when those Category rows are persisted.

The initializer calculates Event dates from the campus-local day at insertion. Those dates remain persisted, and Events naturally move out of the upcoming catalog as their start times pass.

### Run the initializer only in Development


Now evolve:

**File: `src/CampusEvents/Program.cs`**

to its Lesson 2 startup form:

```csharp
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
```

The new Development block creates a service scope, resolves `ApplicationDbContext`, and calls:

```csharp
await DbInitializer.SeedAsync(context, timeProvider);
```

Dependency injection also supplies the initializer with its database context and application clock.

Build and run it:

```cmd
dotnet build
dotnet run --project src\CampusEvents
```

On the first Development run against the empty schema, inspect the database and confirm:

- `AspNetUsers` contains `seed-organizer-001`
- Categories contains Academic, Social, and Career
- Events contains five rows

At this point, the database contains Event data, but `/Events` still uses the temporary Lesson 1 string. Persistence has been created first; the web request will use it next.

## Query Published upcoming Events


The public `/Events` endpoint selects **Published upcoming Events**. The controller applies this eligibility rule in the database query so Razor receives the public collection to render.

```text
all stored Events
		↓
database query applies filtering
		↓
Published + upcoming Events only
		↓
materialize the eligible result
		↓
Razor renders that result
```

### Replace the temporary Event value with a database query

Retire the Lesson 1 temporary-title test

The existing tests/CampusEvents.Tests/EventsControllerTests.cs verifies the temporary string-based controller contract introduced in Lesson 1. The next change replaces that contract with a database-backed Event catalog and adds ApplicationDbContext and TimeProvider constructor dependencies.

Before replacing EventsController.cs, delete tests/CampusEvents.Tests/EventsControllerTests.cs. The old test will no longer compile after the constructor changes, and its temporary-title assertion no longer describes the required application behavior.

Later, in Guided Verification and Validation → Automated, recreate EventsControllerTests.cs using the new database-backed catalog and Details regression tests.

Do not delete the CampusEvents.Tests project or unrelated tests.
Open:

**File: `src/CampusEvents/Controllers/EventsController.cs`**

```csharp
using CampusEvents.Data;
using CampusEvents.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CampusEvents.Controllers;

public class EventsController : Controller
{
	private readonly ILogger<EventsController> _logger;
	private readonly ApplicationDbContext _context;
	private readonly TimeProvider _timeProvider;

	public EventsController(ILogger<EventsController> logger, ApplicationDbContext context, TimeProvider timeProvider)
	{
		_logger = logger;
		_context = context;
		_timeProvider = timeProvider;
	}

	public async Task<IActionResult> Index()
	{
		_logger.LogInformation("Loading Published upcoming CEMS events.");

		var now = _timeProvider.GetLocalNow().DateTime;
		var query = _context.Events.AsNoTracking()
			.Include(eventItem => eventItem.Category)
			.Where(eventItem => eventItem.Status == EventStatus.Published && eventItem.StartDateTime > now)
			.OrderBy(eventItem => eventItem.StartDateTime).ThenBy(eventItem => eventItem.Id);

		var events = await query.ToListAsync();
		return View(events);
	}
}
```

CEMS interprets stored offset-free Event timestamps in the configured campus local time zone. Configure the application host and the fixed test clock for the same convention; compare Event start values against that local time. The controller captures the local cutoff once for the entire request. Both the application and its tests use the same clock interface, so the eligibility rule does not depend on the test machine’s current time.

The important transition is this:

```text
Lesson 1:
controller returns the temporary title "Student Research Showcase"

Lesson 2:
controller asks the database which Events qualify
```

### Read the composed query from top to bottom


The query starts from:

```csharp
_context.Events
```

This identifies the persisted Event set that EF Core can query.


#### Read without change tracking


The page is read-only, so the query uses:

```csharp
.AsNoTracking()
```

EF Core normally tracks materialized entities so it can detect updates. `AsNoTracking()` avoids that overhead when retrieving Events for read-only presentation.


#### Eagerly load the related Category


The View needs the Category name, so the query loads that relationship:

```csharp
.Include(eventItem => eventItem.Category)
```

The `Category` navigation property describes the relationship in the model. The `Include(...)` call explicitly retrieves Category data for this query. EF Core **eagerly loads** the related Category so Razor can render `eventItem.Category.Name`.

For a simple reference navigation such as this one, EF Core commonly retrieves the related row through a SQL join, although the exact SQL strategy can vary.


#### Apply filtering before materialization


Apply the public browse rule in the database query:

```csharp
.Where(eventItem => eventItem.Status == EventStatus.Published && eventItem.StartDateTime > now)
```

SQL Server selects Published Events whose start times are later than the captured clock value. Draft, Cancelled, and earlier-starting Events are excluded from the catalog result.


#### Order the result deterministically


The remaining Events are ordered chronologically, with `Id` as a tie-breaker when two Events have the same start time:

```csharp
.OrderBy(eventItem => eventItem.StartDateTime).ThenBy(eventItem => eventItem.Id)
```

`StartDateTime` remains the product-defined primary ordering. Because `Id` is unique, the secondary ordering makes equal-start-time results deterministic without changing their chronological grouping.

The unique `Id` produces a repeatable order for Events that share a start time. See Guided Discussion for the relationship between ordering and bounded retrieval.

### Execute and materialize the catalog query

The composed `query` describes the eligible rows and their order. Execute it with asynchronous database I/O:

```csharp
var events = await query.ToListAsync();
```

`ToListAsync()` retrieves the database-selected rows and materializes them as a list of `Event` objects. The `async Task<IActionResult>` action awaits that result before passing the collection to Razor. Run `/Events` after updating the View to observe the rendered catalog.

## Render persisted Events


Update the View’s model declaration to accept the Event collection returned by the database-backed controller.

Open:

**File: `src/CampusEvents/Views/Events/Index.cshtml`**

Replace the Lesson 1 string View with this first persisted version:

```cshtml
@model System.Collections.Generic.IReadOnlyList<CampusEvents.Models.Event>

@{
	ViewData["Title"] = "Events";
}

<h1>Upcoming Campus Events</h1>

<section aria-labelledby="event-list-heading">
	<h2 id="event-list-heading" class="cems-visually-hidden">Published upcoming events</h2>

	@if (Model.Count == 0)
	{
		<p>There are no upcoming published events to display.</p>
	}
	else
	{
		foreach (var eventItem in Model)
		{
			<article class="event-card">
				<h3 class="event-card-title">@eventItem.Title</h3>

				<p><strong>Category:</strong> @eventItem.Category.Name</p>
				<p><strong>Location:</strong> @eventItem.Location</p>
				<p>
					<strong>Starts:</strong>
					<time>@eventItem.StartDateTime.ToString("f")</time>
				</p>
			</article>
		}
	}
</section>
```

The model has changed from:

```cshtml
@model string
```

to:

```cshtml
@model System.Collections.Generic.IReadOnlyList<CampusEvents.Models.Event>
```

The controller returns a collection, and the View now declares a compatible collection model.

You may avoid using the fully qualified namespaces if your `_ViewImports.cshtml` contains:

```cshtml
@using System.Collections.Generic
@using CampusEvents.Models
```

then your view can simply use:

```cshtml
@model IReadOnlyList<Event>
```

This is an MVC contract. `return View(events);` passes an Event collection to Razor, so the View must declare a model type that can receive that collection. The database and EF Core are no longer involved at this point; Razor is working with the C# objects the controller supplied.

Each Event is rendered as an `<article>`, with semantic headings and a `<time>` element for its start date. The `event-card` and `event-card-title` classes give the elements names that CEMS can style without changing their HTML meaning.

The section heading names the Event list for assistive technology. The `cems-visually-hidden` class will keep that heading available to screen readers without repeating it visually below the page's main heading.

The catalog now renders each eligible Event as a semantic article with its title, Category, location, and start time.

### Style the catalog in `site.css`


**File: `src/CampusEvents/wwwroot/css/site.css`**

Keep the shared layout and foundational styles from Lesson 1. Add these rules at the end of the existing stylesheet:

```css
.event-card {
	padding: 0 0 1rem;
	border-bottom: 1px solid #d0d7de;
}

.event-card + .event-card {
	margin-top: 1rem;
}

.event-card-title {
	margin: 0 0 0.5rem;
	font-size: 1.25rem;
}

.event-card p {
	margin: 0.35rem 0;
}

.cems-visually-hidden {
	position: absolute;
	width: 1px;
	height: 1px;
	padding: 0;
	margin: -1px;
	overflow: hidden;
	clip: rect(0, 0, 0, 0);
	white-space: nowrap;
	border: 0;
}
```

The `event-card` rule separates successive Events without requiring an HTML table or a CSS framework. The adjacent-sibling selector, `.event-card + .event-card`, adds space only when another Event follows a card. The heading and paragraph rules create a predictable hierarchy inside each card.

The visually hidden heading still provides a programmatic label for `<section aria-labelledby="event-list-heading">`. Removing the heading from the markup or using `display: none` would not serve the same accessibility purpose.

The shared `_Layout.cshtml` already loads `~/css/site.css` for every View. The catalog therefore needs no additional `<link>` tag and no JavaScript.

Run it:

```cmd
dotnet build
dotnet run --project src\CampusEvents
```

Request:

```text
/Events
```

The published upcoming Events should appear as separate cards with readable titles and metadata. The site's existing navigation, content width, and keyboard focus styles should still work. In the browser's Network panel, check that `/css/site.css` loads successfully. Resize the page to a narrow width and confirm that the Event text remains readable without horizontal scrolling.

### Observe query composition and execution


Use EF Core logging and the debugger to inspect query composition, command execution, and the resulting objects.

Temporarily inspect the composed SQL by adding this line after building the query and before materialization:

```csharp
var generatedSql = query.ToQueryString();
```

Place one breakpoint at the beginning of the `Index()`, run the application under the debugger, and request:

```text
/Events
```

Step to `generatedSql` and inspect its value in the debugger. `ToQueryString()` shows EF Core's SQL representation of the composed query before database execution.

Continue execution. In the application output, find an EF Core log information entry that shows you the actual SQL command sent to the DBMS. The logged command should contain SQL corresponding to the LINQ expression. Look for the relational equivalents of:

```text
Include(...)	→ related Category data, commonly through a JOIN
Where(...)		→ WHERE
OrderBy(...)	→ ORDER BY StartDateTime
ThenBy(...)		→ additional ORDER BY key for Id
```

Before executing the query, predict which records satisfy `Status == Published && StartDateTime > now` using the stored Event timestamps. With a freshly initialized Development database, the expected eligible Events are **Library Research Night** and **Career Networking Lunch**, in that order. On a previously seeded database, compare the persisted start times with the current application clock. To repeat the initial scenario, use a new, instructor-approved disposable Development database and apply the existing migration before starting the application.

Continue to just after `await query.ToListAsync()` and inspect `events`. Compare the objects and their related `Category` data with the composed SQL and the executed `Database.Command` log. This traces three distinct stages:

```text
composed IQueryable<Event> → inspect with ToQueryString()
SQL execution → inspect EF Core Database.Command logs
materialized List<Event> → inspect after ToListAsync()
```

Remove the temporary SQL diagnostic statement when finished. The `/Events` page now renders persisted eligible Events in chronological order. Review the modified models, migration, initializer, controller, View, and stylesheet using `git diff`.

## Add an Event detail page


The catalog now lists persisted Events. The next product problem is resource identity: when a visitor selects one Event, the server needs a stable way to identify which persisted Event is being requested.

The Event primary key provides that identity. The route can carry the ID to the controller, but the ID alone is **not** enough to decide whether the public request should succeed. The Event must also satisfy the public availability rule for this endpoint. This is preferable to first loading any Event by ID and then deciding in the View whether it should be shown.

The existing conventional route is:

```text
{controller=Home}/{action=Index}/{id?}
```

A request can therefore place an integer in the optional `id` segment, for example:

```text
/Events/Details/1
```

Routing can bind that value to a parameter named `id`.

### Add the controller action


Open:

**File: `src/CampusEvents/Controllers/EventsController.cs`**

Add the following action below `Index()`:

```csharp
public async Task<IActionResult> Details(int id)
{
	var query = _context.Events.AsNoTracking()
		.Include(item => item.Category).Include(item => item.Organizer)
		.Where(item => item.Id == id && item.Status == EventStatus.Published);

	var eventItem = await query.SingleOrDefaultAsync();

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
```

The controller uses the existing ASP.NET Core HTTP status constants. Keep `Index()` and `Details()` in that **same** `EventsController`, with its logger, context, and `TimeProvider` constructor.

The query loads the Category and Organizer relationships needed by the page:

```csharp
.Include(item => item.Category).Include(item => item.Organizer)
```

Notice that the predicate combines identity and public eligibility:

```csharp
item.Id == id && item.Status == EventStatus.Published
```

so a guessed Draft ID is not exposed through this public workflow.

The query combines Event identity and Published eligibility. Both an unknown ID and a non-public Event ID produce the same controlled HTTP 404 response.

`SingleOrDefaultAsync(...)` returns one matching Event or `null`. Because `Id` is the primary key, more than one matching Event should not exist.

### Create the Razor view

Create:

**File: `src/CampusEvents/Views/Events/Details.cshtml`**

```cshtml
@model CampusEvents.Models.Event

@{
	ViewData["Title"] = Model.Title;
}

<article class="event-detail">
	<header>
		<h1>@Model.Title</h1>
	</header>

	<p>@Model.Description</p>

	<dl class="event-detail-facts">
		<dt>Status</dt>
		<dd>@Model.Status</dd>

		<dt>Category</dt>
		<dd>@Model.Category.Name</dd>

		<dt>Location</dt>
		<dd>@Model.Location</dd>

		<dt>Starts</dt>
		<dd><time>@Model.StartDateTime.ToString("f")</time></dd>

		<dt>Ends</dt>
		<dd><time>@Model.EndDateTime.ToString("f")</time></dd>

		<dt>Organizer</dt>
		<dd>@Model.Organizer.DisplayName</dd>

		<dt>Configured capacity</dt>
		<dd>@Model.Capacity places</dd>
	</dl>
</article>

<p>
	<a asp-controller="Events" asp-action="Index">Back to Events</a>
</p>
```

This page displays the Event's:

- title
- description
- status
- category
- location
- start date/time
- end date/time
- organizer display name
- configured capacity

The `<dl>` groups each fact's label (`<dt>`) with its value (`<dd>`). This is useful for Event Details because the page presents named properties of one resource. The `event-detail` and `event-detail-facts` classes control layout while preserving that meaning.


#### Style the Details page in `site.css`


**File: `src/CampusEvents/wwwroot/css/site.css`**

Add the following styles after the catalog rules:

```css
.event-detail {
	max-width: 48rem;
}

.event-detail-facts {
	display: grid;
	grid-template-columns: minmax(10rem, 12rem) minmax(0, 1fr);
	gap: 0.4rem 1rem;
}

.event-detail-facts dt {
	font-weight: 700;
}

.event-detail-facts dd {
	margin: 0;
	overflow-wrap: anywhere;
}

@media (max-width: 40rem) {
	.event-detail-facts {
		grid-template-columns: 1fr;
		gap: 0.15rem;
	}

	.event-detail-facts dd {
		margin-bottom: 0.75rem;
	}
}
```

On wider screens, the grid places each label and its value in two columns. On narrow screens, the definition list uses a single column so longer values can wrap without forcing sideways scrolling. The HTML stays a definition list at either width; CSS changes only its visual arrangement.

When you can open a populated Details page, inspect it at both a wide and narrow browser width. Confirm that the labels remain next to the correct values and that the longer availability message wraps within the content area.

### Capacity and registration availability


Suppose the page displays:

```text
Configured capacity: 60
```

That value represents the maximum number of participants permitted for an Event. Remaining places use a separate calculation:

```text
remaining capacity 	= configured capacity - current registration count
```

Display the stored capacity as **Configured capacity**. If the interface also labels availability, use **Availability information unavailable** until registrations can be counted. This keeps the two values distinct and accurately represents the current `FR-SRH-004` implementation.

## Link the catalog to Event Details


The destination action and its Razor View now exist. Return to:

**File: `src/CampusEvents/Views/Events/Index.cshtml`**

Inside the `foreach`, replace

```cshtml
<h3 class="event-card-title">@eventItem.Title</h3>
```

with:

```cshtml
<h3 class="event-card-title">
	<a asp-controller="Events"
	   asp-action="Details"
	   asp-route-id="@eventItem.Id">@eventItem.Title</a>
</h3>
```

Keep the `event-card-title` class on the heading so that the linked title uses the same catalog typography.

The Tag Helpers use the controller, action, and Event ID to generate the link for that Event. The generated URL now takes the form:

```text
/Events/Details/1
```

Build and run the application, open `/Events`, and select one of the published upcoming Event titles. The request should load the corresponding persisted Event rather than a hard-coded page. Confirm that the catalog cards retain their spacing and that the Details page uses the two-column fact layout on a wide screen and a stacked layout on a narrow screen. The navigation and back link should work with an ordinary keyboard- or mouse-activated HTML link.

## Handle missing Events


A matching Published Event receives its HTML Details page. An unavailable public resource receives HTTP 404. For this endpoint, the same public outcome is appropriate when:

```text
the requested ID does not exist or does not identify a Published event
		↓
404 Not Found
```

The response should still be useful to a person, but it should also preserve correct HTTP semantics for browsers, automated clients, logs, and diagnostics. The controller already checks whether the database query returned `null`. Now add the View used for the controlled response.

Create:

**File: `src/CampusEvents/Views/Events/EventNotFound.cshtml`**

```cshtml
@{
	ViewData["Title"] = "Event Not Found";
}

<h1>Event Not Found</h1>

<p>
	The requested published event could not be found.
</p>

<p>
	<a asp-controller="Events" asp-action="Index">Back to Events</a>
</p>
```

When no matching Published Event exists, the action does two things:

```csharp
private ViewResult EventNotFound()
{
	Response.StatusCode = StatusCodes.Status404NotFound;
	return View("EventNotFound");
}
```

The browser receives a human-readable page **and** the HTTP response status remains 404.

Now that the concrete action exists, the request can be traced precisely:

```text
GET /Events/Details/{id}
	↓
route binds {id}
	↓
EventsController.Details(id)
	↓
Published Event lookup + Category + Organizer
	↓
found? ── no ──> 404 EventNotFound View
	│
	 yes
	↓
Views/Events/Details.cshtml
	↓
HTML response
```

Request an ID that does not exist:

```text
/Events/Details/999999
```

You should see:

- the application remains running
- the Event Not Found page appears
- the response status is 404
- no null-reference exception is thrown
- no database details or stack trace are exposed

A missing resource is an expected web condition. Handling it deliberately is better than allowing `null` to flow into code that assumes an Event exists. The controlled 404 also avoids revealing an unnecessary distinction between “there is no Event with that ID” and “an Event with that ID exists but is not available through this public endpoint.” The public contract is simply that no matching public Event was found.

# Verification and Validation

**Verification** checks the implementation, SQL schema, queries, and HTTP result contracts. **Validation** checks whether visitors can browse and understand the delivered pages. Use automated controller checks, database inspection, and manual browser scenarios for their respective questions.

## Automated

Use the following inventory to verify public catalog selection and controlled Details responses. The first two tests are authored in this lesson; the remaining cases are optional extensions.

| Test / case | Behavior or boundary | Expected assertion / evidence |
| --- | --- | --- |
| `Index_ReturnsPublishedUpcomingEventsInChronologicalOrder` (authored) | Public eligibility and stable order | Published future records appear in start-time/ID order, including equal-start-time ties; Draft, Cancelled, past, and exact-now records are excluded. |
| `Details_DraftOrMissing_ReturnsEventNotFoundWith404` (authored; one test, two ID scenarios) | Draft and nonexistent Details resources | `EventNotFound` View with HTTP 404 for both scenarios. |
| Published Details retrieval (optional) | Relationship loading and public Details model | Selected Published Event includes Category and Organizer references. |
| SQL Server integrity rejection (optional) | Persisted Capacity invariant | The migrated SQL Server database rejects an invalid zero-capacity record. |

Prepare the isolated SQL Server fixture, run the authored tests, and record the discovered, passed, failed, and skipped cases reported by the test runner.

### Prepare an isolated SQL server test database

The Development `DbInitializer` supplies records for browser exploration. Automated tests arrange their **own** rows so results do not depend on a previously used database, changing seed data, or the current wall clock. Each test must create a fresh SQL Server database from the real `InitialCemsCatalog` EF Core migration, retaining actual SQL Server foreign keys, check constraints, and migration history. Do not use `EnsureCreated()`, EF Core InMemory, or SQLite as a substitute for this regression suite.

**Windows LocalDB default:** Install SQL Server Express LocalDB and confirm that `(localdb)\MSSQLLocalDB` is available. The fixture uses this connection automatically; there are **no mandatory test environment variables** and no requirement that the instance be empty:

```bat
sqllocaldb info MSSQLLocalDB
```

The fixture connects to `master`, generates a private `CemsL02Test_<GUID>` database name, applies `MigrateAsync()` against that database, and deletes **only** that exact database when the test completes. Other databases, including `CampusEventsDb`, are never selected for deletion. A name prefix alone is not a database permission boundary: run tests under a trusted local account and never reuse credentials or connection strings pointing to production data.

**Optional non-default SQL Server endpoint:** Windows users can specify another LocalDB instance; macOS/Linux or CI users can specify an authorized SQL Server endpoint whose account can create and drop its own test databases. Supply one variable with `Database=master` and the correct authentication details; never commit secrets. For example, on Windows:

```bat
set "CEMS_TEST_SQLSERVER=Server=(localdb)\MSSQLLocalDB;Database=master;Integrated ^
Security=True;TrustServerCertificate=True"
```

This variable is optional for the default LocalDB instance. If you previously configured an override and want to return to the default, clear it in the terminal with `set "CEMS_TEST_SQLSERVER="`.  For an external endpoint, obtain the server and restricted testing credentials from the responsible administrator. Avoid connecting this test fixture to a production server or a database service on which temporary database creation is prohibited. It never reads `ConnectionStrings:DefaultConnection` from Development.

**Working directory:** outer `CampusEvents` solution root. Verify both `.csproj` files target `net10.0`, then add the test-only clock dependency:

```bat
dotnet package add Microsoft.Extensions.TimeProvider.Testing --project tests\CampusEvents.Tests
dotnet build
```

`FakeTimeProvider` is in `Microsoft.Extensions.Time.Testing`; `TimeProvider` itself is built into .NET 10. The application project already supplies the EF Core SQL Server dependency to the test project through its project reference. No additional application library or mocking framework is needed.

Create **File: `tests/CampusEvents.Tests/IsolatedSqlServerDatabase.cs`**:

```csharp
using CampusEvents.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Tests;

/// <summary>
/// Each test owns a GUID-named SQL Server database, created from real migrations.
/// LocalDB is the default; an explicit environment override can target an authorized test server.
/// </summary>
public sealed class IsolatedSqlServerDatabase : IAsyncDisposable
{
	private const string Prefix = "CemsL02Test_";
	private const string DefaultMasterConnection =
		@"Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=True;TrustServerCertificate=True";
	private readonly string _masterConnection;
	private readonly string _databaseName;
	private bool _migrationAttempted;

	private IsolatedSqlServerDatabase(string masterConnection, string databaseName, string eventConnection)
	{
		_masterConnection = masterConnection;
		_databaseName = databaseName;
		ConnectionString = eventConnection;
	}

	public string ConnectionString { get; }

	public ApplicationDbContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(ConnectionString).Options;
		return new ApplicationDbContext(options);
	}

	public static async Task<IsolatedSqlServerDatabase> CreateAsync()
	{
		string raw = Environment.GetEnvironmentVariable("CEMS_TEST_SQLSERVER") ?? DefaultMasterConnection;
		var master = new SqlConnectionStringBuilder(raw);

		// Operate from master, never from an existing application database or an attached file.
		if (!string.Equals(master.InitialCatalog, "master", StringComparison.OrdinalIgnoreCase) ||
			!string.IsNullOrEmpty(master.AttachDBFilename))
		{
			throw new InvalidOperationException(
				"CEMS_TEST_SQLSERVER must target the master catalog without AttachDBFilename. " +
				"By default CEMS tests use (localdb)\\MSSQLLocalDB; no configuration is required.");
		}

		string name = Prefix + Guid.NewGuid().ToString("N");
		var target = new SqlConnectionStringBuilder(master.ConnectionString)
		{
			InitialCatalog = name
		};
		var database = new IsolatedSqlServerDatabase(master.ConnectionString, name, target.ConnectionString);

		try
		{
			database._migrationAttempted = true;
			await using var context = database.CreateContext();
			await context.Database.MigrateAsync();
			return database;
		}
		catch
		{
			await database.DisposeAsync();
			throw;
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (!_migrationAttempted)
		{
			return;
		}

		// Release pooled connections before dropping this fixture-owned database.
		await using (var targetConnection = new SqlConnection(ConnectionString))
		{
			SqlConnection.ClearPool(targetConnection);
		}
		await using var connection = new SqlConnection(_masterConnection);
		await connection.OpenAsync();
		await using var exists = new SqlCommand("SELECT DB_ID(@name)", connection);
		exists.Parameters.AddWithValue("@name", _databaseName);
		var databaseId = await exists.ExecuteScalarAsync();
		if (databaseId is not null && databaseId is not DBNull)
		{
			// The identifier is fixture-generated from a GUID and never accepted from a caller.
			string identifier = "[" + _databaseName + "]";
			string sql = "ALTER DATABASE " + identifier + " SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
				"DROP DATABASE " + identifier + ";";
			await using var drop = new SqlCommand(sql, connection);
			await drop.ExecuteNonQueryAsync();
		}
		_migrationAttempted = false;
	}
}
```

**Isolation and cleanup contract:** Each test creates a GUID-named database, applies migrations, and disposes its contexts before the fixture drops that database. The cleanup releases the fixture connection pool, switches the owned database to single-user mode, and reports SQL failures. Use authorized test credentials; after an interrupted run, identify the exact fixture-owned database before manual cleanup. The Appendix explains the connection-pool and isolation design.

### Implement focused catalog and Details regression tests


Replace the old Lesson 1 test file's temporary-title assertion. Create or update **File: `tests/CampusEvents.Tests/EventsControllerTests.cs`** with the following two tests. Both use the existing MVC `EventsController` contract; `NullLogger` handles the logging dependency without a mocking library.

```csharp
using CampusEvents.Controllers;
using CampusEvents.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
			"CemsFixedUtcMinus4", TimeSpan.FromHours(-4), "CEMS fixed UTC-04 test zone",
			"CEMS fixed UTC-04 test zone");
		clock.SetLocalTimeZone(zone);
		return clock;
	}

	private static Event MakeEvent(string title, EventStatus status, DateTime start, int categoryId, string organizerId)
	{
		return new Event
		{
			Title = title,
			Description = "CEMS relational regression example",
			Location = "Campus Student Center",
			StartDateTime = start,
			EndDateTime = start.AddHours(2),
			Capacity = 50,
			Status = status,
			CreatedAt = start.AddDays(-7),
			CategoryId = categoryId,
			OrganizerId = organizerId
		};
	}

	[Fact]
	public async Task Index_ReturnsPublishedUpcomingEventsInChronologicalOrder()
	{
		var clock = FixedClock();
		var now = clock.GetLocalNow().DateTime;
		Assert.Equal(new DateTime(2026, 4, 15, 12, 0, 0), now);
		await using var database = await IsolatedSqlServerDatabase.CreateAsync();

		int sameTimeAId;
		int sameTimeBId;
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

			var sameTimeA = MakeEvent("Same-Time A", EventStatus.Published, now.AddDays(3), category.Id, organizer.Id);
			var sameTimeB = MakeEvent("Same-Time B", EventStatus.Published, now.AddDays(3), category.Id, organizer.Id);
			arrange.Events.AddRange(
				MakeEvent("Later Workshop", EventStatus.Published, now.AddDays(10), category.Id, organizer.Id),
				MakeEvent("Earlier Seminar", EventStatus.Published, now.AddDays(2), category.Id, organizer.Id),
				sameTimeB,
				sameTimeA,
				MakeEvent("Starts Now", EventStatus.Published, now, category.Id, organizer.Id),
				MakeEvent("Draft Planning", EventStatus.Draft, now.AddDays(3), category.Id, organizer.Id),
				MakeEvent("Cancelled Mixer", EventStatus.Cancelled, now.AddDays(4), category.Id, organizer.Id),
				MakeEvent("Past Panel", EventStatus.Published, now.AddDays(-2), category.Id, organizer.Id));
			await arrange.SaveChangesAsync();
			sameTimeAId = sameTimeA.Id;
			sameTimeBId = sameTimeB.Id;
		}

		await using var verify = database.CreateContext();
		var controller = new EventsController(NullLogger<EventsController>.Instance, verify, clock);
		var view = Assert.IsType<ViewResult>(await controller.Index());
		var events = Assert.IsType<List<Event>>(view.Model);

		var expectedTiedOrder = new[] { (Id: sameTimeAId, Title: "Same-Time A"),
			(Id: sameTimeBId, Title: "Same-Time B") }.OrderBy(item => item.Id).ToArray();
		var expectedTitles = new[] { "Earlier Seminar", expectedTiedOrder[0].Title,
			expectedTiedOrder[1].Title, "Later Workshop" };
		Assert.Equal(expectedTitles, events.Select(item => item.Title));
		Assert.Equal(expectedTiedOrder.Select(item => item.Id), events.Skip(1).Take(2).Select(item => item.Id));
		Assert.All(events, item => Assert.Equal(EventStatus.Published, item.Status));
		Assert.All(events, item => Assert.Equal("Academic", item.Category.Name));
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
		foreach (int requestedId in new[] { draftId, int.MaxValue })
		{
			var controller = new EventsController(NullLogger<EventsController>.Instance, verify, clock)
			{
				ControllerContext = new ControllerContext
				{
					HttpContext = new DefaultHttpContext()
				}
			};
			var result = Assert.IsType<ViewResult>(await controller.Details(requestedId));
			Assert.Equal("EventNotFound", result.ViewName);
			Assert.Equal(StatusCodes.Status404NotFound, controller.Response.StatusCode);
		}
	}
}
```

In the catalog test, the eight records include four eligible Events, an exact-now boundary, and three other exclusion cases. The fixed fake clock makes the exact-now eligibility boundary repeatable; the equal-time records exercise the unique-ID tie-breaker. The Details test checks **both** parts of the existing behavior: the action selects the `EventNotFound` View and sets HTTP status **404**. The `DefaultHttpContext` is needed when directly invoking a controller that writes to `Response`.

These controller tests examine action results against real SQL Server queries. Browser scenarios separately exercise routed requests, Razor-rendered pages, and navigation.

### Run the suite and interpret results


**Working directory:** outer `CampusEvents` solution root. On Windows, confirm that `(localdb)\MSSQLLocalDB` is installed; no test environment variables are necessary. For an authorized alternate SQL Server endpoint only, set `CEMS_TEST_SQLSERVER` to a `master` connection string using test-scoped credentials. Never use the application's `DefaultConnection` as a test fixture setting.

```cmd
dotnet build
dotnet test
```

Record the actual xUnit output, execution date, operating system, SQL Server endpoint type (without credentials), discovered/pass/fail/skip counts, and fixture cleanup outcome. Resolve connection, permission, or migration errors and rerun the affected checks.

### Extend the regression coverage


For additional automated coverage, implement two focused tests **without changing the existing two**:

- **Published Details retrieval:** insert one Published Event with a Category and Organizer; retrieve it through `Details(id)`; assert that the View model identifies that Event and contains both loaded relationships. Set `ControllerContext.HttpContext` even when expecting a successful View so the test setup is consistent with the HTTP-response code paths.
- **SQL Server integrity failure:** insert an Event with `Capacity = 0` into a migration-created test database; assert `SaveChangesAsync()` fails with `DbUpdateException` (rather than matching an environment-specific message); verify in a fresh context that the invalid Event was not persisted. Dispose the fixture so it releases connections and deletes only its own GUID-named database.

The Published Details check targets the selected model and relationships; the capacity check targets SQL Server enforcement of the stored-data invariant. Record each added test in the inventory when implemented.

## Manual

**Time-zone deployment check:** Record the course-approved application time zone and the actual Development/deployment host time zone. Confirm that the host zone matches the documented policy before interpreting `TimeProvider.System.GetLocalNow()` cutoffs. The fixed UTC−04 clock in tests provides controlled local-time examples.

Development seed dates are persisted when **each missing sample Event** is inserted; they remain unchanged on subsequent application restarts. A partially seeded database can have sample Events originating on different days, so compare real timestamps rather than assume uniform relative offsets. For the original two-Event catalog scenario, use a freshly migrated, instructor-approved disposable Development database. When using an existing database, inspect saved start times against the application clock before evaluating the public catalog.

Use a Development SQL Server database whose schema has been migrated and which contains the five representative seed Events. Run from the outer solution directory:

```cmd
dotnet run --project src\CampusEvents
```

In a browser, record the URL, observation date, Development environment, relevant dataset, expected outcome, and actual result for each scenario. Check the HTTP status in the browser Network panel when appropriate; check the response status as well as the rendered error content.

| Scenario | Prerequisites, inputs, and steps | Expected result and evidence |
| --- | --- | --- |
| Public catalog | Open `/Events` after the initializer creates its data | Only **Library Research Night** followed by **Career Networking Lunch**, assuming their saved starts are still in the future. |
| Exclusion boundary | Compare Events table rows with the public list | Draft, Cancelled, and past Published seed records remain persisted but do not appear in the browse list. |
| Real persistence | Navigate away, refresh, then normally stop/restart the app and return to `/Events` | The previously persisted records remain; no replacement hard-coded title and no duplicate seed set. |
| Details relations | Activate each public title link and inspect `/Events/Details/{id}` | Matching title, description, Category, location, schedule, Organizer name, and configured capacity. |
| Controlled not-found | Request `/Events/Details/999999` and a known Draft ID | Understandable `Event Not Found` HTML with HTTP **404**; no stack trace, connection details, or disclosure of Draft data. |
| Query evidence | Inspect `query.ToQueryString()` before execution and EF Core command logs during a request | Eligible-record predicates and ordering appear in the SQL representation; materialized objects appear only after execution. Exclude sensitive connection information from evidence. |
| Keyboard and semantics | Tab through catalog titles and the Details back link; inspect headings and the Details definition list | Links are keyboard accessible with visible focus; headings convey structure; facts remain associated with labels. |
| Responsive interface | Check common laptop width and a narrow mobile-width viewport | Event cards and the Details facts layout wrap without normal-content horizontal scrolling. |
| Empty public result | On a new, instructor-approved disposable Development database, seed records that do not qualify, then browse | Only the empty-state message appears; no non-public records appear. The catalog displays “There are no upcoming published events to display.” in its empty state. |

Inspect viewports, links, HTTP statuses, and visual feedback through the manual browser scenarios.

## SRS Traceability Matrix Update with Evidence

Build on the Lesson 1 evidence practice. Update the working **File: `docs/traceability.md`** established in Lesson 1, recording Lesson 2 evidence under a clearly identified lesson entry. Use the table and evidence-context record below as its starting content; leave the approved SRS unchanged. Keep planned verification separate from **observed** results, and revise the record after each real test execution, database inspection, or browser observation. Use the status values to distinguish the implemented slice from broader SRS acceptance.

| Requirement / Reference | Implementation / Artifact | Verification / Evidence | Status | Notes |
| --- | --- | --- | --- | --- |
| UC-001 | `EventsController.Index`, `Details`; Events Razor Views | Catalog and Details browser scenarios | PARTIAL | Public catalog and Details retrieval are implemented. Remaining acceptance behavior: search, Category filtering, pagination, and registration availability. |
| FR-SRH-001 / BR-EVT-005 | `EventsController.Index`; `Views/Events/Index.cshtml` | migrated SQL Server catalog test output and public-list browser observation. | IMPLEMENTED—UNVERIFIED | The public catalog query uses the Published/upcoming predicate. |
| FR-SRH-002 | `EventsController.Index` | ordering assertion output and visible order. | IMPLEMENTED—UNVERIFIED | chronological order and ID tie-breaker are implemented. |
| FR-SRH-003 | `EventsController.Details`; `Views/Events/Details.cshtml` | Required manual Published Details browser scenario; authored automated Draft/missing-ID 404 test. Positive Details automated test is optional. | IMPLEMENTED—UNVERIFIED | Details and controlled missing-resource response are implemented. |
| FR-SRH-004 | `Views/Events/Details.cshtml` | Displayed Event fields and configured capacity | PARTIAL | Current availability requires the active-registration count. |
| FR-SRH-008 | Published-only `Details` query | Direct-link Cancelled Event scenario | DEFERRED | Remaining acceptance behavior: direct Details access to previously Published Cancelled Events with their cancelled status visible. |
| BR-EVT-001 / BR-EVT-003 / BR-EVT-004 | `ApplicationDbContext`; `InitialCemsCatalog` migration | Required applied-schema check-constraint and FK inspection; optional invalid-write integration test. | IMPLEMENTED—UNVERIFIED | Date, capacity, and Category constraints are mapped in the initial migration. |
| BR-CAT-001 | Category relational mapping | Category-name uniqueness scenario | DEFERRED | Remaining acceptance behavior: case-insensitive Category-name uniqueness. |
| BR-CAT-002 | Event–Category restricted-delete mapping | Required migration FK inspection; optional rejected-delete test. | IMPLEMENTED—UNVERIFIED | Restricted Category deletion is configured. |
| NFR-SEC-004–005 / NFR-ACC-002–003 / NFR-COM-002 | Configuration and 404 handling; `Views/Events`; `wwwroot/css/site.css` | safe-config review, keyboard/focus inspection, error-page observation, and viewport checks. | PARTIAL | Security, accessibility, and responsive behavior require environment-specific observations. |
| NFR-PER-001–002 | Composed public query in `EventsController.Index` | generated SQL and executed database-command logs. | PARTIAL | eligibility is evaluated in SQL, but result count remains unbounded in this lesson. |
| AC-001 / AC-005 / AC-008–011 / AC-013 | Relational models, queries, migrations, tests, and setup workflow | actual migration/restart observations, test output, browser checks, and setup/repository references. | PARTIAL | Record requirements individually as their related workflows and checks are completed. |

**Evidence context:** Run `dotnet build` and `dotnet test`, record the test results, and compare the manual inspector output with the expected scenarios. Both are necessary to evidence that requirements have been implemented and verified.

Confirm that your project builds, passes all tests, and runs without errors.

Stage and commit:

```cmd
git add .
git commit -m "Lesson 02: Implemented and Verified"
```

**NOTE:** Never stage or commit source code that does not build, fails a test, or runs with errors.

# Discussion

## Persistence and separation of responsibilities

Follow the completed catalog request from the browser to the database and back:

```text
GET /Events
    ↓
MVC routing → EventsController.Index()
    ↓
Compose a Published/upcoming Event query
    ↓
EF Core translates it; SQL Server executes it
    ↓
Materialize selected Event and Category records
    ↓
Razor renders semantic HTML → HTTP 200
```

The same MVC request contract established in Lesson 1 now obtains durable data through an injected context. Controllers make the public-selection decision, the database evaluates predicates and relationships, and the View presents the selected records. This separation lets a new source of data fit within the established request-and-rendering flow.

## Relational identity and data integrity

A Category can classify many Events; each Event references one Category. Likewise, one organizer-reference user may own many Events, and each Event identifies one organizer. Keys and relationships express those facts across stored rows:

```text
Category (Id PK)         ApplicationUser (Id PK)
        │                           │
        └──────┐             ┌──────┘
               ↓             ↓
            Event (Id PK)
            CategoryId (FK)
            OrganizerId (FK)
```

Foreign-key constraints protect references even when data is written outside the MVC controller. Restricted deletes preserve referenced parents, and the database check constraints enforce positive Capacity and a start preceding the end. Application validation can additionally explain invalid entries before a write is attempted. These mechanisms complement one another by operating at different boundaries.

For Category names, `BR-CAT-001` calls for case-insensitive uniqueness. Designing a unique index involves SQL Server collation or a consistent normalization rule. Record the current rule as `DEFERRED` in traceability until a reviewed schema change implements it.

## Entity models, migrations, and persisted schema

The application entity model expresses the records and relationships the code understands. For example, `Event.Capacity` is a numeric property in C#, `InitialCemsCatalog` creates its column and positive-value check constraint, and the migrated SQL Server database enforces the rule for inserted rows. A migration captures versioned schema operations. The actual database holds the applied tables, constraints, rows, and migration history. Compare `ApplicationDbContext`, `InitialCemsCatalog`, and the inspected SQL Server schema to identify where each responsibility belongs.

As the model evolves, review the generated migration before applying it and confirm the database's recorded version afterward. This makes schema change reviewable and repeatable for other developers and environments.

## Public eligibility, query composition, and execution

The catalog combines several distinct responsibilities. `Where(...)` applies public eligibility in SQL; `Include(...)` supplies Category data used by the page; `OrderBy(...).ThenBy(...)` establishes a total order for a fixed dataset; and `ToListAsync()` executes the composed retrieval and materializes its rows. `AsNoTracking()` avoids change-tracking overhead for this read-only representation. Awaiting asynchronous database I/O allows the server to use threads for other work while the database operation is pending. Selection and resource limits govern separate concerns.

**Eligibility and cardinality answer different questions.** A filter decides which records qualify; it places no fixed ceiling on the number that qualify. A database with 50,000 eligible Events could return 50,000 rows. A bounded selection such as `Take(50)` limits returned rows, while a projection such as `Select(...)` limits columns or shapes. Those choices affect distinct dimensions of retrieval and must align with the intended catalog contract.

Stable ordering makes equal-start-time results reproducible for a fixed dataset. It also provides a necessary foundation for navigating bounded pages. Query constraints, async I/O, and explicit limits solve distinct problems: selection, thread utilization during I/O, and limiting work. The Appendix compares additional operational controls.

## Time-dependent behavior and repeatability

The future-start rule depends on a clock reading. Capturing it once gives each request a consistent cutoff; injecting the clock lets tests reproduce boundary cases such as an Event starting *exactly now*. CEMS interprets offset-free Event timestamps using the agreed campus-local convention. Align host and test time-zone configurations with that convention.

Test-owned SQL Server databases provide another kind of control. A fresh, migrated database with explicitly arranged Events isolates tests from Development seeds and from unrelated local changes. Compare that controlled environment with browser observations on the Development database to understand the different questions each check answers.

## Resource representation, HTTP behavior, and accessibility

The catalog represents a collection using headings, articles, descriptive text, and standard links. Event Details use a definition list to associate labels with values. CSS may change the spatial layout while retaining the semantic relationships. Native links also support keyboard navigation and identifiable destinations.

A valid public Details request returns the resource representation, while an unavailable public resource receives HTTP 404 and a useful HTML page. This is both an HTTP contract and a disclosure boundary: the public response communicates availability without exposing private Event data. Use the same visibility predicate consistently in resource retrieval and explain its HTTP consequences.

# Exercises

The exercises progress from analysis of an existing request to safe changes and independent regression reasoning. Complete the expected deliverables and verify them; challenge tasks are optional.

## Exercise: diagram the persistence boundary

**Purpose and expected outcome:** Explain how an object relationship becomes stored keys and how a browser request turns database rows into a rendered response.

**Prerequisites and SRS ties:** Use the migrated models and `/Events/Details/{id}` implementation; focus on `FR-SRH-003`, `BR-EVT-004`, and data integrity expectations. Do not change code.

**Task:** Draw a one-to-many relationship diagram for Event–Category and Event–Organizer, then annotate the request path for one Published Details ID and one Draft Details ID. Mark the foreign keys, the query predicate, execution/materialization, and the resulting HTTP outcomes. Explain why neither navigation property stores an entire related object as an Event column.

**Hints or checkpoints:** Follow the migration foreign keys through the Include/query to the Details response; a Draft ID should not resolve through the public Published lookup.

**Deliverable:** Commit or submit one annotated diagram and a short explanation. Check it against the actual migration and query; both public lookup outcomes must be correct.

**Completion and verification:** Match both relationship arrows to actual key columns and both Detail outcomes to the action predicate/status.

**Challenge (optional):** Contrast a foreign-key failure with a missing public resource.

## Exercise: inspect a query without broadening access

**Purpose and expected outcome:** Apply eligibility constraints, deterministic ordering, and execution-boundary reasoning to a diagnostic problem.

**Prerequisites and SRS ties:** Existing `Index()` and seed data, with `FR-SRH-001–002` and `BR-EVT-005` unchanged. Use breakpoints and SQL inspection; no new public features.

**Task:** Predict the returned Events, capture the SQL representation before `ToListAsync()`, and inspect the final list after execution. Then temporarily remove **one** predicate on a throwaway local branch, observe the changed row eligibility, restore the predicate, and confirm the original behavior. Do not commit the deliberately incorrect query.

**Hints or checkpoints:** Capture query composition before materialization and actual result rows afterward; ensure the removed predicate is restored before commits.

**Deliverable:** A short before/after explanation with sanitized evidence that identifies which layer enforces public visibility.

**Completion and verification:** Repeat the eligible-row check after restoring the predicate and document the observed SQL/result difference.

**Challenge (optional):** Add two equal-start-time Published seed rows in an approved disposable database and explain why the ID tie-breaker matters.

## Exercise: extend the Development seed dataset safely

**Purpose and expected outcome:** Practice controlled data changes, repeated migration setup, and the distinction between a seed recipe and persisted records.

**Prerequisites and SRS ties:** An approved **new empty disposable Development database**, existing `InitialCemsCatalog` migration, and the initializer's independent stable-key existence checks. This is exploration of `FR-SRH-001`, not production content management.

**Task:** Add one Published Event titled **Study Skills Workshop** to the initializer's fixed sample Event list, starting at `today.AddDays(60).AddHours(16)`, with a one-hour duration, valid Category/Organizer references, and Capacity 40. Predict what happens in a database that already contains the original five sample Events: only the new missing title is inserted, existing sample rows are not updated. Verify both an existing disposable Development database and a freshly migrated disposable database; restore your normal Development connection afterward and do not delete an unrelated database.

**Hints or checkpoints:** The initializer runs in Development and adds **missing** fixed-key sample rows even if unrelated Events already exist; use a separately named disposable database with valid Category and organizer keys. Existing matching sample rows are retained without updates.

**Deliverable:** The seed change (on a practice branch if required), one screenshot or browser observation showing its chronological position, and an explanation of why editing seed code alone does not update existing rows.

**Completion and verification:** Verify the new sample row appears **once** in both approved disposable databases, while previously seeded Event dates and IDs remain unchanged. Run the initializer twice and verify no duplicate sample rows.

**Challenge (optional):** Add a Draft Event with similar title text and confirm it remains absent from public browsing.

## Exercise: challenge an automated result

**Purpose and expected outcome:** Design checks that differentiate a correct controller result from a full user-visible contract and from database enforcement.

**Prerequisites and SRS ties:** The per-test, GUID-named SQL Server database fixture and two existing focused tests; relevant `FR-SRH-003`, `BR-EVT-001`, and `BR-EVT-003`. Keep database ownership and exact-name cleanup safeguards.

**Task:** Implement one of the optional extension tests for positive Details retrieval or invalid Capacity. Explain its arrange/act/assert boundaries. Identify the separate browser or migration scenario that complements the new test. Review the test's cleanup path and exact generated-database ownership.

**Hints or checkpoints:** Test against a fixture-owned migrated SQL Server database and fixed clock; inspect actual schema constraints rather than relying on an in-memory provider.

**Deliverable:** One additional regression test, its recorded execution result when available, and a brief explanation of the behavior it verifies.

**Completion and verification:** Run the test against the authorized SQL Server fixture and record its actual result. When a test environment is unavailable, label the activity `NOT EXECUTED`.

**Challenge (optional):** Introduce an Event with `StartDateTime == EndDateTime` and test the date-order constraint in a fresh fixture-owned database.

# Troubleshooting Common Problems

| Symptom | Likely cause / diagnosis | Correction / verification |
| --- | --- | --- |
| Provider extension method not recognized | Required EF Core provider/package or `using` directive is missing. | Check installed provider version and namespace; rebuild after restoring the documented package. |
| `dotnet ef` command not recognized | Repository-local tool was not restored or correct CLI version is absent. | Restore the local tool and run `dotnet ef --version` before migrations. |
| SQL Server connection fails | LocalDB is stopped or approved SQL Server credentials/endpoint are incorrect. | Check instance availability and sanitized Development connection configuration. |
| EF command cannot find `DefaultConnection` | Design-time command uses an unexpected environment. | Set/forward Development environment and inspect config providers without exposing secrets. |
| EF SQL logging is missing | Log level or category excludes command output. | Check configured EF Core command logging and rerun one bounded request. |
| Migration has no schema operations | Model changes were not recognized or wrong project/context selected. | Inspect model snapshot, migration source, and target project/context. |
| Development seed insert fails | Invalid FK, check-constraint value, or incompatible seed preconditions. | Inspect real SQL exception and seed references in a disposable database. |
| Draft or Cancelled Events appear in catalog | Public-eligibility predicate was removed or broadened. | Restore Published and future-start filters and rerun catalog regression. |
| Category data fails in catalog | Relationship/migration mismatch or missing Include. | Inspect FK, model navigation, and eager-loaded query. |
| Organizer name/data fails in Details | Organizer reference does not resolve to persisted user row. | Inspect seeded user key and relationship and requery Details. |
| Razor View model type mismatch | Controller result and `@model` declaration disagree. | Align the View with the controller and reload the rendered page. |
| New seed Event does not appear | Initializer skips databases already containing Events. | Use a new approved disposable Development database to test revised seed recipe. |
| Isolated SQL Server test cannot start | LocalDB is missing/stopped, the optional SQL Server override is invalid, or database creation/migration fails. | Check `sqllocaldb info MSSQLLocalDB`, verify an optional `CEMS_TEST_SQLSERVER` points to `master`, and inspect SQL permissions. Never redirect tests to an existing application database. |
| Catalog or Details CSS missing | Stylesheet request or class wiring is broken. | Inspect `/css/site.css` request and markup, then reload at narrow viewport. |

The table gives a quick diagnosis. Use the following detailed walkthroughs when a symptom needs additional code or environment-specific checks.

## Diagnostic walkthroughs


### `UseSqlServer` or `UseSqlite` is not recognized


Confirm the provider package is installed:

```cmd
dotnet package add Microsoft.EntityFrameworkCore.SqlServer --version 10.0.13 --project ^
src\CampusEvents
```

The optional SQLite exploration uses its separate compatible 10.0.13 package only outside the canonical SQL Server workflow. Also check that `Program.cs` contains:

```csharp
using Microsoft.EntityFrameworkCore;
```

### `dotnet ef` is not recognized


From the outer solution root, use `dotnet tool restore` when the repository contains `.config/dotnet-tools.json`. Otherwise install the documented `dotnet-ef` **10.0.13** local tool using the EF CLI setup. Check `dotnet ef --version` before running migrations.

### The SQL server connection fails


Confirm the SQL Server LocalDB instance:

```cmd
sqllocaldb info MSSQLLocalDB
```
If you are on macOS/Linux, verify reachability and credentials for the instructor-provided SQL Server Development endpoint rather than running `sqllocaldb`. For optional SQLite learning, its `sqlite3 --version` check does not diagnose SQL Server connectivity.

Verify that your Development user secrets or environment provide:

```text
ConnectionStrings:DefaultConnection = (Development SQL Server connection string from User Secrets)
```

or that `system environment variables` contains:

```text
ConnectionStrings__DefaultConnection = (Development SQL Server connection string from your environment)
```

**NOTE:** In JSON, the backslash appears as `\\`.

Use the following commands to verify:

```cmd
dotnet user-secrets list --project src\CampusEvents
```

For a terminal-process environment override, use `set ConnectionStrings__DefaultConnection` on Windows to check whether the variable is defined. Do not copy connection-string secrets into shared logs.

### An EF command cannot find `DefaultConnection`


The connection string is in Development configuration. Schema-changing EF commands in this lesson must forward that environment, for example:

```cmd
dotnet ef database update --project src\CampusEvents --startup-project ^
src\CampusEvents -- --environment Development
```

### EF Core SQL does not appear in the application output


Confirm that the application is running with the Development logging configuration and that `appsettings.Development.json` contains:

```json
"Microsoft.EntityFrameworkCore.Database.Command": "Information"
```

Then request `/Events` again and look for a log category similar to:

```text
Microsoft.EntityFrameworkCore.Database.Command
```

### The migration is empty


Check that:

- `ApplicationDbContext` derives from `DbContext`
- its `DbSet` properties exist
- SQL Server and Design packages are installed at version `10.0.13`
- `Program.cs` registers `ApplicationDbContext`

### Seed insertion fails


Check that each Event satisfies:

```text
StartDateTime < EndDateTime
Capacity > 0
```

Also confirm that its category and organizer rows exist.

### `/Events` shows Draft or Cancelled Events


The final query must include both:

```csharp
eventItem.Status == EventStatus.Published && eventItem.StartDateTime > now
```

Do not load every row and hide unwanted Events only in Razor. That would move the public-data boundary into the presentation layer. It would also over-fetch rows, increase in-memory data exposure and resource use, and make the page dependent on every rendering path remembering to suppress non-public data. Keep the eligibility predicate in the database query.

### Category data fails in the catalog


The Index query needs:

```csharp
.Include(eventItem => eventItem.Category)
```

because the View reads `eventItem.Category.Name`.

### Organizer data fails on the Event page


The one-Event query needs:

```csharp
.Include(item => item.Organizer)
```

because the page reads the organizer's `DisplayName`.

### The Razor model does not match the controller


The final Index View expects:

```cshtml
@model System.Collections.Generic.IReadOnlyList<CampusEvents.Models.Event>
```

and the controller passes the Event collection returned by `ToListAsync()`.

The Event page expects:

```cshtml
@model CampusEvents.Models.Event
```

and the controller passes one Event object.

### Your new seed Event does not appear after editing `DbInitializer`


The initializer only inserts **missing** fixed sample titles for the seed Organizer. If a sample Event already exists, editing its initializer template will **not** overwrite its persisted values. If a newly added sample Event still does not appear, confirm the configured Development database, the `OrganizerId` and title matching, the Event status and start time, and whether the initializer actually ran. Use an instructor-confirmed disposable database for reproducible verification; do not delete an unrelated database.

### An isolated SQL server test cannot start


On Windows, run `sqllocaldb info MSSQLLocalDB` and verify the LocalDB service is available; the default connection needs no environment variables. The fixture connects to `master`, creates a private GUID-named database, and deletes that database only. If you set `CEMS_TEST_SQLSERVER`, confirm the connection targets `master` and the selected authorized SQL Server endpoint permits database creation and deletion. Do not pass `DefaultConnection` or point tests to an existing application database. SQL connectivity failures and migration failures are distinct from test assertion failures; inspect the full exception. Confirm the .NET 10 SDK, `net10.0` target, `dotnet-ef` 10.0.13 and the test-only fake-clock dependency when diagnostics concern compilation or migration.

### The catalog or Details page has no CEMS styling


Check that `src/CampusEvents/wwwroot/css/site.css` contains the `event-card`, `event-card-title`, `cems-visually-hidden`, `event-detail`, and `event-detail-facts` rules. Confirm the corresponding class names match the Razor markup exactly.

The shared `Views/Shared/_Layout.cshtml` should link to `~/css/site.css` with `asp-append-version="true"`, and `Program.cs` should serve static files. In the browser's Network panel, verify that `/css/site.css` loads successfully. The catalog and Details pages use that same stylesheet; they do not need page-specific CSS links or client-side libraries.

# Checking Your Understanding

<details><summary>What is the difference between <code>CategoryId</code> and <code>Category</code> on an Event?</summary>

`CategoryId` is the foreign-key value stored on the Event row. It identifies which Category the Event belongs to.

`Category` is the navigation property that gives C# code access to the related `Category` object when EF Core loads that relationship.

</details>

<details><summary>Why does an Event contain both <code>OrganizerId</code> and <code>Organizer</code>?</summary>

`OrganizerId` is the foreign-key value stored on the Event row and identifies the related organizer.

`Organizer` is the navigation property that EF Core can populate with the related `ApplicationUser` object. Together, they provide both the stored relational reference and an object-oriented way to work with the organizer in C#.

</details>

<details><summary>What does <code>ApplicationDbContext</code> inherit from, and what role does it play in CEMS?</summary>

`ApplicationDbContext` inherits from EF Core's `DbContext`.

It represents CEMS's database context and provides EF Core with the configuration and entry points needed to query and save `ApplicationUser`, `Category`, and `Event` data. It also defines relationships and database constraints for the current model.

</details>

<details><summary>What is the purpose of the <code>InitialCemsCatalog</code> migration?</summary>

The `InitialCemsCatalog` migration records how EF Core believes the relational database schema should be created to match the C# model.

It creates the initial tables, primary keys, foreign keys, relationships, and check constraints needed for the CEMS catalog.

</details>

<details><summary>Why are Draft and Cancelled Events stored in the database but absent from normal public browsing?</summary>

Draft and Cancelled Events are valid stored Event records, but they do not satisfy the public browsing rule.

The public query requires an Event to have `EventStatus.Published`, so Draft and Cancelled Events remain in the database for their lifecycle state but are excluded from the public catalog.

</details>

<details><summary>Why is <code>.OrderBy(eventItem =&gt; eventItem.StartDateTime)</code> part of a product requirement rather than merely a visual preference?</summary>

CEMS SRS requires upcoming Events to appear in chronological order by start time. The ordering therefore determines how the public catalog is supposed to behave, rather than serving only as a presentation choice.

Applying `OrderBy` in the query ensures the Events are returned from the database in the required order.

</details>

<details><summary>Why does the Index query use <code>.Include(eventItem =&gt; eventItem.Category)</code>?</summary>

The Index View displays each Event's Category name through `eventItem.Category.Name`.

`.Include(eventItem => eventItem.Category)` tells EF Core to load the related Category data along with the Events so that information is available when the View renders the catalog.

</details>

<details><summary>Why does the one-Event query include both Category and Organizer?</summary>

The Event detail page displays information from both related records: the category `Name` and the organizer's `DisplayName`.

The query therefore includes both `Category` and `Organizer` so EF Core loads the related objects needed by the View.

</details>

<details><summary>Why is configured capacity available now while current registration availability is not?</summary>

Configured capacity is stored directly on the Event through its `Capacity` property, so it is already available.

Current availability is calculated by subtracting the number of active registrations from configured capacity. An Event’s relationship to its active registrations supplies that count.

</details>

<details><summary>What HTTP status should a nonexistent Event request produce, and why is that preferable to an unhandled null-reference exception?</summary>

A nonexistent or unavailable Event request should produce HTTP `404 Not Found`.

A 404 is the appropriate controlled response when the requested resource does not exist. It allows CEMS to display a human-readable not-found page while preserving the correct HTTP status, instead of allowing `null` to reach code that assumes an Event exists and causing an unhandled exception.

</details>

<details><summary>What is the difference between the EF Core model, the <code>InitialCemsCatalog</code> migration, and the SQL Server schema?</summary>

The EF Core model is the persistence model EF Core builds from the C# entity classes and `ApplicationDbContext` configuration.

`InitialCemsCatalog` is the versioned schema-change description generated from that model for this stage of the project.

The SQL Server schema is the set of tables, columns, keys, and constraints that physically exists after the migration is applied with `dotnet ef database update`.

</details>

<details><summary>What does <code>ToListAsync()</code> change in the Index query?</summary>

Before `ToListAsync()`, the EF Core LINQ operations describe the database query to perform. `ToListAsync()` executes that query asynchronously and materializes the returned rows as C# `Event` objects.

Because the Published/upcoming predicate appears before this materialization point, DBMS can restrict the result before those Event objects enter application memory.

</details>

<details><summary>What observable evidence shows the transition from a composed EF Core query to executed SQL and then to materialized C# objects?</summary>

Before execution, the debugger can inspect the composed `query`, and `query.ToQueryString()` can show EF Core's SQL representation without running the query.

When `await query.ToListAsync()` executes, an `Information`-level `Microsoft.EntityFrameworkCore.Database.Command` log entry provides runtime evidence that EF Core sent a database command.

After the `await` completes, the debugger can inspect the populated `List<Event>`. Those objects are the materialized result of the database operation.

</details>

<details><summary>Why is filtering non-public Events only in Razor a poor design even if the final HTML appears correct?</summary>

Razor is a presentation layer. Loading broad data and hiding rows only during rendering over-fetches data, unnecessarily exposes non-public rows to application memory, increases processing and transfer work, and creates more opportunities for accidental disclosure.

The public eligibility rule belongs in the data query so the controller receives only the rows that this public workflow should operate on.

</details>

<details><summary>Why does a foreign key still matter when application code normally supplies a valid related ID?</summary>

A foreign key matters because it makes the database itself guarantee that relationships stay valid, even if application code, scripts, migrations, or concurrent transactions make mistakes. Application validation improves behavior; foreign keys protect data integrity.
</details>

---

# Summary

You learned how to **design and justify a relational structure** by identifying independent records, primary keys, relationship cardinalities, required foreign keys, and integrity rules. The modeling exercise and database inspection also showed why application validation complements, rather than replaces, constraints on saved data.

You **compared three distinct stages of schema evolution**: the conceptual/application representation, a reviewable migration, and the schema physically applied to a database. Checking these stages in order is a general safeguard against accidental or undocumented data changes.

You **analyzed composed queries and their execution boundaries**, including when predicates and ordering are applied, how related data is requested, and how materialized objects differ from a query description. A controlled query and fixed clock supported that reasoning. You also **distinguished eligibility constraints from hard resource bounds**, explaining why correct filtering does not automatically prevent large result sets or long-running work.

You **designed and assessed resource-oriented web behavior** by separating public data selection from presentation, preserving meaningful success and not-found HTTP outcomes, and using semantic, keyboard-operable, responsive markup. These principles are useful even when the presentation technology changes.

You **developed reproducible verification practices** through independently arranged data, negative and boundary cases, isolated test environments, and repeatable manual scenarios. Finally, you **applied secure configuration and version-control practices** to associate reviewed changes with reproducible application states without disclosing secrets. Together, these practices make iterative full-stack development easier to reason about, validate, and maintain.

# Checkpoint

**Inherited baseline:** A `net10.0` `CampusEvents` ASP.NET Core MVC application with conventional routing, `/Events` rendering a temporary controller-supplied title, a Razor string-model View, shared layout and stylesheet, a sibling xUnit project, Git repository, and an initial traceability record.

**New in this lesson:**

- The application now defines `EventStatus`, `Event`, `Category`, and the string-key `ApplicationUser` organizer reference.
- `ApplicationDbContext` maps the Event–Category and Event–Organizer relationships to SQL Server, configures restricted deletes and Event integrity checks, and uses the `AspNetUsers` table name as a stable ownership foundation. The application has an `InitialCemsCatalog` migration, a configurable Development connection, dependency-injected database context and application clock, and a Development-only initializer with eligible and ineligible example Events.
- `EventsController.Index()` replaces the temporary title with an asynchronous SQL Server query for Published future-start Events in stable chronological order. The existing `Views/Events/Index.cshtml` now receives a collection of Events and renders linked catalog cards.
- `EventsController.Details(id)` retrieves a Published Event and its Category/Organizer, renders `Views/Events/Details.cshtml`, or returns the controlled `EventNotFound` View with HTTP 404. Shared `site.css` supports the catalog and responsive Details presentation. The existing test project now contains migration-backed SQL Server controller regression checks and their guarded isolated-database fixture; the existing `docs/traceability.md` file records the lesson's implementation and verification status.

**Resulting application baseline:** CEMS now has a relational, database-backed **public catalog and Details slice**. `/Events` returns the eligible Published upcoming collection in deterministic order; Details links provide an individual Published resource or a controlled 404 response. The public retrieval query preserves deterministic ordering and the selected eligibility boundary.

# Next Lesson Preview

Lesson 3 — **Search, Filtering, Pagination, and View-Specific Models** — extends the existing Published/upcoming query with optional title/location search, Category selection, and bounded result pages. You will keep the eligibility rule and deterministic ordering in the database, count matches before selecting a page, and introduce a view-specific model that carries both persisted results and temporary request state. The relational model, query composition, Razor catalog, and regression baseline established here make that incremental change possible without restarting the application architecture.

# Appendix

## Relational persistence technologies and schema evolution

### SQL Server and SQLite as relational providers

SQL Server is the CEMS implementation and integration-test provider. SQLite offers a useful comparison because its embedded deployment model, data-type system, constraint handling, migration operations, and SQL translation differ. An EF Core entity model may look similar across providers while its effective schema and query behavior differ. For instance, SQL Server's `datetime2` stores a date and time without a zone offset; SQLite typically represents dates using provider conversions to storage classes.

Use a separate project or experimental database when comparing providers. Inspect the generated migration operations and actual constraints for each provider, rather than assuming provider portability from identical C# classes. The canonical Event migration and SQL Server tests remain the reference for this project.

### Schema changes across environments

For example, adding an optional `Room` field permits existing Event rows to retain a null value until updated. Replacing an existing required `Location` column, by contrast, calls for planning how existing Event data will be preserved and how deployed application versions read it. A destructive migration that drops or renames data requires explicit compatibility and recovery planning. Teams commonly evaluate backups, phased application/database deployment, schema ownership, migration credentials, and rollback procedures before applying changes to shared environments.

The reliable sequence is model change → migration review → targeted database update → schema inspection → application verification. Apply it to the existing Event model when considering an additional index or constraint.

## Configuration and operational resource controls

### Configuration precedence and secret handling

ASP.NET Core configuration combines sources, with higher-priority providers overriding matching keys from lower-priority providers. Under the default web application builder, the application configuration order commonly progresses from `appsettings.json` to environment-specific settings, Development User Secrets, environment variables, and command-line arguments. Explicitly added providers or custom host configuration can alter that order.

For example, suppose `appsettings.json` defines `ConnectionStrings:DefaultConnection` as `Server=development;...`, while the environment variable `ConnectionStrings__DefaultConnection` provides `Server=testing;...`. The default configuration order selects the environment value. A command-line override of the same key takes precedence under the default setup. Determine the effective *source* during diagnosis without printing credentials. A fallback expression such as `GetConnectionString(...) ?? "fallback"` is application code executed after configuration resolution; it is not a separate configuration provider.

User Secrets store Development values outside the repository, but are not encrypted secret storage. Deployed applications use the hosting environment's approved protected configuration mechanism, such as a managed secrets service. Keep migration/testing credentials restricted to their intended databases.

### Operational resource bounds and design alternatives

An eligibility predicate constrains which records enter a query result, while a result cap constrains how many can be returned. Other systems impose bounds on elapsed time, input size, work frequency, parallelism, or retries. Match a control to the resource or failure mode it addresses:

| Control | Primary concern | Design trade-off |
| --- | --- | --- |
| Eligibility predicate (`Where`) | Which records qualify | Correct scope, but potentially many matching rows |
| Projection (`Select`) | Fields or representation per row | Smaller payload and less coupling; row count is unchanged |
| Result cap (`Take`) | Maximum returned rows | Predictable response size; requires a defined selection policy |
| Pagination | One bounded portion of an ordered collection | Additional navigation and stable-order requirements |
| Timeout or cancellation | Elapsed time or obsolete work | Must distinguish deadline limits from cooperative cancellation |
| Rate/concurrency limit | Requests over time or simultaneous work | May delay or reject callers to protect shared resources |
| Bounded retries/backoff | Transient-failure recovery | Needs limits and idempotency awareness |
| Transaction scope and locking | Consistency during multi-operation decisions | Broader protection can increase contention |

For the public catalog, stable chronological ordering and eligibility are already meaningful controls. A catalog with a growing matching population can apply a defined page-size limit while preserving the public predicate. Evaluate more elaborate controls using real workload and failure requirements.

### Isolated test databases and connection pooling

Database integration tests benefit from distinct, disposable databases so each run begins with a known schema and dataset. A connection pool may retain idle physical connections after a context is disposed; SQL Server can treat those connections as active while an isolated database is being dropped. Clearing the pool for the fixture connection string targets those connections without disrupting other test connections. The fixture creates a GUID-named database, migrates it, disposes its contexts, and deletes only that specific name. Failed cleanup should surface a diagnostic, and interrupted runs require exact-name, ownership-verified cleanup.

Database isolation protects test data boundaries; it does not isolate CPU, memory, or server-wide settings when tests share the same SQL Server instance. For larger suites, balance parallelism, database creation cost, and resource contention.

## Retrieval and persistence design alternatives

### Entity materialization versus projection

The introductory catalog materializes `Event` entities with related Category data. A purpose-specific projection shapes a response around the fields needed for each catalog card:

```csharp
var cards = await query.Select(eventItem => new
{
	eventItem.Id,
	eventItem.Title,
	eventItem.StartDateTime,
	CategoryName = eventItem.Category.Name
}).ToListAsync();
```

A projection reduces the per-row data shape, whereas `Take` reduces the maximum number of rows. These options can be combined with eligibility, ordering, and asynchronous execution. Purpose-specific models become particularly useful when a page needs both stored values and transient retrieval state.


### Local time, UTC, and offset-aware values

A local date/time such as `2026-11-05 18:00` expresses the time shown on a campus clock. UTC identifies an instant on a common timeline, and an offset-aware timestamp associates a particular instant with a numeric UTC offset. An offset alone does not identify a geographic time zone or its future daylight-saving rules.

CEMS uses a single campus-local convention for stored Event dates and the application clock. In a multi-zone scheduling system, preserve the intended local date/time together with a named zone identifier (for example, an IANA zone) so the event can be interpreted under the applicable civil-time rules; resolve and store a corresponding instant when the workflow requires one. Test ambiguous and nonexistent local times at daylight-saving transitions, and define how scheduling changes are handled if time-zone rules are updated.

### Offset and keyset retrieval

Offset pagination (`Skip`/`Take`) supports direct numbered pages and clear links, although deep offsets can become expensive on large datasets. Keyset retrieval continues after the last seen ordering key and often performs better for forward traversal, while complicating arbitrary page-number jumps. Either can produce normal server-rendered HTML. Choose based on the interface contract, dataset size, and behavior under concurrent modifications.
