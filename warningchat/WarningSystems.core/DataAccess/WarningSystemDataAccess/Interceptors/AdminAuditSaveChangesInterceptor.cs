using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;
using WarningSystems.core.DataAccess.WarningSystemDataAccess.Context.Entities;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;

namespace WarningSystems.Core.DataAccess.WarningSystemDataAccess.Interceptors;

public sealed class AdminAuditSaveChangesInterceptor
    : SaveChangesInterceptor
{
    private static readonly HashSet<string> IgnoredProperties =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "CreatedOn",
            "CreatedBy",
            "UpdatedOn",
            "UpdatedBy"
        };

    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminAuditSaveChangesInterceptor(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AddAuditLogs(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddAuditLogs(eventData.Context);

        return base.SavingChangesAsync(
            eventData,
            result,
            cancellationToken);
    }

    private void AddAuditLogs(DbContext? context)
    {
        if (context is null)
            return;

        // Ensure EF has detected the property changes.
        context.ChangeTracker.DetectChanges();

        var auditedEntries = context.ChangeTracker
            .Entries()
            .Where(entry =>
                entry.Entity is IAutomaticallyAuditedEntity &&
                entry.State == EntityState.Modified)
            .ToList();

        foreach (var entry in auditedEntries)
        {
            AddUpdateAuditLog(context, entry);
        }
    }

    private void AddUpdateAuditLog(
        DbContext context,
        EntityEntry entry)
    {
        var modifiedProperties = entry.Properties
            .Where(property =>
                property.IsModified &&
                !IgnoredProperties.Contains(property.Metadata.Name))
            .ToList();

        // Do not create an audit record when no meaningful fields changed.
        if (modifiedProperties.Count == 0)
            return;

        var oldValues = modifiedProperties.ToDictionary(
            property => property.Metadata.Name,
            property => property.OriginalValue);

        var newValues = modifiedProperties.ToDictionary(
            property => property.Metadata.Name,
            property => property.CurrentValue);

        var changedFields = modifiedProperties
            .Select(property => property.Metadata.Name)
            .ToList();

        context.Set<AdminAuditLog>().Add(new AdminAuditLog
        {
            EntityName = entry.Metadata.ClrType.Name,
            EntityId = GetEntityId(entry),
            Action = "UPDATE",
            ChangedBy = GetChangedBy(entry),
            ChangedOn = DateTime.UtcNow,
            Summary =
                $"Updated fields: {string.Join(", ", changedFields)}",
            OldValues = JsonSerializer.Serialize(oldValues),
            NewValues = JsonSerializer.Serialize(newValues)
        });
    }

    private static string GetEntityId(EntityEntry entry)
    {
        var primaryKey = entry.Metadata.FindPrimaryKey();

        if (primaryKey is null)
            return string.Empty;

        return string.Join(
            ":",
            primaryKey.Properties.Select(property =>
                entry.Property(property.Name)
                    .CurrentValue?
                    .ToString() ?? string.Empty));
    }

    private string GetChangedBy(EntityEntry entry)
    {
        var updatedBy = entry.Properties
            .FirstOrDefault(property =>
                property.Metadata.Name.Equals(
                    "UpdatedBy",
                    StringComparison.OrdinalIgnoreCase))
            ?.CurrentValue?
            .ToString();

        if (!string.IsNullOrWhiteSpace(updatedBy))
            return updatedBy;

        return _httpContextAccessor
                   .HttpContext?
                   .User?
                   .Identity?
                   .Name
               ?? "System";
    }
}