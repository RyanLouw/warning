using Microsoft.EntityFrameworkCore;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;

namespace WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context;

public class WarningSystemDbContext : DbContext
{
    public WarningSystemDbContext(DbContextOptions<WarningSystemDbContext> options)
        : base(options)
    {
    }

    public DbSet<TransgressionCategory> TransgressionCategories => Set<TransgressionCategory>();

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<CategoryQuestion> CategoryQuestions => Set<CategoryQuestion>();
    public DbSet<NoteTypeLookup> NoteTypeLookups => Set<NoteTypeLookup>();
    public DbSet<Warning> Warnings => Set<Warning>();
    public DbSet<WarningAnswer> WarningAnswers => Set<WarningAnswer>();
    public DbSet<WarningEvidence> WarningEvidence => Set<WarningEvidence>();

    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

    public DbSet<WarningCategory> WarningCategories => Set<WarningCategory>();
    public DbSet<WarningNote> WarningNotes => Set<WarningNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ws");

        // --------------------------
        // TransgressionCategory
        // --------------------------
        modelBuilder.Entity<TransgressionCategory>(e =>
        {
            e.ToTable("TransgressionCategory");
            e.HasKey(x => x.CategoryId);

            e.Property(x => x.Name)
                .IsUnicode(false)
                .IsRequired();

            e.Property(x => x.IsActive)
                .HasDefaultValue(true);

            e.Property(x => x.CreatedOn)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            e.Property(x => x.CreatedBy)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasDefaultValue("system")
                .IsRequired();

            e.Property(x => x.UpdatedOn);

            e.Property(x => x.UpdatedBy)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        // --------------------------
        // Question (MASTER BANK)
        // --------------------------
        modelBuilder.Entity<Question>(e =>
        {
            e.ToTable("Question");
            e.HasKey(x => x.QuestionId);

            e.Property(x => x.QuestionText)
                .HasMaxLength(400)
                .IsUnicode(false)
                .IsRequired();

            e.Property(x => x.ControlType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .IsRequired();

            e.Property(x => x.DefaultConfigJson)
                .IsUnicode(false);

            e.Property(x => x.IsActive)
                .IsRequired()
                .ValueGeneratedNever();

            // Audit fields
            e.Property(x => x.CreatedOn)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            e.Property(x => x.CreatedBy)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasDefaultValue("system")
                .IsRequired();

            e.Property(x => x.UpdatedOn);

            e.Property(x => x.UpdatedBy)
                .HasMaxLength(100)
                .IsUnicode(false);

            // UQ_Question UNIQUE (QuestionText, ControlType)
            e.HasIndex(x => new { x.QuestionText, x.ControlType })
                .IsUnique()
                .HasDatabaseName("UQ_Question");
        });

        // --------------------------
        // CategoryQuestion (LINK TABLE)
        // --------------------------
        modelBuilder.Entity<CategoryQuestion>(e =>
        {
            e.ToTable("CategoryQuestion", "ws");
            e.HasKey(x => new { x.CategoryId, x.QuestionId });

            e.Property(x => x.IsRequired)
                .IsRequired()
                .ValueGeneratedNever();

            e.Property(x => x.SortOrder)
                .IsRequired()
                .ValueGeneratedNever();

            e.Property(x => x.IsActive)
                .IsRequired()
                .ValueGeneratedNever();

            e.Property(x => x.ConfigJson)
                .IsUnicode(false);

            e.Property(x => x.CreatedOn)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            e.Property(x => x.CreatedBy)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasDefaultValue("system")
                .IsRequired();

            e.Property(x => x.UpdatedOn);

            e.Property(x => x.UpdatedBy)
                .HasMaxLength(100)
                .IsUnicode(false);

            e.HasOne(x => x.Category)
                .WithMany(c => c.CategoryQuestions)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CategoryQuestion_Category");

            e.HasOne(x => x.Question)
                .WithMany(q => q.CategoryLinks)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CategoryQuestion_Question");

            e.HasIndex(x => new { x.CategoryId, x.SortOrder })
                .HasDatabaseName("IX_CategoryQuestion_Category_Sort");
        });

        // --------------------------
        // Warning
        // --------------------------
        modelBuilder.Entity<Warning>(e =>
        {
            e.ToTable("Warning");
            e.HasKey(x => x.WarningId);

            e.Property(x => x.EmployeeId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .IsRequired();

            e.Property(x => x.CreatedBy)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired();

            e.Property(x => x.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            e.Property(x => x.Type)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Issue")
                .IsRequired();

            e.Property(x => x.WarningSubtype)
                .HasMaxLength(30)
                .IsUnicode(false);

            e.Property(x => x.CreatedOn)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            e.Property(x => x.LastStatusChangedBy)
                .HasMaxLength(100)
                .IsUnicode(false);

            e.HasOne(x => x.Category)
                .WithMany(c => c.Warnings)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Warning_Category");

            e.Property(x => x.Completed)
                .HasDefaultValue(false)
                .IsRequired();

            e.Property(x => x.IsDeleted)
                .HasDefaultValue(false)
                .IsRequired();

            e.Property(x => x.HideFromTeamLead)
                .HasDefaultValue(false)
                .IsRequired();
        });

        // --------------------------
        // WarningAnswer
        // --------------------------
        modelBuilder.Entity<WarningAnswer>(e =>
        {
            e.ToTable("WarningAnswer");
            e.HasKey(x => x.AnswerId);

            e.Property(x => x.AnswerText).IsUnicode(false);
            e.Property(x => x.AnswerJson).IsUnicode(false);

            e.Property(x => x.CreatedOn)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            // UQ_WarningAnswer UNIQUE (WarningId, QuestionId)
            e.HasIndex(x => new { x.WarningId, x.QuestionId })
                .IsUnique()
                .HasDatabaseName("UQ_WarningAnswer");

            e.HasOne(x => x.Warning)
                .WithMany(w => w.Answers)
                .HasForeignKey(x => x.WarningId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_WarningAnswer_Warning");

            // IMPORTANT CHANGE: now points to ws.Question (not ws.CategoryQuestion)
            e.HasOne(x => x.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_WarningAnswer_Question");
        });

        // --------------------------
        // WarningEvidence
        // --------------------------
        modelBuilder.Entity<WarningEvidence>(e =>
        {
            e.ToTable("WarningEvidence");
            e.HasKey(x => x.EvidenceId);

            e.Property(x => x.FileName)
                .HasMaxLength(260)
                .IsUnicode(false)
                .IsRequired();

            e.Property(x => x.FileType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            e.Property(x => x.StorageUrl)
                .HasMaxLength(2048)
                .IsUnicode(false)
                .IsRequired();

            e.Property(x => x.UploadedBy)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired();

            e.Property(x => x.UploadedOn)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(x => x.Warning)
                .WithMany(w => w.Evidence)
                .HasForeignKey(x => x.WarningId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_WarningEvidence_Warning");
        });

        modelBuilder.Entity<AdminAuditLog>(e =>
        {
            e.ToTable("AdminAuditLog");
            e.HasKey(x => x.AuditId);

            e.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
            e.Property(x => x.EntityId).HasMaxLength(50).IsRequired();
            e.Property(x => x.Action).HasMaxLength(20).IsRequired();
            e.Property(x => x.ChangedBy).HasMaxLength(100).IsRequired();
            e.Property(x => x.Summary).HasMaxLength(400);

            e.Property(x => x.ChangedOn)
                .HasPrecision(0)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasIndex(x => new { x.EntityName, x.EntityId, x.ChangedOn })
                .HasDatabaseName("IX_AdminAuditLog_Entity");
        });

        modelBuilder.Entity<WarningCategory>(entity =>
        {
            entity.ToTable("WarningCategory", "ws");

            entity.HasKey(x => new { x.WarningId, x.CategoryId });

            entity.Property(x => x.CreatedBy)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.CreatedOn)
                .IsRequired();

            entity.HasOne(x => x.Warning)
                .WithMany(w => w.WarningCategories)
                .HasForeignKey(x => x.WarningId);

            entity.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId);
        });

        // --------------------------
        // WarningNote
        // --------------------------

        modelBuilder.Entity<WarningNote>(e =>
        {
            e.ToTable("WarningNote", "ws");

            e.HasKey(x => x.NoteId);

            e.Property(x => x.NoteText)
                .IsUnicode(false)
                .IsRequired();

            e.Property(x => x.CreatedBy)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired();

            e.Property(x => x.CreatedOn)
                .HasPrecision(0)
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .IsRequired();

            e.Property(x => x.NoteTypeId)
                .IsRequired(false);

            e.HasOne(x => x.Warning)
                .WithMany(w => w.Notes)
                .HasForeignKey(x => x.WarningId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_WarningNote_Warning");

            e.HasOne(x => x.Evidence)
                .WithMany(evidence => evidence.Notes)
                .HasForeignKey(x => x.EvidenceId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_WarningNote_Evidence");

            e.HasOne(x => x.NoteType)
                .WithMany(noteType => noteType.WarningNotes)
                .HasForeignKey(x => x.NoteTypeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_WarningNote_NoteTypeLookup");

            e.HasIndex(x => new
            {
                x.WarningId,
                x.CreatedOn
            })
            .HasDatabaseName("IX_WarningNote_WarningId_CreatedOn");

            e.HasIndex(x => x.EvidenceId)
                .HasDatabaseName("IX_WarningNote_EvidenceId");

            e.HasIndex(x => x.NoteTypeId)
                .HasDatabaseName("IX_WarningNote_NoteTypeId");
        });

        // --------------------------
        // NoteTypeLookup
        // --------------------------
        modelBuilder.Entity<NoteTypeLookup>(e =>
        {
            e.ToTable("NoteTypeLookup");
            e.HasKey(x => x.NoteTypeId);

            e.Property(x => x.Name)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired();

            e.Property(x => x.CreatedBy)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasDefaultValue("system")
                .IsRequired();

            e.Property(x => x.CreatedOn)
                .HasPrecision(0)
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .IsRequired();

            e.Property(x => x.IsActive)
                .HasDefaultValue(true)
                .IsRequired();

            e.Property(x => x.IsSystemOnly)
                .HasDefaultValue(false)
                .IsRequired();

            e.HasIndex(x => x.Name)
                .IsUnique()
                .HasDatabaseName("UQ_NoteTypeLookup_Name");
        });
    }
}
