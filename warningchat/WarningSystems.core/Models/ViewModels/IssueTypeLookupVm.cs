using WarningSystems.Core.Models.Enum;

namespace WarningSystems.Core.ViewModels;

public class IssueTypeLookupVm
{
    public int IssueTypeId { get; set; }
    public string IssueTypeName { get; set; } = string.Empty;
    public SubTypeSelectionMode SubTypeSelectionMode { get; set; }
    public List<IssueSubTypeLookupVm> SubTypes { get; set; } = [];
}

public class IssueSubTypeLookupVm
{
    public int IssueSubTypeId { get; set; }
    public string IssueSubTypeName { get; set; } = string.Empty;
}
