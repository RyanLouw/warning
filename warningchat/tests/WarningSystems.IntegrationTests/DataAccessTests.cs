using Microsoft.EntityFrameworkCore;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;
using WarningSystems.Core.Models.Enum;
using WarningSystems.Testing;

namespace WarningSystems.IntegrationTests;

public class DataAccessTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    private WarningSystemDataAccess Data(WarningSystemDbContext db) => new(db, fixture.App.Graph.Object);

    private async Task<long> NewWarningAsync(int status = 1)
    {
        await using var db = fixture.App.OpenDatabase();
        var lookup = await db.LookupIssueStatuses.FindAsync(status);
        var warning = new Warning { EmployeeId = "alice", CreatedBy = SeedData.ManagerId, CategoryId = 1,
            CreatedOn = DateTime.UtcNow, IssueStatusId = status, Status = lookup!.IssueStatusName };
        db.Warnings.Add(warning);
        await db.SaveChangesAsync();
        return warning.WarningId;
    }

    [Fact]
    public async Task Create_issue_persists_distinct_category_links_before_returning()
    {
        await using var db = fixture.App.OpenDatabase();
        var warning = new Warning { EmployeeId = "alice", CreatedBy = SeedData.ManagerId,
            CreatedOn = DateTime.UtcNow, CategoryId = 1, IssueStatusId = 1, Status = "Draft" };
        var id = await Data(db).CreateWarningAsync(warning, [1, 2, 2, 0, -1]);
        await using var independent = fixture.App.OpenDatabase();
        Assert.True(await independent.Warnings.AnyAsync(w => w.WarningId == id));
        Assert.Equal(new[] { 1, 2 }, await independent.WarningCategories.Where(c => c.WarningId == id).OrderBy(c => c.CategoryId).Select(c => c.CategoryId).ToArrayAsync());
    }

    [Fact]
    public async Task Failed_category_link_rolls_back_entire_issue()
    {
        await using var db = fixture.App.OpenDatabase();
        var warning = new Warning { EmployeeId = "alice", CreatedBy = SeedData.ManagerId,
            CreatedOn = DateTime.UtcNow, CategoryId = 1, IssueStatusId = 1, Status = "Draft" };
        await Assert.ThrowsAsync<DbUpdateException>(() => Data(db).CreateWarningAsync(warning, [999999]));
        await using var independent = fixture.App.OpenDatabase();
        Assert.False(await independent.Warnings.AnyAsync(w => w.WarningId == warning.WarningId));
    }

    [Fact]
    public async Task Answer_upsert_updates_one_row_instead_of_duplicating()
    {
        var id = await NewWarningAsync();
        await using var db = fixture.App.OpenDatabase();
        await Data(db).UpsertWarningAnswerAsync(id, 2, "First", null);
        await Data(db).UpsertWarningAnswerAsync(id, 2, "Updated", "{}");
        await using var independent = fixture.App.OpenDatabase();
        var answer = await independent.WarningAnswers.SingleAsync(a => a.WarningId == id && a.QuestionId == 2);
        Assert.Equal("Updated", answer.AnswerText);
        Assert.Equal("{}", answer.AnswerJson);
    }

    [Theory]
    [InlineData(1, IssueStatusGroup.Draft, 2, "New")]
    [InlineData(2, IssueStatusGroup.LegalReview, 3, "In Progress")]
    [InlineData(4, IssueStatusGroup.EmployeeAction, 5, "Completed")]
    [InlineData(5, IssueStatusGroup.LegalValidation, 6, "Validated")]
    public async Task Status_transition_persists_lookup_legacy_status_and_actor(int source, IssueStatusGroup group, int target, string display)
    {
        var id = await NewWarningAsync(source);
        await using var db = fixture.App.OpenDatabase();
        await Data(db).AdvanceWarningStatusAsync(id, group, " actor ");
        await using var independent = fixture.App.OpenDatabase();
        var warning = await independent.Warnings.SingleAsync(w => w.WarningId == id);
        Assert.Equal(target, warning.IssueStatusId);
        Assert.Equal(display, warning.Status);
        Assert.Equal("actor", warning.LastStatusChangedBy);
        Assert.NotNull(warning.LastStatusChangedOn);
        if (source == 1) Assert.NotNull(warning.SubmittedOn);
    }

    [Fact]
    public async Task Invalid_transition_does_not_change_database()
    {
        var id = await NewWarningAsync(1);
        await using var db = fixture.App.OpenDatabase();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Data(db).AdvanceWarningStatusAsync(id, IssueStatusGroup.LegalReview, "actor"));
        await using var independent = fixture.App.OpenDatabase();
        Assert.Equal(1, (await independent.Warnings.FindAsync(id))!.IssueStatusId);
    }

    [Fact]
    public async Task Closed_issue_has_no_next_status()
    {
        var id = await NewWarningAsync(6);
        await using var db = fixture.App.OpenDatabase();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Data(db).AdvanceWarningStatusAsync(id, IssueStatusGroup.Closed, "actor"));
    }

    [Fact]
    public async Task Missing_issue_status_change_fails()
    {
        await using var db = fixture.App.OpenDatabase();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Data(db).SetWarningStatusAsync(long.MaxValue, IssueStatusGroup.Draft, "actor"));
    }

    [Fact]
    public async Task Decision_persists_type_subtype_and_resulting_status()
    {
        var id = await NewWarningAsync(3);
        await using var db = fixture.App.OpenDatabase();
        var type = (await Data(db).GetActiveIssueTypesAsync()).Single(t => t.IssueTypeId == 2);
        await Data(db).UpdateWarningDecisionAsync(id, type, type.IssueSubTypes.Single(), "legal-actor");
        await using var independent = fixture.App.OpenDatabase();
        var result = (await independent.Warnings.FindAsync(id))!;
        Assert.Equal(2, result.IssueTypeId);
        Assert.Equal(1, result.IssueSubTypeId);
        Assert.Equal(4, result.IssueStatusId);
        Assert.Equal("Warning", result.Type);
        Assert.Equal("Written warning", result.WarningSubtype);
        Assert.Equal("Pending", result.Status);
        Assert.Equal("legal-actor", result.LastStatusChangedBy);
    }

    [Fact]
    public async Task Quick_discussion_is_final_without_legal_due_date()
    {
        var id = await NewWarningAsync();
        await using var db = fixture.App.OpenDatabase();
        await Data(db).CompleteDiscussionAsync(id, 2, "actor");
        await using var independent = fixture.App.OpenDatabase();
        var result = (await independent.Warnings.FindAsync(id))!;
        Assert.Equal(3, result.IssueTypeId);
        Assert.Equal(2, result.IssueSubTypeId);
        Assert.Equal(6, result.IssueStatusId);
        Assert.Null(result.LegalExpiryDate);
    }

    [Fact]
    public async Task Evidence_file_and_note_are_linked_in_database()
    {
        var id = await NewWarningAsync();
        await using var db = fixture.App.OpenDatabase();
        var evidenceId = await Data(db).SaveEvidenceAsync(id, " proof.pdf ", "pdf", 123, " actor ", " Evidence note ", 1);
        await using var independent = fixture.App.OpenDatabase();
        var file = await independent.WarningEvidence.SingleAsync(e => e.EvidenceId == evidenceId);
        Assert.Equal(id, file.WarningId);
        Assert.Equal("proof.pdf", file.FileName);
        Assert.Equal(123, file.FileSizeBytes);
        Assert.Equal("actor", file.UploadedBy);
        var note = await independent.WarningNotes.SingleAsync(n => n.WarningId == id);
        Assert.Equal(evidenceId, note.EvidenceId);
        Assert.Equal("Evidence note", note.NoteText);
    }

    [Fact]
    public async Task Note_only_evidence_does_not_create_empty_file()
    {
        var id = await NewWarningAsync();
        await using var db = fixture.App.OpenDatabase();
        Assert.Null(await Data(db).SaveEvidenceAsync(id, null, null, null, "actor", "note only", 1));
        await using var independent = fixture.App.OpenDatabase();
        Assert.False(await independent.WarningEvidence.AnyAsync(e => e.WarningId == id));
        Assert.Equal("note only", (await independent.WarningNotes.SingleAsync(n => n.WarningId == id)).NoteText);
    }

    [Theory]
    [InlineData(0, "file.pdf", "note")]
    [InlineData(1, null, null)]
    [InlineData(1, " ", " ")]
    public async Task Empty_or_unidentified_evidence_is_rejected(long id, string? file, string? note)
    {
        await using var db = fixture.App.OpenDatabase();
        await Assert.ThrowsAsync<ArgumentException>(() => Data(db).SaveEvidenceAsync(id, file, null, null, "actor", note, 1));
    }

    [Theory]
    [InlineData(0, 1, "note")]
    [InlineData(1, 0, "note")]
    [InlineData(1, 1, " ")]
    public async Task Note_validation_rejects_invalid_requests(long id, int type, string text)
    {
        await using var db = fixture.App.OpenDatabase();
        await Assert.ThrowsAsync<ArgumentException>(() => Data(db).AddWarningNoteAsync(id, null, type, text, "actor"));
    }

    [Fact]
    public async Task Evidence_from_another_issue_cannot_be_linked_to_note()
    {
        var first = await NewWarningAsync();
        var second = await NewWarningAsync();
        await using var db = fixture.App.OpenDatabase();
        var evidence = await Data(db).SaveEvidenceAsync(first, "file.pdf", "pdf", 1, "actor", null, null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Data(db).AddWarningNoteAsync(second, evidence, 1, "note", "actor"));
        await using var independent = fixture.App.OpenDatabase();
        Assert.False(await independent.WarningNotes.AnyAsync(n => n.WarningId == second));
    }

    [Fact]
    [Trait("Database", "SqlServer")]
    public async Task Create_category_installs_default_questions_using_real_stored_procedure()
    {
        await using var db = fixture.App.OpenDatabase();
        var category = new TransgressionCategory { Name = $"Test category {Guid.NewGuid():N}", IsActive = true, CreatedBy = "actor" };
        var id = await Data(db).CreateCategoryAsync(category);
        await using var independent = fixture.App.OpenDatabase();
        Assert.Equal(new[] { 1, 2, 3 }, await independent.CategoryQuestions.Where(q => q.CategoryId == id).OrderBy(q => q.QuestionId).Select(q => q.QuestionId).ToArrayAsync());
    }

    [Fact]
    public async Task Unknown_category_update_is_explicit()
    {
        await using var db = fixture.App.OpenDatabase();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Data(db).UpdateCategoryAsync(new() { CategoryId = int.MaxValue }));
    }

    [Fact]
    public async Task Unknown_question_update_is_explicit()
    {
        await using var db = fixture.App.OpenDatabase();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Data(db).UpdateQuestionAsync(new() { QuestionId = int.MaxValue }));
    }

    [Fact]
    public async Task Empty_warning_collections_return_empty_results()
    {
        await using var db = fixture.App.OpenDatabase();
        Assert.Empty(await Data(db).GetWarningAnswersByWarningIdsAsync([]));
        Assert.Empty(await Data(db).GetWarningNotesByWarningIdsAsync([]));
        Assert.Empty(await Data(db).GetActiveCategoryQuestionsAsync([]));
    }
}
