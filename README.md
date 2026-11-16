# Campus Events Management System (CEMS)

Course Project #1 — server-rendered ASP.NET Core MVC application.

## Prerequisites

- .NET 10.x SDK
- SQL Server Express LocalDB on Windows (`(localdb)\MSSQLLocalDB`), or an approved SQL Server endpoint.

## Run from the solution folder

```cmd
dotnet tool restore
dotnet restore
dotnet build
dotnet test
dotnet run --project src/CampusEvents
```

Before running for the first time, configure the Development connection outside
tracked files and apply the existing migration:

```cmd
dotnet user-secrets init --project src/CampusEvents
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=CampusEventsDb;Trusted_Connection=True" --project src/CampusEvents
dotnet ef database update --project src/CampusEvents --startup-project src/CampusEvents -- --environment Development
```

`dotnet-ef` and EF Core use version 10.0.13. Never reset an existing database as
part of normal setup. The Development initializer adds missing sample rows;
it preserves existing sample IDs, dates and values. It is not run in Production.

The host must use the institution-approved campus time zone. This local run used
`SA Western Standard Time` (UTC-04); no institution-wide policy is assumed.
Stored `DateTime` values represent local wall-clock times. Tests use an explicit
fixed UTC-04 clock, independently of the machine clock.

Tests create separate `CemsL02Test_<GUID>` databases from the real migration and
delete only their own databases. The default test endpoint is LocalDB/master;
an authorized alternate test server can be configured with `CEMS_TEST_SQLSERVER`
targeting `master`. Tests never use the application's `DefaultConnection`.

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

The application serves Home, Privacy, a persisted public Events catalog and
Published Event Details through MVC, with shared navigation and project-owned
CSS. The catalog selects only Published future Events, ordered by start time
and ID. It supports title/location search, Category filtering and bounded
10/25/50 pagination. First/Previous/Next/Last links preserve criteria; the GET
form restarts at page 1, and Clear preserves page size. Invalid explicit page
sizes return an explanatory HTML 400. Draft, Cancelled and missing Details
requests retain the inherited HTML 404 behavior.

Development seeds three Categories, one organizer and the five lesson sample
Events. The seed exercise adds a sixth row, Study Skills Workshop, so a fresh
dataset has three public upcoming Events. Configured capacity is displayed;
remaining registration availability and Cancelled direct-link visibility
remain deferred as the lessons specify. Lesson 3 does not change the models,
schema, migrations or permanent Development seed definitions. Its 52 pagination
practice Events were inserted and removed only in a separate private database;
they are not part of normal startup.

Verification evidence is recorded in [docs/requisitos-leccion-02.md](docs/requisitos-leccion-02.md).
The required exercise diagram and explanations are in
[docs/ejercicios-leccion-02.md](docs/ejercicios-leccion-02.md).

Lesson 3 results, dataset boundaries and the commit reference are recorded in
[docs/requisitos-leccion-03.md](docs/requisitos-leccion-03.md). Its exercise
deliverables are in [docs/ejercicios-leccion-03.md](docs/ejercicios-leccion-03.md).
