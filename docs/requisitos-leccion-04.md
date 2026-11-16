# Lesson 04 — Requirements and verification

Date: 2026-11-15, America/La_Paz.
Branch / commit reference: `lesson-04`, `Lesson 04: Implemented and Verified`.
Application and tests: net10.0. SDK 10.0.402, EF Core/Identity 10.0.13,
SQL Server 2022 Express LocalDB. Browser: Microsoft Edge.
Development: local SQL Server database; HTTP and trusted local HTTPS profiles.
Tests: separate GUID-named databases created from real migrations.

| SRS reference | Implementation | Check | Result |
| --- | --- | --- | --- |
| UC-002, FR-ACC-001–002 | Register input model, action and Identity store | New account, duplicate email, four rejected-input cases | Verified for lesson slice; rejected requests create no unintended account |
| FR-ACC-003–004 | Login and SignInManager | Valid credentials, wrong password, unknown email | Verified; generic Invalid login attempt for both failures |
| FR-ACC-005, NFR-SEC-003 | Protected Logout POST | Tokenless POST, valid POST, subsequent protected GET | Verified; tokenless request rejected without signing out; valid request clears session |
| FR-ACC-006, BR-AUT-001, BR-AUT-004 | Participant Authorize and test-only Organizer boundary | Anonymous request, forged header, authenticated requests and roles | Verified; identity is reconstructed from the valid ticket, not client identity headers |
| FR-ACC-007 | Local return validation | Participant local return and external-return attempt | Verified; local return succeeds; external destination falls back to Events |
| NFR-SEC-001–002 | IdentityDbContext, managers, roles and migration | Ownership continuity, role transition, normalized fields, password/stamp presence | Verified for local lesson scope |
| NFR-SEC-004–005 | User Secrets, safe account errors and AccessDenied | Source review, browser feedback, sanitized evidence | No classroom passwords or live cookies recorded in source/evidence |
| AC-002, AC-010 | Cumulative account/catalog regression suite | Full suite, populated database upgrade and browser checks | Lesson-level evidence recorded below; final system acceptance remains broader |

## Migration and ownership continuity

Before upgrade: one user, `seed-organizer-001`; six Events with IDs 1–6, all
owned by that same user; three Categories. The sixth Event is the retained
Lesson 2 Study Skills Workshop exercise.

Generated `AddIdentityAuthentication` adds Identity columns to AspNetUsers,
the six role/claim/login/token tables, and Identity indexes. It does not drop
AspNetUsers or Events, change user IDs, replace the Event table or modify its
Category/Organizer foreign keys. UserNameIndex is unique with a non-null filter;
EmailIndex is a normalized lookup index. Identity enforces the configured
unique-email rule. Newly nullable normalized fields/stamps are populated by
UserManager during Development reconciliation; bool/count columns have their
generated defaults. Down removes the new Identity schema objects/columns.

After migration and seeding, the original user ID, Event IDs/OrganizerIds,
Category rows, both Event foreign keys and both Event check constraints matched
the before-upgrade snapshot. The Administrator was added with its fixed ID;
the later browser registration added one ordinary account. No existing database
was dropped. The original migration history remains alongside the new migration.

Read-only continuity queries used:

```sql
SELECT Id, DisplayName FROM dbo.AspNetUsers WHERE Id = 'seed-organizer-001';
SELECT Id, OrganizerId FROM dbo.Events ORDER BY Id;
SELECT Id, Name FROM dbo.Categories ORDER BY Id;
SELECT name FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('Events');
SELECT name FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID('Events');
```

The Organizer and Administrator had normalized identity fields, a password hash,
a security stamp and the intended membership after provisioning. Real local
HTTPS sign-ins for both seeded accounts returned 302 followed by Participant
200, showing their own identity and role label. Password/hash values and cookies
were not printed. The temporary inspection source was removed after verification.

## Automated evidence

Commands: `dotnet build CampusEvents.slnx --no-restore`, test discovery and
`dotnet test CampusEvents.slnx --no-build --logger "trx;LogFileName=lesson-04.trx"`.
Build: 0 errors, 0 warnings.

Final inventory: 41 cases: 24 inherited, eight guide authentication cases,
four registration-boundary cases, one role-transition exercise, one legacy-email
conflict case and three signed-in-name cases. Tests use the unchanged SQL Server
fixture with both real migrations; serialization is enabled as the lesson asks.
Final execution: 41 passed, 0 failed, 0 skipped. Fixture disposal completed
without cleanup failures. The local TRX result is ignored build/test output.

The eight guide cases cover anonymous return flow, authenticated Participant
access, Participant role denial, duplicate registration, generic login errors,
external return handling, token-protected logout and Organizer access. The
independent role transition compares the same user before trusted assignment
and after a fresh sign-in. The old ticket remains denied. The legacy-conflict
case starts with empty normalized fields and verifies that provisioning fails
before any role or user-role row is created. No test falls back to Development
credentials or an in-memory database provider.

## Browser observations

| Scenario | Observed result |
| --- | --- |
| Anonymous Participant | Login with local ReturnUrl; protected View not exposed |
| Registration | Account confirmation on Events; still signed out until Login |
| Duplicate registration | Username/email already-taken feedback; no second browser account created |
| Login and return | Credentials establish session; local return opens Participant and Signed in as |
| Unknown/wrong credentials | Same Invalid login attempt message |
| External return | Successful sign-in remains on local Events |
| Logout | Rendered form uses POST and antiforgery; later Participant request redirects to Login |
| Access denied | Readable explanatory View and return navigation while signed in |
| Public catalog | Available signed in and signed out; three eligible Events in the retained six-row dataset |
| Search/Category | Library + Academic returns the two actual matching records; Movie + Social returns no results |
| Responsive/keyboard | Register, Login, signed-in navigation and Participant usable at 390 × 844; scrollWidth 390; associated labels and visible keyboard focus |

HTTPS browser checks used the trusted localhost Development certificate.
Lesson 3's pagination and malformed-input cases remain in the passing cumulative
suite; no permanent practice records or test-only role route were added to the
application assembly.

## Repository review

- Only intended application, migration, test, setup and lesson-deliverable files.
- Existing Event/Category models, catalog, pagination and permanent seed definitions retained.
- Isolated SQL Server fixture unchanged; exact-owned database cleanup retained.
- No classroom secrets, database files, temporary inspection source or generated build outputs staged.
- Approved SRS unchanged. Remaining registration availability and Cancelled Details policy remain future work.
