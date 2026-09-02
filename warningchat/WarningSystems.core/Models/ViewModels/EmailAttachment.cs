namespace WarningSystems.Core.ViewModels;

public class EmailAttachment
{
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "application/octet-stream";
    public byte[] ContentBytes { get; set; } = Array.Empty<byte>();
}