# Warning system tests

The pull-request workflow is `.github/workflows/warning-system-tests.yml`. It builds the app and tests on .NET 10, runs xUnit and headless Chromium against a disposable SQL Server 2022 service, merges application coverage, enforces the checked-in coverage baseline, and uploads TRX, HTML coverage, browser traces, screenshots, and JavaScript execution data. It also runs for pushes to `main` and `codex/**`, manual dispatch, and merge queues.

## Before merging

In GitHub Settings → Rules → Rulesets (or Branch protection), require the **Warning system tests** status check for `main`. The workflow alone does not change repository protection. Keep the rule enabled for administrators if bypasses should be disallowed. Do not mark a failed or unexecuted database/browser job as optional.

## What is covered

- `WarningSystems.UnitTests`: validation, claims and roles, helper functions, email rendering/recipients/content, status transitions and legal decisions, manager commands, attachment handling, controllers, report/task visibility, Graph serialization and hierarchy traversal.
- `WarningSystemTest`: repaired existing storage, Graph membership, and mapper tests, using xUnit consistently.
- `WarningSystems.IntegrationTests`: real EF queries and transactions, persisted category links, rollback, answers, evidence, notes, workflow changes, authorization, CSRF rejection, and the real category-question stored procedure.
- `WarningSystems.UiTests`: real MVC pages in Kestrel and headless Playwright. Tests cover report filters, filter combinations, inclusive date boundaries, empty results, resets, dependent employee options, different roles, task search, discussion creation, and admin question creation.

The report assertions compare **row IDs, every visible cell, summary counts, and department totals** with independent parameterized SQL through another connection. They do not calculate expected results from browser `data-*` attributes, reuse application filter code, or mock report responses. `ws.TestDirectory` contains synthetic expected directory data because production employee names/departments come from Microsoft Graph, not the warning database.

## Authentication and external services

`TestAuthentication`, synthetic roles, fake Graph, SOP, and file storage live in the test assembly only. The application has no test-login endpoint and ships no authentication bypass. Authorization and antiforgery remain active. Each browser case gets a fresh context. Unexpected mail sends fail instead of contacting Microsoft. The warning data access and business manager remain real in integration/browser tests.

The test host deliberately does not contact central configuration. Tests build with `-p:EnableCentralConfig=false`; this excludes the private central-config package and produces a build that refuses to run outside the `Testing` environment. Normal builds retain central config and Microsoft authentication by default. Do not deploy test-build outputs.

The suite targets the web application directly through .NET 10 `WebApplicationFactory.UseKestrel(0)`. It does not need the persistent development Aspire databases or fixed ports. Each fixture generates a unique database name, creates its schema and synthetic data, and deletes only that database afterward. `TEST_SQLSERVER` must name `master`, never an application database. Tests use EF `EnsureCreated` plus the real category-question procedure; they are **not a full migration-upgrade test**.

## Run locally

Requirements: .NET 10 SDK, Python 3, and PowerShell 7 for installing Playwright browsers. SQL Server 2022 is required for the full provider-specific suite.

```bash
# Start a SQL Server used only for tests; the password below is synthetic.
docker run --rm -d --name warning-tests-sql \
  -p 127.0.0.1:14339:1433 -e ACCEPT_EULA=Y \
  -e 'MSSQL_SA_PASSWORD=Test_only_Passw0rd!' \
  mcr.microsoft.com/mssql/server:2022-latest

# Wait until SQL Server is healthy, then run:
export TEST_SQLSERVER='Server=localhost,14339;Database=master;User Id=sa;Password=Test_only_Passw0rd!;TrustServerCertificate=True'
./scripts/test.sh sqlserver

docker stop warning-tests-sql
```

For a quicker local run without Docker, explicitly select SQLite:

```bash
./scripts/test.sh sqlite
```

This uses disposable relational SQLite files and excludes the test tagged `Database=SqlServer` (the stored procedure test). It does not certify SQL Server behavior; the pull-request job always uses SQL Server and runs that test. There is no automatic fallback in CI.

An existing Chromium executable can be selected with `PLAYWRIGHT_CHROMIUM_EXECUTABLE=/path/to/chromium`; otherwise the script installs Playwright's matching Chromium. CI always installs the matching browser. Tests of the report and native forms block external CDNs to remain deterministic. Rich editors and third-party identity-provider screens need separate coverage.

On Windows, use equivalent `dotnet restore`, `dotnet build`, `playwright.ps1 install chromium`, and `dotnet test` commands from the workflow, setting environment variables with `$env:NAME='value'`.

## Coverage

The clean local run on 26 October 2026 passed **395 tests** (304 unit, 46 integration, 45 browser) using SQLite, excluding the SQL Server stored-procedure case. Measured coverage was **68.2% lines, 53.8% branches, 80.4% methods**. The initial regression floors are 67% lines and 52% branches, allowing small provider/runtime differences. These are measured starting points, not the desired final target.

Open `TestResults/coverage/index.html`. The workflow summary shows merged C# line and branch coverage. The checked-in `scripts/coverage-baseline.json` prevents regression from the established baseline; increase it as missing cases are added. The goal is 95%+ meaningful line and branch coverage, approaching 100% where practical. **The initial suite does not claim 100% coverage or coverage of every function.** Database migrations, real Entra/MFA flows, cloud integration, rich editor interactions, and remaining email/error branches require further work.

Coverage includes the application/core assemblies, not mocks, third-party libraries, test code, or generated build files. Chromium precise-coverage JSON is saved separately under `playwright-artifacts`; it is execution evidence for exercised JavaScript and is not represented as complete JavaScript branch coverage.

For a clean measurement, use a fresh `TestResults` directory. Merging results from obsolete builds can overstate coverage. The CI runner is clean on every job.

## Inspect a browser failure

Download `warning-system-test-results`, open the screenshot and HTML, and inspect the trace:

```bash
pwsh warningchat/tests/WarningSystems.UiTests/bin/Release/net10.0/playwright.ps1 show-trace path/to/trace.zip
```

Synthetic test fixtures intentionally contain no real employee information, production credentials, or saved Entra sessions.
