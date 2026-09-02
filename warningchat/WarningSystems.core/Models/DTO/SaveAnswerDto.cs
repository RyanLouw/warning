namespace WarningSystems.Models.DTO;

public class SaveAnswerDto
{
    public long WarningId { get; set; }
    public int QuestionId { get; set; }
    public string? AnswerText { get; set; }
    public string? AnswerJson { get; set; }
}
