using WarningSystemTest.Fixtures;
using WarningSystemTest.Pages;
using static Microsoft.Playwright.Assertions;

namespace WarningSystemTest.Tests.UI.Legal;

[Collection("Warning browser")]
[Trait("Category", "UI")]
[Trait("Role", "Legal")]
public sealed class DialogTests(BrowserFixture browser) : UiTestBase(browser)
{
    [Fact]
    public Task Cancelling_due_date_keeps_the_original_database_date() => RunAsync(nameof(Cancelling_due_date_keeps_the_original_database_date), async page =>
    {
        var id = Database.Warnings["Decision"];
        var before = Assert.Single(await Database.QueryAsync("SELECT LegalExpiryDate FROM ws.Warning WHERE WarningId=@Id", ("@Id", id)))["LegalExpiryDate"];
        var legal = new LegalPage(page);
        await legal.OpenAsync(id);
        await legal.OpenDueDateAsync();
        await legal.FillDueDateAsync("2027-02-01", "UI cancelled due date");

        await legal.CancelDueDateAsync();

        await Expect(legal.DueDateDialog).ToBeHiddenAsync();
        var after = Assert.Single(await Database.QueryAsync("SELECT LegalExpiryDate FROM ws.Warning WHERE WarningId=@Id", ("@Id", id)))["LegalExpiryDate"];
        Assert.Equal(before, after);
    }, "Legal");

    [Fact]
    public Task Cancelling_information_request_does_not_save_the_note() => RunAsync(nameof(Cancelling_information_request_does_not_save_the_note), async page =>
    {
        var legal = new LegalPage(page);
        await legal.OpenAsync(Database.Warnings["Decision"]);
        await legal.OpenInformationRequestAsync();
        await legal.SetInformationMessageAsync("UI cancelled information request");

        await legal.CancelInformationRequestAsync();

        await Expect(legal.InformationDialog).ToBeHiddenAsync();
        Assert.Empty(await Database.QueryAsync("SELECT NoteId FROM ws.WarningNote WHERE NoteText=@Text", ("@Text", "UI cancelled information request")));
    }, "Legal");

    [Fact]
    public Task Empty_information_request_shows_a_validation_message() => RunAsync(nameof(Empty_information_request_shows_a_validation_message), async page =>
    {
        var legal = new LegalPage(page);
        await legal.OpenAsync(Database.Warnings["Decision"]);
        await legal.OpenInformationRequestAsync();

        await page.Locator("#btnRequestTeamLeadInfo").ClickAsync();

        await Expect(page.Locator("#requestInfoError")).ToContainTextAsync("Please add a message");
    }, "Legal");

    [Fact]
    public Task Hiding_an_issue_changes_the_button_to_show_after_refresh() => RunAsync(nameof(Hiding_an_issue_changes_the_button_to_show_after_refresh), async page =>
    {
        var legal = new LegalPage(page);
        page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
        await legal.OpenAsync(Database.Warnings["Decision"]);

        var response = await legal.ToggleVisibilityAsync();
        Assert.True(response.Ok, $"Changing visibility returned HTTP {response.Status}");
        await legal.ReloadAsync();

        await Expect(legal.Action("Visibility")).ToHaveTextAsync("Show to Team Leads");
    }, "Legal");

    [Fact]
    public Task Legal_still_sees_an_issue_hidden_from_its_creator() => RunAsync(nameof(Legal_still_sees_an_issue_hidden_from_its_creator), async page =>
    {
        await Database.QueryAsync("UPDATE ws.Warning SET HideFromTeamLead=1 WHERE WarningId=@Id", ("@Id", Database.Warnings["Pending"]));
        var home = new HomePage(page);

        await home.OpenAsync();

        await Expect(home.TaskFor("UI Alice", "Pending")).ToBeVisibleAsync();
    }, "Legal");
}
