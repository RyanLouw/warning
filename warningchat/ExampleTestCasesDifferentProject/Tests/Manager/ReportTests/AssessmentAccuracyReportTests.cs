using CallQualityUITesting.Helpers;
using CallQualityUITesting.Pages.Manager.Reports;
using Microsoft.Playwright;

namespace CallQualityUITesting.Tests.Manager.ReportTests;

[Collection("Manager Tests")]
public class AssessmentAccuracyReportTests
{
    private readonly ManagerBrowserFixture _browser;

    public AssessmentAccuracyReportTests(
        ManagerBrowserFixture browser)
    {
        _browser = browser;
    }

    [Fact]
    public async Task Manager_Can_View_Assessment_Accuracy_Charts()
    {
        var reportPage =
            new AssessmentAccuracyReportPage(
                _browser.Page,
                _browser.BaseUrl);

        await reportPage.GoToAsync();

        const string monthWithData = "2026-07";

        await reportPage.SearchAsync(
            monthWithData,
            "Assessor");

        var chartCount =
            await reportPage.Charts.CountAsync();

        Assert.True(
            chartCount > 0,
            "Expected at least one assessment accuracy chart.");

        for (var i = 0; i < chartCount; i++)
        {
            Assert.True(
                await reportPage.ChartHasDataAsync(i),
                $"Expected chart {i + 1} to contain data.");
        }

        await ScreenshotHelper.TakeScreenshotAsync(
            _browser.Page,
            "Assessment-Accuracy-Assessor");
    }


    [Fact]
    public async Task Manager_Can_Search_Assessment_Accuracy_By_Month_And_Role()
    {
        var reportPage =
            new AssessmentAccuracyReportPage(
                _browser.Page,
                _browser.BaseUrl);

        await reportPage.GoToAsync();

        const string monthWithData = "2026-07";

        await reportPage.SearchAsync(
            monthWithData,
            "Assessor");

        await Assertions
            .Expect(reportPage.MonthInput)
            .ToHaveValueAsync(monthWithData);

        await Assertions
            .Expect(reportPage.RoleDropdown)
            .ToHaveValueAsync("Assessor");

        var chartCount =
            await reportPage.Charts.CountAsync();

        Assert.True(
            chartCount > 0,
            "Expected accuracy data after searching.");
    }


    [Fact]
    public async Task Manager_Can_View_Accuracy_Percentage()
    {
        var reportPage =
            new AssessmentAccuracyReportPage(
                _browser.Page,
                _browser.BaseUrl);

        await reportPage.GoToAsync();

        await reportPage.SearchAsync(
            "2026-07",
            "Assessor");

        var summaryCount =
            await reportPage.AccuracySummaries.CountAsync();

        Assert.True(
            summaryCount > 0,
            "Expected at least one accuracy summary.");

        await Assertions
            .Expect(reportPage.AccuracySummaries.First)
            .ToContainTextAsync("Accuracy:");
    }


    [Fact]
    public async Task Manager_Can_Download_Assessment_Accuracy_Excel()
    {
        var reportPage =
            new AssessmentAccuracyReportPage(
                _browser.Page,
                _browser.BaseUrl);

        await reportPage.GoToAsync();

        await reportPage.MonthInput.FillAsync(
            "2026-07");

        await reportPage.RoleDropdown
            .SelectOptionAsync("Assessor");

        var download =
            await reportPage.DownloadExcelAsync();

        Assert.EndsWith(
            ".xlsx",
            download.SuggestedFilename);
    }
}