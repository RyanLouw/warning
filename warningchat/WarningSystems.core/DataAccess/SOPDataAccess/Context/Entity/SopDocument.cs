namespace WarningSystems.Core.DataAccess.SOPDataAccess.Context.Entity;

public class SopDocument
{
    public int SOPDocumentId { get; set; }
    public int SOPDocumentCategoryId { get; set; }
    public string DocumentNumber { get; set; }
    public string DocumentName { get; set; }
    public DateTime? ReviewDate { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string VersionNumber { get; set; }
    public int Status { get; set; }
    public string SharePointId { get; set; }
    public string PlannerTaskId { get; set; }
}