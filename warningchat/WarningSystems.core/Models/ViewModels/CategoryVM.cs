using WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;

namespace WarningSystems.Core.ViewModels;

public class CategoryVM
{
    public CategoryVM()
    {
    }

    public CategoryVM(
        TransgressionCategory entity,
        string? createdByDisplayName = null,
        string? updatedByDisplayName = null)
    {
        CategoryId = entity.CategoryId;
        Name = entity.Name;
        IsActive = entity.IsActive;
        CreatedOn = entity.CreatedOn;

        CreatedBy = createdByDisplayName
                    ?? entity.CreatedBy;

        UpdatedOn = entity.UpdatedOn;

        UpdatedBy = updatedByDisplayName
                    ?? entity.UpdatedBy;
    }

    public int CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedOn { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? UpdatedOn { get; set; }

    public string? UpdatedBy { get; set; }
}