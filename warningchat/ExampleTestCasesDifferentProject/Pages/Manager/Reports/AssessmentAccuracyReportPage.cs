using Microsoft.Playwright;

namespace CallQualityUITesting.Pages.Manager.Reports;

public class AssessmentAccuracyReportPage
{
    private readonly IPage _page;
    private readonly string _baseUrl;

    public AssessmentAccuracyReportPage(
        IPage page,
        string baseUrl)
    {
        _page = page;
        _baseUrl = baseUrl;
    }

    public ILocator MonthInput =>
        _page.Locator("input[type='month']");

    public ILocator RoleDropdown =>
        _page.Locator(
            ".assessment-accuracy-role-dropdown");

    public ILocator SearchButton =>
        _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Search" });

    public ILocator DownloadExcelButton =>
        _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Download Excel" });

    public ILocator ChartCards =>
        _page.Locator(
            ".assessment-accuracy-charts .page-card");

    public ILocator Charts =>
        _page.Locator(
            ".assessment-accuracy-charts canvas");

    public ILocator AccuracySummaries =>
        _page.Locator(
            ".assessment-accuracy-summary");

    public async Task GoToAsync()
    {
        await _page.GotoAsync(
            $"{_baseUrl.TrimEnd('/')}/Manager/AssessmentAccuracy");
    }

    public async Task SearchAsync(
        string month,
        string role)
    {
        await MonthInput.FillAsync(month);

        await RoleDropdown.SelectOptionAsync(role);

        await SearchButton.ClickAsync();

        await Charts.First.WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000
        });
    }

    public async Task<bool> ChartHasDataAsync(
        int chartIndex)
    {
        var chart =
            Charts.Nth(chartIndex);

        return await chart.EvaluateAsync<bool>(
            @"canvas => {
                const chart = Chart.getChart(canvas);

                if (!chart)
                    return false;

                const hasLabels =
                    chart.data.labels &&
                    chart.data.labels.length > 0;

                const hasData =
                    chart.data.datasets &&
                    chart.data.datasets.some(
                        dataset =>
                            dataset.data &&
                            dataset.data.length > 0);

                return hasLabels && hasData;
            }");
    }

    public async Task<IDownload> DownloadExcelAsync()
    {
        var downloadTask =
            _page.WaitForDownloadAsync();

        await DownloadExcelButton.ClickAsync();

        return await downloadTask;
    }
}