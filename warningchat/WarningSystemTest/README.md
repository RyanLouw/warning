# Warning system tests

All tests live in this project. The example project remains reference material.
UI, HTTP integration and database tests start the real application through the
existing Aspire AppHost, wait for its migrations, and use its session-only SQL
Server database. This expansion does not change AppHost or migration files.
Unit tests start neither Aspire nor a browser. Their dependencies use FakeItEasy
(`A.Fake`, `A.CallTo`, argument matchers and explicit call assertions); Moq is
not a dependency of this test project.

## Test groups and files

Paths below are relative to WarningSystemTest:

| Location | Purpose |
|---|---|
| `Unit/` | Controllers, manager commands, workflow rules, claims/roles, Graph adapters, email, attachment helpers and view-model mapping using FakeItEasy fakes. |
| `Tests/LoginTests.cs` | Existing synthetic-role authentication and missing-key checks. |
| `Tests/UI/User/` | User page access, draft creation/editing, discussions, task visibility, read-only team-lead controls, evidence and submission. |
| `Tests/UI/Legal/` | Legal access, one button/state per case, dialogs/cancellation, requests for information/email, decisions, validation, due dates and visibility. |
| `Tests/UI/Admin/` | Admin access, question types/editing, category creation/editing, and separate cases for default questions 1, 2 and 3. |
| `Tests/UI/Chairperson/` | Chairperson access, Hearing exclusion and visibility of issues hidden only from their creator. |
| `Tests/UI/Reporting/` | Individual/combined filters, dates, no results, reset, displayed values, totals and department breakdown against independent SQL. |
| `Pages/` | Reusable page interactions and locators; expected business rules remain in tests. |
| `Tests/Integration/HttpTests.cs` | Actual HTTP authorization, antiforgery, answer writes, invalid requests, PDF response and redirects. |
| `Tests/Database/DataAccessTests.cs` | SQL Server persistence, rollback, transitions, category links, answer upserts, evidence/notes and stored procedures. |
| `Database/TestDatabase.cs` | Deterministic synthetic records and parameterized SQL through Aspire's connection. |
| `Database/ReportOracle.cs` | Independent SQL calculates expected filtered records without calling the application's report manager or using DOM values. |
| `SystemUnderTest/WarningSystemSut.cs` | Lazy Aspire startup, health checks, session database connection and per-run auth key. |
| `Fixtures/BrowserFixture.cs`, `Fixtures/UiTestBase.cs` | Chromium, fresh identity contexts, sequential database resets and failure artifacts. |
| `coverage.runsettings` | Cobertura coverage for application/core code executed inside the test process. |

Before each database-backed test, synthetic Alice/Bob/Cara warnings are replaced
with a known set of statuses, departments, categories, dates and latest actions.
Migration seed records are preserved. The report assertions compare visible row
values and counts with SQL on a separate connection to that same database.
Database-backed tests share a nonparallel collection to prevent reset races.
No developer/production connection string is loaded by the test project.

## Run on Windows

From your `C:\dev\WARNING\src` folder (the directory containing WarningSystemTest):

```powershell
dotnet build WarningSystemTest/WarningSystemTest.csproj -p:WarningSystemTestingBuild=true
pwsh WarningSystemTest/bin/Debug/net10.0/playwright.ps1 install chromium
# All test groups; Aspire starts automatically. Docker must be running.
dotnet test WarningSystemTest/WarningSystemTest.csproj --no-build --no-restore -p:WarningSystemTestingBuild=true --settings WarningSystemTest/playwright.runsettings
```

In this repository checkout the containing directory is `warningchat` instead of
`src`. Run the same commands from that directory. No scripts/test.sh is needed.
Use .NET 10 and PowerShell 7. On Linux build agents use
`playwright.ps1 install --with-deps chromium` and provide a working Docker engine.
Keep HEADED=0 for a build server; HEADED=1 shows the browser locally.
Browser tests load editor/calendar libraries from the application's normal CDN
URLs, so the runner needs network access to those CDNs. Test authentication
headers are removed from third-party browser requests.

Run only a group by adding a filter to `dotnet test`:

```powershell
--filter "Category=Unit"
--filter "Category=UI"
--filter "Category=UI&Role=User"
--filter "Category=UI&Role=Legal"
--filter "Category=UI&Role=Admin"
--filter "Category=UI&Role=Chairperson"
--filter "Category=Integration|Category=Database"
--filter "FullyQualifiedName~LoginTests"
```

Unit-only runs do not require Docker or Chromium. Login tests remain discoverable
by name; the new tests are grouped by their Category trait in Test Explorer.
Build first, then select the project runsettings file in Visual Studio. Changing
`-p:WarningSystemTestingBuild=true` cannot repair stale outputs when --no-build
is used: rebuild with that flag after changing code/build modes.

## Authentication and external services

These are synthetic application identities: User, Legal, Admin and Chairperson.
The existing guarded test authentication checks a random per-run key; real
application role authorization still executes. Tests do not automate or prove
Microsoft MFA, real directory membership or delivery of external email.

The test-only Graph adapter supplies synthetic employees. SOP has one synthetic
document. Upload storage stays in memory. Email is captured in an in-memory
outbox, allowing assertions on recipients, body and attachments without sending
mail. `/__testing/outbox` requires the same test authentication as the other test
endpoints. Normal builds exclude Testing files and all `/__testing` endpoints.
Rebuild normally for deployment; do not deploy test-build outputs.

## Diagnostics and coverage

UI failures save screenshot, HTML, Playwright trace and a `failure.txt` with the
scenario, role, URL and original assertion in `TestResults/UI/`. Test output
prints that directory. A screenshot failure does not hide the original failure.
Login failures use `TestResults/Login/`. Aspire startup logs are under the test
output directory's `TestResults/Aspire/`. Traces contain synthetic data and test
headers; treat them as build artifacts with limited retention.

To collect unit coverage, from the same containing directory:

```powershell
dotnet test WarningSystemTest/WarningSystemTest.csproj --no-build --no-restore -p:WarningSystemTestingBuild=true --filter "Category=Unit" --collect:"XPlat Code Coverage" --settings WarningSystemTest/coverage.runsettings
```

Coverage is written to TestResults as coverage.cobertura.xml. Aspire launches
web code in another process, so this collector does not measure the browser
suite's web-process coverage. There is no 100% gate: external Graph/Azure calls,
full browser execution and remaining uncovered branches need separate evidence.

The unit suite was executed against the application without the earlier product
fixes. It reports **281 passed, 4 failed, 2 skipped**. The four failures expose
business-rule/template problems listed in [KNOWN_ISSUES.md](KNOWN_ISSUES.md).
The two team-lead email cases remain skipped at your earlier request; remove
their InlineData Skip to re-enable them. No additional failures are skipped.
SQL/Aspire browser tests must be run on your working machine/build agent: this
workspace's proxy blocks the DCP orchestration connection. Compilation and test
discovery do not mean those end-to-end cases passed.

## Reading and extending a small test

Each test receives a fresh browser context already authenticated as its named
synthetic role. It does not repeat Microsoft sign-in or MFA. Setup resets the
synthetic database records before the test; tests do not depend on another test
having run first.

Page objects contain navigation, form fills, clicks and named locators. They do
not decide whether the application follows a business rule. The test expresses
one expected outcome. For example, the validated-issue case opens TeamLeadPage
and checks that AddEvidence is hidden. A separate test attempts a direct POST to
prove a hidden button cannot be bypassed. Another checks that Notify Legal is
hidden after the issue is issued. Theory parameters appear in Test Explorer,
so each button/state or role/route combination is a separate case.

| Page object | Interactions |
|---|---|
| `HomePage` | Open home, search tasks, create-issue navigation, discussion form and reports. |
| `WarningWizardPage` | Select employee/categories, save a draft, navigate wizard steps and submit. |
| `TeamLeadPage` | Open issue, add/cancel evidence or notes and notify Legal. |
| `LegalPage` | Named action buttons, due-date dialog, information requests, validation and visibility. |
| `AdminPage` | Create questions/categories, edit categories and inspect linked questions. |
| `ReportPage` | Apply/reset report filters and locate visible report rows. |

## Application scope

This update removes the product fixes previously bundled with the tests. The
core project, warning data access, home view and calendar script are restored to
the login-setup baseline. The added InternalsVisibleTo attribute is removed too.
The recipient helper tests use reflection rather than changing core visibility.
No new product feature, authorization fix, status rule, AppHost or migration
change is included. Existing test-only authentication/external adapters and the
fake email outbox remain available for Aspire tests; normal builds exclude them.
Product defects should be reviewed and fixed separately from this test work.
