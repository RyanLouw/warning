using System.ComponentModel.DataAnnotations;

namespace WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;

public class AdminAuditLog
{
    public long AuditId { get; set; }
    public string EntityName { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string Action { get; set; } = null!;
    public DateTime ChangedOn { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(100)]
    public string ChangedBy { get; set; } = null!;

    [MaxLength(400)]
    public string? Summary { get; set; }

    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
}



