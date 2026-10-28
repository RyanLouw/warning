using WarningSystemTest.Fixtures;
using WarningSystemTest.Pages;
using static Microsoft.Playwright.Assertions;

namespace WarningSystemTest.Tests.UI.Legal;

[Collection("Warning browser")]
[Trait("Category", "UI")]
[Trait("Role", "Legal")]
public sealed class ButtonVisibilityTests(BrowserFixture browser) : UiTestBase(browser)
{
    [Theory]
    [InlineData("Decision", "Decision", true)]
    [InlineData("Pending", "Decision", false)]
    [InlineData("Issued", "Decision", false)]
    [InlineData("Validated", "Decision", false)]
    [InlineData("Decision", "Validate", false)]
    [InlineData("Pending", "Validate", false)]
    [InlineData("Issued", "Validate", true)]
    [InlineData("Validated", "Validate", false)]
    [InlineData("Decision", "DueDate", true)]
    [InlineData("Pending", "DueDate", true)]
    [InlineData("Issued", "DueDate", true)]
    [InlineData("Validated", "DueDate", false)]
    [InlineData("Decision", "CategoriesAndDates", true)]
    [InlineData("Pending", "CategoriesAndDates", true)]
    [InlineData("Issued", "CategoriesAndDates", true)]
    [InlineData("Validated", "CategoriesAndDates", false)]
    [InlineData("Decision", "MoreInformation", true)]
    [InlineData("Pending", "MoreInformation", true)]
    [InlineData("Issued", "MoreInformation", true)]
    [InlineData("Validated", "MoreInformation", false)]
    [InlineData("Decision", "Visibility", true)]
    [InlineData("Pending", "Visibility", true)]
    [InlineData("Issued", "Visibility", true)]
    [InlineData("Validated", "Visibility", false)]
    public Task Action_button_matches_issue_state(string state, string action, bool visible) =>
        RunAsync($"{nameof(Action_button_matches_issue_state)}-{state}-{action}", async page =>
        {
            var legal = new LegalPage(page);
            await legal.OpenAsync(Database.Warnings[state]);

            if (visible) await Expect(legal.Action(action)).ToBeVisibleAsync();
            else await Expect(legal.Action(action)).ToBeHiddenAsync();
        }, "Legal");

    [Theory]
    [InlineData("DueDate", "#dueDateModal")]
    [InlineData("Decision", "#completeModal")]
    [InlineData("CategoriesAndDates", "#editIssueDetailsModal")]
    [InlineData("MoreInformation", "#requestTeamLeadInfoModal")]
    public Task Action_opens_its_own_dialog(string action, string dialog) =>
        RunAsync($"{nameof(Action_opens_its_own_dialog)}-{action}", async page =>
        {
            var legal = new LegalPage(page);
            await legal.OpenAsync(Database.Warnings["Decision"]);

            await legal.Action(action).ClickAsync();

            await Expect(page.Locator(dialog)).ToBeVisibleAsync();
        }, "Legal");
}
