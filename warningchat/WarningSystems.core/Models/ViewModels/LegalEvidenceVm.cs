namespace WarningSystems.Core.ViewModels;

public class LegalEvidenceVm
{
    public long EvidenceId { get; set; }
    public string FileName { get; set; } = "";
    public string FileType { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public string StorageUrl { get; set; } = "";
    public string UploadedBy { get; set; } = "";
    public string UploadedByDisplayName { get; set; } = "";
    public DateTime UploadedOn { get; set; }
    public List<WarningNoteVm> Notes { get; set; } = [];
}
