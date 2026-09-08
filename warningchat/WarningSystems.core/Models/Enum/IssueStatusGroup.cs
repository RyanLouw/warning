namespace WarningSystems.Core.Models.Enum;

public enum IssueStatusGroup : byte
{
    Draft = 1,
    LegalReview = 2,
    LegalDecision = 3,
    EmployeeAction = 4,
    LegalValidation = 5,
    Closed = 6
}
