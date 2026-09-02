namespace WarningSystems.Models.Validation;

public class SaveIssueStepResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public long WarningId { get; set; }

    public static SaveIssueStepResult Ok(long warningId)
    {
        return new SaveIssueStepResult
        {
            Success = true,
            WarningId = warningId
        };
    }

    public static SaveIssueStepResult Fail(string message)
    {
        return new SaveIssueStepResult
        {
            Success = false,
            Message = message
        };
    }
}