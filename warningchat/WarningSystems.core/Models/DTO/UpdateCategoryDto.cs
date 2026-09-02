namespace WarningSystems.Models.DTO;

public record UpdateCategoryDto(
    int CategoryId,
    string Name,
    bool IsActive
);
