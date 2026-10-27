using CallQualityUITesting.Helpers;
using CallQualityUITesting.Pages.Manager.Reports;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Text;

namespace CallQualityUITesting.Tests.Manager.ReportTests;

[Collection("Manager Tests")]
public class AssessorTrackingReportTests
{
    private readonly ManagerBrowserFixture _browser;

    public AssessorTrackingReportTests(
        ManagerBrowserFixture browser)
    {
        _browser = browser;
    }



    [Fact]
    public async Task Manager_Can_View_Assessor_Tracking_Data()
    {
        var reportPage =
            new AssessorTrackingReportPage(
                _browser.Page,
                _browser.BaseUrl);

        await reportPage.GoToAsync();

        const string fromDate = "2026-07-01";
        const string toDate = "2026-07-31";

        await reportPage.RetrieveAsync(
            fromDate,
            toDate);

        var rowCount =
            await reportPage.TableRows.CountAsync();

        Assert.True(
            rowCount > 0,
            "Expected Assessor Tracking data.");

        await ScreenshotHelper.TakeScreenshotAsync(
            _browser.Page,
            "Assessor-Tracking-Report");
    }


    [Fact]
    public async Task Manager_Can_View_Assessor_Tracking_Columns()
    {
        var reportPage =
            new AssessorTrackingReportPage(
                _browser.Page,
                _browser.BaseUrl);

        await reportPage.GoToAsync();

        await Assertions
            .Expect(reportPage.Table)
            .ToContainTextAsync("Agent");

        await Assertions
            .Expect(reportPage.Table)
            .ToContainTextAsync("Department");

        await Assertions
            .Expect(reportPage.Table)
            .ToContainTextAsync("Assessor");

        await Assertions
            .Expect(reportPage.Table)
            .ToContainTextAsync("Number of Assessments");

        await Assertions
            .Expect(reportPage.Table)
            .ToContainTextAsync("Percentage");
    }

    [Fact]
    public async Task Manager_Can_Search_Assessor_Tracking_Table()
    {
        var reportPage =
            new AssessorTrackingReportPage(
                _browser.Page,
                _browser.BaseUrl);

        await reportPage.GoToAsync();

        await reportPage.RetrieveAsync(
            "2026-07-01",
            "2026-07-31");

        var firstRow =
            reportPage.TableRows.First;

        var firstAgent =
            await firstRow
                .Locator("td")
                .Nth(0)
                .InnerTextAsync();

        Assert.False(
            string.IsNullOrWhiteSpace(firstAgent));

        await reportPage.SearchAsync(firstAgent);

        await Assertions
            .Expect(reportPage.TableRows.First)
            .ToContainTextAsync(firstAgent);
    }



    [Fact]
    public async Task Manager_Can_Sort_Assessor_Tracking_Table()
    {
        var reportPage =
            new AssessorTrackingReportPage(
                _browser.Page,
                _browser.BaseUrl);

        await reportPage.GoToAsync();

        await reportPage.RetrieveAsync(
            "2026-07-01",
            "2026-07-31");

        await reportPage.AgentHeader.ClickAsync();

        await Assertions
            .Expect(reportPage.AgentHeader)
            .ToHaveAttributeAsync(
                "data-sort",
                "asc");

        await reportPage.AgentHeader.ClickAsync();

        await Assertions
            .Expect(reportPage.AgentHeader)
            .ToHaveAttributeAsync(
                "data-sort",
                "desc");
    }


    [Fact]
    public async Task Manager_Can_Export_Assessor_Tracking_Excel()
    {
        var reportPage =
            new AssessorTrackingReportPage(
                _browser.Page,
                _browser.BaseUrl);

        await reportPage.GoToAsync();

        await reportPage.FromDate.FillAsync(
            "2026-07-01");

        await reportPage.ToDate.FillAsync(
            "2026-07-31");

        var download =
            await reportPage.ExportAsync();

        Assert.EndsWith(
            ".xlsx",
            download.SuggestedFilename);
    }

}


