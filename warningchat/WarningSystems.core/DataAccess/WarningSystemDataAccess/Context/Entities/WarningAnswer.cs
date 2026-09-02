namespace WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;

public class WarningAnswer
{
    public long AnswerId { get; set; }

    public long WarningId { get; set; }

    public int QuestionId { get; set; }

    public string? AnswerText { get; set; }

    public string? AnswerJson { get; set; }

    public DateTime CreatedOn { get; set; }

    public Warning Warning { get; set; } = null!;

    public Question Question { get; set; } = null!;
}