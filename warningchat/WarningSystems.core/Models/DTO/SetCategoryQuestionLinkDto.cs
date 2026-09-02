namespace WarningSystems.Models.DTO;

public class SetCategoryQuestionLinkDto
{
    public int CategoryId { get; set; }
    public int QuestionId { get; set; }

    public bool IsActive { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public string? ConfigJson { get; set; }
}