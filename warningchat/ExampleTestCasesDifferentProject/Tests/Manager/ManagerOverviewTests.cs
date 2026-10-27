using CallQualityUITesting.Pages.Manager;
using Microsoft.Playwright;

namespace CallQualityUITesting.Tests.Manager;

[Collection("Manager Tests")]
public class ManagerOverviewTests
{
    private readonly ManagerBrowserFixture _browser;

    public ManagerOverviewTests(
        ManagerBrowserFixture browser)
    {
        _browser = browser;
    }

    [Fact]
    public async Task Manager_Can_View_Manager_Overview()
    {
        var managerOverviewPage =
            new ManagerOverviewPage(
                _browser.Page,
                _browser.BaseUrl);

        await managerOverviewPage.GoToAsync();

        await Assertions
            .Expect(managerOverviewPage.Heading)
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task Manager_Can_Export_Overview()
    {
        var managerOverviewPage =
            new ManagerOverviewPage(
                _browser.Page,
                _browser.BaseUrl);

        await managerOverviewPage.GoToAsync();

        var download =
            await managerOverviewPage.ExportAsync();

        Assert.EndsWith(
            ".xlsx",
            download.SuggestedFilename);
    }
}