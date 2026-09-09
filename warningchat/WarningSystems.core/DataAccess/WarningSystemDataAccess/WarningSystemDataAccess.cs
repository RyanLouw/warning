using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WarningSystems.Core.DataAccess.GraphDataAccess;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;
using WarningSystems.Core.Models.Enum;

namespace WarningSystems.Core.DataAccess.WarningSystemDataAccess;

public class WarningSystemDataAccess : IWarningSystemDataAccess
{
    private readonly WarningSystemDbContext _context;
    private readonly IGraphUserDataAccess _graphUserDataAccess;

    public WarningSystemDataAccess(WarningSystemDbContext context, IGraphUserDataAccess graphUserDataAccess)
    {
        _context = context;
        _graphUserDataAccess = graphUserDataAccess;
    }

    private static DateTime NowSast =>
    DateTime.UtcNow.AddHours(2);

    public async Task<List<Warning>> GetAllWarningsAsync()
    {
        return await _context.Warnings
            .AsNoTracking()
            .Include(warning => warning.IssueStatus)
            .Include(warning => warning.IssueType)
            .Include(warning => warning.IssueSubType)
            .Where(warning => !warning.IsDeleted)
            .Include(warning => warning.WarningCategories)
                .ThenInclude(link => link.Category)
            .Include(warning => warning.Notes)
                .ThenInclude(note => note.NoteType)
            .OrderByDescending(warning => warning.CreatedOn)
            .ToListAsync();
    }

    public async Task<List<Warning>>
        GetAllWarningsForTaskListAsync()
    {
        return await _context.Warnings
            .AsNoTracking()
            .Include(warning => warning.IssueStatus)
            .Include(warning => warning.IssueType)
            .Include(warning => warning.IssueSubType)
            .Where(warning => !warning.IsDeleted)
            .Include(warning =>
                warning.WarningCategories)
                .ThenInclude(link =>
                    link.Category)
            .Include(warning =>
                warning.Notes)
                .ThenInclude(note =>
                    note.NoteType)
            .OrderByDescending(warning =>
                warning.CreatedOn)
            .ToListAsync();
    }

    public async Task<List<Warning>>
        GetAllWarningsForReportingAsync()
    {
        return await _context.Warnings
            .AsNoTracking()
            .Include(warning => warning.IssueStatus)
            .Include(warning => warning.IssueType)
            .Include(warning => warning.IssueSubType)
            .Where(warning =>
                !warning.IsDeleted)
            .Include(warning =>
                warning.WarningCategories)
                .ThenInclude(link =>
                    link.Category)
            .Include(warning =>
                warning.Notes)
                .ThenInclude(note =>
                    note.NoteType)
            .OrderByDescending(warning =>
                warning.CreatedOn)
            .ToListAsync();
    }

    public async Task<List<Question>> GetAllQuestionsAsync()
    {
        return await _context.Questions
            .AsNoTracking()
            .OrderBy(q => q.QuestionText)
            .ToListAsync();
    }

    public async Task<List<TransgressionCategory>> GetAllCategoriesAsync()
    {
        return await _context.TransgressionCategories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<bool> QuestionExistsAsync(string questionText, string controlType)
    {
        return await _context.Questions
            .AsNoTracking()
            .AnyAsync(q =>
                q.QuestionText == questionText &&
                q.ControlType == controlType);
    }

    public async Task<int> CreateQuestionAsync(Question question)
    {
        _context.Questions.Add(question);
        await _context.SaveChangesAsync();

        return question.QuestionId;
    }

    public async Task UpdateQuestionAsync(Question updatedQuestion)
    {
        var existingQuestion = await _context.Questions
            .FirstOrDefaultAsync(q => q.QuestionId == updatedQuestion.QuestionId);

        if (existingQuestion == null)
            throw new KeyNotFoundException("Question not found.");

        existingQuestion.QuestionText = updatedQuestion.QuestionText;
        existingQuestion.ControlType = updatedQuestion.ControlType;
        existingQuestion.DefaultConfigJson = updatedQuestion.DefaultConfigJson;
        existingQuestion.IsActive = updatedQuestion.IsActive;
        existingQuestion.UpdatedBy = updatedQuestion.UpdatedBy;
        existingQuestion.UpdatedOn = updatedQuestion.UpdatedOn;


        await _context.SaveChangesAsync();
    }

    public async Task<int> CreateCategoryAsync(TransgressionCategory category, CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _context.TransgressionCategories.Add(category);

            await _context.SaveChangesAsync(cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 EXEC ws.sp_EnsureDefaultCategoryQuestions
                     @CreatedBy = {category.CreatedBy}
                 """,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return category.CategoryId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task UpdateCategoryAsync(TransgressionCategory updatedCategory, CancellationToken cancellationToken = default)
    {
        var existingCategory = await _context.TransgressionCategories
            .FirstOrDefaultAsync(
                c => c.CategoryId == updatedCategory.CategoryId,
                cancellationToken);

        if (existingCategory == null)
            throw new KeyNotFoundException("Category not found.");



        existingCategory.Name = updatedCategory.Name;
        existingCategory.IsActive = updatedCategory.IsActive;
        existingCategory.UpdatedBy = updatedCategory.UpdatedBy;
        existingCategory.UpdatedOn = updatedCategory.UpdatedOn;


        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Question>> GetAvailableQuestionsAsync(int categoryId)
    {
        return await _context.Questions
            .AsNoTracking()
            .Where(q =>
                q.IsActive &&
                !_context.CategoryQuestions.Any(cq =>
                    cq.CategoryId == categoryId &&
                    cq.QuestionId == q.QuestionId &&
                    cq.IsActive))
            .OrderBy(q => q.QuestionText)
            .ToListAsync();
    }

    public async Task<List<CategoryQuestion>> GetLinkedQuestionsAsync(int categoryId)
    {
        return await _context.CategoryQuestions
            .AsNoTracking()
            .Include(cq => cq.Question)
            .Where(cq =>
                cq.CategoryId == categoryId &&
                cq.IsActive)
            .OrderBy(cq => cq.SortOrder)
            .ThenBy(cq => cq.Question!.QuestionText)
            .ToListAsync();
    }

    public async Task SetCategoryQuestionLinkAsync(CategoryQuestion requestedLink, string changedBy)
    {
        var existingLink = await _context.CategoryQuestions
            .FirstOrDefaultAsync(x =>
                x.CategoryId == requestedLink.CategoryId &&
                x.QuestionId == requestedLink.QuestionId);

        if (existingLink == null)
        {
            if (!requestedLink.IsActive)
                throw new KeyNotFoundException("Link not found.");

            var newLink = new CategoryQuestion
            {
                CategoryId = requestedLink.CategoryId,
                QuestionId = requestedLink.QuestionId,
                IsRequired = requestedLink.IsRequired,
                SortOrder = requestedLink.SortOrder,
                ConfigJson = requestedLink.ConfigJson,
                IsActive = true,
                CreatedBy = changedBy,
                CreatedOn = NowSast
            };

            _context.CategoryQuestions.Add(newLink);


            var affected = await _context.SaveChangesAsync();

            if (affected == 0)
            {
                throw new InvalidOperationException(
                    $"No database changes were saved for CategoryId={newLink.CategoryId}, QuestionId={newLink.QuestionId}.");
            }

            return;
        }


        existingLink.IsActive = requestedLink.IsActive;
        existingLink.IsRequired = requestedLink.IsRequired;
        existingLink.SortOrder = requestedLink.SortOrder;
        existingLink.ConfigJson = requestedLink.ConfigJson;
        existingLink.UpdatedBy = changedBy;
        existingLink.UpdatedOn = NowSast;





        await _context.SaveChangesAsync();
    }

    public async Task<Warning?> GetWarningByIdAsync(long warningId)
    {
        return await _context.Warnings
            .AsNoTracking()
            .AsSplitQuery()
            .Include(warning => warning.IssueStatus)
            .Include(warning => warning.IssueType)
            .Include(warning => warning.IssueSubType)
            .Include(warning => warning.Category)
            .Include(warning => warning.Evidence)
                .ThenInclude(evidence => evidence.Notes)
            .Include(warning => warning.Notes)
            .SingleOrDefaultAsync(warning =>
                warning.WarningId == warningId);
    }

    public async Task<List<WarningCategory>> GetWarningCategoriesByWarningIdAsync(long warningId)
    {
        return await _context.WarningCategories
            .AsNoTracking()
            .Include(wc => wc.Category)
            .Where(wc => wc.WarningId == warningId)
            .OrderBy(wc => wc.Category!.Name)
            .ToListAsync();
    }

    public async Task<List<CategoryQuestion>> GetActiveCategoryQuestionsAsync(IReadOnlyCollection<int> categoryIds)
    {
        if (categoryIds.Count == 0)
            return new List<CategoryQuestion>();

        return await _context.CategoryQuestions
            .AsNoTracking()
            .Include(cq => cq.Question)
            .Where(cq =>
                categoryIds.Contains(cq.CategoryId) &&
                cq.IsActive &&
                cq.Question != null &&
                cq.Question.IsActive)
            .OrderBy(cq => cq.SortOrder)
            .ThenBy(cq => cq.Question!.QuestionText)
            .ToListAsync();
    }

    public async Task<List<WarningAnswer>> GetWarningAnswersByWarningIdAsync(long warningId)
    {
        return await _context.WarningAnswers
            .AsNoTracking()
            .Where(a => a.WarningId == warningId)
            .ToListAsync();
    }

    public async Task<List<WarningNote>> GetWarningNotesByWarningIdAsync(long warningId)
    {
        return await _context.WarningNotes
            .AsNoTracking()
            .Include(n => n.NoteType)
            .Where(n => n.WarningId == warningId)
            .OrderByDescending(n => n.CreatedOn)
            .ToListAsync();
    }

    public async Task<List<WarningEvidence>> GetWarningEvidenceByWarningIdAsync(long warningId)
    {
        return await _context.WarningEvidence
            .AsNoTracking()
            .Where(e => e.WarningId == warningId)
            .OrderByDescending(e => e.UploadedOn)
            .ToListAsync();
    }

    public async Task<List<Warning>> GetWarningsByEmployeeIdAsync(string employeeId)
    {
        return await _context.Warnings
            .AsNoTracking()
            .Include(w => w.Category)
            .Where(w => w.EmployeeId == employeeId)
            .OrderByDescending(w =>
                w.LastStatusChangedOn ?? w.CreatedOn)
            .ToListAsync();
    }



    public async Task<bool?> SetHideFromTeamLeadAsync(
     long warningId,
     bool hideFromTeamLead)
    {
        var warning = await _context.Warnings
            .FirstOrDefaultAsync(x =>
                x.WarningId == warningId &&
                !x.IsDeleted);

        if (warning == null)
        {
            return null;
        }

        warning.HideFromTeamLead = hideFromTeamLead;

        await _context.SaveChangesAsync();

        return warning.HideFromTeamLead;
    }
    public async Task<List<WarningAnswer>> GetWarningAnswersByWarningIdsAsync(IReadOnlyCollection<long> warningIds)
    {
        if (warningIds.Count == 0)
            return new List<WarningAnswer>();

        return await _context.WarningAnswers
            .AsNoTracking()
            .Include(a => a.Warning)
            .Where(a =>
                a.Warning != null &&
                warningIds.Contains(a.Warning.WarningId))
            .ToListAsync();
    }

    public async Task<List<WarningNote>> GetWarningNotesByWarningIdsAsync(IReadOnlyCollection<long> warningIds)
    {
        if (warningIds.Count == 0)
            return new List<WarningNote>();

        return await _context.WarningNotes
            .AsNoTracking()
            .Where(n => warningIds.Contains(n.WarningId))
            .OrderByDescending(n => n.CreatedOn)
            .ToListAsync();
    }

    public async Task<long> CreateWarningAsync(Warning warning, IReadOnlyCollection<int> categoryIds)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            _context.Warnings.Add(warning);

            await _context.SaveChangesAsync();

            await ReplaceWarningCategoriesAsync(
                warning.WarningId,
                categoryIds,
                warning.CreatedBy);

            await transaction.CommitAsync();

            return warning.WarningId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<long> UpdateWarningIssueAsync(Warning updatedWarning, IReadOnlyCollection<int> categoryIds)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var existingWarning = await _context.Warnings
                .SingleOrDefaultAsync(w =>
                    w.WarningId == updatedWarning.WarningId);

            if (existingWarning == null)
            {
                throw new KeyNotFoundException(
                    $"Warning with ID {updatedWarning.WarningId} not found.");
            }

            existingWarning.EmployeeId =
                updatedWarning.EmployeeId;

            existingWarning.CategoryId =
                updatedWarning.CategoryId;

            existingWarning.LastStatusChangedOn =
                updatedWarning.LastStatusChangedOn;

            existingWarning.LastStatusChangedBy =
                updatedWarning.LastStatusChangedBy;

            await ReplaceWarningCategoriesAsync(
                existingWarning.WarningId,
                categoryIds,
                updatedWarning.LastStatusChangedBy ?? "system");

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return existingWarning.WarningId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task ReplaceWarningCategoriesAsync(
        long warningId,
        IReadOnlyCollection<int> categoryIds,
        string changedBy)
    {
        var existingLinks = await _context.WarningCategories
            .Where(x => x.WarningId == warningId)
            .ToListAsync();

        if (existingLinks.Count > 0)
        {
            _context.WarningCategories.RemoveRange(existingLinks);
        }

        var now = NowSast;

        var newLinks = categoryIds
            .Where(categoryId => categoryId > 0)
            .Distinct()
            .Select(categoryId => new WarningCategory
            {
                WarningId = warningId,
                CategoryId = categoryId,
                CreatedBy = changedBy,
                CreatedOn = now
            })
            .ToList();

        await _context.WarningCategories.AddRangeAsync(newLinks);
    }

    public async Task UpsertWarningAnswerAsync(long warningId, int questionId, string? answerText, string? answerJson)
    {
        var existing = await _context.WarningAnswers
            .FirstOrDefaultAsync(a => a.WarningId == warningId && a.QuestionId == questionId);

        if (existing is null)
        {
            _context.WarningAnswers.Add(new WarningAnswer
            {
                WarningId = warningId,
                QuestionId = questionId,
                AnswerText = answerText,
                AnswerJson = answerJson,
                CreatedOn = NowSast
            });
        }
        else
        {
            existing.AnswerText = answerText;
            existing.AnswerJson = answerJson;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<NoteTypeLookup>> GetActiveNoteTypesAsync()
    {
        return await _context.NoteTypeLookups
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                !x.IsSystemOnly)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task UpdateWarningDueDateAsync(long warningId, DateOnly? dueDate, string user)
    {
        var w = await _context.Warnings
            .FirstOrDefaultAsync(x => x.WarningId == warningId);

        if (w == null) return;
        if (dueDate.HasValue)
        {
            w.LegalExpiryDate = dueDate.Value;
        }
        w.LastStatusChangedOn = NowSast;
        w.LastStatusChangedBy = user;

        await _context.SaveChangesAsync();
    }

    public async Task<List<LookupIssueType>> GetActiveIssueTypesAsync()
    {
        return await _context.LookupIssueTypes
            .AsNoTracking()
            .Where(type => type.IsActive)
            .Include(type => type.ResultIssueStatus)
            .Include(type => type.IssueSubTypes.Where(subType => subType.IsActive))
            .OrderBy(type => type.IssueTypeId)
            .ToListAsync();
    }

    public async Task<LookupIssueStatus?> GetIssueStatusByGroupAsync(IssueStatusGroup group)
    {
        return await _context.LookupIssueStatuses
            .AsNoTracking()
            .OrderBy(status => status.IssueStatusId)
            .FirstOrDefaultAsync(status => status.IsActive && status.IssueStatusGroup == group);
    }

    public async Task UpdateWarningDecisionAsync(
        long warningId,
        LookupIssueType issueType,
        LookupIssueSubType? issueSubType,
        string user)
    {
        var warning = await _context.Warnings
            .FirstOrDefaultAsync(x => x.WarningId == warningId && !x.IsDeleted)
            ?? throw new KeyNotFoundException($"Warning not found. WarningId={warningId}");

        warning.IssueTypeId = issueType.IssueTypeId;
        warning.IssueSubTypeId = issueSubType?.IssueSubTypeId;
        warning.IssueStatusId = issueType.ResultIssueStatusId;

        // Keep the legacy display columns synchronized during the migration period.
        warning.Type = issueType.IssueTypeName;
        warning.WarningSubtype = issueSubType?.IssueSubTypeName;
        warning.Status = issueType.ResultIssueStatus.IssueStatusName;
        warning.LastStatusChangedOn = NowSast;
        warning.LastStatusChangedBy = string.IsNullOrWhiteSpace(user) ? "system" : user.Trim();

        await _context.SaveChangesAsync();
    }

    public async Task CompleteDiscussionAsync(long warningId, string subType, string user)
    {
        var discussion = await _context.LookupIssueTypes
            .Include(type => type.ResultIssueStatus)
            .SingleAsync(type => type.IsActive && type.IssueTypeName == "Discussion");

        var warning = await _context.Warnings
            .SingleAsync(item => item.WarningId == warningId && !item.IsDeleted);

        warning.IssueTypeId = discussion.IssueTypeId;
        warning.IssueSubTypeId = null;
        warning.Type = discussion.IssueTypeName;
        warning.WarningSubtype = subType.Trim();
        warning.IssueStatusId = discussion.ResultIssueStatusId;
        warning.Status = discussion.ResultIssueStatus.IssueStatusName;
        warning.LastStatusChangedOn = NowSast;
        warning.LastStatusChangedBy = user.Trim();

        await _context.SaveChangesAsync();
    }

    public async Task AdvanceWarningStatusAsync(
        long warningId,
        IssueStatusGroup requiredGroup,
        string user)
    {
        var warning = await _context.Warnings
            .Include(x => x.IssueStatus)
                .ThenInclude(status => status!.NextIssueStatus)
            .FirstOrDefaultAsync(x => x.WarningId == warningId && !x.IsDeleted)
            ?? throw new KeyNotFoundException($"Warning not found. WarningId={warningId}");

        if (warning.IssueStatus?.IssueStatusGroup != requiredGroup)
            throw new InvalidOperationException("This action is not available for the issue's current status.");

        var nextStatus = warning.IssueStatus.NextIssueStatus;
        if (nextStatus is null || !nextStatus.IsActive)
            throw new InvalidOperationException("The next issue status has not been configured.");

        warning.IssueStatusId = nextStatus.IssueStatusId;
        warning.Status = nextStatus.IssueStatusName;
        if (requiredGroup == IssueStatusGroup.Draft && !warning.SubmittedOn.HasValue)
            warning.SubmittedOn = NowSast;
        warning.LastStatusChangedOn = NowSast;
        warning.LastStatusChangedBy = string.IsNullOrWhiteSpace(user) ? "system" : user.Trim();

        await _context.SaveChangesAsync();
    }

    public async Task<long?> SaveEvidenceAsync(long warningId, string? fileName, string? mediaType, long? fileSizeBytes, string user, string? notes, int? noteTypeId)
    {
        if (warningId is 0)
            throw new ArgumentException("Invalid warningId", nameof(warningId));

        var uploadedBy = string.IsNullOrWhiteSpace(user)
            ? "system"
            : user.Trim();

        bool hasFile = !string.IsNullOrWhiteSpace(fileName);
        bool hasNotes = !string.IsNullOrWhiteSpace(notes);

        if (!hasFile && !hasNotes)
            throw new ArgumentException("Evidence must contain a file or notes.");

        WarningEvidence? evidence = null;
        DateTime now = NowSast;


        if (hasFile)
        {
            evidence = new WarningEvidence
            {
                WarningId = warningId,
                FileName = fileName!.Trim(),
                FileType = mediaType?.Trim() ?? string.Empty,
                FileSizeBytes = fileSizeBytes ?? 0,
                UploadedBy = uploadedBy,
                UploadedOn = now,
                StorageUrl = string.Empty
            };

            _context.WarningEvidence.Add(evidence);
            await _context.SaveChangesAsync();
        }

        if (hasNotes)
        {
            var note = new WarningNote
            {
                WarningId = warningId,
                EvidenceId = evidence?.EvidenceId,
                NoteText = notes!.Trim(),
                CreatedBy = uploadedBy,
                NoteTypeId = noteTypeId,
                CreatedOn = now
            };

            _context.WarningNotes.Add(note);
            await _context.SaveChangesAsync();
        }

        return evidence?.EvidenceId;
    }

    public async Task AddWarningNoteAsync(long warningId, long? evidenceId, int noteTypeId, string noteText, string user)
    {
        if (warningId is 0)
            throw new ArgumentException("Invalid warningId.", nameof(warningId));

        if (noteTypeId is 0)
            throw new ArgumentException("Note type is required.", nameof(noteTypeId));

        if (string.IsNullOrWhiteSpace(noteText))
            throw new ArgumentException("Note text is required.", nameof(noteText));

        var createdBy = string.IsNullOrWhiteSpace(user)
            ? "system"
            : user.Trim();

        var noteTypeExists = await _context.NoteTypeLookups
            .AsNoTracking()
            .AnyAsync(x =>
                x.NoteTypeId == noteTypeId &&
                x.IsActive &&
                !x.IsSystemOnly);

        if (!noteTypeExists)
            throw new InvalidOperationException("Invalid note type.");

        if (evidenceId.HasValue)
        {
            var evidenceExists = await _context.WarningEvidence
                .AsNoTracking()
                .AnyAsync(e =>
                    e.WarningId == warningId &&
                    e.EvidenceId == evidenceId.Value);

            if (!evidenceExists)
                throw new InvalidOperationException("Selected evidence item does not belong to this warning.");
        }

        var note = new WarningNote
        {
            WarningId = warningId,
            EvidenceId = evidenceId,
            NoteTypeId = noteTypeId,
            NoteText = noteText.Trim(),
            CreatedBy = createdBy,
            CreatedOn = NowSast
        };

        _context.WarningNotes.Add(note);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateWarningDueDateAsync(Warning updatedWarning, string reason)
    {
        var existingWarning = await _context.Warnings
            .SingleOrDefaultAsync(w =>
                w.WarningId == updatedWarning.WarningId);

        if (existingWarning == null)
        {
            throw new KeyNotFoundException(
                $"Warning with ID {updatedWarning.WarningId} was not found.");
        }

        var oldDueDate = existingWarning.LegalExpiryDate;

        existingWarning.LegalExpiryDate =
            updatedWarning.LegalExpiryDate;

        existingWarning.LastStatusChangedOn =
            updatedWarning.LastStatusChangedOn;

        existingWarning.LastStatusChangedBy =
            updatedWarning.LastStatusChangedBy;



        await _context.SaveChangesAsync();
    }

    public async Task SaveTeamLeadEvidenceAsync(long warningId, IReadOnlyCollection<WarningEvidence> evidence, WarningNote? note)
    {
        var warningExists = await _context.Warnings
            .AnyAsync(w =>
                w.WarningId == warningId &&
                !w.IsDeleted);

        if (!warningExists)
        {
            throw new KeyNotFoundException(
                $"Warning with ID {warningId} was not found.");
        }

        if (evidence.Count > 0)
        {
            _context.WarningEvidence.AddRange(evidence);
        }

        if (note != null)
        {
            _context.WarningNotes.Add(note);
        }

        await _context.SaveChangesAsync();
    }
}
