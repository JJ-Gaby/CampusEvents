# CEMS Lesson 3 — Working Traceability Evidence

Date: 2026-11-15, America/La_Paz.
Commit reference: `lesson-03`, subject `Lesson 03: Implemented and Verified`.
Previous checkpoint: `76ec750` (Lesson 02). No GitHub push is part of this lesson.
Lesson 2's working record is retained as `docs/lesson-02-traceability.md`.

| Requirement / Reference | Implementation / Artifact | Verification / Evidence | Status | Notes |
| --- | --- | --- | --- | --- |
| UC-001 | Events Index and retained Details | Search, Category, pagination and Details browser scenarios | PARTIAL | Registration availability and Cancelled Details remain open |
| FR-SRH-001–002 | EventsController.Index | SQL Server eligibility/order cases and private baseline | VERIFIED FOR LESSON SLICE | Published/future base predicate retained; ordered start/ID |
| FR-SRH-003 | Details action and View | Details 404 test and keyboard link walk | VERIFIED FOR LESSON SLICE | Existing Details page and HTML 404 retained |
| FR-SRH-004 / FR-SRH-008 | Details scope | Inherited capacity display; no availability/Cancelled extension | PARTIAL | Registration counts and previously Published Cancelled Details remain future work |
| FR-SRH-005–006 | Index predicates and GET form | Combined filter test; title/location and Category browser checks | VERIFIED FOR LESSON SLICE | Search OR applies within the public set; Category intersects it |
| FR-SRH-007 | Index empty state | Zero-match test and HTTP 200 checks | VERIFIED FOR LESSON SLICE | No range or page links for zero matches |
| FR-SRH-009 | Count, bounded rows and navigation | 10/25/50 tests, real HTTP ranges, browser Next/Last/Clear | VERIFIED FOR LESSON SLICE | All page links retain search, Category and size |
| SRS §11 | Page normalization | Missing/zero/negative/over-range cases; malformed page over HTTP | VERIFIED FOR LESSON SLICE | Invalid page selects first; over-range uses filtered last; empty selects page 1 |
| Lesson page-size contract | Presence check, allowed sizes, ErrorPageViewModel and shared View | Unsupported/malformed explicit input cases and HTML 400 checks | VERIFIED FOR LESSON SLICE | Missing size defaults to 10; explicit invalid size never silently defaults |
| NFR-PER-002 | SQL-side query | Logs show COUNT, WHERE, ORDER BY, OFFSET/FETCH before materialization | PARTIAL | Returned rows bounded; no representative performance benchmark claimed |
| NFR-ACC-001–003 / NFR-COM-002 | Labels, native controls, page nav and project CSS | Edge keyboard and 390px viewport observations | VERIFIED FOR LESSON SLICE | Limited to observed catalog/error interactions; no global accessibility certification |
| AC-001 / AC-010 | Catalog and regression suite | 24 final cases plus HTTP/browser scenarios | PARTIAL | Final-system acceptance, authorization and registration tests remain open |

## Environment and test inventory

Windows, .NET SDK 10.0.402 / runtime 10.0.13, EF Core/dotnet-ef 10.0.13,
Microsoft.AspNetCore.Mvc.Testing 10.0.13, SQL Server 2022 Express LocalDB,
Microsoft Edge. Host local zone is SA Western Standard Time (UTC-04).
Automated tests use explicit fixed UTC-04 clock fixtures. The institution's
approved zone policy is not supplied, so no broader deployment claim is made.

Final commands: `dotnet build CampusEvents.slnx --no-restore`,
`dotnet test CampusEvents.slnx --no-build --list-tests`, and
`dotnet test CampusEvents.slnx --no-build --logger "trx;LogFileName=lesson-03.trx"`.
Build: 0 errors, 0 warnings. Final suite: 24 discovered, 24 passed, 0 failed,
0 skipped. The first pre-exercise run contained 23 cases; the independent
default-size boundary adds the twenty-fourth.

| Group | Cases | Observed outcome |
| --- | --- | --- |
| Authored Lesson 3 controller inventory | 14 | Passed: eligibility, Details 404, combined filtering, allowed sizes, rejected sizes, page normalization and zero matches |
| Authored real-HTTP binding inventory | 3 | Passed: malformed page normalizes; malformed/zero size returns useful HTML 400 |
| Retained Lesson 2 seed, positive Details and integrity checks | 6 | Passed |
| Independent omitted-size exercise | 1 | Passed: 12 matches, second page, default size 10, two rows; exact-now excluded |

All tests use the unchanged exact-owned GUID database fixture and real migrations.
Fixture disposal completed without cleanup errors. The local TRX result is in
`tests/CampusEvents.Tests/TestResults/lesson-03.trx` and is ignored by Git.

## Private Development dataset and observed requests

Verification used a new personally controlled LocalDB database,
`CemsL03Practice_20261115_c04fcf81`, at `http://localhost:5106`, in Development.
The normal `CampusEventsDb`, User Secrets and permanent seed definitions were
not changed. Because Lesson 2's exercise added Study Skills Workshop, that
single exact-owned sample was excluded only in the private copy to reproduce
this lesson's original five-row baseline. Academic ID was 1, Social 2, Career 3.

| Baseline request or interaction | HTTP / actual result |
| --- | --- |
| `/Events` | 200; Library Research Night then Career Networking Lunch; Showing 1–2 of 2 events; size 10 |
| Search Library, All categories | 200; Library Research Night only |
| Search Student Center | 200; Career Networking Lunch only, Draft excluded |
| Academic, no text | 200; Library Research Night only |
| Social, no text | 200; no results, no range or page links |
| Search Movie | 200; no results, Cancelled excluded |
| Library + Career | 200; no results |
| Library + Academic | 200; one Event; criteria retained in form and URL |
| Published Details title | Existing page with description, Category, location, schedule, organizer and configured capacity; back link works |
| Draft/missing Details | Retained automated HTML-404 contract and direct-controller named View/status assertions |

The guarded temporary snippet then inserted exactly 52 practice rows only in
this private database. Result: 57 stored Events, 54 eligible public results;
the three excluded baseline rows remained stored. Equal-start pairs exercise
tie ordering. No practice rows were added to the application's normal database.

| Request | HTTP / actual visible result |
| --- | --- |
| `?page=1&pageSize=10` | 200; 10 cards; Page 1 of 6; Showing 1–10 of 54; Next/Last only |
| `?page=6&pageSize=10` | 200; 4 cards; Showing 51–54 of 54; First/Previous only |
| `?page=2&pageSize=25` | 200; 25 cards; Showing 26–50 of 54; all four links |
| `?page=3&pageSize=25` | 200; 4 cards; Showing 51–54 of 54 |
| `?page=2&pageSize=50` | 200; 4 cards; Page 2 of 2 |
| `?page=0`, `?page=-3`, `?page=abc&pageSize=25` | 200; normalized first page, with bounded selected size |
| `?page=999` | 200; last unfiltered page, 4 cards |
| Practice title + Academic + `page=999&pageSize=25` | 200; filtered total 52, Page 3 of 3, 2 cards, Showing 51–52 of 52 |
| `?pageSize=0`, `-10`, `37`, `abc`, `1000000`, empty value | 400; Invalid Page Size, allowed-size message and Return to Events link |
| `?search=unmatched-value` | 200; no-results message, zero cards, no range or pagination |

Before the shared View was enabled, the required minimal-400 experiment
observed size 1 and abc returning status 400 with a zero-length body. After
restoration, invalid sizes kept status 400 and rendered explanatory HTML.

Browser navigation with Practice title, Academic and size 25 showed 25 rows on
page 1, 25 on page 2 and 2 on page 3. URLs retained all criteria. Submitting
Library from page 3 reset to page 1 with size 25. Clear removed search/Category,
kept size 25 and restarted page 1. The form contained no page input.

SQL logs showed a complete matching COUNT followed by separately bounded page
retrieval: public eligibility plus optional text/Category WHERE clauses,
ORDER BY StartDateTime/Id, OFFSET/FETCH, and Category loading. The Category
dropdown query remained independent of the filtered Event set.

## Browser, exercises and cleanup

Desktop and 390 × 844 screenshots were inspected. At mobile width the form had
one column; document scrollWidth was 375, no greater than viewport 390. All
three labels referenced existing control IDs. Tab moved through Category,
size, Apply filters and Clear with solid focus outlines. Catalog Details
and return links worked with Enter. Invalid Page Size HTML was readable on
mobile. Styles came from `/css/site.css`; no CSS/JavaScript framework was added.

The uncommitted tie-order/Next-link experiment and independent test are
documented in `docs/lesson03-exercises.md`, together with the required retrieval
diagram and offset/keyset decision record. Restored Next was rechecked in the
browser: `pageSize=25&page=2`, 25 cards, Showing 26–50 of 52 events.

After verification, the guide's exact title-prefix/description/location/owner
cleanup selected 52 rows in the private database. Its count guard passed;
exactly 52 rows were removed. Five baseline rows remained and the browser again
showed Showing 1–2 of 2 events with no practice cards. All temporary startup,
deletion and defective-query/link code was removed before the final tests.

Permanent model, Data, migration and isolated-fixture files are unchanged from
Lesson 2. The normal Development database retains the Lesson 2 workshop exercise,
so normal startup can show three eligible Events rather than the private
five-row verification scenario's two. Future behavior remains tracked as
PARTIAL/DEFERRED above; no new schema or registration features were added.
