namespace WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;

public class LookupIssueSubType
{
    public int IssueSubTypeId { get; set; }
    public int IssueTypeId { get; set; }
    public string IssueSubTypeName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public LookupIssueType IssueType { get; set; } = null!;
}
