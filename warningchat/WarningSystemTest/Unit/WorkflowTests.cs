using FakeItEasy;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;
using WarningSystems.Core.Models.Enum;

namespace WarningSystemTest.Unit;

[Trait("Category", "Unit")]
public class WorkflowTests
{
    private readonly ManagerFixture _f = new();

    [Theory]
    [InlineData(IssueStatusGroup.Draft)]
    [InlineData(IssueStatusGroup.LegalReview)]
    [InlineData(IssueStatusGroup.LegalDecision)]
    [InlineData(IssueStatusGroup.EmployeeAction)]
    [InlineData(IssueStatusGroup.Closed)]
    public async Task Validation_rejects_non_issued_issues(IssueStatusGroup group)
    {
        WarningIn(group);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.Manager.ValidateWarningAsync(42));
        A.CallTo(() => _f.Data.AdvanceWarningStatusAsync(A<long>._, A<IssueStatusGroup>._, A<string>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task Issued_issue_can_be_validated()
    {
        WarningIn(IssueStatusGroup.LegalValidation);
        A.CallTo(() => _f.Data.AdvanceWarningStatusAsync(42, IssueStatusGroup.LegalValidation, "actor")).Returns(Task.CompletedTask);
        await _f.Manager.ValidateWarningAsync(42);
        A.CallTo(() => _f.Data.GetWarningByIdAsync(42)).MustHaveHappened();
        A.CallTo(() => _f.Data.AdvanceWarningStatusAsync(42, IssueStatusGroup.LegalValidation, "actor")).MustHaveHappened();
    }

    [Fact]
    public async Task Missing_issue_cannot_be_validated()
    {
        A.CallTo(() => _f.Data.GetWarningByIdAsync(42)).Returns((Warning?)null);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _f.Manager.ValidateWarningAsync(42));
    }

    [Theory]
    [InlineData(IssueStatusGroup.LegalReview)]
    [InlineData(IssueStatusGroup.EmployeeAction)]
    [InlineData(IssueStatusGroup.Closed)]
    public async Task Only_drafts_can_be_submitted(IssueStatusGroup group)
    {
        WarningIn(group);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.Manager.UpdateWarningStatusAsync(42, null, "New"));
        A.CallTo(() => _f.Data.UpdateWarningDueDateAsync(A<long>._, A<DateOnly?>._, A<string>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task Draft_submission_advances_status_and_updates_due_date()
    {
        WarningIn(IssueStatusGroup.Draft);
        var date = new DateOnly(2027, 1, 4);
        A.CallTo(() => _f.Data.AdvanceWarningStatusAsync(42, IssueStatusGroup.Draft, "actor")).Returns(Task.CompletedTask);
        A.CallTo(() => _f.Data.UpdateWarningDueDateAsync(42, date, "actor")).Returns(Task.CompletedTask);
        await _f.Manager.UpdateWarningStatusAsync(42, date, "New");
        A.CallTo(() => _f.Data.GetWarningByIdAsync(42)).MustHaveHappened();
        A.CallTo(() => _f.Data.AdvanceWarningStatusAsync(42, IssueStatusGroup.Draft, "actor")).MustHaveHappened();
        A.CallTo(() => _f.Data.UpdateWarningDueDateAsync(42, date, "actor")).MustHaveHappened();
    }

    [Fact]
    public async Task Due_date_only_change_does_not_submit()
    {
        A.CallTo(() => _f.Data.UpdateWarningDueDateAsync(42, null, "actor")).Returns(Task.CompletedTask);
        await _f.Manager.UpdateWarningStatusAsync(42, null, null);
        A.CallTo(() => _f.Data.UpdateWarningDueDateAsync(42, null, "actor")).MustHaveHappened();
    }

    [Theory]
    [InlineData(IssueStatusGroup.Draft)]
    [InlineData(IssueStatusGroup.LegalReview)]
    [InlineData(IssueStatusGroup.Closed)]
    public async Task Decision_requires_ready_status(IssueStatusGroup group)
    {
        WarningIn(group);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.Manager.ApplyLegalDecisionAsync(42, 2, null));
    }

    [Theory]
    [InlineData(IssueStatusGroup.LegalDecision)]
    [InlineData(IssueStatusGroup.AddedMoreInformation)]
    public async Task Decision_is_allowed_when_ready(IssueStatusGroup group)
    {
        WarningIn(group);
        var type = new LookupIssueType { IssueTypeId = 2, SubTypeSelectionMode = SubTypeSelectionMode.Required, IssueSubTypes = [new() { IssueSubTypeId = 3 }] };
        A.CallTo(() => _f.Data.GetActiveIssueTypesAsync()).Returns([type]);
        A.CallTo(() => _f.Data.UpdateWarningDecisionAsync(42, type, type.IssueSubTypes.Single(), "actor")).Returns(Task.CompletedTask);
        await _f.Manager.ApplyLegalDecisionAsync(42, 2, 3);
        A.CallTo(() => _f.Data.GetWarningByIdAsync(42)).MustHaveHappened();
        A.CallTo(() => _f.Data.GetActiveIssueTypesAsync()).MustHaveHappened();
        A.CallTo(() => _f.Data.UpdateWarningDecisionAsync(42, type, type.IssueSubTypes.Single(), "actor")).MustHaveHappened();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(999)]
    public async Task Required_subtype_must_belong_to_selected_type(int? subtype)
    {
        WarningIn(IssueStatusGroup.LegalDecision);
        A.CallTo(() => _f.Data.GetActiveIssueTypesAsync()).Returns([new() { IssueTypeId = 2, SubTypeSelectionMode = SubTypeSelectionMode.Required }]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.Manager.ApplyLegalDecisionAsync(42, 2, subtype));
    }

    [Fact]
    public async Task Unknown_issue_type_is_rejected()
    {
        WarningIn(IssueStatusGroup.LegalDecision);
        A.CallTo(() => _f.Data.GetActiveIssueTypesAsync()).Returns([]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _f.Manager.ApplyLegalDecisionAsync(42, 999, null));
    }

    [Fact]
    public async Task Types_without_subtypes_ignore_subtype_input()
    {
        WarningIn(IssueStatusGroup.LegalDecision);
        var type = new LookupIssueType { IssueTypeId = 1, SubTypeSelectionMode = SubTypeSelectionMode.None };
        A.CallTo(() => _f.Data.GetActiveIssueTypesAsync()).Returns([type]);
        A.CallTo(() => _f.Data.UpdateWarningDecisionAsync(42, type, null, "actor")).Returns(Task.CompletedTask);
        await _f.Manager.ApplyLegalDecisionAsync(42, 1, 99);
        A.CallTo(() => _f.Data.GetWarningByIdAsync(42)).MustHaveHappened();
        A.CallTo(() => _f.Data.GetActiveIssueTypesAsync()).MustHaveHappened();
        A.CallTo(() => _f.Data.UpdateWarningDecisionAsync(42, type, null, "actor")).MustHaveHappened();
    }

    [Fact]
    public async Task Mark_in_progress_requires_nonzero_id() =>
        await Assert.ThrowsAsync<ArgumentException>(() => _f.Manager.MarkWarningInProgressAsync(0));

    [Fact]
    public async Task Mark_in_progress_advances_legal_review()
    {
        A.CallTo(() => _f.Data.AdvanceWarningStatusAsync(42, IssueStatusGroup.LegalReview, "actor")).Returns(Task.CompletedTask);
        await _f.Manager.MarkWarningInProgressAsync(42);
        A.CallTo(() => _f.Data.AdvanceWarningStatusAsync(42, IssueStatusGroup.LegalReview, "actor")).MustHaveHappened();
    }

    private void WarningIn(IssueStatusGroup group) =>
        A.CallTo(() => _f.Data.GetWarningByIdAsync(42)).Returns(new Warning { WarningId = 42, IssueStatus = new() { IssueStatusGroup = group } });
}
