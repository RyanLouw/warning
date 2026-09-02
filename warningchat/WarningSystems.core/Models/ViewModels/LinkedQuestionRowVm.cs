namespace WarningSystems.Core.ViewModels;

public class LinkedQuestionRowVm
{
    public int CategoryId { get; set; }
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = "";
    public string ControlType { get; set; } = "";
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public string? ConfigJson { get; set; }
}