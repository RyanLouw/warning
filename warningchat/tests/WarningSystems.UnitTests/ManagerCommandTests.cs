using Microsoft.Graph.Models;
using Moq;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;
using WarningSystems.Core.Models.Enum;
using WarningSystems.Models.DTO;

namespace WarningSystems.UnitTests;

public class ManagerCommandTests
{
    private readonly ManagerFixture _f = new();

    [Theory]
    [InlineData("", "text")]
    [InlineData(" ", "text")]
    [InlineData("Question", "")]
    [InlineData("Question", " ")]
    public async Task Create_question_requires_text_and_type(string question, string type)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.Manager.CreateQuestionAsync(new(question, type)));
        _f.Data.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Duplicate_question_is_rejected()
    {
        _f.Data.Setup(x => x.QuestionExistsAsync("Question", "text")).ReturnsAsync(true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.Manager.CreateQuestionAsync(new("Question", "text")));
        _f.Data.Verify(x => x.CreateQuestionAsync(It.IsAny<Question>()), Times.Never);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task Create_question_trims_and_records_actor(bool? active, bool expected)
    {
        _f.Data.Setup(x => x.QuestionExistsAsync("Question", "text")).ReturnsAsync(false);
        _f.Data.Setup(x => x.CreateQuestionAsync(It.Is<Question>(q => q.QuestionText == "Question" && q.ControlType == "text" && q.CreatedBy == "actor" && q.IsActive == expected && q.DefaultConfigJson == "{}"))).ReturnsAsync(42);
        Assert.Equal(42, await _f.Manager.CreateQuestionAsync(new(" Question ", " text ", " {} ", active)));
        _f.Data.VerifyAll();
    }

    [Fact]
    public async Task Create_question_requires_actor()
    {
        _f.Me.SetupGet(x => x.ObjectId).Returns((string?)null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.Manager.CreateQuestionAsync(new("Q", "text")));
        _f.Data.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0, "Question", "text", typeof(ArgumentException))]
    [InlineData(1, "", "text", typeof(InvalidOperationException))]
    [InlineData(1, "Question", "", typeof(InvalidOperationException))]
    public async Task Update_question_validates_input(int id, string text, string type, Type exception)
    {
        var thrown = await Record.ExceptionAsync(() => _f.Manager.UpdateQuestionAsync(new(id, text, type, null, true)));
        Assert.IsType(exception, thrown);
        _f.Data.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_question_records_changes()
    {
        _f.Data.Setup(x => x.UpdateQuestionAsync(It.Is<Question>(q => q.QuestionId == 1 && q.QuestionText == "Q" && q.ControlType == "text" && q.UpdatedBy == "actor" && q.DefaultConfigJson == null))).Returns(Task.CompletedTask);
        await _f.Manager.UpdateQuestionAsync(new(1, " Q ", " text ", " ", false));
        _f.Data.VerifyAll();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Category_name_is_required(string name)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.Manager.CreateCategoryAsync(new(name)));
        _f.Data.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_category_records_actor_and_passes_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        _f.Data.Setup(x => x.CreateCategoryAsync(It.Is<TransgressionCategory>(c => c.Name == "Safety" && c.CreatedBy == "actor" && c.IsActive), cancellation.Token)).ReturnsAsync(7);
        Assert.Equal(7, await _f.Manager.CreateCategoryAsync(new(" Safety "), cancellation.Token));
        _f.Data.VerifyAll();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task Update_category_rejects_invalid_ids(int id)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _f.Manager.UpdateCategoryAsync(new(id, "Name", true)));
        _f.Data.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_category_trims_and_records_actor()
    {
        _f.Data.Setup(x => x.UpdateCategoryAsync(It.Is<TransgressionCategory>(c => c.CategoryId == 7 && c.Name == "Safety" && c.UpdatedBy == "actor" && !c.IsActive), default)).Returns(Task.CompletedTask);
        await _f.Manager.UpdateCategoryAsync(new(7, " Safety ", false));
        _f.Data.VerifyAll();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task Question_lists_require_valid_category(int id)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _f.Manager.GetAvailableQuestionsAsync(id));
        await Assert.ThrowsAsync<ArgumentException>(() => _f.Manager.GetLinkedQuestionsAsync(id));
        _f.Data.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0, 1, 0, typeof(ArgumentException))]
    [InlineData(1, 0, 0, typeof(ArgumentException))]
    [InlineData(1, 1, -1, typeof(InvalidOperationException))]
    public async Task Question_links_validate_ids_and_order(int category, int question, int order, Type exception)
    {
        Assert.IsType(exception, await Record.ExceptionAsync(() => _f.Manager.SetCategoryQuestionLinkAsync(new() { CategoryId = category, QuestionId = question, SortOrder = order })));
        _f.Data.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Question_link_saves_trimmed_config_and_actor()
    {
        _f.Data.Setup(x => x.SetCategoryQuestionLinkAsync(It.Is<CategoryQuestion>(q => q.CategoryId == 1 && q.QuestionId == 2 && q.ConfigJson == "{}" && q.SortOrder == 3), "actor")).Returns(Task.CompletedTask);
        await _f.Manager.SetCategoryQuestionLinkAsync(new() { CategoryId = 1, QuestionId = 2, SortOrder = 3, ConfigJson = " {} " });
        _f.Data.VerifyAll();
    }

    [Theory]
    [InlineData("", 1, "Please select an employee.")]
    [InlineData("employee", 0, "Please select at least one category.")]
    public async Task Issue_requires_employee_and_categories(string employee, int category, string error)
    {
        var result = await _f.Manager.SaveIssueStepAsync(new() { EmployeeId = employee, CategoryId = category });
        Assert.False(result.Success);
        Assert.Equal(error, result.Message);
        _f.Data.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Issue_requires_current_actor()
    {
        _f.Me.SetupGet(x => x.ObjectId).Returns((string?)null);
        var result = await _f.Manager.SaveIssueStepAsync(new() { EmployeeId = "employee", CategoryId = 1 });
        Assert.False(result.Success);
        Assert.Equal("Current user could not be identified.", result.Message);
    }

    [Fact]
    public async Task New_issue_deduplicates_categories_and_uses_configured_draft_status()
    {
        _f.Data.Setup(x => x.GetIssueStatusByGroupAsync(IssueStatusGroup.Draft)).ReturnsAsync(new LookupIssueStatus { IssueStatusId = 99, IssueStatusName = "Draft" });
        _f.Data.Setup(x => x.CreateWarningAsync(It.Is<Warning>(w => w.EmployeeId == "employee" && w.CreatedBy == "actor" && w.IssueStatusId == 99), It.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { 2, 3 })))).ReturnsAsync(42);
        var result = await _f.Manager.SaveIssueStepAsync(new() { EmployeeId = " employee ", CategoryIds = [2, 2, -1, 0, 3] });
        Assert.True(result.Success);
        Assert.Equal(42, result.WarningId);
        _f.Data.VerifyAll();
    }

    [Fact]
    public async Task Missing_draft_configuration_is_reported()
    {
        _f.Data.Setup(x => x.GetIssueStatusByGroupAsync(IssueStatusGroup.Draft)).ReturnsAsync((LookupIssueStatus?)null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.Manager.SaveIssueStepAsync(new() { EmployeeId = "employee", CategoryId = 1 }));
    }

    [Fact]
    public async Task Updating_missing_issue_returns_failure()
    {
        _f.Data.Setup(x => x.GetWarningByIdAsync(42)).ReturnsAsync((Warning?)null);
        var result = await _f.Manager.SaveIssueStepAsync(new() { WarningId = 42, EmployeeId = "employee", CategoryId = 1 });
        Assert.False(result.Success);
        Assert.Equal("The warning could not be found.", result.Message);
    }

    [Fact]
    public async Task Existing_issue_updates_employee_categories_and_actor()
    {
        _f.Data.Setup(x => x.GetWarningByIdAsync(42)).ReturnsAsync(new Warning { WarningId = 42 });
        _f.Data.Setup(x => x.UpdateWarningIssueAsync(It.Is<Warning>(w => w.EmployeeId == "employee" && w.CategoryId == 1 && w.LastStatusChangedBy == "actor"), It.IsAny<IReadOnlyCollection<int>>())).ReturnsAsync(42);
        Assert.True((await _f.Manager.SaveIssueStepAsync(new() { WarningId = 42, EmployeeId = " employee ", CategoryId = 1 })).Success);
        _f.Data.VerifyAll();
    }

    [Theory]
    [InlineData(0, true, false, "text", "Please select a discussion subtype.")]
    [InlineData(1, false, false, "text", "Please select at least one date.")]
    [InlineData(1, true, true, "text", "Discussion dates cannot be in the future.")]
    [InlineData(1, true, false, " ", "Please enter a description.")]
    public async Task Discussion_validation(int subtype, bool hasDate, bool future, string description, string expected)
    {
        var dto = new CreateAbsenceDiscussionDto { IssueSubTypeId = subtype, Description = description,
            Dates = hasDate ? [DateOnly.FromDateTime(DateTime.UtcNow.AddHours(2)).AddDays(future ? 1 : -1)] : [] };
        var result = await _f.Manager.CreateAbsenceDiscussionAsync(dto);
        Assert.False(result.Success);
        Assert.Equal(expected, result.Message);
        _f.Data.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Save_answer_preserves_text_and_json()
    {
        _f.Data.Setup(x => x.UpsertWarningAnswerAsync(42, 3, "text", "{}")).Returns(Task.CompletedTask);
        await _f.Manager.SaveAnswerAsync(new() { WarningId = 42, QuestionId = 3, AnswerText = "text", AnswerJson = "{}" });
        _f.Data.VerifyAll();
    }

    [Fact]
    public async Task Ordinary_notes_record_actor_without_sending_email()
    {
        _f.Data.Setup(x => x.AddWarningNoteAsync(42, null, 1, "note", "actor")).Returns(Task.CompletedTask);
        await _f.Manager.AddWarningNoteAsync(42, null, 1, "note");
        _f.Data.VerifyAll();
        _f.Graph.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Ordinary_evidence_preserves_metadata()
    {
        _f.Data.Setup(x => x.SaveEvidenceAsync(42, "file.pdf", "application/pdf", 100, "actor", "note", 1)).ReturnsAsync(7);
        Assert.Equal(7, await _f.Manager.SaveEvidenceAsync(42, "file.pdf", "application/pdf", 100, "note", 1, false));
        _f.Data.VerifyAll();
    }

    [Fact]
    public async Task Hide_toggle_returns_data_result()
    {
        _f.Data.Setup(x => x.SetHideFromTeamLeadAsync(42, true)).ReturnsAsync(true);
        Assert.True(await _f.Manager.SetHideFromTeamLeadAsync(42, true, "actor"));
    }

    [Fact]
    public async Task Missing_employee_dashboard_denies_access_without_external_calls()
    {
        Assert.False((await _f.Manager.GetEmployeeDashboardAsync(" ")).HasAccess);
        _f.Data.VerifyNoOtherCalls();
        _f.Graph.VerifyNoOtherCalls();
    }
}
