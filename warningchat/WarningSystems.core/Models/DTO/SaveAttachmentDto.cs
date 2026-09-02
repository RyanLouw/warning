using Microsoft.AspNetCore.Http;

namespace WarningSystems.Models.DTO;

public class SaveAttachmentDto
{
    public int WarningId { get; set; }
    public string AttachmentType { get; set; }
    public string AttachmentName { get; set; }
    public IFormFile File { get; set; }
}