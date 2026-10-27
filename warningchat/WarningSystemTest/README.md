# Aspire + Playwright login setup

This restart uses **WarningSystemTest only**, following the structure of
`ExampleTestCasesDifferentProject`. The example is unchanged reference material.
The obsolete service tests in WarningSystemTest were removed as requested.
There are no warning workflow, report, or business-rule tests in this step.

## Startup order

1. xUnit's assembly fixture starts `WarningSystemsAppHost` with `Testing:Enabled=true`.
2. That AppHost starts a session-only SQL Server container and the `PitStop` database.
3. The real `database-migrations` project runs against that database.
4. Aspire starts `WarningSystems` only after migrations complete successfully.
5. The fixture waits for the HTTP health check and gets the web URL from Aspire.
6. Playwright opens a fresh browser context for each synthetic role.

No WebApplicationFactory, separate Kestrel test host, SQLite database, or shell
script is used. Aspire owns the database, migrations, and warning web process.
The normal development AppHost still uses its existing persistent volume and
port. Automated tests use a fresh session container so they cannot overwrite
that development database. The database name stays PitStop for migration compatibility.

## Files to read in order

All paths below are inside `warningchat/WarningSystemTest/`:

| Order | File | Purpose |
|---|---|---|
| 1 | `WarningSystemTest.csproj` | xUnit v3, Playwright, Aspire testing packages, and the AppHost reference. |
| 2 | `Models/TestUser.cs` | Four synthetic profiles: User, Legal, Admin, Chairperson. No Microsoft passwords. |
| 3 | `SystemUnderTest/WarningSystemSut.cs` | Starts/disposes Aspire, waits for migrations/web health, supplies the app URL and per-run auth headers. Exposes a connection to Aspire's same database for future data checks. |
| 4 | `Fixtures/BrowserFixture.cs` | Starts Chromium and creates a fresh browser context per identity. Blocks external CDN requests so the test key stays local. |
| 5 | `Tests/LoginTests.cs` | Four role login checks and one unauthenticated-request check. Each role opens the real app and verifies its actual server-side identity. |
| 6 | `playwright.runsettings` | Optional local runner settings. HEADED=0 runs without a window; HEADED=1 shows the browser. |
| 7 | `Helpers/StartupLogProvider.cs` | Saves Aspire startup diagnostics, redacting the per-run test authentication key. |

## MFA and application changes

These tests authenticate **synthetic application users**, not real Microsoft
accounts. They verify that the warning system can run under each application
role without automating Microsoft MFA. They do not prove the real Entra/MFA
sign-in flow or real Entra group membership works.

The application needs a test authentication adapter because Aspire starts it
as another process; the test runner cannot inject services into that process.

- `warningchat/Program.cs` chooses the test adapter only when compiled with
  `WarningSystemTestingBuild=true` and requires the `Testing` environment.
- `warningchat/Testing/TestAuthenticationHandler.cs` checks a randomly generated,
  per-run key and selects one predefined identity. Role authorization stays active.
- `warningchat/Testing/TestServices.cs` supplies local Graph/role/SOP adapters.
  Mail is disabled. `TestFileStorage.cs` holds test uploads in memory.
- `/__testing/session` reports the authenticated identity, and `/__testing/health`
  provides Aspire's readiness check. Both routes exist only in the test build.

**Normal builds exclude the entire Testing folder and those routes.** Their
Microsoft authentication and central configuration remain in place. The test
build excludes the private central-config package and refuses to start outside
Testing. Do not publish test-build outputs. Rebuild normally before deployment.

Supporting build repairs align AppHost/migrations with .NET 10, correct a stale
project path, exclude misplaced web/test copies from the core library, and fix
the error-model namespace. Migration startup now uses its host correctly,
returns a failure exit code on errors, uses portable script paths, and copies
all scripts (including 0013).

## Run from the repository root

Requirements: .NET 10 SDK, a running Docker engine, and PowerShell 7 for the
Playwright browser installer. You do not need to start Aspire manually.

```powershell
dotnet build warningchat/WarningSystemTest/WarningSystemTest.csproj -p:WarningSystemTestingBuild=true
pwsh warningchat/WarningSystemTest/bin/Debug/net10.0/playwright.ps1 install chromium
dotnet test warningchat/WarningSystemTest/WarningSystemTest.csproj --no-build -p:WarningSystemTestingBuild=true --settings warningchat/WarningSystemTest/playwright.runsettings
```

On a Linux build server, use `install --with-deps chromium` in the installer step.
Docker must be available to Aspire; the runner pulls SQL Server itself. No
separate SQL Server service or manually configured database connection is needed.
The AppHost project reference also sets the testing-build property, but the
explicit command makes the build mode unambiguous.

For a visible browser, set HEADED to 1 in `playwright.runsettings`. An existing
Chromium can be selected with `PLAYWRIGHT_CHROMIUM_EXECUTABLE` when needed.
In Visual Studio, select this runsettings file and run the WarningSystemTest
project. Do not run the unrelated example project as warning-system tests.

Failures save a screenshot and trace under `TestResults/Login/` relative to the
test runner's working directory. Open a trace with `playwright.ps1 show-trace`.
No PR, pipeline, coverage gate, or additional business tests are added in this step.

## Verification in this workspace

The targeted test-project build succeeds. Five login checks are discovered.
End-to-end execution is **not verified here**: the managed environment proxy
rejects Aspire's orchestration connection with HTTP 403, before the web
application starts. This is not a successful login test run. Startup logs are
saved under `bin/Debug/net10.0/TestResults/Aspire/` to help diagnose local failures.
Normal-build item evaluation confirms that test authentication/adapters are
excluded when `WarningSystemTestingBuild=false`.
