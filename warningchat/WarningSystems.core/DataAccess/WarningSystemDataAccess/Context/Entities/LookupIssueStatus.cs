using WarningSystems.Core.Models.Enum;

namespace WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;

public class LookupIssueStatus
{
    public int IssueStatusId { get; set; }
    public string IssueStatusName { get; set; } = string.Empty;
    public IssueStatusGroup IssueStatusGroup { get; set; }
    public int? NextIssueStatusId { get; set; }
    public bool IsActive { get; set; } = true;

    public LookupIssueStatus? NextIssueStatus { get; set; }
}
