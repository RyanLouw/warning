using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace CallQualityUITesting.Pages.Manager;

public class AssessmentsPage
{
    private readonly IPage _page;
    private readonly string _baseUrl;

    public AssessmentsPage(
        IPage page,
        string baseUrl)
    {
        _page = page;
        _baseUrl = baseUrl;
    }

    public ILocator PossibleReassessmentTab =>
        _page.GetByRole(
            AriaRole.Tab,
            new() { NameRegex = new Regex("Possible Reassessment") });

    public ILocator AlreadyReassessedTab =>
        _page.GetByRole(
            AriaRole.Tab,
            new() { NameRegex = new Regex("Already Reassessed") });

    public async Task GoToAsync()
    {
        await _page.GotoAsync(
            $"{_baseUrl.TrimEnd('/')}/Manager/Assessments");
    }

    public async Task OpenPossibleReassessmentsAsync()
    {
        await PossibleReassessmentTab.ClickAsync();
    }

    public async Task OpenAlreadyReassessedAsync()
    {
        await AlreadyReassessedTab.ClickAsync();
    }
    public ILocator PossibleAssessmentRows =>
    _page
        .Locator("#possible")
        .Locator(".over-assessment-row");




    public async Task SelectFirstPossibleReassessmentAsync()
    {
        await OpenPossibleReassessmentsAsync();

        var firstRow =
            PossibleAssessmentRows.First;

        await firstRow.ClickAsync();
    }
    public async Task<ReassessmentPage> OpenSelectedReassessmentAsync()
    {
        var reassessLink =
            _page.GetByRole(
                AriaRole.Link,
                new() { Name = "Reassess This Assessment" });

        await reassessLink.ClickAsync();

        return new ReassessmentPage(
            _page,
            _baseUrl);
    }

  
}