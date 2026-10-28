using System.Text.Json;
using Microsoft.Playwright;
using WarningSystemTest.Fixtures;
using WarningSystemTest.Pages;

namespace WarningSystemTest.Tests.UI.Legal;

[Collection("Warning browser")]
[Trait("Category", "UI")]
[Trait("Role", "Legal")]
public sealed class InformationRequestTests(BrowserFixture browser) : UiTestBase(browser)
{
    private async Task SendRequestAsync(IPage page, string message)
    {
        var legal = new LegalPage(page);
        await legal.OpenAsync(Database.Warnings["Decision"]);
        await legal.OpenInformationRequestAsync();
        await legal.SetInformationMessageAsync(message);
        var response = await legal.SendInformationRequestAsync();
        Assert.True(response.Ok, $"Information request failed with HTTP {response.Status}: {await response.TextAsync()}");
    }

    [Fact]
    public Task Request_moves_issue_to_more_information_status_group() => RunAsync(nameof(Request_moves_issue_to_more_information_status_group), async page =>
    {
        await SendRequestAsync(page, "UI clarification needed for the incident");
        var row = Assert.Single(await Database.QueryAsync("SELECT s.IssueStatusGroup FROM ws.Warning w JOIN ws.LookupIssueStatuses s ON s.IssueStatusId=w.IssueStatusId WHERE WarningId=@Id", ("@Id", Database.Warnings["Decision"])));

        Assert.Equal(7, Convert.ToInt32(row["IssueStatusGroup"]));
    }, "Legal");

    [Fact]
    public Task Request_saves_the_message_in_interaction_history() => RunAsync(nameof(Request_saves_the_message_in_interaction_history), async page =>
    {
        const string message = "UI please provide the attendance register";

        await SendRequestAsync(page, message);

        Assert.Single(await Database.QueryAsync("SELECT NoteId FROM ws.WarningNote WHERE WarningId=@Id AND NoteText=@Text", ("@Id", Database.Warnings["Decision"]), ("@Text", message)));
    }, "Legal");

    [Fact]
    public Task Request_email_targets_the_issue_creator() => RunAsync(nameof(Request_email_targets_the_issue_creator), async page =>
    {
        const string message = "UI clarification for email recipient test";
        await SendRequestAsync(page, message);
        using var outbox = JsonDocument.Parse(await (await page.Context.APIRequest.GetAsync("/__testing/outbox")).TextAsync());
        var issue = Database.Warnings["Decision"].ToString();
        var mail = Assert.Single(outbox.RootElement.EnumerateArray(), item =>
            item.GetProperty("subject").GetString()!.EndsWith($"Issue {issue}"));

        Assert.Equal(new[] { "user@example.test" }, mail.GetProperty("recipients").EnumerateArray().Select(item => item.GetString()));
    }, "Legal");
}
