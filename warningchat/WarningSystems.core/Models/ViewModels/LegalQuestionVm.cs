namespace WarningSystems.Core.ViewModels;

public class LegalQuestionVm
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = "";
    public string ControlType { get; set; } = "";
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }

    public string? DefaultConfigJson { get; set; }
    public string? CategoryConfigJson { get; set; }

    public string? AnswerText { get; set; }
    public string? AnswerJson { get; set; }
}
