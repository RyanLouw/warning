using CallQualityUITesting.Helpers;
using CallQualityUITesting.Pages.Manager.Reports;
using Microsoft.Playwright;

namespace CallQualityUITesting.Tests.Manager.ReportTests;

[Collection("Manager Tests")]
public class AssessorListeningTimeReportTests
{
    private readonly ManagerBrowserFixture _browser;

    public AssessorListeningTimeReportTests(
        ManagerBrowserFixture browser)
    {
        _browser = browser;
    }


    [Fact]
    public async Task Manager_Can_View_Assessor_Listening_Time_Charts()
    {
        var reportPage =
            new AssessorListeningTimeReportPage(
                _browser.Page,
                _browser.BaseUrl);

        await reportPage.GoToAsync();

        const string monthWithData = "2026-07";


        await reportPage.SearchAsync(monthWithData);

        var chartCount =
            await reportPage.Charts.CountAsync();

        Assert.True(
            chartCount > 0,
            $"Expected at least one listening-time chart for {monthWithData}.");

        for (var i = 0; i < chartCount; i++)
        {
            var hasData =
                await reportPage.ChartHasDataAsync(i);

            Assert.True(
                hasData,
                $"Expected chart {i + 1} to contain data.");
        }

        await ScreenshotHelper.TakeScreenshotAsync(
            _browser.Page,
            "Assessor_Listening_Time-Report");
    }

    [Fact]
    public async Task Manager_Can_Search_Listening_Time_By_Month()
    {
        var reportPage =
            new AssessorListeningTimeReportPage(
                _browser.Page,
                _browser.BaseUrl);

        await reportPage.GoToAsync();


        const string monthWithData = "2026-07"; // backup was made in August 2026, so July 2026 should have data

        await reportPage.SearchAsync(
            monthWithData);

        await Assertions
            .Expect(reportPage.MonthInput)
            .ToHaveValueAsync(monthWithData);

        var chartCount =
            await reportPage.Charts.CountAsync();

        Assert.True(
            chartCount > 0,
            $"Expected listening-time charts for {monthWithData}.");

        for (var i = 0; i < chartCount; i++)
        {
            Assert.True(
                await reportPage.ChartHasDataAsync(i),
                $"Chart {i + 1} contained no data.");
        }

        await ScreenshotHelper.TakeScreenshotAsync(
            _browser.Page,
            "Assessor-Listening-Time-Report");
    }

    [Fact]
    public async Task Manager_Can_Download_Assessor_Listening_Time_Excel()
    {
        var reportPage =
            new AssessorListeningTimeReportPage(
                _browser.Page,
                _browser.BaseUrl);

        await reportPage.GoToAsync();

        const string monthWithData = "2026-07";

        await reportPage.MonthInput.FillAsync(
            monthWithData);

        var download =
            await reportPage.DownloadExcelAsync();

        Assert.EndsWith(
            ".xlsx",
            download.SuggestedFilename);
    }


}