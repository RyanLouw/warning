namespace WarningSystems.Models.DTO;

public record CreateQuestionDto(
    string QuestionText,
    string ControlType,
    string? DefaultConfigJson = null,
    bool? IsActive = true
);
