using System.Text;
using Microsoft.Playwright;
using WarningSystemTest.Fixtures;
using WarningSystemTest.Pages;
using static Microsoft.Playwright.Assertions;

namespace WarningSystemTest.Tests.UI.User;

[Collection("Warning browser")]
[Trait("Category", "UI")]
[Trait("Role", "User")]
public sealed class EvidenceTests(BrowserFixture browser) : UiTestBase(browser)
{
    private async Task<TeamLeadPage> OpenInformationRequestAsync(IPage page)
    {
        var id = Database.Warnings["Pending"];
        await Database.QueryAsync("""
            UPDATE w SET IssueStatusId=s.IssueStatusId,Status=s.IssueStatusName
            FROM ws.Warning w JOIN ws.LookupIssueStatuses s ON s.IssueStatusGroup=7
            WHERE w.WarningId=@Id
            """, ("@Id", id));
        var teamLead = new TeamLeadPage(page);
        await teamLead.OpenAsync(id);
        await teamLead.OpenEvidenceDialogAsync();
        return teamLead;
    }

    [Fact]
    public Task Requested_information_can_be_saved_as_a_note() => RunAsync(nameof(Requested_information_can_be_saved_as_a_note), async page =>
    {
        const string note = "UI team lead provides the missing incident details";
        var teamLead = await OpenInformationRequestAsync(page);
        await teamLead.SetNoteAsync(note);

        var response = await teamLead.SaveEvidenceAsync();
        Assert.True(response.Status is 200 or 302, $"Saving information returned HTTP {response.Status}: {await response.TextAsync()}");

        Assert.Single(await Database.QueryAsync("SELECT NoteId FROM ws.WarningNote WHERE WarningId=@Id AND NoteText=@Note", ("@Id", Database.Warnings["Pending"]), ("@Note", note)));
    });

    [Fact]
    public Task Requested_document_is_saved_with_the_logged_in_uploader() => RunAsync(nameof(Requested_document_is_saved_with_the_logged_in_uploader), async page =>
    {
        var teamLead = await OpenInformationRequestAsync(page);
        await teamLead.SetFilesAsync(new FilePayload { Name="ui-register.txt", MimeType="text/plain", Buffer=Encoding.UTF8.GetBytes("UI attendance register") });

        var response = await teamLead.SaveEvidenceAsync();
        Assert.True(response.Status is 200 or 302, $"Saving a document returned HTTP {response.Status}: {await response.TextAsync()}");

        var file = Assert.Single(await Database.QueryAsync("SELECT UploadedBy FROM ws.WarningEvidence WHERE WarningId=@Id", ("@Id", Database.Warnings["Pending"])));
        Assert.Equal("test-user", file["UploadedBy"]);
    });

    [Fact]
    public Task Saving_requested_information_updates_the_status_group() => RunAsync(nameof(Saving_requested_information_updates_the_status_group), async page =>
    {
        var teamLead = await OpenInformationRequestAsync(page);
        await teamLead.SetNoteAsync("UI requested information is now complete");

        var response = await teamLead.SaveEvidenceAsync();
        Assert.True(response.Status is 200 or 302, $"Saving information returned HTTP {response.Status}");

        var row = Assert.Single(await Database.QueryAsync("SELECT s.IssueStatusGroup FROM ws.Warning w JOIN ws.LookupIssueStatuses s ON s.IssueStatusId=w.IssueStatusId WHERE WarningId=@Id", ("@Id", Database.Warnings["Pending"])));
        Assert.Equal(8, Convert.ToInt32(row["IssueStatusGroup"]));
    });
}
