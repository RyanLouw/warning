using Microsoft.Playwright;
using WarningSystemTest.Helpers;
using WarningSystemTest.Models;
using WarningSystemTest.SystemUnderTest;

namespace WarningSystemTest.Fixtures;

[CollectionDefinition("Warning browser", DisableParallelization = true)]
public sealed class BrowserCollection : ICollectionFixture<BrowserFixture> { }

public sealed class BrowserFixture(WarningSystemSut sut) : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    public string BaseUrl => sut.BaseUrl;
    public WarningSystemSut Sut => sut;

    public async ValueTask InitializeAsync()
    {
        await sut.EnsureStartedAsync();
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = Environment.GetEnvironmentVariable("HEADED") != "1",
            ExecutablePath = Environment.GetEnvironmentVariable("PLAYWRIGHT_CHROMIUM_EXECUTABLE")
        });
    }

    public async Task<IBrowserContext> CreateContextAsync(TestUser? user)
    {
        var context = await (_browser ?? throw new InvalidOperationException("Browser has not started.")).NewContextAsync(new()
        {
            BaseURL = BaseUrl,
            ExtraHTTPHeaders = user is null ? null : sut.HeadersFor(user),
            Locale = "en-ZA",
            TimezoneId = "Africa/Johannesburg"
        });
        // Never send the test authentication headers to third-party CDNs.
        var origin = new Uri(BaseUrl).GetLeftPart(UriPartial.Authority);
        await context.RouteAsync("**/*", route => BrowserAssets.RouteAsync(route, origin));
        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true });
        return context;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
    }
}
