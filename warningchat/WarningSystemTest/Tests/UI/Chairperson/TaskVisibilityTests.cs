using WarningSystemTest.Fixtures;
using WarningSystemTest.Pages;
using static Microsoft.Playwright.Assertions;

namespace WarningSystemTest.Tests.UI.Chairperson;

[Collection("Warning browser")]
[Trait("Category", "UI")]
[Trait("Role", "Chairperson")]
public sealed class TaskVisibilityTests(BrowserFixture browser) : UiTestBase(browser)
{
    [Fact]
    public Task Non_hearing_task_is_visible() => RunAsync(nameof(Non_hearing_task_is_visible), async page =>
    {
        var home = new HomePage(page);

        await home.OpenAsync();

        await Expect(home.TaskFor("UI Alice", "Pending")).ToBeVisibleAsync();
    }, "Chairperson");

    [Fact]
    public Task Hearing_issue_type_is_hidden_from_task_list() => RunAsync(nameof(Hearing_issue_type_is_hidden_from_task_list), async page =>
    {
        // The Validated fixture is a Hearing (IssueTypeId=4); the discussion is not.
        var home = new HomePage(page);

        await home.OpenAsync();

        await Expect(home.TaskFor("UI Cara", "Validated")).ToHaveCountAsync(1);
    }, "Chairperson");

    [Fact]
    public Task Hearing_issue_type_is_hidden_from_reports() => RunAsync(nameof(Hearing_issue_type_is_hidden_from_reports), async page =>
    {
        var home = new HomePage(page);

        await home.OpenReportsAsync();

        await Expect(home.ReportRows.Filter(new() { Has = page.Locator("td:nth-child(3):text-is('Hearing')") })).ToHaveCountAsync(0);
    }, "Chairperson");

    [Fact]
    public Task Hearing_legacy_status_is_hidden_from_task_list() => RunAsync(nameof(Hearing_legacy_status_is_hidden_from_task_list), async page =>
    {
        await Database.QueryAsync("UPDATE ws.Warning SET Status='Hearing',IssueStatusId=NULL WHERE WarningId=@Id", ("@Id", Database.Warnings["Pending"]));
        var home = new HomePage(page);

        await home.OpenAsync();

        await Expect(home.TaskFor("UI Alice", "Hearing")).ToHaveCountAsync(0);
    }, "Chairperson");

    [Fact]
    public Task Hidden_from_creator_does_not_hide_the_task_from_chairperson() => RunAsync(nameof(Hidden_from_creator_does_not_hide_the_task_from_chairperson), async page =>
    {
        await Database.QueryAsync("UPDATE ws.Warning SET HideFromTeamLead=1 WHERE WarningId=@Id", ("@Id", Database.Warnings["Pending"]));
        var home = new HomePage(page);

        await home.OpenAsync();

        await Expect(home.TaskFor("UI Alice", "Pending")).ToBeVisibleAsync();
    }, "Chairperson");
}
