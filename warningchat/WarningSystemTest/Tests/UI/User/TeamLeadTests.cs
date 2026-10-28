using Microsoft.Playwright;
using WarningSystemTest.Fixtures;
using WarningSystemTest.Pages;
using static Microsoft.Playwright.Assertions;

namespace WarningSystemTest.Tests.UI.User;

[Collection("Warning browser")]
[Trait("Category", "UI")]
[Trait("Role", "User")]
public sealed class TeamLeadTests(BrowserFixture browser) : UiTestBase(browser)
{
    [Theory]
    [InlineData("Pending", true)]
    [InlineData("Issued", false)]
    [InlineData("Validated", false)]
    public Task Add_evidence_button_matches_issue_state(string state, bool visible) =>
        RunAsync(nameof(Add_evidence_button_matches_issue_state), async page =>
        {
            var teamLead = new TeamLeadPage(page);
            await teamLead.OpenAsync(Database.Warnings[state]);

            if (visible) await Expect(teamLead.AddEvidence).ToBeVisibleAsync();
            else await Expect(teamLead.AddEvidence).ToBeHiddenAsync();
        });

    [Theory]
    [InlineData("Pending", true)]
    [InlineData("Issued", false)]
    [InlineData("Validated", false)]
    public Task Notify_legal_button_matches_issue_state(string state, bool visible) =>
        RunAsync(nameof(Notify_legal_button_matches_issue_state), async page =>
        {
            var teamLead = new TeamLeadPage(page);
            await teamLead.OpenAsync(Database.Warnings[state]);

            if (visible) await Expect(teamLead.NotifyLegal).ToBeVisibleAsync();
            else await Expect(teamLead.NotifyLegal).ToBeHiddenAsync();
        });

    [Fact]
    public Task Add_evidence_opens_the_dialog() => RunAsync(nameof(Add_evidence_opens_the_dialog), async page =>
    {
        var teamLead = new TeamLeadPage(page);
        await teamLead.OpenAsync(Database.Warnings["Pending"]);

        await teamLead.OpenEvidenceDialogAsync();

        await Expect(teamLead.EvidenceDialog).ToBeVisibleAsync();
    });

    [Fact]
    public Task Cancelling_evidence_does_not_save_the_note() => RunAsync(nameof(Cancelling_evidence_does_not_save_the_note), async page =>
    {
        const string note = "UI cancelled team lead note";
        var teamLead = new TeamLeadPage(page);
        await teamLead.OpenAsync(Database.Warnings["Pending"]);
        await teamLead.OpenEvidenceDialogAsync();
        await teamLead.SetNoteAsync(note);

        await teamLead.CancelEvidenceAsync();

        await Expect(teamLead.EvidenceDialog).ToBeHiddenAsync();
        Assert.Empty(await Database.QueryAsync("SELECT NoteId FROM ws.WarningNote WHERE NoteText=@Note", ("@Note", note)));
    });

    [Fact]
    public Task Notify_legal_hides_the_notify_button_after_refresh() => RunAsync(nameof(Notify_legal_hides_the_notify_button_after_refresh), async page =>
    {
        var teamLead = new TeamLeadPage(page);
        page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
        await teamLead.OpenAsync(Database.Warnings["Pending"]);

        var response = await teamLead.NotifyLegalAsync();
        Assert.True(response.Ok, $"Notify Legal failed with HTTP {response.Status}: {await response.TextAsync()}");
        await teamLead.ReloadAsync();

        await Expect(teamLead.NotifyLegal).ToBeHiddenAsync();
    });

    [Fact]
    public Task Notify_legal_makes_evidence_read_only_after_refresh() => RunAsync(nameof(Notify_legal_makes_evidence_read_only_after_refresh), async page =>
    {
        var teamLead = new TeamLeadPage(page);
        page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
        await teamLead.OpenAsync(Database.Warnings["Pending"]);

        var response = await teamLead.NotifyLegalAsync();
        Assert.True(response.Ok, $"Notify Legal returned HTTP {response.Status}");
        await teamLead.ReloadAsync();

        await Expect(teamLead.AddEvidence).ToBeHiddenAsync();
    });

    [Fact]
    public Task Validated_issue_rejects_a_direct_note_submission() => RunAsync(nameof(Validated_issue_rejects_a_direct_note_submission), async page =>
    {
        // Hidden controls alone are insufficient: the server must reject a direct POST too.
        var id = Database.Warnings["Validated"];
        var teamLead = new TeamLeadPage(page);
        await teamLead.OpenAsync(id);
        var form = page.Context.APIRequest.CreateFormData()
            .Set("WarningId", id.ToString())
            .Set("NoteText", "UI forbidden note on validated issue")
            .Set("__RequestVerificationToken", await teamLead.AntiforgeryTokenAsync());

        var response = await page.Context.APIRequest.PostAsync("/Transgression/AddTeamLeadEvidence", new() { Form = form, MaxRedirects = 0 });

        Assert.Empty(await Database.QueryAsync("SELECT NoteId FROM ws.WarningNote WHERE WarningId=@Id AND NoteText=@Note",
            ("@Id", id), ("@Note", "UI forbidden note on validated issue")));
        Assert.True(response.Status is 400 or 403 or 409,
            $"A validated issue must reject editing; actual HTTP {response.Status}.");
    });
}
