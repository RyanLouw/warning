using CallQualityUITesting.Helpers;
using CallQualityUITesting.Models;
using CallQualityUITesting.Pages.Manager;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace CallQualityUITesting.Tests.Manager;

[Collection("Manager Tests")]
public class ManagerAssessmentTests
{
    private readonly ManagerBrowserFixture _browser;

    public ManagerAssessmentTests(
        ManagerBrowserFixture browser)
    {
        _browser = browser;
    }


    [Fact]
    public async Task Manager_Can_View_Possible_Reassessments()
    {
        var assessmentsPage =
            new AssessmentsPage(
                _browser.Page,
                _browser.BaseUrl);

        await assessmentsPage.GoToAsync();

        await assessmentsPage.OpenPossibleReassessmentsAsync();

        await Assertions
            .Expect(assessmentsPage.PossibleReassessmentTab)
            .ToHaveAttributeAsync(
                "aria-selected",
                "true");
    }


    [Fact]
    public async Task Manager_Can_View_Already_Reassessed()
    {
        var assessmentsPage =
            new AssessmentsPage(
                _browser.Page,
                _browser.BaseUrl);

        await assessmentsPage.GoToAsync();

        await assessmentsPage.OpenAlreadyReassessedAsync();

        await Assertions
            .Expect(assessmentsPage.AlreadyReassessedTab)
            .ToHaveAttributeAsync(
                "aria-selected",
                "true");
    }


    [Fact]
    public async Task Manager_Can_View_Possible_Reassessment_Data()
    {
        var assessmentsPage =
            new AssessmentsPage(
                _browser.Page,
                _browser.BaseUrl);

        await assessmentsPage.GoToAsync();

        await assessmentsPage.OpenPossibleReassessmentsAsync();

        var count =
            await assessmentsPage
                .PossibleAssessmentRows
                .CountAsync();

        Assert.True(
            count > 0,
            "Expected at least one possible reassessment.");
    }

    [Fact]
    public async Task Manager_Can_Complete_Reassessment()
    {
        var assessmentsPage =
            new AssessmentsPage(
                _browser.Page,
                _browser.BaseUrl);

        await assessmentsPage.GoToAsync();

        await assessmentsPage
            .SelectFirstPossibleReassessmentAsync();


        var reassessmentPage =
            await assessmentsPage
                .OpenSelectedReassessmentAsync();


        await reassessmentPage
            .CompleteAllQuestionsAsync();

        await ScreenshotHelper.TakeScreenshotAsync(
            _browser.Page,
            "Reassessment-Before-Save");


        await reassessmentPage.SaveAsync();

        await ScreenshotHelper.TakeScreenshotAsync(
            _browser.Page,
            "Reassessment-After-Save");
    }
}