using WarningSystemTest.Fixtures;
using WarningSystemTest.Pages;
using static Microsoft.Playwright.Assertions;

namespace WarningSystemTest.Tests.UI.User;

[Collection("Warning browser")]
[Trait("Category", "UI")]
[Trait("Role", "User")]
public sealed class TaskVisibilityTests(BrowserFixture browser) : UiTestBase(browser)
{
    [Fact]
    public Task Creator_cannot_see_an_issue_hidden_from_them() => RunAsync(nameof(Creator_cannot_see_an_issue_hidden_from_them), async page =>
    {
        await Database.QueryAsync("UPDATE ws.Warning SET HideFromTeamLead=1 WHERE WarningId=@Id", ("@Id", Database.Warnings["Pending"]));
        var home = new HomePage(page);

        await home.OpenAsync();

        await Expect(home.TaskFor("UI Alice", "Pending")).ToHaveCountAsync(0);
    });

    [Fact]
    public Task Validated_task_does_not_offer_an_edit_link() => RunAsync(nameof(Validated_task_does_not_offer_an_edit_link), async page =>
    {
        var home = new HomePage(page);
        await home.OpenAsync();

        var rows = await home.TaskFor("UI Cara", "Validated").AllAsync();
        Assert.NotEmpty(rows);
        foreach (var row in rows) await Expect(row).ToHaveAttributeAsync("data-url", "");
    });

    [Fact]
    public Task Create_issue_button_opens_the_warning_wizard() => RunAsync(nameof(Create_issue_button_opens_the_warning_wizard), async page =>
    {
        var home = new HomePage(page);
        await home.OpenAsync();

        await home.ClickNewIssueAsync();

        await Expect(new WarningWizardPage(page).Employee).ToBeVisibleAsync();
    });

    [Fact]
    public Task Discussion_button_opens_the_discussion_form() => RunAsync(nameof(Discussion_button_opens_the_discussion_form), async page =>
    {
        var home = new HomePage(page);
        await home.OpenAsync();

        await home.OpenDiscussionAsync();

        await Expect(home.DiscussionDialog).ToBeVisibleAsync();
    });
    [Theory]
    [InlineData("/Transgression/TeamLeadWarning/")]
    [InlineData("/Transgression/Index/")]
    public Task Creator_cannot_open_a_hidden_issue_using_its_direct_url(string route) =>
        RunAsync(nameof(Creator_cannot_open_a_hidden_issue_using_its_direct_url), async page =>
        {
            var id = Database.Warnings["Pending"];
            await Database.QueryAsync("UPDATE ws.Warning SET HideFromTeamLead=1 WHERE WarningId=@Id", ("@Id", id));
            var home = new HomePage(page);

            var status = await home.OpenRouteAsync(route + id);

            Assert.Equal(403, status);
        });
}
