using Microsoft.Playwright;
using WarningSystemTest.Database;
using WarningSystemTest.Models;

namespace WarningSystemTest.Fixtures;

public abstract class UiTestBase(BrowserFixture browser) : IAsyncLifetime
{
    protected BrowserFixture Browser => browser;
    protected TestDatabase Database { get; } = new(browser.Sut);
    public ValueTask InitializeAsync() => new(Database.ResetAsync());
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    protected async Task RunAsync(string name, Func<IPage, Task> test, string role = "User")
    {
        await using var context = await browser.CreateContextAsync(TestUsers.ForRole(role));
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(20_000);
        try { await test(page); }
        catch (Exception failure)
        {
            var directory = Path.GetFullPath(Path.Combine("TestResults", "UI", $"{name}-{Guid.NewGuid():N}"));
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(Path.Combine(directory, "failure.txt"),
                $"Scenario: {name}\nRole: {role}\nURL: {page.Url}\n{failure}");
            TestContext.Current.TestOutputHelper?.WriteLine($"UI failure artifacts: {directory}");
            // A browser crash must not replace the original assertion with a screenshot error.
            try { await page.ScreenshotAsync(new() { Path = Path.Combine(directory, "failure.png"), FullPage = true }); }
            catch { /* Original failure is recorded above. */ }
            try { await File.WriteAllTextAsync(Path.Combine(directory, "page.html"), await page.ContentAsync()); }
            catch { /* Page may already be closed. */ }
            try { await context.Tracing.StopAsync(new() { Path = Path.Combine(directory, "trace.zip") }); }
            catch { /* Tracing may be unavailable after a browser crash. */ }
            throw;
        }
    }
}
