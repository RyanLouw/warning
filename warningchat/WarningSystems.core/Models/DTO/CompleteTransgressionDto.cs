using Microsoft.AspNetCore.Http;

namespace WarningSystems.Models.DTO;

public class CompleteTransgressionDto
{
    public int WarningId { get; set; }
    public int IssueTypeId { get; set; }
    public int? IssueSubTypeId { get; set; }
    public IFormFile? File { get; set; }
}

public class UpdateWarningStatusDto
{
    public long WarningId { get; set; }
    public string Status { get; set; } = "";
    public string? DueDate { get; set; }
}
