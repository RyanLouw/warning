using WarningSystemTest.Fixtures;
using static Microsoft.Playwright.Assertions;
using WarningSystemTest.Pages;

namespace WarningSystemTest.Tests.UI.User;

[Collection("Warning browser")]
[Trait("Category", "UI")]
[Trait("Role", "User")]
public sealed class AccessTests(BrowserFixture browser) : UiTestBase(browser)
{
    [Theory]
    [InlineData("/")]
    [InlineData("/Transgression/Index")]
    public Task Allowed_page_opens(string route) => RunAsync(nameof(Allowed_page_opens), async page =>
    {
        var home = new HomePage(page);
        var status = await home.OpenRouteAsync(route.Replace("{id}", Database.Warnings["Decision"].ToString()));

        Assert.Equal(200, status);
    }, "User");

    [Theory]
    [InlineData("/Admin/Index")]
    [InlineData("/Legal/Index/{id}")]
    public Task Restricted_page_returns_forbidden(string route) => RunAsync(nameof(Restricted_page_returns_forbidden), async page =>
    {
        var home = new HomePage(page);
        var status = await home.OpenRouteAsync(route.Replace("{id}", Database.Warnings["Decision"].ToString()));

        Assert.Equal(403, status);
    }, "User");
    [Theory]
    [InlineData("CreateIssue")]
    [InlineData("Discussion")]
    public Task Creation_button_visibility_matches_role(string action) => RunAsync($"{nameof(Creation_button_visibility_matches_role)}-{action}", async page =>
    {
        var home = new HomePage(page);
        await home.OpenAsync();
        var button = action == "CreateIssue" ? home.NewIssue : home.NewDiscussion;

        await Expect(button).ToBeVisibleAsync();
    }, "User");

    [Theory]
    [InlineData("/Legal/ValidateTransgression")]
    [InlineData("/Legal/UpdateDueDate")]
    [InlineData("/Legal/SetTeamLeadVisibility")]
    [InlineData("/Legal/CompleteTransgression")]
    [InlineData("/Legal/RequestTeamLeadInformation")]
    public Task Restricted_action_returns_forbidden(string route) => RunAsync(nameof(Restricted_action_returns_forbidden), async page =>
    {
        var home = new HomePage(page);
        await home.OpenAsync();
        var token = await home.AntiforgeryTokenAsync();
        var response = await page.Context.APIRequest.PostAsync(route, new()
        {
            DataObject = new { WarningId = Database.Warnings["Pending"], QuestionId = 2, AnswerText = "UI forbidden edit" },
            Headers = new Dictionary<string,string> { ["RequestVerificationToken"] = token }
        });

        Assert.Equal(403, response.Status);
    }, "User");
}
