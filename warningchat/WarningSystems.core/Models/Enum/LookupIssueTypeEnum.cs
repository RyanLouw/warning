namespace WarningSystems.Core.Models.Enum;

// Seed identifiers only. Runtime options must always come from LookupIssueTypes
// so types added to the database do not require an application deployment.
public enum LookupIssueTypeEnum
{
    Invalid = 1,
    Warning = 2,
    Discussion = 3,
    Hearing = 4
}
