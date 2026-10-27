using CallQualityUITesting.Pages.Manager;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace CallQualityUITesting.Tests.Manager;

[Collection("Manager Tests")]
public class ManagerReportTests
{
    private readonly ManagerBrowserFixture _browser;

    public ManagerReportTests(
        ManagerBrowserFixture browser)
    {
        _browser = browser;
    }

    [Fact]
    public async Task Manager_Can_View_All_Report_Links()
    {
        var reportsPage =
            new ManagerReportsPage(
                _browser.Page,
                _browser.BaseUrl);

        await reportsPage.GoToAsync();

        await Assertions
            .Expect(reportsPage.AssessorListeningTimeLink)
            .ToBeVisibleAsync();

        await Assertions
            .Expect(reportsPage.AssessmentAccuracyLink)
            .ToBeVisibleAsync();

        await Assertions
            .Expect(reportsPage.AssessorTrackingLink)
            .ToBeVisibleAsync();

        await Assertions
            .Expect(reportsPage.AssessorBreakdownLink)
            .ToBeVisibleAsync();

        await Assertions
            .Expect(reportsPage.QuestionBreakdownLink)
            .ToBeVisibleAsync();
    }


    [Fact]
    public async Task Manager_Can_Open_Assessment_Accuracy_Report()
    {
        var reportsPage =
            new ManagerReportsPage(
                _browser.Page,
                _browser.BaseUrl);

        await reportsPage.GoToAsync();

        await reportsPage
            .AssessmentAccuracyLink
            .ClickAsync();

        await Assertions
            .Expect(_browser.Page)
            .ToHaveURLAsync(
                new Regex(@"/Manager/AssessmentAccuracy"));
    }


    [Fact]
    public async Task Manager_Can_Open_Report_AssessorTracking_Report()
    {
        var reportsPage =
            new ManagerReportsPage(
                _browser.Page,
                _browser.BaseUrl);

        await reportsPage.GoToAsync();

        await reportsPage
            .AssessorTrackingLink
            .ClickAsync();

        await Assertions
            .Expect(_browser.Page)
            .ToHaveURLAsync(
                new Regex(@"/Manager/Report_AssessorTracking"));
    }


    [Fact]
    public async Task Manager_Can_Open_Report_AssessorBreakdown_Report()
    {
        var reportsPage =
            new ManagerReportsPage(
                _browser.Page,
                _browser.BaseUrl);

        await reportsPage.GoToAsync();

        await reportsPage
            .AssessorBreakdownLink
            .ClickAsync();

        await Assertions
            .Expect(_browser.Page)
            .ToHaveURLAsync(
                new Regex(@"/Manager/Report_AssessorBreakdown"));
    }


    [Fact]
    public async Task Manager_Can_Open_QuestionWrongStats_Report()
    {
        var reportsPage =
            new ManagerReportsPage(
                _browser.Page,
                _browser.BaseUrl);

        await reportsPage.GoToAsync();

        await reportsPage
            .QuestionBreakdownLink
            .ClickAsync();

        await Assertions
            .Expect(_browser.Page)
            .ToHaveURLAsync(
                new Regex(@"/Manager/QuestionWrongStatsReport"));
    }
}