using System.Text.Json;
using Microsoft.Playwright;
using WarningSystems.Testing;

namespace WarningSystems.UiTests;

public sealed class BrowserFixture : IAsyncLifetime
{
    public WarningApplication App { get; } = new();
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private HttpClient _client = null!;
    public string BaseUrl => _client.BaseAddress!.ToString();

    public async Task InitializeAsync()
    {
        await App.InitializeDatabaseAsync();
        App.UseKestrel(0);
        _client = App.CreateClient();
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() {
            Headless = true,
            ExecutablePath = Environment.GetEnvironmentVariable("PLAYWRIGHT_CHROMIUM_EXECUTABLE")
        });
    }

    public async Task RunAsync(string name, Func<IPage, Task> test, string roles = "User,Legal")
    {
        var directory = Path.GetFullPath(Path.Combine("TestResults", "playwright-artifacts", $"{name}-{Guid.NewGuid():N}"));
        await using var context = await _browser.NewContextAsync(new() {
            BaseURL = BaseUrl, Locale = "en-ZA", TimezoneId = "Africa/Johannesburg",
            ViewportSize = new() { Width = 1440, Height = 1000 },
            ExtraHTTPHeaders = new Dictionary<string, string> { [TestAuthentication.RoleHeader] = roles }
        });
        // Reports and native forms use local assets. Tests must not depend on a CDN.
        await context.RouteAsync("**/*", route => new Uri(route.Request.Url).Host == new Uri(BaseUrl).Host
            ? route.ContinueAsync() : route.AbortAsync());
        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(15_000);
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        var cdp = await context.NewCDPSessionAsync(page);
        await cdp.SendAsync("Profiler.enable");
        await cdp.SendAsync("Profiler.startPreciseCoverage", new Dictionary<string, object> { ["callCount"] = true, ["detailed"] = true });
        try
        {
            await test(page);
            Assert.Empty(errors);
        }
        catch
        {
            Directory.CreateDirectory(directory);
            await page.ScreenshotAsync(new() { Path = Path.Combine(directory, "failure.png"), FullPage = true });
            await File.WriteAllTextAsync(Path.Combine(directory, "page.html"), await page.ContentAsync());
            await File.WriteAllLinesAsync(Path.Combine(directory, "browser-errors.txt"), errors);
            await context.Tracing.StopAsync(new() { Path = Path.Combine(directory, "trace.zip") });
            throw;
        }
        finally
        {
            Directory.CreateDirectory(directory);
            var coverage = await cdp.SendAsync("Profiler.takePreciseCoverage");
            await File.WriteAllTextAsync(Path.Combine(directory, "javascript-coverage.json"), JsonSerializer.Serialize(coverage));
        }
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
        _client?.Dispose();
        await App.DisposeAsync();
        await App.DeleteDatabaseAsync();
    }
}
