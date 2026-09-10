using Microsoft.AspNetCore.Http;

using Microsoft.Extensions.Options;
using Microsoft.Graph.Models;
using WarningSystems.core.Models.DTO;
using WarningSystems.core.Services.Interface;
using WarningSystems.Core.Auth.Interface;
using WarningSystems.Core.DataAccess.AzureFileStorage;
using WarningSystems.Core.DataAccess.GraphDataAccess;
using WarningSystems.Core.DataAccess.SOPDataAccess;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;
using WarningSystems.Core.Models.Enum;
using WarningSystems.Core.Services;
using WarningSystems.Core.Services.Interface;
using WarningSystems.Core.ViewModels;
using WarningSystems.Models.DTO;
using WarningSystems.Models.Validation;

using EmailSettings = WarningSystems.Core.ViewModels.EmailSettings;

namespace WarningSystems.Core.Manager;

public class TransgressionManager : ITransgressionManager
{
    private readonly IWarningSystemDataAccess _data;
    private readonly IGraphUserDataAccess _graphUserDataAccess;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISOPDataAccess _sop;
    private readonly IAzureFileStorageDataAccess _fileStorage;
    private readonly IUserRoleService _roles;
    private readonly EmailSettings _emailSettings;
    private readonly ApplicationOptions _applicationOptions;
    private readonly IEmailTemplateRenderer _emailTemplateRenderer;

    public TransgressionManager(
        IWarningSystemDataAccess data,
        IGraphUserDataAccess graphUserDataAccess,
        ICurrentUserAccessor currentUser,
        ISOPDataAccess sop,
        IAzureFileStorageDataAccess fileStorage,
        IUserRoleService roles,
        IOptions<EmailSettings> emailOptions,
        IOptions<ApplicationOptions> applicationOptions,
        IEmailTemplateRenderer emailTemplateRenderer)
    {
        _data = data;
        _graphUserDataAccess = graphUserDataAccess;
        _currentUser = currentUser;
        _sop = sop;
        _fileStorage = fileStorage;
        _roles = roles;

        _emailSettings = emailOptions.Value;
        _applicationOptions = applicationOptions.Value;
        _emailTemplateRenderer = emailTemplateRenderer;
    }
    private static DateTime NowSast => DateTime.UtcNow.AddHours(2);
    public async Task<WarningRedirectTarget> RedirectPicker(string status)
    {
        var roles = await _roles.GetCurrentUserRolesAsync();

        status = (status ?? "").Trim().ToLowerInvariant();

        bool isUser = roles.Contains("User");
        bool isLegal = roles.Contains("Legal");

        bool isDraft = status == "draft";
        if (isUser && isDraft)
            return WarningRedirectTarget.TransgressionIndex;

        // Legal must be able to reopen every submitted issue, including a
        // Completed issue that is waiting for final validation.
        if (isLegal && !isDraft)
            return WarningRedirectTarget.LegalIndex;

        if (isUser && !isDraft)
            return WarningRedirectTarget.TransgressionTeamLeadWarning;

        return WarningRedirectTarget.HomeIndex;
    }

    public async Task<TaskListVM> BuildTaskListAsync()
    {
        var warningEntities =
            await _data.GetAllWarningsForTaskListAsync();

        var reportWarningEntities =
            await _data.GetAllWarningsForReportingAsync();

        var underMe = await UnderMe();

        var categories = (await _data.GetAllCategoriesAsync())
            .Select(category => new CategoryVM(category))
            .ToList();

        var discussionSubTypes = (await _data.GetActiveIssueTypesAsync())
            .Where(type => type.IssueTypeId == (int)LookupIssueTypeEnum.Discussion)
            .SelectMany(type => type.IssueSubTypes)
            .OrderBy(subType => subType.IssueSubTypeId)
            .Select(subType => new IssueSubTypeLookupVm
            {
                IssueSubTypeId = subType.IssueSubTypeId,
                IssueSubTypeName = subType.IssueSubTypeName
            })
            .ToList();

        var me = await _graphUserDataAccess.GetMeAsync();

        var roles = await _roles.GetCurrentUserRolesAsync();
        var roleFlags = GetRoleFlags(roles);

        var allRows = MapWarningRows(warningEntities);

        var context = new FilterContext(
            allRows,
            underMe,
            roleFlags.IsLegal,
            roleFlags.IsChairperson,
            roleFlags.IsUser);

        var rows = FilterRowsForUser(context);

        await PopulateDisplayNamesAsync(rows);

        var reportRows = MapWarningRows(reportWarningEntities);

        var reportContext = new FilterContext(
            reportRows,
            underMe,
            roleFlags.IsLegal,
            roleFlags.IsChairperson,
            roleFlags.IsUser);

        reportRows = FilterRowsForUser(reportContext);

        await PopulateDisplayNamesAsync(reportRows);

        var vm = new TaskListVM(rows, underMe)
        {
            Category = categories,
            CurrentUserRoles = roles,
            CurrentUserId = me?.Id ?? string.Empty,
            ReportRows = reportRows,
            DiscussionSubTypes = discussionSubTypes
        };

        ApplyStatusAndDueCounts(vm, rows);

        vm.EmployeeRows = await BuildEmployeeRowsAsync(
            rows,
            underMe,
            me?.DisplayName ?? "Me");

        var employeeLookup = vm.EmployeeRows
            .Where(employee =>
                !string.IsNullOrWhiteSpace(employee.EmployeeId))
            .GroupBy(
                employee => employee.EmployeeId,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.OrdinalIgnoreCase);

        foreach (var row in vm.ReportRows)
        {
            if (string.IsNullOrWhiteSpace(row.EmployeeId))
            {
                continue;
            }

            if (!employeeLookup.TryGetValue(
                    row.EmployeeId,
                    out var employee))
            {
                continue;
            }

            row.Department =
                employee.Department ?? string.Empty;

            row.Manager =
                employee.Manager ?? string.Empty;

            row.TeamLeader =
                employee.TeamLeader ?? string.Empty;
        }

        return vm;
    }

    private static List<TransgretionsVM> MapWarningRows(
    IEnumerable<Warning> warnings)
    {
        return warnings
            .Select(warning =>
            {
                var latestNote = warning.Notes
                    .OrderByDescending(note => note.CreatedOn)
                    .ThenByDescending(note => note.NoteId)
                    .FirstOrDefault();

                var categoryIds = warning.WarningCategories
                    .Select(link => link.CategoryId)
                    .Distinct()
                    .ToList();

                var categoryNames = warning.WarningCategories
                    .Where(link => link.Category != null)
                    .Select(link => link.Category!.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var issuedToEmployee = warning.Notes.Any(note =>
                    string.Equals(
                        note.NoteType?.Name,
                        "Issued to Employee",
                        StringComparison.OrdinalIgnoreCase));

                return new TransgretionsVM
                {
                    WarningId = warning.WarningId,

                    EmployeeId = warning.EmployeeId,
                    EmployeeDisplayName = warning.EmployeeId,

                    CreatedBy = warning.CreatedBy,
                    CreatedByDisplayName = warning.CreatedBy,

                    CreatedOn = warning.CreatedOn,
                    Status = warning.Status,

                    Type = warning.Type,
                    WarningSubtype = warning.WarningSubtype,

                    CategoryId = warning.CategoryId,
                    CategoryIds = categoryIds,
                    CategoryNames = categoryNames,

                    IssueType = string.Join(
                        ", ",
                        categoryNames),
                    IssueTypeName = warning.IssueType?.IssueTypeName ?? string.Empty,
                    IssueSubTypeName = warning.IssueSubType?.IssueSubTypeName ?? string.Empty,
                    IsAbsenceDiscussion = warning.IssueTypeId == (int)LookupIssueTypeEnum.Discussion &&
                        !warning.LegalExpiryDate.HasValue,

                    SubmittedOn = warning.SubmittedOn,
                    LegalExpiryDate = warning.LegalExpiryDate,

                    LastStatusChangedOn =
                        warning.LastStatusChangedOn,

                    LastStatusChangedBy =
                        warning.LastStatusChangedBy,

                    HideFromTeamLead =
                        warning.HideFromTeamLead,

                    LatestNoteType =
                        latestNote?.NoteType?.Name
                        ?? string.Empty,

                    LatestNoteText =
                        latestNote?.NoteText
                        ?? string.Empty,

                    LatestNoteDate =
                        latestNote?.CreatedOn,

                    IssuedToEmployee =
                        issuedToEmployee
                };
            })
            .ToList();
    }



    public async Task<EmployeeDashboardVm> GetEmployeeDashboardAsync(
       string employeeId)
    {
        if (string.IsNullOrWhiteSpace(employeeId))
        {
            return new EmployeeDashboardVm
            {
                HasAccess = false
            };
        }

        var warnings =
            await _data.GetWarningsByEmployeeIdAsync(employeeId);

        var warningIds = warnings
            .Select(w => w.WarningId)
            .ToList();

        var answers =
            await _data.GetWarningAnswersByWarningIdsAsync(warningIds);

        var warningNotes =
            await _data.GetWarningNotesByWarningIdsAsync(warningIds);

        var descriptionLookup = answers
            .Where(a =>
                a.QuestionId == 2 &&
                a.Warning != null)
            .GroupBy(a => a.Warning!.WarningId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(a => a.CreatedOn)
                    .Select(a =>
                        !string.IsNullOrWhiteSpace(a.AnswerText)
                            ? a.AnswerText
                            : a.AnswerJson)
                    .FirstOrDefault(value =>
                        !string.IsNullOrWhiteSpace(value))
                    ?? string.Empty
            );

        var noteVms = warningNotes
            .Select(note => new EmployeeWarningNoteVm
            {
                WarningId = note.WarningId,
                CreatedOn = note.CreatedOn,
                CreatedBy = note.CreatedBy ?? string.Empty,
                NoteType = note.EvidenceId.HasValue
                    ? "Evidence Note"
                    : "Warning Note",
                NoteText = note.NoteText
            })
            .ToList();

        var today = DateOnly.FromDateTime(DateTime.Today);

        var vm = new EmployeeDashboardVm
        {
            EmployeeId = employeeId,

            EmployeeName = employeeId,

            Department = string.Empty,
            TeamLeader = string.Empty,
            Manager = string.Empty,
            HasAccess = true,

            TotalWarnings = warnings.Count,

            DraftCount = warnings.Count(w =>
                string.Equals(
                    w.Status,
                    "Draft",
                    StringComparison.OrdinalIgnoreCase)),

            NewCount = warnings.Count(w =>
                string.Equals(
                    w.Status,
                    "New",
                    StringComparison.OrdinalIgnoreCase)),

            InProgressCount = warnings.Count(w =>
                string.Equals(
                    w.Status,
                    "In Progress",
                    StringComparison.OrdinalIgnoreCase)),

            DueCount = warnings.Count(w =>
                w.LegalExpiryDate.HasValue),

            OverdueCount = warnings.Count(w =>
                w.LegalExpiryDate.HasValue &&
                w.LegalExpiryDate.Value < today),

            LastActionedDate = warnings
                .Where(w => w.LastStatusChangedOn.HasValue)
                .Select(w => w.LastStatusChangedOn)
                .OrderByDescending(date => date)
                .FirstOrDefault(),

            CurrentState = warnings
                .OrderByDescending(w =>
                    w.LastStatusChangedOn ?? w.CreatedOn)
                .Select(w => w.Status)
                .FirstOrDefault()
                ?? string.Empty,

            Warnings = warnings
                .Select(warning => new EmployeeWarningCardVm
                {
                    WarningId = warning.WarningId,

                    Status = warning.Status
                        ?? string.Empty,

                    CategoryName = warning.Category?.Name
                        ?? string.Empty,

                    IssueTypeName = warning.IssueType?.IssueTypeName
                        ?? string.Empty,

                    IssueSubTypeName = warning.IssueSubType?.IssueSubTypeName
                        ?? string.Empty,

                    IsAbsenceDiscussion = warning.IssueTypeId ==
                        (int)LookupIssueTypeEnum.Discussion &&
                        !warning.LegalExpiryDate.HasValue,

                    CreatedByDisplayName = warning.CreatedBy
                        ?? string.Empty,

                    CreatedOn = warning.CreatedOn,

                    DueDate = warning.LegalExpiryDate.HasValue
                        ? warning.LegalExpiryDate.Value
                            .ToDateTime(TimeOnly.MinValue)
                        : null,

                    LastActionedDate =
                        warning.LastStatusChangedOn,

                    DescriptionSummary =
                        descriptionLookup.TryGetValue(
                            warning.WarningId,
                            out var description)
                            ? description
                            : string.Empty
                })
                .ToList(),

            Notes = noteVms
        };

        await PopulateEmployeeDashboardDisplayNamesAsync(vm);

        return vm;
    }

    private async Task PopulateEmployeeDashboardDisplayNamesAsync(
     EmployeeDashboardVm vm)
    {
        if (vm is null)
            return;

        var warnings =
            vm.Warnings ?? Enumerable.Empty<EmployeeWarningCardVm>();

        var notes =
            vm.Notes ?? Enumerable.Empty<EmployeeWarningNoteVm>();

        var ids = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        // Top-level employee
        if (!string.IsNullOrWhiteSpace(vm.EmployeeId))
        {
            ids.Add(vm.EmployeeId);
        }

        // Warning creators
        foreach (var warning in warnings)
        {
            if (!string.IsNullOrWhiteSpace(
                    warning.CreatedByDisplayName))
            {
                ids.Add(warning.CreatedByDisplayName);
            }
        }

        // Note creators
        foreach (var note in notes)
        {
            if (!string.IsNullOrWhiteSpace(note.CreatedBy))
            {
                ids.Add(note.CreatedBy);
            }
        }

        if (ids.Count == 0)
            return;

        var tasks = ids.Select(async id =>
        {
            try
            {
                var user =
                    await _graphUserDataAccess.GetUserAsync(id);

                var displayName =
                    string.IsNullOrWhiteSpace(user?.DisplayName)
                        ? id
                        : user.DisplayName;

                return (
                    Id: id,
                    DisplayName: displayName
                );
            }
            catch
            {
                return (
                    Id: id,
                    DisplayName: id
                );
            }
        });

        var results = await Task.WhenAll(tasks);

        var lookup = results.ToDictionary(
            result => result.Id,
            result => result.DisplayName,
            StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(vm.EmployeeId) &&
            lookup.TryGetValue(
                vm.EmployeeId,
                out var employeeName))
        {
            vm.EmployeeName = employeeName;
        }

        foreach (var warning in warnings)
        {
            if (!string.IsNullOrWhiteSpace(
                    warning.CreatedByDisplayName) &&
                lookup.TryGetValue(
                    warning.CreatedByDisplayName,
                    out var createdByName))
            {
                warning.CreatedByDisplayName =
                    createdByName;
            }
        }

        foreach (var note in notes)
        {
            if (!string.IsNullOrWhiteSpace(note.CreatedBy) &&
                lookup.TryGetValue(
                    note.CreatedBy,
                    out var noteCreatedByName))
            {
                note.CreatedBy = noteCreatedByName;
            }
        }
    }

    private async Task PopulateDisplayNamesAsync(List<TransgretionsVM> rows)
    {
        if (rows == null || rows.Count == 0)
            return;

        var userLookup = await BuildUserLookupAsync(
            rows.SelectMany(r => new[] { r.EmployeeId, r.CreatedBy })
        );

        foreach (var row in rows)
        {
            row.EmployeeDisplayName =
                !string.IsNullOrWhiteSpace(row.EmployeeId) &&
                userLookup.TryGetValue(row.EmployeeId.Trim(), out var employeeName)
                    ? employeeName
                    : row.EmployeeId;

            row.CreatedByDisplayName =
                !string.IsNullOrWhiteSpace(row.CreatedBy) &&
                userLookup.TryGetValue(row.CreatedBy.Trim(), out var createdByName)
                    ? createdByName
                    : row.CreatedBy;
        }
    }


    private async Task<Dictionary<string, string>> BuildUserLookupAsync(
        IEnumerable<string?> ids)
    {
        var uniqueIds = ids
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (uniqueIds.Count == 0)
        {
            return new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);
        }

        var tasks = uniqueIds.Select(async id =>
        {
            try
            {
                var user = await _graphUserDataAccess.GetUserAsync(id);

                var displayName =
                    string.IsNullOrWhiteSpace(user?.DisplayName)
                        ? id
                        : user.DisplayName;

                return (
                    Id: id,
                    DisplayName: displayName
                );
            }
            catch
            {
                return (
                    Id: id,
                    DisplayName: id
                );
            }
        });

        var results = await Task.WhenAll(tasks);

        return results.ToDictionary(
            result => result.Id,
            result => result.DisplayName,
            StringComparer.OrdinalIgnoreCase);
    }

    private static (bool IsLegal, bool IsChairperson, bool IsUser) GetRoleFlags(IReadOnlyList<string> roles)
    {
        return (
            IsLegal: roles.Contains("Legal", StringComparer.OrdinalIgnoreCase),
            IsChairperson: roles.Contains("Chairperson", StringComparer.OrdinalIgnoreCase),
            IsUser: roles.Contains("User", StringComparer.OrdinalIgnoreCase)
        );
    }

    private record FilterContext(
        List<TransgretionsVM> AllRows,
        List<User> UnderMe,
        bool IsLegal,
        bool IsChairperson,
        bool IsUser
    );

    private static List<TransgretionsVM> FilterRowsForUser(FilterContext ctx)
    {
        var underMeIds = BuildUnderMeIdSet(ctx.UnderMe);

        IEnumerable<TransgretionsVM> filtered = ctx.AllRows;

        if (ctx.IsUser && ctx.IsLegal)
            return ctx.AllRows;

        if (ctx.IsLegal)
        {
            filtered = filtered.Where(r =>
                !string.Equals((r.Status ?? "").Trim(), "Draft", StringComparison.OrdinalIgnoreCase));
        }
        else if (ctx.IsUser)
        {
            filtered = filtered.Where(r =>
                !string.IsNullOrWhiteSpace(r.EmployeeId) &&
                underMeIds.Contains(r.EmployeeId.Trim()));
        }

        if (ctx.IsChairperson)
        {
            filtered = filtered.Where(r =>
                !string.Equals((r.Status ?? "").Trim(), "Hearing", StringComparison.OrdinalIgnoreCase));
        }

        return filtered.ToList();
    }

    private static HashSet<string> BuildUnderMeIdSet(List<User> underme)
    {
        return new HashSet<string>(
            underme.Where(u => !string.IsNullOrWhiteSpace(u.Id))
                   .Select(u => u.Id!.Trim()),
            StringComparer.OrdinalIgnoreCase
        );
    }

    private static void ApplyStatusAndDueCounts(TaskListVM vm, List<TransgretionsVM> rows)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var dueWindowDays = 3;

        foreach (var row in rows)
        {
            var status = (row.Status ?? "").Trim();

            if (status.Equals("Draft", StringComparison.OrdinalIgnoreCase))
            { vm.DraftCount++; }
            else if (status.Equals("New", StringComparison.OrdinalIgnoreCase))
            { vm.New++; }
            else if (status.Equals("InProgress", StringComparison.OrdinalIgnoreCase) ||
                     status.Equals("In Progress", StringComparison.OrdinalIgnoreCase))
            { vm.InProgress++; }

            if (row.LegalExpiryDate.HasValue)
            {
                var expiry = row.LegalExpiryDate.Value;

                if (expiry < today)
                { vm.OverDue++; }
                else if (expiry <= today.AddDays(dueWindowDays))
                { vm.Due++; }
            }
        }
    }

    private static string GetWarningSubStatus(string? type, string? warningSubtype)
    {
        var cleanType = type?.Trim() ?? string.Empty;
        var cleanSubtype = warningSubtype?.Trim() ?? string.Empty;

        if (cleanType.Equals("Warning", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(cleanSubtype))
        {
            return $"Warning - {cleanSubtype}";
        }

        return cleanType.Equals("Issue", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : cleanType;
    }

    private static int GetStatusSortOrder(string status)
    {
        string[] workflow = ["Draft", "New", "In Progress", "Pending", "Issued", "Validated", "Invalid"];
        var index = Array.FindIndex(workflow,
            value => value.Equals(status, StringComparison.OrdinalIgnoreCase));
        return index < 0 ? workflow.Length : index;
    }

    private async Task<List<EmployeeRowVm>> BuildEmployeeRowsAsync(
     List<TransgretionsVM> rows,
     List<User> underme,
     string managerName)
    {
        var warningsByEmployee = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.EmployeeId))
            .GroupBy(r => r.EmployeeId!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Count = g.Count(),
                    LastActioned = g.Max(x => x.LastStatusChangedOn),
                    State = g.OrderByDescending(x => x.LastStatusChangedOn)
                             .FirstOrDefault()?.Status ?? "None"
                },
                StringComparer.OrdinalIgnoreCase
            );

        var result = new List<EmployeeRowVm>();

        foreach (var u in underme.Where(u => !string.IsNullOrWhiteSpace(u.Id)))
        {
            warningsByEmployee.TryGetValue(u.Id!.Trim(), out var info);

            var employeeName = u.DisplayName ?? "(No name)";

            try
            {
                var user = await _graphUserDataAccess.GetUserAsync(u.Id!.Trim());
                if (!string.IsNullOrWhiteSpace(user?.DisplayName))
                    employeeName = user.DisplayName;
            }
            catch
            {
            }

            result.Add(new EmployeeRowVm
            {
                EmployeeId = u.Id!,
                EmployeeName = employeeName,
                Department = u.Department ?? "",
                TeamLeader = "",
                Manager = managerName,
                NoWarnings = info?.Count ?? 0,
                LastActionedDate = info?.LastActioned,
                CurrentState = info?.State ?? "None"
            });
        }

        return result
            .OrderBy(r => r.EmployeeName)
            .ToList();
    }

    private async Task<List<User>> UnderMe()
    {
        return await _graphUserDataAccess.GetUsersBelowMyLevelAsync();
    }

    public async Task<AdminVM> BuildAdminView()
    {
        var categoryEntities = await _data.GetAllCategoriesAsync();
        var questionEntities = await _data.GetAllQuestionsAsync();

        var userNameCache = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        var categories = new List<CategoryVM>();

        foreach (var category in categoryEntities)
        {
            var createdByDisplayName =
                await ResolveUserDisplayNameAsync(
                    category.CreatedBy,
                    userNameCache);

            string? updatedByDisplayName = null;

            if (!string.IsNullOrWhiteSpace(category.UpdatedBy))
            {
                updatedByDisplayName =
                    await ResolveUserDisplayNameAsync(
                        category.UpdatedBy,
                        userNameCache);
            }

            categories.Add(new CategoryVM(
                category,
                createdByDisplayName,
                updatedByDisplayName));
        }

        var questions = new List<QuestionVM>();

        foreach (var question in questionEntities)
        {
            var createdByDisplayName =
                await ResolveUserDisplayNameAsync(
                    question.CreatedBy,
                    userNameCache);

            string? updatedByDisplayName = null;

            if (!string.IsNullOrWhiteSpace(question.UpdatedBy))
            {
                updatedByDisplayName =
                    await ResolveUserDisplayNameAsync(
                        question.UpdatedBy,
                        userNameCache);
            }

            questions.Add(new QuestionVM(
                question,
                createdByDisplayName,
                updatedByDisplayName));
        }

        return new AdminVM
        {
            Categories = categories,
            Questions = questions
        };
    }

    private async Task<string> ResolveUserDisplayNameAsync(
    string? value,
    Dictionary<string, string> cache)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        value = value.Trim();

        if (!Guid.TryParse(value, out _))
            return value;

        if (cache.TryGetValue(value, out var cachedName))
            return cachedName;

        var user = await _graphUserDataAccess.GetUserAsync(value);

        var displayName =
            user?.DisplayName
            ?? user?.UserPrincipalName
            ?? user?.Mail
            ?? value;

        cache[value] = displayName;

        return displayName;
    }

    public async Task<int> CreateQuestionAsync(CreateQuestionDto dto)
    {
        var userId = _currentUser.ObjectId;

        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("User not found in context.");

        var questionText = dto.QuestionText?.Trim();
        var controlType = dto.ControlType?.Trim();

        if (string.IsNullOrWhiteSpace(questionText))
            throw new InvalidOperationException("Please enter a question.");

        if (string.IsNullOrWhiteSpace(controlType))
            throw new InvalidOperationException("Please select a control type.");

        var alreadyExists = await _data.QuestionExistsAsync(
            questionText,
            controlType);

        if (alreadyExists)
        {
            throw new InvalidOperationException(
                $"The question \"{questionText}\" already exists as a {controlType} question.");
        }

        var question = new Question
        {
            QuestionText = questionText,
            ControlType = controlType,
            DefaultConfigJson = string.IsNullOrWhiteSpace(dto.DefaultConfigJson)
                ? null
                : dto.DefaultConfigJson.Trim(),
            IsActive = dto.IsActive ?? true,
            CreatedBy = userId,
            CreatedOn = NowSast
        };

        return await _data.CreateQuestionAsync(question);
    }

    public async Task UpdateQuestionAsync(UpdateQuestionDto dto)
    {
        var userId = _currentUser.ObjectId;

        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("User not found in context.");

        if (dto.QuestionId is 0)
            throw new ArgumentException("Invalid question id.");

        var questionText = dto.QuestionText?.Trim();
        var controlType = dto.ControlType?.Trim();

        if (string.IsNullOrWhiteSpace(questionText))
            throw new InvalidOperationException("Please enter a question.");

        if (string.IsNullOrWhiteSpace(controlType))
            throw new InvalidOperationException("Please select a control type.");

        var question = new Question
        {
            QuestionId = dto.QuestionId,
            QuestionText = questionText,
            ControlType = controlType,
            DefaultConfigJson = string.IsNullOrWhiteSpace(dto.DefaultConfigJson)
                ? null
                : dto.DefaultConfigJson.Trim(),
            IsActive = dto.IsActive,
            UpdatedBy = userId,
            UpdatedOn = NowSast
        };

        await _data.UpdateQuestionAsync(question);
    }

    public async Task<int> CreateCategoryAsync(
        CreateCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.ObjectId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException(
                "User not found in context.");
        }

        var categoryName = dto.Name?.Trim();

        if (string.IsNullOrWhiteSpace(categoryName))
        {
            throw new InvalidOperationException(
                "Please enter a category name.");
        }

        var category = new TransgressionCategory
        {
            Name = categoryName,
            IsActive = dto.IsActive,
            CreatedBy = userId,
            CreatedOn = NowSast
        };

        return await _data.CreateCategoryAsync(
            category,
            cancellationToken);
    }

    public async Task UpdateCategoryAsync(
        UpdateCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.ObjectId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException(
                "User not found in context.");
        }

        if (dto.CategoryId <= 0)
        {
            throw new ArgumentException(
                "Invalid category id.",
                nameof(dto.CategoryId));
        }

        var categoryName = dto.Name?.Trim();

        if (string.IsNullOrWhiteSpace(categoryName))
        {
            throw new InvalidOperationException(
                "Please enter a category name.");
        }

        var category = new TransgressionCategory
        {
            CategoryId = dto.CategoryId,
            Name = categoryName,
            IsActive = dto.IsActive,
            UpdatedBy = userId,
            UpdatedOn = NowSast
        };

        await _data.UpdateCategoryAsync(
            category,
            cancellationToken);
    }

    public async Task<List<QuestionVM>> GetAvailableQuestionsAsync(
        int categoryId)
    {
        if (categoryId <= 0)
        {
            throw new ArgumentException(
                "Invalid category id.",
                nameof(categoryId));
        }

        var questions =
            await _data.GetAvailableQuestionsAsync(categoryId);

        return questions
            .Select(question => new QuestionVM(question))
            .ToList();
    }

    public async Task<List<LinkedQuestionRowVm>> GetLinkedQuestionsAsync(
        int categoryId)
    {
        if (categoryId <= 0)
        {
            throw new ArgumentException(
                "Invalid category id.",
                nameof(categoryId));
        }

        var linkedQuestions =
            await _data.GetLinkedQuestionsAsync(categoryId);

        return linkedQuestions
            .Where(link => link.Question != null)
            .Select(link => new LinkedQuestionRowVm
            {
                CategoryId = link.CategoryId,
                QuestionId = link.QuestionId,
                QuestionText = link.Question!.QuestionText,
                ControlType = link.Question.ControlType,
                IsRequired = link.IsRequired,
                SortOrder = link.SortOrder,
                ConfigJson = link.ConfigJson
            })
            .ToList();
    }

    public async Task SetCategoryQuestionLinkAsync(
        SetCategoryQuestionLinkDto dto)
    {
        var userId = _currentUser.ObjectId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException(
                "User not authenticated.");
        }

        if (dto.CategoryId is 0)
        {
            throw new ArgumentException(
                "Invalid category id.",
                nameof(dto.CategoryId));
        }

        if (dto.QuestionId is 0)
        {
            throw new ArgumentException(
                "Invalid question id.",
                nameof(dto.QuestionId));
        }

        if (dto.SortOrder < 0)
        {
            throw new InvalidOperationException(
                "Sort order cannot be negative.");
        }

        var link = new CategoryQuestion
        {
            CategoryId = dto.CategoryId,
            QuestionId = dto.QuestionId,
            IsRequired = dto.IsRequired,
            SortOrder = dto.SortOrder,
            ConfigJson = string.IsNullOrWhiteSpace(dto.ConfigJson)
                ? null
                : dto.ConfigJson.Trim(),
            IsActive = dto.IsActive
        };

        await _data.SetCategoryQuestionLinkAsync(
            link,
            userId);
    }

    public async Task<WarningWizardVm> GetWarningWizardAsync(long? id)
    {
        var underMe = await UnderMe();

        var sopEntities = await _sop.GetActiveDocumentsAsync();

        var sopDocuments = sopEntities
            .Select(document => new SopDocumentLiteVm(document))
            .ToList();

        if (!id.HasValue || id.Value <= 0)
        {
            var categoryEntities = await _data.GetAllCategoriesAsync();

            return new WarningWizardVm
            {
                Categories = categoryEntities
                    .Select(category => new CategoryVM(category))
                    .ToList(),

                SOPs = sopDocuments,
                UnderMe = underMe
            };
        }

        var existingVm = await BuildWarningWizardAsync(id.Value);

        existingVm.SOPs = sopDocuments;
        existingVm.UnderMe = underMe;

        var storedFiles = await _fileStorage.ListWarningFilesAsync(id.Value);
        var recordedFiles = (existingVm.Evidence ?? [])
            .Select(evidence => evidence.FileName)
            .Where(fileName => !string.IsNullOrWhiteSpace(fileName))
            .Select(fileName => fileName!);

        existingVm.EvidenceStored = storedFiles
            .Concat(recordedFiles)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        existingVm.HasAccess =
            existingVm.CreatedBy == _currentUser.ObjectId;

        return existingVm;
    }

    private async Task<WarningWizardVm> BuildWarningWizardAsync(long warningId)
    {
        var warning = await _data.GetWarningByIdAsync(warningId);

        if (warning is null)
        {
            throw new KeyNotFoundException(
                $"Warning with ID {warningId} not found.");
        }

        var selectedCategories =
            await _data.GetWarningCategoriesByWarningIdAsync(warningId);

        var categoryIds = selectedCategories
            .Select(x => x.CategoryId)
            .Distinct()
            .ToList();

        var categoryNames = selectedCategories
            .Select(x => x.Category?.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct()
            .ToList();

        if (categoryIds.Count == 0 && warning.CategoryId > 0)
        {
            categoryIds.Add(warning.CategoryId);

            if (!string.IsNullOrWhiteSpace(warning.Category?.Name))
            {
                categoryNames.Add(warning.Category.Name);
            }
        }

        var user = await _graphUserDataAccess.GetUserAsync(
            warning.EmployeeId);

        var categoryEntities = await _data.GetAllCategoriesAsync();

        var categoryVms = categoryEntities
            .Select(category => new CategoryVM
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                IsActive = category.IsActive,
                CreatedOn = category.CreatedOn,
                CreatedBy = category.CreatedBy,
                UpdatedOn = category.UpdatedOn,
                UpdatedBy = category.UpdatedBy
            })
            .ToList();

        var categoryQuestions =
            await _data.GetActiveCategoryQuestionsAsync(categoryIds);

        var answers =
            await _data.GetWarningAnswersByWarningIdAsync(warningId);

        var answerMap = answers
            .GroupBy(x => x.QuestionId)
            .ToDictionary(
                group => group.Key,
                group => group.First());

        var questionVms = categoryQuestions
            .Where(x => x.Question != null)
            .GroupBy(x => x.QuestionId)
            .Select(group =>
            {
                var first = group
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Question!.QuestionText)
                    .First();

                var question = first.Question!;

                var questionVm = new WarningQuestionVm
                {
                    QuestionId = question.QuestionId,
                    QuestionText = question.QuestionText,
                    ControlType = question.ControlType,
                    DefaultConfigJson = question.DefaultConfigJson,
                    IsQuestionActive = question.IsActive,

                    IsRequired = group.Any(x => x.IsRequired),
                    SortOrder = group.Min(x => x.SortOrder),
                    ConfigJson = first.ConfigJson,
                    IsCategoryLinkActive = group.Any(x => x.IsActive)
                };

                if (answerMap.TryGetValue(
                        question.QuestionId,
                        out var answer))
                {
                    questionVm.AnswerText = answer.AnswerText;
                    questionVm.AnswerJson = answer.AnswerJson;
                }

                return questionVm;
            })
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.QuestionText)
            .ToList();

        // Older databases can be missing category links for the three core
        // wizard questions. Never discard an answer that was successfully
        // saved just because its CategoryQuestion row is absent or inactive.
        var missingAnsweredQuestionIds = answerMap.Keys
            .Where(questionId => questionVms.All(question =>
                question.QuestionId != questionId))
            .ToHashSet();

        if (missingAnsweredQuestionIds.Count > 0)
        {
            var questions = await _data.GetAllQuestionsAsync();

            questionVms.AddRange(questions
                .Where(question => missingAnsweredQuestionIds.Contains(question.QuestionId))
                .Select(question =>
                {
                    var answer = answerMap[question.QuestionId];

                    return new WarningQuestionVm
                    {
                        QuestionId = question.QuestionId,
                        QuestionText = question.QuestionText,
                        ControlType = question.ControlType,
                        DefaultConfigJson = question.DefaultConfigJson,
                        IsQuestionActive = question.IsActive,
                        IsCategoryLinkActive = true,
                        SortOrder = question.QuestionId,
                        AnswerText = answer.AnswerText,
                        AnswerJson = answer.AnswerJson
                    };
                }));

            questionVms = questionVms
                .OrderBy(question => question.SortOrder)
                .ThenBy(question => question.QuestionText)
                .ToList();
        }

        var isSopNotApplicable = false;
        int? selectedSopDocumentId = null;
        var sopAnswerJson = questionVms
            .FirstOrDefault(question => question.QuestionId == 3)
            ?.AnswerJson;

        if (!string.IsNullOrWhiteSpace(sopAnswerJson))
        {
            try
            {
                using var sopAnswer = System.Text.Json.JsonDocument.Parse(sopAnswerJson);
                var root = sopAnswer.RootElement;

                if (root.TryGetProperty("notApplicable", out var notApplicable) &&
                    (notApplicable.ValueKind == System.Text.Json.JsonValueKind.True ||
                     notApplicable.ValueKind == System.Text.Json.JsonValueKind.False))
                {
                    isSopNotApplicable = notApplicable.GetBoolean();
                }

                if (root.TryGetProperty("sopDocumentId", out var documentId) &&
                    documentId.ValueKind == System.Text.Json.JsonValueKind.Number &&
                    documentId.TryGetInt32(out var parsedDocumentId) &&
                    parsedDocumentId > 0)
                {
                    selectedSopDocumentId = parsedDocumentId;
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Keep the SOP controls empty when an older answer is malformed.
            }
        }

        var noteEntities =
            await _data.GetWarningNotesByWarningIdAsync(warningId);

        var noteVms = noteEntities
            .Select(note => new WarningNoteVm
            {
                NoteId = note.NoteId,
                WarningId = note.WarningId,
                EvidenceId = note.EvidenceId,
                NoteTypeId = note.NoteTypeId,
                NoteTypeName = note.NoteType?.Name ?? string.Empty,
                NoteText = note.NoteText,
                CreatedBy = note.CreatedBy,
                CreatedOnUtc = note.CreatedOn
            })
            .ToList();

        var evidenceNotesLookup = noteVms
            .Where(note => note.EvidenceId.HasValue)
            .GroupBy(note => note.EvidenceId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.ToList());

        var evidenceEntities =
            await _data.GetWarningEvidenceByWarningIdAsync(warningId);

        var evidenceVms = evidenceEntities
            .Select(evidence => new WarningEvidenceVm
            {
                EvidenceId = evidence.EvidenceId,
                FileName = evidence.FileName,
                FileType = evidence.FileType,
                FileSizeBytes = evidence.FileSizeBytes,
                StorageUrl = evidence.StorageUrl,
                UploadedBy = evidence.UploadedBy,
                UploadedOnUtc = evidence.UploadedOn
            })
            .ToList();

        foreach (var evidence in evidenceVms)
        {
            if (evidenceNotesLookup.TryGetValue(
                    evidence.EvidenceId,
                    out var notes))
            {
                evidence.Notes = notes;
            }
        }

        return new WarningWizardVm
        {
            WarningId = warning.WarningId,
            EmployeeId = warning.EmployeeId,
            EmployeeName = user.DisplayName,
            CategoryIds = categoryIds,
            CategoryNames = categoryNames,
            Categories = categoryVms,

            Status = warning.Status,
            CreatedOnUtc = warning.CreatedOn,
            CreatedBy = warning.CreatedBy,
            SubmittedOnUtc = warning.SubmittedOn,
            LegalExpiryDate = warning.LegalExpiryDate,
            LastStatusChangedOnUtc = warning.LastStatusChangedOn,
            LastStatusChangedBy = warning.LastStatusChangedBy,

            Questions = questionVms,
            IsSopNonCompliance = isSopNotApplicable,
            SelectedSOPDocumentId = selectedSopDocumentId,
            Evidence = evidenceVms,

            Notes = noteVms
                .Where(note => note.EvidenceId == null)
                .ToList()
        };
    }

    public async Task<LegalWizardVm?> GetWarningLegalAsync(long warningId)
    {
        if (warningId <= 0)
        {
            return null;
        }

        var warning = await _data.GetWarningByIdAsync(warningId);

        if (warning is null)
        {
            return null;
        }

        var categoryIds = new List<int>();

        if (warning.CategoryId > 0)
        {
            categoryIds.Add(warning.CategoryId);
        }

        var categoryQuestions =
            await _data.GetActiveCategoryQuestionsAsync(categoryIds);

        var answers =
            await _data.GetWarningAnswersByWarningIdAsync(warningId);

        var answerLookup = answers
            .GroupBy(answer => answer.QuestionId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(answer => answer.CreatedOn)
                    .First());

        var questions = categoryQuestions
            .Where(categoryQuestion =>
                categoryQuestion.Question is not null)
            .Select(categoryQuestion =>
            {
                var question = categoryQuestion.Question!;

                answerLookup.TryGetValue(
                    question.QuestionId,
                    out var answer);

                return new LegalQuestionVm
                {
                    QuestionId = question.QuestionId,
                    QuestionText = question.QuestionText,
                    ControlType = question.ControlType,
                    IsRequired = categoryQuestion.IsRequired,
                    SortOrder = categoryQuestion.SortOrder,
                    DefaultConfigJson = question.DefaultConfigJson,
                    CategoryConfigJson = categoryQuestion.ConfigJson,
                    AnswerText = answer?.AnswerText,
                    AnswerJson = answer?.AnswerJson
                };
            })
            .OrderBy(question => question.SortOrder)
            .ThenBy(question => question.QuestionText)
            .ToList();

        var evidence = warning.Evidence
            .OrderByDescending(item => item.UploadedOn)
            .Select(item => new LegalEvidenceVm
            {
                EvidenceId = item.EvidenceId,
                FileName = item.FileName,
                FileType = item.FileType,
                FileSizeBytes = item.FileSizeBytes,
                StorageUrl = item.StorageUrl,
                UploadedBy = item.UploadedBy,
                UploadedOn = item.UploadedOn,

                Notes = item.Notes
                    .OrderByDescending(note => note.CreatedOn)
                    .Select(note => new WarningNoteVm
                    {
                        NoteId = note.NoteId,
                        WarningId = note.WarningId,
                        EvidenceId = note.EvidenceId,
                        NoteText = note.NoteText,
                        CreatedBy = note.CreatedBy,
                        CreatedOnUtc = note.CreatedOn
                    })
                    .ToList()
            })
            .ToList();

        var warningLevelNotes = warning.Notes
            .Where(note => note.EvidenceId is null)
            .OrderByDescending(note => note.CreatedOn)
            .Select(note => new WarningNoteVm
            {
                NoteId = note.NoteId,
                WarningId = note.WarningId,
                EvidenceId = note.EvidenceId,
                NoteText = note.NoteText,
                CreatedBy = note.CreatedBy,
                CreatedOnUtc = note.CreatedOn
            })
            .ToList();

        var allNotes = warningLevelNotes
            .Concat(
                evidence.SelectMany(item => item.Notes))
            .ToList();

        var warningHistory =
            await _data.GetWarningsByEmployeeIdAsync(
                warning.EmployeeId);

        var history = warningHistory
            .Select(historyWarning => new LegalWarningHistoryVm
            {
                WarningId = historyWarning.WarningId,
                CreatedOn = historyWarning.CreatedOn,
                Status = historyWarning.Status,
                LastStatusChangedOn =
                    historyWarning.LastStatusChangedOn,
                LastStatusChangedBy =
                    historyWarning.LastStatusChangedBy,
                CategoryName =
                    historyWarning.Category?.Name
                    ?? string.Empty
            })
            .OrderByDescending(item => item.CreatedOn)
            .ToList();

        var userIdentifiers = allNotes
            .Select(note => note.CreatedBy)
            .Concat(
                evidence.Select(item =>
                    item.UploadedBy))
            .Concat(
                history.Select(item =>
                    item.LastStatusChangedBy))
            .Concat(
                new string?[]
                {
                warning.EmployeeId,
                warning.CreatedBy,
                warning.LastStatusChangedBy
                });

        var userLookup =
            await BuildUserDisplayNameLookupAsync(
                userIdentifiers);

        foreach (var note in allNotes)
        {
            note.CreatedByDisplayName =
                ResolveDisplayName(
                    note.CreatedBy,
                    userLookup);
        }

        foreach (var evidenceItem in evidence)
        {
            evidenceItem.UploadedByDisplayName =
                ResolveDisplayName(
                    evidenceItem.UploadedBy,
                    userLookup);
        }

        foreach (var historyItem in history)
        {
            historyItem.LastStatusChangedByDisplayName =
                ResolveDisplayName(
                    historyItem.LastStatusChangedBy,
                    userLookup);
        }

        var employeeDisplayName =
            ResolveDisplayName(
                warning.EmployeeId,
                userLookup);

        var createdByDisplayName =
            ResolveDisplayName(
                warning.CreatedBy,
                userLookup);

        var noteTypeEntities =
            await _data.GetActiveNoteTypesAsync();

        var noteTypes = noteTypeEntities
            .Select(noteType => new NoteTypeLookupVm
            {
                NoteTypeId = noteType.NoteTypeId,
                Name = noteType.Name
            })
            .ToList();

        var issueTypes = (await _data.GetActiveIssueTypesAsync())
            .Select(type => new IssueTypeLookupVm
            {
                IssueTypeId = type.IssueTypeId,
                IssueTypeName = type.IssueTypeName,
                SubTypeSelectionMode = type.SubTypeSelectionMode,
                SubTypes = type.IssueSubTypes
                    .OrderBy(subType => subType.IssueSubTypeId)
                    .Select(subType => new IssueSubTypeLookupVm
                    {
                        IssueSubTypeId = subType.IssueSubTypeId,
                        IssueSubTypeName = subType.IssueSubTypeName
                    })
                    .ToList()
            })
            .ToList();

        return new LegalWizardVm
        {
            WarningId = warning.WarningId,
            EmployeeId = warning.EmployeeId,
            EmployeeIdDesplayName = employeeDisplayName,
            HideFromTeamLead = warning.HideFromTeamLead,
            Status = warning.IssueStatus?.IssueStatusName ?? warning.Status,
            IssueStatusGroup = warning.IssueStatus?.IssueStatusGroup ?? IssueStatusGroup.Draft,
            Type = warning.IssueType?.IssueTypeName ?? warning.Type,
            WarningSubtype = warning.IssueSubType?.IssueSubTypeName ?? warning.WarningSubtype,
            CreatedOn = warning.CreatedOn,
            CreatedBy = warning.CreatedBy,
            CreatedByDesplayName = createdByDisplayName,
            CategoryId = warning.CategoryId,
            CategoryName =
                warning.Category?.Name
                ?? string.Empty,
            SubmittedOn = warning.SubmittedOn,
            LegalExpiryDate = warning.LegalExpiryDate,
            Questions = questions,
            Evidence = evidence,
            Notes = warningLevelNotes,
            NoteTypes = noteTypes,
            IssueTypes = issueTypes,
            TransgretionHistory = history
        };
    }

    private static string ResolveDisplayName(
        string? userIdentifier,
        IReadOnlyDictionary<string, string> userLookup)
    {
        if (string.IsNullOrWhiteSpace(userIdentifier))
        {
            return string.Empty;
        }

        return userLookup.TryGetValue(
            userIdentifier,
            out var displayName)
                ? displayName
                : userIdentifier;
    }


    public async Task<bool?> SetHideFromTeamLeadAsync(
    long warningId,
    bool hideFromTeamLead,
    string changedBy)
    {

        return await _data
            .SetHideFromTeamLeadAsync(
                warningId,
                hideFromTeamLead
            );
    }
    private async Task<Dictionary<string, string>> BuildUserDisplayNameLookupAsync(IEnumerable<string?> userIds)
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var distinctUserIds = userIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var userId in distinctUserIds)
        {
            try
            {
                var user = await _graphUserDataAccess.GetUserAsync(userId);

                lookup[userId] = !string.IsNullOrWhiteSpace(user?.DisplayName)
                    ? user.DisplayName
                    : userId;
            }
            catch
            {
                lookup[userId] = userId;
            }
        }

        return lookup;
    }

    public async Task MarkWarningInProgressAsync(long warningId)
    {
        if (warningId is 0)
            throw new ArgumentException("Invalid warning id.", nameof(warningId));

        var userId = _currentUser?.ObjectId ?? "system";

        await _data.AdvanceWarningStatusAsync(warningId, IssueStatusGroup.LegalReview, userId);
    }

    public async Task<SaveIssueStepResult> SaveIssueStepAsync(
    CreateTransgressionDTO dto)
    {
        var categoryIds = dto.CategoryIds
            .Where(categoryId => categoryId > 0)
            .Distinct()
            .ToList();

        if (categoryIds.Count == 0 && dto.CategoryId > 0)
        {
            categoryIds.Add(dto.CategoryId);
        }

        if (string.IsNullOrWhiteSpace(dto.EmployeeId))
        {
            return SaveIssueStepResult.Fail(
                "Please select an employee.");
        }

        if (categoryIds.Count == 0)
        {
            return SaveIssueStepResult.Fail(
                "Please select at least one category.");
        }

        var currentUserId = _currentUser.ObjectId;

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return SaveIssueStepResult.Fail(
                "Current user could not be identified.");
        }

        long warningId;

        if (dto.WarningId is > 0)
        {
            var warning = await _data.GetWarningByIdAsync(
                dto.WarningId.Value);

            if (warning is null)
            {
                return SaveIssueStepResult.Fail(
                    "The warning could not be found.");
            }

            warning.EmployeeId = dto.EmployeeId.Trim();
            warning.CategoryId = categoryIds.First();

            warning.LastStatusChangedBy = currentUserId;
            warning.LastStatusChangedOn = NowSast;

            warningId = await _data.UpdateWarningIssueAsync(
                warning,
                categoryIds);
        }
        else
        {
            var draftStatus = await _data.GetIssueStatusByGroupAsync(IssueStatusGroup.Draft)
                ?? throw new InvalidOperationException("The initial Draft status has not been configured.");

            var warning = new Warning
            {
                EmployeeId = dto.EmployeeId.Trim(),


                CategoryId = categoryIds.First(),

                Status = draftStatus.IssueStatusName,
                IssueStatusId = draftStatus.IssueStatusId,
                Type = "Issue",
                CreatedBy = currentUserId,
                CreatedOn = NowSast,
                LastStatusChangedBy = currentUserId,
                LastStatusChangedOn = NowSast,

                WarningCategories = categoryIds
                 .Select(categoryId => new WarningCategory
                 {
                     CategoryId = categoryId
                 })
                 .ToList()
            };

            warningId = await _data.CreateWarningAsync(
                warning,
                categoryIds);
        }

        return SaveIssueStepResult.Ok(warningId);
    }

    public async Task<SaveIssueStepResult> CreateAbsenceDiscussionAsync(
        CreateAbsenceDiscussionDto dto)
    {
        const int otherUnknownCategoryId = 31;

        if (dto.IssueSubTypeId <= 0)
            return SaveIssueStepResult.Fail("Please select a discussion subtype.");
        if (dto.Dates.Count == 0)
            return SaveIssueStepResult.Fail("Please select at least one date.");
        if (dto.Dates.Any(date => date > DateOnly.FromDateTime(NowSast)))
            return SaveIssueStepResult.Fail("Discussion dates cannot be in the future.");
        if (string.IsNullOrWhiteSpace(dto.Description))
            return SaveIssueStepResult.Fail("Please enter a description.");

        var discussionType = (await _data.GetActiveIssueTypesAsync())
            .SingleOrDefault(type => type.IssueTypeId == (int)LookupIssueTypeEnum.Discussion);
        var isValidSubType = discussionType?.IssueSubTypes.Any(subType =>
            subType.IssueSubTypeId == dto.IssueSubTypeId && subType.IsActive) == true;

        if (!isValidSubType)
            return SaveIssueStepResult.Fail("Please select a valid discussion subtype.");

        var createResult = await SaveIssueStepAsync(new CreateTransgressionDTO
        {
            EmployeeId = dto.EmployeeId,
            CategoryId = otherUnknownCategoryId,
            CategoryIds = [otherUnknownCategoryId]
        });

        if (!createResult.Success)
            return createResult;

        var dates = dto.Dates.Distinct().OrderBy(date => date)
            .Select(date => date.ToString("yyyy-MM-dd")).ToList();

        await _data.UpsertWarningAnswerAsync(createResult.WarningId, 1,
            string.Join(", ", dates),
            $"{{\"dates\":[{string.Join(",", dates.Select(date => $"\"{date}\""))}]}}");
        await _data.UpsertWarningAnswerAsync(createResult.WarningId, 2,
            dto.Description.Trim(), null);
        await _data.CompleteDiscussionAsync(createResult.WarningId, dto.IssueSubTypeId,
            _currentUser.ObjectId);

        return createResult;
    }



    public async Task SaveAnswerAsync(SaveAnswerDto dto)
    {
        await _data.UpsertWarningAnswerAsync(dto.WarningId, dto.QuestionId, dto.AnswerText, dto.AnswerJson);
    }

    public async Task AddWarningNoteAsync(
        long warningId,
        long? evidenceId,
        int noteTypeId,
        string noteText)
    {
        var user = _currentUser?.ObjectId ?? "system";

        if (noteTypeId == 4)
        {
            await SendMoreInformationRequiredEmailAsync(warningId, noteText, null);
        }

        await _data.AddWarningNoteAsync(
            warningId,
            evidenceId,
            noteTypeId,
            noteText,
            user);
    }

    public async Task<long?> SaveEvidenceAsync(
     long warningId,
     string? fileName,
     string? mediaType,
     long? fileSizeBytes,
     string? notes,
     int? typeid,
     bool legal)
    {
        var user = _currentUser?.ObjectId ?? "system";

        if (typeid == 4 && legal)
        {
            await SendMoreInformationRequiredEmailAsync(warningId, notes, null);
        }
        else if (typeid == 4)
        {
            await SendMoreInformationAddedEmailAsync(warningId, notes, null);
        }

        return await _data.SaveEvidenceAsync(
            warningId,
            fileName,
            mediaType,
            fileSizeBytes,
            user,
            notes,
            typeid);
    }

    public async Task UpdateWarningStatusAsync(long warningId, DateOnly? duedate, string? status)
    {
        var user = _currentUser.ObjectId;

        if (!string.IsNullOrWhiteSpace(status))
        {
            var warning = await _data.GetWarningByIdAsync(warningId)
                ?? throw new KeyNotFoundException($"Warning not found. WarningId={warningId}");

            if (warning.IssueStatus?.IssueStatusGroup != IssueStatusGroup.Draft)
                throw new InvalidOperationException("Only Draft issues can be submitted to Legal as New.");

            await _data.AdvanceWarningStatusAsync(warningId, IssueStatusGroup.Draft, user);
        }

        await _data.UpdateWarningDueDateAsync(warningId, duedate, user);
    }

    public async Task ApplyLegalDecisionAsync(
        long warningId,
        int issueTypeId,
        int? issueSubTypeId)
    {
        var warning = await _data.GetWarningByIdAsync(warningId)
            ?? throw new KeyNotFoundException($"Warning not found. WarningId={warningId}");

        if (warning.IssueStatus?.IssueStatusGroup != IssueStatusGroup.LegalDecision)
            throw new InvalidOperationException("Legal can only record a decision while an issue is In Progress.");

        var issueType = (await _data.GetActiveIssueTypesAsync())
            .SingleOrDefault(type => type.IssueTypeId == issueTypeId)
            ?? throw new InvalidOperationException("Select a valid issue type.");

        var issueSubType = issueSubTypeId.HasValue
            ? issueType.IssueSubTypes.SingleOrDefault(subType => subType.IssueSubTypeId == issueSubTypeId)
            : null;

        if (issueType.SubTypeSelectionMode == SubTypeSelectionMode.Required && issueSubType is null)
            throw new InvalidOperationException("A valid issue subtype is required.");

        if (issueType.SubTypeSelectionMode == SubTypeSelectionMode.None)
            issueSubType = null;

        await _data.UpdateWarningDecisionAsync(warningId, issueType, issueSubType, _currentUser.ObjectId);
    }

    public async Task ValidateWarningAsync(long warningId)
    {
        var warning = await _data.GetWarningByIdAsync(warningId)
            ?? throw new KeyNotFoundException($"Warning not found. WarningId={warningId}");

        if (warning.IssueStatus?.IssueStatusGroup != IssueStatusGroup.LegalValidation)
            throw new InvalidOperationException("Only an Issued issue can be Validated.");

        await _data.AdvanceWarningStatusAsync(
            warningId, IssueStatusGroup.LegalValidation, _currentUser.ObjectId);
    }

    public async Task SendEmailToLegalAsync(long warningId, DateOnly? duedate, string? status)
    {
        var warning = await _data.GetWarningByIdAsync(warningId)
            ?? throw new InvalidOperationException($"Warning not found. WarningId={warningId}");

        if (string.IsNullOrWhiteSpace(_applicationOptions.BaseUrl))
            throw new InvalidOperationException("The application base URL has not been configured.");

        var employeeDisplayName = await GetDisplayNameFromUserIdAsync(warning.EmployeeId);
        var submittedByDisplayName = await GetDisplayNameFromUserIdAsync(warning.CreatedBy);
        var legalUrl = $"{_applicationOptions.BaseUrl.TrimEnd('/')}/Legal/Index/{warning.WarningId}";

        var bodyHtml = _emailTemplateRenderer.Render(
            "IssueCreated",
            new Dictionary<string, string?>
            {
                ["EmployeeDisplayName"] = string.IsNullOrWhiteSpace(employeeDisplayName)
                    ? warning.EmployeeId
                    : employeeDisplayName,
                ["SubmittedByDisplayName"] = string.IsNullOrWhiteSpace(submittedByDisplayName)
                    ? warning.CreatedBy
                    : submittedByDisplayName,
                ["Category"] = warning.Category?.Name ?? warning.CategoryId.ToString(),
                ["Status"] = string.IsNullOrWhiteSpace(status) ? warning.Status : status.Trim(),
                ["WarningId"] = warning.WarningId.ToString(),
                ["DueDate"] = duedate.HasValue ? duedate.Value.ToString("dd/MM/yyyy") : "—",
                ["LegalUrl"] = legalUrl
            });

        var email = new SendEmailRequest
        {
            Subject = "Warning System | Issue Created",
            BodyHtml = bodyHtml,
            ToRecipients = _emailSettings.LegalRecipients,
        };

        await EmailSender(email);
    }

    private async Task EmailSender(SendEmailRequest email)
    {
        ArgumentNullException.ThrowIfNull(email);

        if (email.ToRecipients is null ||
            email.ToRecipients.Count == 0)
        {
            throw new ArgumentException(
                "At least one To recipient is required.",
                nameof(email));
        }

        var toList = EmailRecipientNormalizer.Normalize(email.ToRecipients)
            .Select(address => new Recipient
            {
                EmailAddress = new EmailAddress
                {
                    Address = address
                }
            })
            .ToList();

        if (toList.Count == 0)
        {
            throw new ArgumentException(
                "At least one valid To recipient is required.",
                nameof(email));
        }

        var message = new Message
        {
            Subject = email.Subject,
            Body = new ItemBody
            {
                ContentType = BodyType.Html,
                Content = email.BodyHtml
            },
            ToRecipients = toList
        };

        if (email.CcRecipients is { Count: > 0 })
        {
            var ccList = EmailRecipientNormalizer.Normalize(email.CcRecipients)
                .Select(address => new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = address
                    }
                })
                .ToList();

            if (ccList.Count > 0)
            {
                message.CcRecipients = ccList;
            }
        }

        if (email.BccRecipients is { Count: > 0 })
        {
            var bccList = EmailRecipientNormalizer.Normalize(email.BccRecipients)
                .Select(address => new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = address
                    }
                })
                .ToList();

            if (bccList.Count > 0)
            {
                message.BccRecipients = bccList;
            }
        }

        if (email.Attachments is { Count: > 0 })
        {
            var attachments = email.Attachments
                .Where(attachment =>
                    attachment is not null &&
                    !string.IsNullOrWhiteSpace(attachment.FileName) &&
                    attachment.ContentBytes is { Length: > 0 })
                .Select(attachment => (Attachment)new FileAttachment
                {
                    OdataType = "#microsoft.graph.fileAttachment",
                    Name = attachment!.FileName,
                    ContentType =
                        string.IsNullOrWhiteSpace(attachment.ContentType)
                            ? "application/octet-stream"
                            : attachment.ContentType,
                    ContentBytes = attachment.ContentBytes
                })
                .ToList();

            if (attachments.Count > 0)
            {
                message.Attachments = attachments;
            }
        }

        await _graphUserDataAccess.SendEmailAsync(message);
    }

    public async Task<(bool Success, string? Message)>
        UpdateDueDateWithAuditAsync(
            UpdateDueDateDto dto,
            string changedBy)
    {
        if (dto.WarningId is 0)
        {
            return (false, "Missing WarningId.");
        }

        var reason = dto.Reason?.Trim();

        if (string.IsNullOrWhiteSpace(reason))
        {
            return (false, "Reason is required.");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);

        if (dto.DueDate <= today)
        {
            return (false, "Due date must be in the future.");
        }

        if (dto.DueDate.DayOfWeek is
            DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return (false, "Due date cannot be on a weekend.");
        }

        var userId = string.IsNullOrWhiteSpace(changedBy)
            ? "System"
            : changedBy.Trim();

        var warning = new Warning
        {
            WarningId = dto.WarningId,
            LegalExpiryDate = dto.DueDate,
            LastStatusChangedOn = NowSast,
            LastStatusChangedBy = userId
        };

        try
        {
            await _data.UpdateWarningDueDateAsync(
                warning,
                reason);

            return (true, null);
        }
        catch (KeyNotFoundException)
        {
            return (false, "Warning not found.");
        }
    }

    public async Task SendEmailToTeamLeadAsync(CompleteTransgressionDto dto)
    {
        var stage = "Starting";

        try
        {
            stage = "Validating input";

            if (dto is null)
                throw new ArgumentNullException(nameof(dto));

            if (dto.WarningId is 0)
                throw new ArgumentException("Invalid warning id.", nameof(dto.WarningId));

            stage = $"Getting warning info. WarningId={dto.WarningId}";

            var warningInfo = await _data.GetWarningByIdAsync(dto.WarningId);

            if (warningInfo == null)
                throw new InvalidOperationException($"Warning not found. WarningId={dto.WarningId}");

            if (string.IsNullOrWhiteSpace(warningInfo.CreatedBy))
                throw new InvalidOperationException($"Warning CreatedBy is empty. WarningId={dto.WarningId}");

            var createdByUserId = warningInfo.CreatedBy.Trim();

            stage = $"Getting created by user from Graph. CreatedBy={createdByUserId}, WarningId={dto.WarningId}";

            var user = await _graphUserDataAccess.GetUserAsync(createdByUserId);

            if (user == null)
                throw new InvalidOperationException(
                    $"Graph user not found for CreatedBy={createdByUserId} (WarningId={dto.WarningId})");

            var username = !string.IsNullOrWhiteSpace(user.DisplayName)
                ? user.DisplayName
                : createdByUserId;

            var userEmail = !string.IsNullOrWhiteSpace(user.Mail)
                ? user.Mail
                : user.UserPrincipalName;

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                throw new InvalidOperationException(
                    $"Creator has no Mail/UPN. UserId={user.Id}, DisplayName={user.DisplayName}, CreatedBy={createdByUserId}, WarningId={dto.WarningId}");
            }

            stage = $"Getting employee display name. EmployeeId={warningInfo.EmployeeId}, WarningId={dto.WarningId}";

            var employeeDisplayName = await GetDisplayNameFromUserIdAsync(warningInfo.EmployeeId);

            if (string.IsNullOrWhiteSpace(employeeDisplayName))
                employeeDisplayName = warningInfo.EmployeeId;

            stage = $"Building email request. WarningId={dto.WarningId}, To={userEmail}";

            if (string.IsNullOrWhiteSpace(_applicationOptions.BaseUrl))
            {
                throw new InvalidOperationException(
                    "The application base URL has not been configured.");
            }

            var warningUrl =
                $"{_applicationOptions.BaseUrl.TrimEnd('/')}" +
                $"/Transgression/TeamLeadWarning/{warningInfo.WarningId}";

            var safeUsername = System.Net.WebUtility.HtmlEncode(username);
            var safeStatus = System.Net.WebUtility.HtmlEncode(warningInfo.Status ?? "");
            var safeEmployeeDisplayName = System.Net.WebUtility.HtmlEncode(employeeDisplayName);
            var safeCategory = System.Net.WebUtility.HtmlEncode(
                warningInfo.Category.Name ?? warningInfo.CategoryId.ToString());
            var safeWarningUrl = System.Net.WebUtility.HtmlEncode(warningUrl);

            var req = new SendEmailRequest
            {
                Subject = $"Warning System | Issue {warningInfo.WarningId} updated",
                BodyHtml = $@"
                        <div style=""font-family: Arial, sans-serif; color:#333; line-height:1.5;"">

                            <h2 style=""color:#014678; margin-bottom:8px;"">
                                Issue Updated
                            </h2>

                            <p>Hi {safeUsername},</p>

                            <p>
                                Issue <strong>#{warningInfo.WarningId}</strong> has been updated.
                            </p>

                            <table style=""border-collapse:collapse; margin-top:15px; margin-bottom:15px; width:100%; max-width:650px;"">
                                <tr>
                                    <td style=""padding:8px; border:1px solid #ddd; font-weight:bold; background:#f8f9fa;"">
                                        Employee
                                    </td>
                                    <td style=""padding:8px; border:1px solid #ddd;"">
                                        {safeEmployeeDisplayName}
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding:8px; border:1px solid #ddd; font-weight:bold; background:#f8f9fa;"">
                                        Category
                                    </td>
                                    <td style=""padding:8px; border:1px solid #ddd;"">
                                        {safeCategory}
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding:8px; border:1px solid #ddd; font-weight:bold; background:#f8f9fa;"">
                                        Status
                                    </td>
                                    <td style=""padding:8px; border:1px solid #ddd;"">
                                        {safeStatus}
                                    </td>
                                </tr>
                            </table>

                            <p>
                                Please open the link below to review the issue:
                            </p>

                            <p>
                                <a href=""{warningUrl}""
                                   style=""
                                       display:inline-block;
                                       padding:10px 18px;
                                       background:#014678;
                                       color:#ffffff;
                                       text-decoration:none;
                                       border-radius:999px;
                                       font-weight:bold;"">
                                    Open Issue
                                </a>
                            </p>

                            <p style=""font-size:12px; color:#666; margin-top:20px;"">
                                If the button does not work, copy and paste this link into your browser:<br/>
                                <span>{safeWarningUrl}</span>
                            </p>

                        </div>",
                ToRecipients = new List<string> { userEmail },
                SaveToSentItems = true
            };

            if (dto.File is not null && dto.File.Length is > 0)
            {
                stage = $"Adding attachment. FileName={dto.File.FileName}, Size={dto.File.Length}, WarningId={dto.WarningId}";

                using var ms = new MemoryStream();
                await dto.File.CopyToAsync(ms);

                req.Attachments.Add(new EmailAttachment
                {
                    FileName = dto.File.FileName,
                    ContentType = string.IsNullOrWhiteSpace(dto.File.ContentType)
                        ? "application/octet-stream"
                        : dto.File.ContentType,
                    ContentBytes = ms.ToArray()
                });
            }

            stage = $"Sending email. WarningId={dto.WarningId}, To={userEmail}, Attachments={req.Attachments.Count}";

            await _graphUserDataAccess.SendEmailAsync(req);

            stage = $"Email sent successfully. WarningId={dto.WarningId}, To={userEmail}";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to send team lead email. Stage: {stage}. WarningId={dto?.WarningId}. Error: {ex.Message}",
                ex);
        }
    }

    public async Task SendMoreInformationRequiredEmailAsync(
        long warningId,
        string noteText,
        IFormFile? file = null)
    {
        var stage = "Starting";

        try
        {
            stage = "Validating input";

            if (warningId is 0)
                throw new ArgumentException("Invalid warning id.", nameof(warningId));

            if (string.IsNullOrWhiteSpace(noteText))
                throw new ArgumentException("Note text is required.", nameof(noteText));

            stage = $"Getting warning info. WarningId={warningId}";

            var warningInfo = await _data.GetWarningByIdAsync(warningId);

            if (warningInfo == null)
                throw new InvalidOperationException($"Warning not found. WarningId={warningId}");

            if (string.IsNullOrWhiteSpace(warningInfo.CreatedBy))
                throw new InvalidOperationException($"Warning CreatedBy is empty. WarningId={warningId}");

            var createdByUserId = warningInfo.CreatedBy.Trim();

            stage = $"Getting created by user from Graph. CreatedBy={createdByUserId}, WarningId={warningId}";

            var user = await _graphUserDataAccess.GetUserAsync(createdByUserId);

            if (user == null)
                throw new InvalidOperationException(
                    $"Graph user not found for CreatedBy={createdByUserId} (WarningId={warningId})");

            var username = !string.IsNullOrWhiteSpace(user.DisplayName)
                ? user.DisplayName
                : createdByUserId;

            var userEmail = !string.IsNullOrWhiteSpace(user.Mail)
                ? user.Mail
                : user.UserPrincipalName;

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                throw new InvalidOperationException(
                    $"Creator has no Mail/UPN. UserId={user.Id}, DisplayName={user.DisplayName}, CreatedBy={createdByUserId}, WarningId={warningId}");
            }

            stage = $"Getting employee display name. EmployeeId={warningInfo.EmployeeId}, WarningId={warningId}";

            var employeeDisplayName = await GetDisplayNameFromUserIdAsync(warningInfo.EmployeeId);

            if (string.IsNullOrWhiteSpace(employeeDisplayName))
            {
                employeeDisplayName = warningInfo.EmployeeId;
            }

            stage = $"Building email request. WarningId={warningId}, To={userEmail}";

            if (string.IsNullOrWhiteSpace(_applicationOptions.BaseUrl))
            {
                throw new InvalidOperationException(
                    "The application base URL has not been configured.");
            }

            var teamLeadUrl =
                $"{_applicationOptions.BaseUrl.TrimEnd('/')}" +
                $"/Transgression/TeamLeadWarning/{warningInfo.WarningId}";

            var safeNoteText = System.Net.WebUtility.HtmlEncode(noteText);
            var safeUsername = System.Net.WebUtility.HtmlEncode(username);
            var safeEmployeeDisplayName = System.Net.WebUtility.HtmlEncode(employeeDisplayName);
            var safeCategory = System.Net.WebUtility.HtmlEncode(
                warningInfo.Category?.Name ?? warningInfo.CategoryId.ToString());
            var safeStatus = System.Net.WebUtility.HtmlEncode(warningInfo.Status ?? "");

            var req = new SendEmailRequest
            {
                Subject = $"Warning System | More information required - Issue {warningInfo.WarningId}",
                BodyHtml = $@"
            <div style=""font-family: Arial, sans-serif; color:#333; line-height:1.5;"">

            <h2 style=""color:#f47c37; margin-bottom:8px;"">
                More Information Required
            </h2>

            <p>Hi {safeUsername},</p>

            <p>
                Legal has requested more information for issue
                <strong>#{warningInfo.WarningId}</strong>.
            </p>

            <table style=""border-collapse:collapse; margin-top:15px; margin-bottom:15px; width:100%; max-width:650px;"">
                <tr>
                    <td style=""padding:8px; border:1px solid #ddd; font-weight:bold; background:#f8f9fa;"">
                        Employee
                    </td>
                    <td style=""padding:8px; border:1px solid #ddd;"">
                        {safeEmployeeDisplayName}
                    </td>
                </tr>
                <tr>
                    <td style=""padding:8px; border:1px solid #ddd; font-weight:bold; background:#f8f9fa;"">
                        Category
                    </td>
                    <td style=""padding:8px; border:1px solid #ddd;"">
                        {safeCategory}
                    </td>
                </tr>
                <tr>
                    <td style=""padding:8px; border:1px solid #ddd; font-weight:bold; background:#f8f9fa;"">
                        Current Status
                    </td>
                    <td style=""padding:8px; border:1px solid #ddd;"">
                        {safeStatus}
                    </td>
                </tr>
            </table>

            <p style=""margin-bottom:6px;""><strong>Message from Legal:</strong></p>

            <div style=""
                border:1px solid #ddd;
                background:#f8f9fa;
                padding:12px;
                border-radius:6px;
                white-space:pre-wrap;
                margin-bottom:18px;"">
                {safeNoteText}
            </div>

            <p>
                Please open the link below to add the requested information:
            </p>

            <p>
                <a href=""{teamLeadUrl}""
                   style=""
                       display:inline-block;
                       padding:10px 18px;
                       background:#f47c37;
                       color:#ffffff;
                       text-decoration:none;
                       border-radius:999px;
                       font-weight:bold;"">
                    Open Issue
                </a>
            </p>

            <p style=""font-size:12px; color:#666; margin-top:20px;"">
                If the button does not work, copy and paste this link into your browser:<br/>
                <span>{teamLeadUrl}</span>
            </p>

        </div>",
                ToRecipients = new List<string> { userEmail },
                SaveToSentItems = true
            };

            if (file != null && file.Length > 0)
            {
                stage = $"Adding attachment. FileName={file.FileName}, Size={file.Length}, WarningId={warningId}";

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);

                req.Attachments.Add(new EmailAttachment
                {
                    FileName = file.FileName,
                    ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                        ? "application/octet-stream"
                        : file.ContentType,
                    ContentBytes = ms.ToArray()
                });
            }

            stage = $"Sending email. WarningId={warningId}, To={userEmail}, Attachments={req.Attachments.Count}";

            await _graphUserDataAccess.SendEmailAsync(req);

            stage = $"Email sent successfully. WarningId={warningId}, To={userEmail}";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to send more information required email. Stage: {stage}. WarningId={warningId}. Error: {ex.Message}",
                ex);
        }
    }

    public async Task SendMoreInformationAddedEmailAsync(
        long warningId,
        string? noteText,
        IReadOnlyCollection<IFormFile>? files = null)
    {
        var stage = "Starting";

        try
        {
            stage = "Validating input";

            if (warningId is 0)
                throw new ArgumentException("Invalid warning id.", nameof(warningId));

            var validFiles = files?
                .Where(x => x != null && x.Length > 0)
                .ToList() ?? new List<IFormFile>();

            if (string.IsNullOrWhiteSpace(noteText) && !validFiles.Any())
                throw new ArgumentException("A note or at least one file is required.");

            stage = $"Getting warning info. WarningId={warningId}";

            var warningInfo = await _data.GetWarningByIdAsync(warningId);

            if (warningInfo == null)
                throw new InvalidOperationException($"Warning not found. WarningId={warningId}");

            stage = $"Getting Legal recipients from settings. WarningId={warningId}";

            var to = _emailSettings.LegalRecipients;

            if (to == null || !to.Any() || to.All(x => string.IsNullOrWhiteSpace(x)))
            {
                throw new InvalidOperationException(
                    $"Legal recipients are not configured. WarningId={warningId}");
            }

            var legalRecipients = to
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var legalUsername = "Legal Team";

            stage = $"Getting employee display name. EmployeeId={warningInfo.EmployeeId}, WarningId={warningId}";

            var employeeDisplayName = await GetDisplayNameFromUserIdAsync(warningInfo.EmployeeId);

            if (string.IsNullOrWhiteSpace(employeeDisplayName))
                employeeDisplayName = warningInfo.EmployeeId;

            stage = $"Getting submitted by display name. CreatedBy={warningInfo.CreatedBy}, WarningId={warningId}";

            var submittedByDisplayName = "Unknown";

            if (!string.IsNullOrWhiteSpace(warningInfo.CreatedBy))
            {
                submittedByDisplayName = await GetDisplayNameFromUserIdAsync(warningInfo.CreatedBy);

                if (string.IsNullOrWhiteSpace(submittedByDisplayName))
                    submittedByDisplayName = warningInfo.CreatedBy;
            }

            stage = $"Building email request. WarningId={warningId}, To={string.Join(",", legalRecipients)}";

            if (string.IsNullOrWhiteSpace(_applicationOptions.BaseUrl))
            {
                throw new InvalidOperationException(
                    "The application base URL has not been configured.");
            }

            var legalUrl =
                $"{_applicationOptions.BaseUrl.TrimEnd('/')}" +
                $"/Legal/Index/{warningInfo.WarningId}";

            var noteDisplay = string.IsNullOrWhiteSpace(noteText)
                ? "No note was added."
                : noteText.Trim();

            var uploadedFilesDisplay = validFiles.Any()
                ? string.Join("<br/>", validFiles.Select(x => System.Net.WebUtility.HtmlEncode(x.FileName)))
                : "No documents were uploaded.";

            var safeNoteText = System.Net.WebUtility.HtmlEncode(noteDisplay);
            var safeLegalUsername = System.Net.WebUtility.HtmlEncode(legalUsername);
            var safeEmployeeDisplayName = System.Net.WebUtility.HtmlEncode(employeeDisplayName);
            var safeSubmittedByDisplayName = System.Net.WebUtility.HtmlEncode(submittedByDisplayName);
            var safeCategory = System.Net.WebUtility.HtmlEncode(
                warningInfo.Category.Name ?? warningInfo.CategoryId.ToString());
            var safeStatus = System.Net.WebUtility.HtmlEncode(warningInfo.Status ?? "");
            var safeLegalUrl = System.Net.WebUtility.HtmlEncode(legalUrl);

            var req = new SendEmailRequest
            {
                Subject = $"Warning System | More information added - Issue {warningInfo.WarningId}",
                BodyHtml = $@"
                <div style=""font-family: Arial, sans-serif; color:#333; line-height:1.5;"">

                    <h2 style=""color:#014678; margin-bottom:8px;"">
                        More Information Added
                    </h2>

                    <p>Hi {safeLegalUsername},</p>

                    <p>
                        The requested information has been added for issue
                        <strong>#{warningInfo.WarningId}</strong>.
                    </p>

                    <table style=""border-collapse:collapse; margin-top:15px; margin-bottom:15px; width:100%; max-width:650px;"">
                        <tr>
                            <td style=""padding:8px; border:1px solid #ddd; font-weight:bold; background:#f8f9fa;"">
                                Employee
                            </td>
                            <td style=""padding:8px; border:1px solid #ddd;"">
                                {safeEmployeeDisplayName}
                            </td>
                        </tr>
                        <tr>
                            <td style=""padding:8px; border:1px solid #ddd; font-weight:bold; background:#f8f9fa;"">
                                Submitted By
                            </td>
                            <td style=""padding:8px; border:1px solid #ddd;"">
                                {safeSubmittedByDisplayName}
                            </td>
                        </tr>
                        <tr>
                            <td style=""padding:8px; border:1px solid #ddd; font-weight:bold; background:#f8f9fa;"">
                                Category
                            </td>
                            <td style=""padding:8px; border:1px solid #ddd;"">
                                {safeCategory}
                            </td>
                        </tr>
                        <tr>
                            <td style=""padding:8px; border:1px solid #ddd; font-weight:bold; background:#f8f9fa;"">
                                Current Status
                            </td>
                            <td style=""padding:8px; border:1px solid #ddd;"">
                                {safeStatus}
                            </td>
                        </tr>
                    </table>

                    <p style=""margin-bottom:6px;""><strong>Information Added:</strong></p>

                    <div style=""
                        border:1px solid #ddd;
                        background:#f8f9fa;
                        padding:12px;
                        border-radius:6px;
                        white-space:pre-wrap;
                        margin-bottom:18px;"">
                        {safeNoteText}
                    </div>

                    <p style=""margin-bottom:6px;""><strong>Documents Uploaded:</strong></p>

                    <div style=""
                        border:1px solid #ddd;
                        background:#f8f9fa;
                        padding:12px;
                        border-radius:6px;
                        margin-bottom:18px;"">
                        {uploadedFilesDisplay}
                    </div>

                    <p>
                        Please open the link below to review the submitted information:
                    </p>

                    <p>
                        <a href=""{safeLegalUrl}""
                           style=""
                               display:inline-block;
                               padding:10px 18px;
                               background:#014678;
                               color:#ffffff;
                               text-decoration:none;
                               border-radius:999px;
                               font-weight:bold;"">
                            Review Issue
                        </a>
                    </p>

                    <p style=""font-size:12px; color:#666; margin-top:20px;"">
                        If the button does not work, copy and paste this link into your browser:<br/>
                        <span>{safeLegalUrl}</span>
                    </p>

                </div>",
                ToRecipients = legalRecipients,
                SaveToSentItems = true
            };

            foreach (var file in validFiles)
            {
                stage = $"Adding attachment. FileName={file.FileName}, Size={file.Length}, WarningId={warningId}";

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);

                req.Attachments.Add(new EmailAttachment
                {
                    FileName = file.FileName,
                    ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                        ? "application/octet-stream"
                        : file.ContentType,
                    ContentBytes = ms.ToArray()
                });
            }

            stage = $"Sending email. WarningId={warningId}, To={string.Join(",", legalRecipients)}, Attachments={req.Attachments.Count}";

            await _graphUserDataAccess.SendEmailAsync(req);

            stage = $"Email sent successfully. WarningId={warningId}, To={string.Join(",", legalRecipients)}";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to send more information added email. Stage: {stage}. WarningId={warningId}. Error: {ex.Message}",
                ex);
        }
    }

    private async Task SendLegalIssueCompletedEmailAsync(long warningId)
    {
        var stage = "Starting";

        try
        {
            stage = "Validating input";

            if (warningId is <= 0)
            {
                throw new ArgumentException(
                    "Invalid warning id.",
                    nameof(warningId));
            }

            stage = $"Getting warning info. WarningId={warningId}";

            var warningInfo =
                await _data.GetWarningByIdAsync(warningId);

            if (warningInfo is null)
            {
                throw new InvalidOperationException(
                    $"Warning not found. WarningId={warningId}");
            }

            stage =
                $"Getting Legal recipients from settings. WarningId={warningId}";

            var legalRecipients = (_emailSettings.LegalRecipients ?? [])
                .Where(recipient =>
                    !string.IsNullOrWhiteSpace(recipient))
                .Select(recipient => recipient.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (legalRecipients.Count is 0)
            {
                throw new InvalidOperationException(
                    $"Legal recipients are not configured. WarningId={warningId}");
            }

            stage =
                $"Getting employee display name. EmployeeId={warningInfo.EmployeeId}, WarningId={warningId}";

            var employeeDisplayName =
                await GetDisplayNameFromUserIdAsync(
                    warningInfo.EmployeeId);

            if (string.IsNullOrWhiteSpace(employeeDisplayName))
            {
                employeeDisplayName = warningInfo.EmployeeId;
            }

            stage =
                $"Getting submitted by display name. CreatedBy={warningInfo.CreatedBy}, WarningId={warningId}";

            var submittedByDisplayName =
                await GetDisplayNameFromUserIdAsync(
                    warningInfo.CreatedBy);

            if (string.IsNullOrWhiteSpace(submittedByDisplayName))
            {
                submittedByDisplayName = warningInfo.CreatedBy;
            }

            stage = $"Building Legal URL. WarningId={warningId}";

            if (string.IsNullOrWhiteSpace(
                    _applicationOptions.BaseUrl))
            {
                throw new InvalidOperationException(
                    "The application base URL has not been configured.");
            }

            var legalUrl =
                $"{_applicationOptions.BaseUrl.TrimEnd('/')}" +
                $"/Legal/Index/{warningInfo.WarningId}";

            var categoryName =
                warningInfo.Category?.Name
                ?? warningInfo.CategoryId.ToString();

            stage =
                $"Rendering email template. WarningId={warningId}";

            var bodyHtml = _emailTemplateRenderer.Render(
                "LegalActionRequired",
                new Dictionary<string, string?>
                {
                    ["LegalUsername"] = "Legal Team",
                    ["WarningId"] =
                        warningInfo.WarningId.ToString(),
                    ["EmployeeDisplayName"] =
                        employeeDisplayName,
                    ["SubmittedByDisplayName"] =
                        submittedByDisplayName,
                    ["Category"] = categoryName,
                    ["Status"] = warningInfo.Status,
                    ["Type"] = warningInfo.Type,
                    ["WarningSubtype"] = string.IsNullOrWhiteSpace(warningInfo.WarningSubtype)
                        ? "—"
                        : warningInfo.WarningSubtype,
                    ["LegalUrl"] = legalUrl
                });

            var request = new SendEmailRequest
            {
                Subject =
                    $"Issue #{warningInfo.WarningId} issued to employee - Legal action required",

                BodyHtml = bodyHtml,

                ToRecipients = legalRecipients,

                SaveToSentItems = true
            };

            stage =
                $"Sending email. WarningId={warningId}, To={string.Join(",", legalRecipients)}";

            await _graphUserDataAccess.SendEmailAsync(request);

            stage =
                $"Email sent successfully. WarningId={warningId}, To={string.Join(",", legalRecipients)}";
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Failed to send legal issue completed email. " +
                $"Stage: {stage}. " +
                $"WarningId={warningId}. " +
                $"Error: {exception.Message}",
                exception);
        }
    }
    private async Task<string> GetDisplayNameFromUserIdAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return string.Empty;

        var trimmedUserId = userId.Trim();

        var user = await _graphUserDataAccess.GetUserAsync(trimmedUserId);

        if (user == null)
            return trimmedUserId;

        return !string.IsNullOrWhiteSpace(user.DisplayName)
            ? user.DisplayName
            : trimmedUserId;
    }

    public async Task AddTeamLeadEvidenceAsync(
        AddTeamLeadEvidenceVm model)
    {
        if (model.WarningId <= 0)
        {
            throw new ArgumentException(
                "Invalid warning id.",
                nameof(model.WarningId));
        }

        var userId = string.IsNullOrWhiteSpace(_currentUser?.ObjectId)
            ? "system"
            : _currentUser.ObjectId;

        var now = NowSast;

        var evidenceToSave = new List<WarningEvidence>();

        if (model.Files != null)
        {
            foreach (var file in model.Files.Where(file => file.Length > 0))
            {
                var uploadedFileName =
                    await _fileStorage.UploadWarningFileAsync(
                        checked((int)model.WarningId),
                        "TeamLeadEvidence",
                        file);

                evidenceToSave.Add(new WarningEvidence
                {
                    WarningId = model.WarningId,
                    FileName = uploadedFileName,
                    FileType = file.ContentType,
                    FileSizeBytes = file.Length,
                    StorageUrl = uploadedFileName,
                    UploadedBy = userId,
                    UploadedOn = now
                });
            }
        }

        var noteText = string.IsNullOrWhiteSpace(model.NoteText)
            ? null
            : model.NoteText.Trim();

        WarningNote? note = null;

        if (noteText != null)
        {
            note = new WarningNote
            {
                WarningId = model.WarningId,
                NoteText = noteText,
                CreatedBy = userId,
                CreatedOn = now
            };
        }

        await _data.SaveTeamLeadEvidenceAsync(
            model.WarningId,
            evidenceToSave,
            note);

        await SendMoreInformationAddedEmailAsync(
            model.WarningId,
            noteText,
            model.Files);
    }

    public async Task NotifyLegalIssueCompletedAsync(long warningId)
    {
        var stage = "Starting";

        try
        {
            stage = "Validating input";

            if (warningId is 0)
                throw new ArgumentException("Invalid warning id.", nameof(warningId));

            stage = $"Getting warning info. WarningId={warningId}";

            var warningInfo = await _data.GetWarningByIdAsync(warningId);

            if (warningInfo == null)
                throw new InvalidOperationException($"Warning not found. WarningId={warningId}");

            await _data.AdvanceWarningStatusAsync(
                warningId, IssueStatusGroup.EmployeeAction, _currentUser.ObjectId);

            stage = $"Sending legal completed email. WarningId={warningId}";

            await SendLegalIssueCompletedEmailAsync(warningId);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to notify Legal issue completed. Stage: {stage}. WarningId={warningId}. Error: {ex.Message}",
                ex);
        }
    }
}
