using Microsoft.AspNetCore.Http;

namespace WarningSystems.Models.DTO;

public class SaveAttachmentsDto
{
    public int WarningId { get; set; }
    public string? AttachmentType { get; set; }
    public string? AttachmentName { get; set; }
    public List<IFormFile> Files { get; set; } = [];
    public string? Notes { get; set; }
    public int Typeid { get; set; }
}