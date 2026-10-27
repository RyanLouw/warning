using Microsoft.Playwright;

namespace CallQualityUITesting.Pages.Manager;

public class ManagerReportsPage
{
    private readonly IPage _page;
    private readonly string _baseUrl;

    public ManagerReportsPage(
        IPage page,
        string baseUrl)
    {
        _page = page;
        _baseUrl = baseUrl;
    }

    public ILocator AssessorListeningTimeLink =>
        _page.GetByRole(
            AriaRole.Link,
            new() { Name = "Assessor Listening Time" });

    public ILocator AssessmentAccuracyLink =>
        _page.GetByRole(
            AriaRole.Link,
            new() { Name = "Assessment Accuracy" });

    public ILocator AssessorTrackingLink =>
        _page.GetByRole(
            AriaRole.Link,
            new() { Name = "Assessor Tracking" });

    public ILocator AssessorBreakdownLink =>
        _page.GetByRole(
            AriaRole.Link,
            new() { Name = "Assessor Breakdown" });

    public ILocator QuestionBreakdownLink =>
        _page.GetByRole(
            AriaRole.Link,
            new() { Name = "Question Breakdown" });

    public async Task GoToAsync()
    {
        await _page.GotoAsync(
            $"{_baseUrl.TrimEnd('/')}/Manager/ReportHome");
    }
}