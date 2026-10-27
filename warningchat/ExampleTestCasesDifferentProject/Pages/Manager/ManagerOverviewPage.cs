using Microsoft.Playwright;

namespace CallQualityUITesting.Pages.Manager;

public class ManagerOverviewPage
{
    private readonly IPage _page;
    private readonly string _baseUrl;

    public ManagerOverviewPage(
        IPage page,
        string baseUrl)
    {
        _page = page;
        _baseUrl = baseUrl;
    }

    public ILocator Heading =>
        _page.GetByRole(
            AriaRole.Heading,
            new() { Name = "Manager Overview" });

    public ILocator ExportButton =>
        _page.GetByRole(
            AriaRole.Link,
            new() { Name = "Export" });

    public async Task GoToAsync()
    {
        await _page.GotoAsync(
            $"{_baseUrl.TrimEnd('/')}/Manager");
    }

    public async Task<IDownload> ExportAsync()
    {
        var downloadTask =
            _page.WaitForDownloadAsync();

        await ExportButton.ClickAsync();

        return await downloadTask;
    }
}