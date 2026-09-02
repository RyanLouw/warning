namespace WarningSystems.Core.ViewModels;

public class WarningQuestionVm
{
    public int QuestionId { get; set; }

    public string QuestionText { get; set; } = "";
    public string ControlType { get; set; } = "";
    public string? DefaultConfigJson { get; set; }
    public bool IsQuestionActive { get; set; } = true;

    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public string? ConfigJson { get; set; }
    public bool IsCategoryLinkActive { get; set; } = true;

    public string? EffectiveConfigJson => !string.IsNullOrWhiteSpace(ConfigJson) ? ConfigJson : DefaultConfigJson;

    public string? AnswerText { get; set; }
    public string? AnswerJson { get; set; }

    public bool HasAnswer =>
        !string.IsNullOrWhiteSpace(AnswerText) || !string.IsNullOrWhiteSpace(AnswerJson);

    public bool IsVisible => IsQuestionActive && IsCategoryLinkActive;

    public int DisplayOrder => SortOrder;
}
