using WarningSystems.Core.Models.Enum;

namespace WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;

public class LookupIssueType
{
    public int IssueTypeId { get; set; }
    public string IssueTypeName { get; set; } = string.Empty;
    public SubTypeSelectionMode SubTypeSelectionMode { get; set; }
    public int ResultIssueStatusId { get; set; }
    public bool IsActive { get; set; } = true;

    public LookupIssueStatus ResultIssueStatus { get; set; } = null!;
    public ICollection<LookupIssueSubType> IssueSubTypes { get; set; } = [];
}
