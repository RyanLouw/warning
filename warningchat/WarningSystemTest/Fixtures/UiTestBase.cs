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
        catch
        {
            var directory = Path.GetFullPath(Path.Combine("TestResults", "UI", $"{name}-{Guid.NewGuid():N}"));
            Directory.CreateDirectory(directory);
            await page.ScreenshotAsync(new() { Path = Path.Combine(directory, "failure.png"), FullPage = true });
            await File.WriteAllTextAsync(Path.Combine(directory, "page.html"), await page.ContentAsync());
            await context.Tracing.StopAsync(new() { Path = Path.Combine(directory, "trace.zip") });
            throw;
        }
    }
}
