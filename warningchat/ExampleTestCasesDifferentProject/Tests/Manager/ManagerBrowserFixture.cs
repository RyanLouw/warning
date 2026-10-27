using CallQualityUITesting.Models;
using CallQualityUITesting.Pages;
using CallQualityUITesting.SystemUnderTest;
using Microsoft.Playwright;

namespace CallQualityUITesting.Tests.Manager;

[CollectionDefinition("Manager Tests")]
public class ManagerTestCollection :
    ICollectionFixture<ManagerBrowserFixture>
{
}


public sealed class ManagerBrowserFixture : IAsyncLifetime
{
 
    private readonly CallQualityServiceSut _sut;

    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public IBrowserContext Context { get; private set; } = null!;
    public IPage Page { get; private set; } = null!;

    public string BaseUrl => _sut.BaseUrl;

    public ManagerBrowserFixture(
        CallQualityServiceSut sut)
    {
        _sut = sut;
    }

    public async ValueTask InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();

        _browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = false,
            SlowMo = 300
        });

        Context = await _browser.NewContextAsync();
        Page = await Context.NewPageAsync();


        var manager =
            TestConfiguration.Users.Manager;

        var loginPage =
            new LoginPage(
                Page,
                _sut.BaseUrl);

        await loginPage.GoToAsync();

        await loginPage.LoginAsync(manager);

        // add dit dat ek kan validate
        await Page
            .GetByRole(
                AriaRole.Heading,
                new() { Name = "Manager Overview" })
            .WaitForAsync(new()
            {
                Timeout = 120_000
            });
    }

    public async ValueTask DisposeAsync()
    {
        if (Context is not null)
            await Context.DisposeAsync();

        if (_browser is not null)
            await _browser.DisposeAsync();

        _playwright?.Dispose();
    }
}



