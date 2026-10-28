using FakeItEasy;
using WarningSystems.core.Models.DTO;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;

namespace WarningSystemTest.Unit;

[Trait("Category", "Unit")]
public class LegalDetailsTests
{
    private readonly ManagerFixture _f = new();

    [Fact]
    public async Task Missing_id_is_rejected_without_database_call()
    {
        Assert.False((await _f.Manager.UpdateLegalIssueDetailsAsync(new(), "actor")).Success);
        Assert.Empty(Fake.GetCalls(_f.Data));
    }

    [Fact]
    public async Task Missing_issue_is_rejected()
    {
        A.CallTo(() => _f.Data.GetWarningByIdAsync(42)).Returns((Warning?)null);
        Assert.Equal("The issue could not be found.", (await _f.Manager.UpdateLegalIssueDetailsAsync(new() { WarningId = 42 }, "actor")).Message);
    }

    [Theory]
    [InlineData("Draft")]
    [InlineData("Validated")]
    [InlineData("Completed")]
    public async Task Non_review_status_cannot_change_details(string status)
    {
        WarningIn(status);
        Assert.False((await _f.Manager.UpdateLegalIssueDetailsAsync(new() { WarningId = 42, UpdateType = "dates", Dates = [new(2026, 1, 1)] }, "actor")).Success);
        A.CallTo(() => _f.Data.SaveLegalIssueDetailsAsync(A<long>._, A<IReadOnlyCollection<WarningCategory>?>._, A<WarningAnswer?>._, A<WarningNote>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task Unknown_update_type_is_rejected()
    {
        WarningIn("New");
        Assert.False((await _f.Manager.UpdateLegalIssueDetailsAsync(new() { WarningId = 42, UpdateType = "unknown" }, "actor")).Success);
    }

    [Theory]
    [InlineData("New")]
    [InlineData("In Progress")]
    [InlineData("Pending")]
    [InlineData("Added More Information")]
    public async Task Dates_are_deduplicated_sorted_and_audited(string status)
    {
        WarningIn(status);
        A.CallTo(() => _f.Data.GetWarningAnswersByWarningIdAsync(42)).Returns([new() { QuestionId = 1, AnswerJson = "{\"dates\":[\"2026-01-01\"]}" }]);
        A.CallTo(() => _f.Data.SaveLegalIssueDetailsAsync(42, null,
            A<WarningAnswer>.That.Matches(a => a.QuestionId == 1 && a.AnswerText == "2026-01-02, 2026-01-03" && a.AnswerJson!.Contains("2026-01-03")),
            A<WarningNote>.That.Matches(n => n.CreatedBy == "actor" && n.NoteText.Contains("2026-01-01") && n.NoteText.Contains("2026-01-03")))).Returns(Task.CompletedTask);
        var result = await _f.Manager.UpdateLegalIssueDetailsAsync(new() { WarningId = 42, UpdateType = " dates ", Dates = [new(2026, 1, 3), new(2026, 1, 2), new(2026, 1, 3)] }, " actor ");
        Assert.True(result.Success);
        A.CallTo(() => _f.Data.GetWarningByIdAsync(42)).MustHaveHappened();
        A.CallTo(() => _f.Data.GetWarningAnswersByWarningIdAsync(42)).MustHaveHappened();
        A.CallTo(() => _f.Data.SaveLegalIssueDetailsAsync(42, null,
            A<WarningAnswer>.That.Matches(a => a.QuestionId == 1 && a.AnswerText == "2026-01-02, 2026-01-03" && a.AnswerJson!.Contains("2026-01-03")),
            A<WarningNote>.That.Matches(n => n.CreatedBy == "actor" && n.NoteText.Contains("2026-01-01") && n.NoteText.Contains("2026-01-03")))).MustHaveHappened();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid json")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"dates\":true}")]
    public async Task Legacy_or_corrupt_dates_can_be_replaced(string? json)
    {
        WarningIn("New");
        A.CallTo(() => _f.Data.GetWarningAnswersByWarningIdAsync(42)).Returns([new() { QuestionId = 1, AnswerJson = json }]);
        A.CallTo(() => _f.Data.SaveLegalIssueDetailsAsync(42, null, A<WarningAnswer>._, A<WarningNote>.That.Matches(n => n.CreatedBy == "System"))).Returns(Task.CompletedTask);
        Assert.True((await _f.Manager.UpdateLegalIssueDetailsAsync(new() { WarningId = 42, UpdateType = "dates", Dates = [new(2026, 1, 1)] }, " ")).Success);
    }

    [Fact]
    public async Task Unchanged_dates_do_not_write()
    {
        WarningIn("New");
        A.CallTo(() => _f.Data.GetWarningAnswersByWarningIdAsync(42)).Returns([new() { QuestionId = 1, AnswerJson = "{\"dates\":[\"2026-01-01\"]}" }]);
        Assert.False((await _f.Manager.UpdateLegalIssueDetailsAsync(new() { WarningId = 42, UpdateType = "dates", Dates = [new(2026, 1, 1)] }, "actor")).Success);
    }

    [Fact]
    public async Task Categories_require_at_least_one_selection()
    {
        WarningIn("New");
        Assert.Equal("Select at least one category.", (await _f.Manager.UpdateLegalIssueDetailsAsync(new() { WarningId = 42, UpdateType = "categories" }, "actor")).Message);
    }

    [Fact]
    public async Task Categories_are_validated_and_audited()
    {
        WarningIn("New");
        A.CallTo(() => _f.Data.GetAllCategoriesAsync()).Returns([new() { CategoryId = 1, Name = "Old", IsActive = true }, new() { CategoryId = 2, Name = "New category", IsActive = true }]);
        A.CallTo(() => _f.Data.GetWarningCategoriesByWarningIdAsync(42)).Returns([new() { CategoryId = 1, Category = new() { Name = "Old" } }]);
        A.CallTo(() => _f.Data.SaveLegalIssueDetailsAsync(42, A<IReadOnlyCollection<WarningCategory>>.That.Matches(c => c.Count == 1 && c.First().CategoryId == 2), null,
            A<WarningNote>.That.Matches(n => n.NoteText.Contains("Old") && n.NoteText.Contains("New category")))).Returns(Task.CompletedTask);
        Assert.True((await _f.Manager.UpdateLegalIssueDetailsAsync(new() { WarningId = 42, UpdateType = "categories", CategoryIds = [2, 2, -1] }, "actor")).Success);
        A.CallTo(() => _f.Data.GetWarningByIdAsync(42)).MustHaveHappened();
        A.CallTo(() => _f.Data.GetAllCategoriesAsync()).MustHaveHappened();
        A.CallTo(() => _f.Data.GetWarningCategoriesByWarningIdAsync(42)).MustHaveHappened();
        A.CallTo(() => _f.Data.SaveLegalIssueDetailsAsync(42, A<IReadOnlyCollection<WarningCategory>>.That.Matches(c => c.Count == 1 && c.First().CategoryId == 2), null,
            A<WarningNote>.That.Matches(n => n.NoteText.Contains("Old") && n.NoteText.Contains("New category")))).MustHaveHappened();
    }

    [Theory]
    [InlineData(1, "The selected categories have not changed.")]
    [InlineData(2, "One or more selected categories are not available.")]
    [InlineData(999, "One or more selected categories are not available.")]
    public async Task Invalid_or_unchanged_categories_do_not_write(int selection, string expected)
    {
        WarningIn("New");
        A.CallTo(() => _f.Data.GetAllCategoriesAsync()).Returns([new() { CategoryId = 1, Name = "Old", IsActive = true }, new() { CategoryId = 2, Name = "Inactive", IsActive = false }]);
        A.CallTo(() => _f.Data.GetWarningCategoriesByWarningIdAsync(42)).Returns([new() { CategoryId = 1, Category = new() { Name = "Old" } }]);
        Assert.Equal(expected, (await _f.Manager.UpdateLegalIssueDetailsAsync(new() { WarningId = 42, UpdateType = "categories", CategoryIds = [selection] }, "actor")).Message);
    }

    [Fact]
    public async Task Data_errors_are_returned_as_validation_messages()
    {
        WarningIn("New");
        A.CallTo(() => _f.Data.GetWarningAnswersByWarningIdAsync(42)).Returns([]);
        A.CallTo(() => _f.Data.SaveLegalIssueDetailsAsync(42, null, A<WarningAnswer>._, A<WarningNote>._)).ThrowsAsync(new InvalidOperationException("Concurrent edit"));
        var result = await _f.Manager.UpdateLegalIssueDetailsAsync(new() { WarningId = 42, UpdateType = "dates", Dates = [new(2026, 1, 1)] }, "actor");
        Assert.False(result.Success);
        Assert.Equal("Concurrent edit", result.Message);
    }

    private void WarningIn(string status) => A.CallTo(() => _f.Data.GetWarningByIdAsync(42)).Returns(new Warning { WarningId = 42, Status = status });
}
