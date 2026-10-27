using Microsoft.Playwright;


namespace CallQualityUITesting.Pages.Manager.Reports;

public class AssessorTrackingReportPage
{
    private readonly IPage _page;
    private readonly string _baseUrl;

    public AssessorTrackingReportPage(
        IPage page,
        string baseUrl)
    {
        _page = page;
        _baseUrl = baseUrl;
    }

    public ILocator FromDate =>
        _page.Locator("#fromDate");

    public ILocator ToDate =>
        _page.Locator("#toDate");

    public ILocator RetrieveButton =>
        _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Retrieve" });

    public ILocator ExportButton =>
        _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Export Excel" });

    public ILocator SearchInput =>
        _page.Locator("#tableSearch");

    public ILocator Table =>
        _page.Locator("#trackingTable");

    public ILocator TableRows =>
        _page.Locator("#trackingTable tbody tr");

    public ILocator AgentHeader =>
        _page.GetByRole(
            AriaRole.Columnheader,
            new() { NameRegex = new("Agent") });

    public ILocator DepartmentHeader =>
        _page.GetByRole(
            AriaRole.Columnheader,
            new() { NameRegex = new("Department") });

    public ILocator AssessorHeader =>
        _page.GetByRole(
            AriaRole.Columnheader,
            new() { NameRegex = new("Assessor") });

    public async Task GoToAsync()
    {
        await _page.GotoAsync(
            $"{_baseUrl.TrimEnd('/')}/Manager/Report_AssessorTracking");
    }

    public async Task RetrieveAsync(
        string from,
        string to)
    {
        await FromDate.FillAsync(from);
        await ToDate.FillAsync(to);

        await RetrieveButton.ClickAsync();

        await _page.WaitForFunctionAsync(
            @"() => {
                const tbody =
                    document.querySelector('#trackingTable tbody');

                return tbody &&
                    !tbody.innerText.includes(
                        'No data available. Please select a date range');
            }");
    }

    public async Task SearchAsync(string text)
    {
        await SearchInput.FillAsync(text);

        await SearchInput.PressAsync("a");
        await SearchInput.PressAsync("Backspace");
    }

    public async Task<IDownload> ExportAsync()
    {
        var downloadTask =
            _page.WaitForDownloadAsync();

        await ExportButton.ClickAsync();

        return await downloadTask;
    }
}
