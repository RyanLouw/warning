using Microsoft.Playwright;

namespace CallQualityUITesting.Pages.Manager.Reports;

public class AssessorListeningTimeReportPage
{
    private readonly IPage _page;
    private readonly string _baseUrl;

    public AssessorListeningTimeReportPage(
        IPage page,
        string baseUrl)
    {
        _page = page;
        _baseUrl = baseUrl;
    }

    public ILocator MonthInput =>
        _page.Locator("input[type='month']");

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
            ".assessor-listening-charts .page-card");

    public ILocator Charts =>
        _page.Locator(
            ".assessor-listening-charts canvas");

    public ILocator ChartHeaders =>
        _page.Locator(".page-card-header");

    public ILocator Placeholder =>
        _page.Locator(
            ".assessor-listening-placeholder");


    public async Task GoToAsync()
    {
        await _page.GotoAsync(
            $"{_baseUrl.TrimEnd('/')}/Manager/Report_AssessorListeningTime");
    }


    public async Task SearchAsync(string month)
    {
        await MonthInput.FillAsync(month);

        await SearchButton.ClickAsync();

        await Charts.First.WaitForAsync(
            new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = 30_000
            });
    }


    public async Task<IDownload> DownloadExcelAsync()
    {
        var downloadTask =
            _page.WaitForDownloadAsync();

        await DownloadExcelButton.ClickAsync();

        return await downloadTask;
    }


    public async Task<bool> ChartHasDataAsync(
        int chartIndex)
    {
        var chartCanvas =
            Charts.Nth(chartIndex);

        return await chartCanvas.EvaluateAsync<bool>(
            @"canvas => {
                const chart = Chart.getChart(canvas);

                if (!chart)
                    return false;

                const hasLabels =
                    chart.data.labels &&
                    chart.data.labels.length > 0;

                const hasDatasetData =
                    chart.data.datasets &&
                    chart.data.datasets.some(
                        dataset =>
                            dataset.data &&
                            dataset.data.length > 0);

                return hasLabels && hasDatasetData;
            }");
    }
}