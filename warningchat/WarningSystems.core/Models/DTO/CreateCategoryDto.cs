namespace WarningSystems.Models.DTO;

public record CreateCategoryDto(
    string Name,
    bool IsActive = true
);
