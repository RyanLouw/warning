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
| `Tests/UI/ReportTests.cs` | Individual and combined filters, date boundaries, no results, reset, role visibility, every displayed cell, totals and department breakdown. |
| `Tests/UI/WarningWizardTests.cs` | Draft creation/editing, dates, rich description, SOP, uploads/downloads and submission. |
| `Tests/UI/LegalTests.cs` | Notes/documents, decisions, validation, due dates, category changes, visibility and information requests. |
| `Tests/UI/AdminTests.cs` | Question types/configuration, duplicate handling, editing/audit, categories/default questions, question links and role restrictions. |
| `Tests/UI/DiscussionTests.cs` | Discussion creation, validation, persisted values and visible report row. |
| `Tests/UI/DashboardTests.cs` | Task search and employee table values/counts. |
| `Tests/Integration/HttpTests.cs` | Actual HTTP authorization, antiforgery, answer writes, invalid requests, PDF response and redirects. |
| `Tests/Database/DataAccessTests.cs` | SQL Server persistence, rollback, transitions, category links, answer upserts, evidence/notes and stored procedures. |
| `Database/TestDatabase.cs` | Deterministic synthetic records and parameterized SQL through Aspire's connection. |
| `Database/ReportOracle.cs` | Independent SQL calculates expected filtered records without calling the application's report manager or using DOM values. |
| `SystemUnderTest/WarningSystemSut.cs` | Lazy Aspire startup, health checks, session database connection and per-run auth key. |
| `Fixtures/BrowserFixture.cs`, `Fixtures/UiTestBase.cs` | Chromium, fresh identity contexts, sequential database resets and failure artifacts. |
| `coverage.runsettings` | Cobertura coverage for application/core code executed inside the test process. |

Before each database-backed test, synthetic Alice/Bob/Cara warnings are replaced
with a known set of statuses, departments, categories, dates and latest actions.
Migration seed records are preserved. The report assertions compare visible IDs,
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

UI failures save screenshot, HTML and Playwright trace in `TestResults/UI/`.
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

The unit suite was executed in the managed workspace. The two team-lead email
cases that initially failed are explicitly skipped at user request so they
remain visible in Test Explorer; remove their InlineData Skip to re-enable them.
They passed after the email templates were embedded, before being skipped. SQL/Aspire browser tests
must also be run on your working local machine/build agent: this workspace's
proxy blocks the DCP orchestration connection. Compilation/discovery does not
mean those end-to-end cases have passed.

## Application changes included with the tests

- Embed the existing IssueUpdated, MoreInformationRequired and
  MoreInformationAdded templates so the manager can load them from the assembly.
- Save new warning/category links before committing the creation transaction.
- Preserve local calendar dates instead of shifting them to UTC when saving.
- Populate the report status filter from report data, including Issued, and add
  a stable warning ID attribute for matching rendered rows to database records.

The test adapter also records mail and provides synthetic SOP/employee data.
No AppHost or migration edits are included in this expansion.
