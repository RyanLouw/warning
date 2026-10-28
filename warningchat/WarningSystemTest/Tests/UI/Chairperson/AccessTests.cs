using WarningSystemTest.Fixtures;
using static Microsoft.Playwright.Assertions;
using WarningSystemTest.Pages;

namespace WarningSystemTest.Tests.UI.Chairperson;

[Collection("Warning browser")]
[Trait("Category", "UI")]
[Trait("Role", "Chairperson")]
public sealed class AccessTests(BrowserFixture browser) : UiTestBase(browser)
{
    [Theory]
    [InlineData("/")]
    public Task Allowed_page_opens(string route) => RunAsync(nameof(Allowed_page_opens), async page =>
    {
        var home = new HomePage(page);
        var status = await home.OpenRouteAsync(route.Replace("{id}", Database.Warnings["Decision"].ToString()));

        Assert.Equal(200, status);
    }, "Chairperson");

    [Theory]
    [InlineData("/Admin/Index")]
    [InlineData("/Legal/Index/{id}")]
    [InlineData("/Transgression/Index")]
    [InlineData("/Transgression/TeamLeadWarning/{id}")]
    public Task Restricted_page_returns_forbidden(string route) => RunAsync(nameof(Restricted_page_returns_forbidden), async page =>
    {
        var home = new HomePage(page);
        var status = await home.OpenRouteAsync(route.Replace("{id}", Database.Warnings["Decision"].ToString()));

        Assert.Equal(403, status);
    }, "Chairperson");
    [Theory]
    [InlineData("CreateIssue")]
    [InlineData("Discussion")]
    public Task Creation_button_visibility_matches_role(string action) => RunAsync($"{nameof(Creation_button_visibility_matches_role)}-{action}", async page =>
    {
        var home = new HomePage(page);
        await home.OpenAsync();
        var button = action == "CreateIssue" ? home.NewIssue : home.NewDiscussion;

        await Expect(button).ToBeHiddenAsync();
    }, "Chairperson");

    [Theory]
    [InlineData("/Transgression/SaveAnswer")]
    [InlineData("/Transgression/SaveAnswers")]
    [InlineData("/Transgression/UpdateStatus")]
    [InlineData("/Home/CreateAbsenceDiscussion")]
    [InlineData("/Transgression/NotifyLegalIssueCompleted")]
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
    }, "Chairperson");
}
