using WarningSystems.Core.DataAccess.SOPDataAccess.Context.Entity;

namespace WarningSystems.Core.ViewModels;

public class SopDocumentLiteVm
{
    public SopDocumentLiteVm()
    {
    }

    public SopDocumentLiteVm(SopDocument entity)
    {
        SOPDocumentId = entity.SOPDocumentId;
       
        SOPDocumentCategoryId = entity.SOPDocumentCategoryId;

        DocumentName = $"{entity.SOPDocumentCategoryId} - {entity.DocumentName}";

    }

    public int SOPDocumentId { get; set; }
    public int SOPDocumentCategoryId { get; set; }

    public string DocumentName { get; set; } = string.Empty;
}