namespace WarningSystems.Models.DTO;

public record UpdateQuestionDto(
    int QuestionId,
    string QuestionText,
    string ControlType,
    string? DefaultConfigJson,
    bool IsActive
);
