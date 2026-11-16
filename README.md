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

Lesson 4 adds the `AddIdentityAuthentication` migration. Apply pending migrations
with the same database-update command before starting the upgraded application.
Development startup also requires these four keys in the existing User Secrets
store: `SeedAccounts:Administrator:Email`, `SeedAccounts:Administrator:Password`,
`SeedAccounts:Organizer:Email` and `SeedAccounts:Organizer:Password`. Configure
different emails and strong unique passwords outside tracked files. Passwords
must satisfy the configured minimum of eight characters, uppercase/lowercase
letters and a digit. Do not copy classroom credentials into source control.

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
Published and Cancelled Event Details through MVC, with shared navigation and project-owned
CSS. The catalog selects only Published future Events, ordered by start time
and ID. It supports title/location search, Category filtering and bounded
10/25/50 pagination. First/Previous/Next/Last links preserve criteria; the GET
form restarts at page 1, and Clear preserves page size. Invalid explicit page
sizes return an explanatory HTML 400. Draft and missing Details requests return
HTML 404. Cancelled Details displays a cancellation notice through its known URL.

Register and Login are available to anonymous visitors. Registration returns
to Events with confirmation and does not sign in automatically. `/Participant`
requires authentication and displays the signed-in account name. Logout is an
antiforgery-protected POST. Organizer and Administrator memberships are seeded
only in Development; every authenticated account has Participant access without
a separate Participant role. The Organizer-only probe exists in the test
assembly, not as a production application feature.

Development seeds three Categories, one organizer and the five lesson sample
Events. The seed exercise adds a sixth row, Study Skills Workshop, so a fresh
dataset has three public upcoming Events. Details displays configured capacity,
current Registrations and remaining availability. Lesson 3 does not change the models,
schema, migrations or permanent Development seed definitions. Its 52 pagination
practice Events were inserted and removed only in a separate private database;
they are not part of normal startup.

Verification evidence is recorded in [docs/requisitos-leccion-02.md](docs/requisitos-leccion-02.md).
The required exercise diagram and explanations are in
[docs/ejercicios-leccion-02.md](docs/ejercicios-leccion-02.md).

Lesson 3 results, dataset boundaries and the commit reference are recorded in
[docs/requisitos-leccion-03.md](docs/requisitos-leccion-03.md). Its exercise
deliverables are in [docs/ejercicios-leccion-03.md](docs/ejercicios-leccion-03.md).

Lesson 4 evidence and exercise deliverables are in
[docs/requisitos-leccion-04.md](docs/requisitos-leccion-04.md) and
[docs/ejercicios-leccion-04.md](docs/ejercicios-leccion-04.md).

Lesson 5 adds Organizer-only Manage, Create and owned-Draft Edit forms with
server-side validation, ownership checks and protected server-assigned fields.
Its required evidence and exercises are in
[docs/requisitos-leccion-05.md](docs/requisitos-leccion-05.md) and
[docs/ejercicios-leccion-05.md](docs/ejercicios-leccion-05.md).

Lesson 6 adds guarded Publish/Cancel, Draft deletion confirmation, Published
Edit and direct Cancelled Details. Evidence and required exercises are in
[docs/requisitos-leccion-06.md](docs/requisitos-leccion-06.md) and
[docs/ejercicios-leccion-06.md](docs/ejercicios-leccion-06.md).

Lesson 7 adds current Participant Registrations, live availability, My
Registrations and protected cancellation. Apply the new
`AddParticipantRegistrations` migration with the database-update command above.
Registration and Published Capacity Edit coordinate through SQL Server Event-row
locks; Draft deletion checks current Registrations. Evidence and required
exercises are in [docs/requisitos-leccion-07.md](docs/requisitos-leccion-07.md) and
[docs/ejercicios-leccion-07.md](docs/ejercicios-leccion-07.md).
