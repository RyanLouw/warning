namespace WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;

public class WarningEvidence
{
    public long EvidenceId { get; set; }
    public long WarningId { get; set; }
    public string FileName { get; set; } = "";
    public string FileType { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public string StorageUrl { get; set; } = "";
    public string UploadedBy { get; set; } = "";
    public DateTime UploadedOn { get; set; }
    public Warning? Warning { get; set; }

    public ICollection<WarningNote> Notes { get; set; } = new List<WarningNote>();
}