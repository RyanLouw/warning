using WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;
using WarningSystems.Core.Models.Enum;

namespace WarningSystems.Core.DataAccess.WarningSystemDataAccess;

public interface IWarningSystemDataAccess
{
    public Task<List<Warning>> GetAllWarningsAsync();

    public Task<List<Question>> GetAllQuestionsAsync();

    public Task<List<TransgressionCategory>> GetAllCategoriesAsync();

    public Task<int> CreateQuestionAsync(Question question);

    public Task UpdateQuestionAsync(Question updatedQuestion);

    public Task<int> CreateCategoryAsync(TransgressionCategory category,
        CancellationToken cancellationToken = default);

    public Task UpdateCategoryAsync(TransgressionCategory updatedCategory,
        CancellationToken cancellationToken = default);

    public Task<List<Question>> GetAvailableQuestionsAsync(int categoryId);

    public Task<List<CategoryQuestion>> GetLinkedQuestionsAsync(int categoryId);

    public Task SetCategoryQuestionLinkAsync(CategoryQuestion requestedLink, string changedBy);

    public Task<Warning?> GetWarningByIdAsync(long warningId);

    public Task<List<WarningCategory>> GetWarningCategoriesByWarningIdAsync(long warningId);

    public Task<List<CategoryQuestion>> GetActiveCategoryQuestionsAsync(IReadOnlyCollection<int> categoryIds);

    public Task<List<WarningAnswer>> GetWarningAnswersByWarningIdAsync(long warningId);

    public Task<List<WarningNote>> GetWarningNotesByWarningIdAsync(long warningId);

    public Task<List<WarningEvidence>> GetWarningEvidenceByWarningIdAsync(long warningId);

    public Task<List<Warning>> GetWarningsByEmployeeIdAsync(string employeeId);

    public Task<List<WarningAnswer>> GetWarningAnswersByWarningIdsAsync(IReadOnlyCollection<long> warningIds);

    public Task<List<WarningNote>> GetWarningNotesByWarningIdsAsync(IReadOnlyCollection<long> warningIds);

    public Task<long> CreateWarningAsync(Warning warning, IReadOnlyCollection<int> categoryIds);

    public Task<bool> QuestionExistsAsync(string questionText, string controlType);

    public Task UpsertWarningAnswerAsync(long warningId, int questionId, string? answerText, string? answerJson);

    public Task<List<NoteTypeLookup>> GetActiveNoteTypesAsync();

    public Task UpdateWarningDueDateAsync(long warningId, DateOnly? dueDate, string user);

    public Task<long?> SaveEvidenceAsync(long warningId, string? fileName, string? mediaType, long? fileSizeBytes,
        string user, string? notes, int? noteTypeId);

    public Task AddWarningNoteAsync(long warningId, long? evidenceId, int noteTypeId, string noteText, string user);

    public Task UpdateWarningDueDateAsync(Warning updatedWarning, string reason);

    public Task SaveTeamLeadEvidenceAsync(long warningId, IReadOnlyCollection<WarningEvidence> evidence,
        WarningNote? note);

    public Task<long> UpdateWarningIssueAsync(Warning updatedWarning, IReadOnlyCollection<int> categoryIds);
    Task<bool?> SetHideFromTeamLeadAsync(long warningId, bool hideFromTeamLead);
    Task<List<Warning>> GetAllWarningsForTaskListAsync();
    Task<List<Warning>> GetAllWarningsForReportingAsync();
    Task<List<LookupIssueType>> GetActiveIssueTypesAsync();
    Task<LookupIssueStatus?> GetIssueStatusByGroupAsync(IssueStatusGroup group);
    Task UpdateWarningDecisionAsync(long warningId, LookupIssueType issueType,
        LookupIssueSubType? issueSubType, string user);
    Task AdvanceWarningStatusAsync(long warningId, IssueStatusGroup requiredGroup, string user);
}
