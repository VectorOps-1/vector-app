using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public sealed record ImportMappingProfileItem(string SourceHeading, string NormalizedSourceHeading, string? TargetFieldKey, bool IsIgnored);
public sealed record ImportRollbackResult(int Reversed, int Blocked, IReadOnlyList<string> BlockedReasons);

public sealed class ImportGovernanceService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly VectorDbContext _db;
    private readonly ImportBatchService _batches;

    public ImportGovernanceService(VectorDbContext db, ImportBatchService batches)
    {
        _db = db;
        _batches = batches;
    }

    public async Task<IReadOnlyList<ImportMappingProfile>> MatchingProfilesAsync(
        AppUser user,
        ImportBatch batch,
        CancellationToken cancellationToken = default)
    {
        var signature = HeadingSignature(batch.ColumnMappings);
        if (string.IsNullOrWhiteSpace(signature)) return [];
        return await _db.ImportMappingProfiles.AsNoTracking()
            .Where(profile => profile.CompanyId == user.CompanyId
                && profile.TargetType == batch.TargetType
                && profile.HeadingSignature == signature)
            .OrderBy(profile => profile.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveMappingProfileAsync(
        AppUser user,
        int batchId,
        string name,
        CancellationToken cancellationToken = default)
    {
        await RequirePrepareAsync(user, cancellationToken);
        name = name?.Trim() ?? string.Empty;
        if (name.Length is < 2 or > 160) throw new InvalidOperationException("Enter a mapping name between 2 and 160 characters.");
        var batch = await LoadBatchAsync(user, batchId, cancellationToken);
        if (batch.ColumnMappings.Count == 0 || batch.ColumnMappings.Any(mapping => !mapping.IsUserConfirmed))
            throw new InvalidOperationException("Confirm every source column before saving a reusable mapping.");
        var signature = HeadingSignature(batch.ColumnMappings);
        var items = batch.ColumnMappings.OrderBy(mapping => mapping.SourceColumnIndex).Select(mapping =>
            new ImportMappingProfileItem(mapping.SourceHeading, mapping.NormalizedSourceHeading, mapping.TargetFieldKey, mapping.IsIgnored)).ToList();
        var profile = await _db.ImportMappingProfiles.SingleOrDefaultAsync(item =>
            item.CompanyId == user.CompanyId && item.TargetType == batch.TargetType && item.Name == name, cancellationToken);
        var now = DateTime.UtcNow;
        if (profile is null)
        {
            profile = new ImportMappingProfile
            {
                CompanyId = user.CompanyId,
                TargetType = batch.TargetType,
                Name = name,
                CreatedByUserId = user.Id,
                CreatedAtUtc = now
            };
            _db.ImportMappingProfiles.Add(profile);
        }
        profile.HeadingSignature = signature;
        profile.MappingJson = JsonSerializer.Serialize(items, JsonOptions);
        profile.UpdatedAtUtc = now;
        AddAudit(user, "Import mapping saved", nameof(ImportMappingProfile), profile.Id,
            $"Saved mapping '{name}' for {batch.TargetType}; {items.Count} source columns.");
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReuseMappingProfileAsync(
        AppUser user,
        int batchId,
        int profileId,
        CancellationToken cancellationToken = default)
    {
        await RequirePrepareAsync(user, cancellationToken);
        var batch = await LoadBatchAsync(user, batchId, cancellationToken);
        var profile = await _db.ImportMappingProfiles.AsNoTracking().SingleOrDefaultAsync(item =>
            item.Id == profileId && item.CompanyId == user.CompanyId && item.TargetType == batch.TargetType, cancellationToken)
            ?? throw new InvalidOperationException("The saved mapping was not found for this company and import target.");
        if (!string.Equals(profile.HeadingSignature, HeadingSignature(batch.ColumnMappings), StringComparison.Ordinal))
            throw new InvalidOperationException("This mapping belongs to a different source-column structure and cannot be reused.");
        var saved = JsonSerializer.Deserialize<List<ImportMappingProfileItem>>(profile.MappingJson, JsonOptions) ?? [];
        var byHeading = saved.ToDictionary(item => item.NormalizedSourceHeading, StringComparer.OrdinalIgnoreCase);
        foreach (var source in batch.ColumnMappings.OrderBy(item => item.SourceColumnIndex))
        {
            if (!byHeading.TryGetValue(source.NormalizedSourceHeading, out var item))
                throw new InvalidOperationException("The source headings changed. Review mappings manually.");
            source.TargetFieldKey = item.IsIgnored ? null : item.TargetFieldKey;
            source.IsIgnored = item.IsIgnored;
            source.IsUserConfirmed = false;
            source.ConversionRule = null;
        }
        batch.Status = ImportBatchStatuses.Mapping;
        batch.UpdatedAtUtc = DateTime.UtcNow;
        batch.ConcurrencyToken = Guid.NewGuid().ToString("D");
        AddAudit(user, "Import mapping reused", nameof(ImportMappingProfile), profile.Id,
            $"Applied mapping '{profile.Name}' to import batch {batch.Id}; user confirmation is still required before commit.");
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ImportRollbackResult> RollbackAsync(AppUser user, int batchId, CancellationToken cancellationToken = default)
    {
        var access = await _batches.CanCommitAsync(user, cancellationToken);
        if (!access.Allowed) throw new UnauthorizedAccessException(access.Message);
        var batch = await _db.ImportBatches
            .Include(item => item.EntityChanges)
            .Include(item => item.SourceAssetFile)
            .SingleOrDefaultAsync(item => item.Id == batchId && item.CompanyId == user.CompanyId
                && item.SourceAssetFile != null && item.SourceAssetFile.CompanyId == user.CompanyId, cancellationToken)
            ?? throw new InvalidOperationException("The import batch was not found for this company.");
        if (batch.Status is not (ImportBatchStatuses.Committed or ImportBatchStatuses.PartiallyRolledBack))
            throw new InvalidOperationException("Only a committed or partially rolled-back import can be rolled back.");

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var reversed = 0;
        var blocked = new List<string>();
        foreach (var change in batch.EntityChanges.OrderByDescending(item => item.Id))
        {
            if (string.Equals(change.RollbackStatus, "RolledBack", StringComparison.OrdinalIgnoreCase)) continue;
            var reason = await TryRollbackChangeAsync(user, change, cancellationToken);
            if (reason is null)
            {
                change.RollbackStatus = "RolledBack";
                change.RolledBackByUserId = user.Id;
                change.RolledBackAtUtc = DateTime.UtcNow;
                change.RollbackReason = null;
                reversed++;
            }
            else
            {
                change.RollbackStatus = "Blocked";
                change.IsRollbackEligible = false;
                change.RollbackReason = reason;
                blocked.Add($"{change.EntityType} #{change.EntityId}: {reason}");
            }
        }

        batch.Status = blocked.Count == 0 ? ImportBatchStatuses.RolledBack : ImportBatchStatuses.PartiallyRolledBack;
        batch.RolledBackByUserId = user.Id;
        batch.RolledBackAtUtc = DateTime.UtcNow;
        batch.UpdatedAtUtc = DateTime.UtcNow;
        batch.ConcurrencyToken = Guid.NewGuid().ToString("D");
        AddAudit(user, blocked.Count == 0 ? "Import rolled back" : "Import rollback partially blocked", nameof(ImportBatch), batch.Id,
            $"Import rollback reversed {reversed} change(s); {blocked.Count} blocked. Historical evidence was not changed.");
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new ImportRollbackResult(reversed, blocked.Count, blocked);
    }

    private async Task<string?> TryRollbackChangeAsync(AppUser user, ImportEntityChange change, CancellationToken cancellationToken)
    {
        if (change.EntityId is null) return "No target record was recorded.";
        var entity = await LoadEntityAsync(user.CompanyId, change.EntityType, change.EntityId.Value, cancellationToken);
        if (entity is null) return "The target record no longer exists.";
        var currentJson = JsonSerializer.Serialize(SnapshotEntity(entity), JsonOptions);
        if (!string.Equals(StateToken(currentJson), change.EntityStateToken, StringComparison.OrdinalIgnoreCase))
            return "The record changed after import.";

        if (string.Equals(change.Action, ImportRowDecisions.Create, StringComparison.OrdinalIgnoreCase))
        {
            var dependency = await DeleteBlockReasonAsync(user.CompanyId, change.EntityType, change.EntityId.Value, entity, cancellationToken);
            if (dependency is not null) return dependency;
            _db.Remove(entity);
            return null;
        }

        if (string.Equals(change.Action, ImportRowDecisions.Update, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(change.BeforeValuesJson)) return "The previous field state was not recorded.";
            RestoreScalarSnapshot(entity, change.BeforeValuesJson);
            return null;
        }
        return "This ledger action does not change a domain record.";
    }

    private async Task<object?> LoadEntityAsync(int companyId, string targetType, int id, CancellationToken cancellationToken) => targetType switch
    {
        ImportTargetTypes.Vehicle => await _db.Vehicles.SingleOrDefaultAsync(item => item.Id == id && item.CompanyId == companyId, cancellationToken),
        ImportTargetTypes.Staff => await _db.AppUsers.SingleOrDefaultAsync(item => item.Id == id && item.CompanyId == companyId, cancellationToken),
        ImportTargetTypes.Equipment => await _db.EquipmentItems.SingleOrDefaultAsync(item => item.Id == id && item.CompanyId == companyId, cancellationToken),
        ImportTargetTypes.Stock => await _db.StockItems.SingleOrDefaultAsync(item => item.Id == id && item.CompanyId == companyId, cancellationToken),
        ImportTargetTypes.Medication => await _db.MedicationItems.SingleOrDefaultAsync(item => item.Id == id && item.CompanyId == companyId, cancellationToken),
        ImportTargetTypes.OperationalArea => await _db.OperationalAreas.SingleOrDefaultAsync(item => item.Id == id && item.CompanyId == companyId, cancellationToken),
        ImportTargetTypes.StorageLocation => await _db.StorageLocations.SingleOrDefaultAsync(item => item.Id == id && item.CompanyId == companyId, cancellationToken),
        nameof(ChecklistTemplate) or ImportTargetTypes.Checklist => await _db.ChecklistTemplates.Include(item => item.Sections)
            .SingleOrDefaultAsync(item => item.Id == id && item.CompanyId == companyId, cancellationToken),
        _ => null
    };

    private async Task<string?> DeleteBlockReasonAsync(int companyId, string targetType, int id, object entity, CancellationToken token)
    {
        switch (entity)
        {
            case Vehicle:
                if (await _db.DailyVehicleReadinessReports.AnyAsync(item => item.CompanyId == companyId && item.VehicleId == id, token)
                    || await _db.VehicleEquipmentAssignments.AnyAsync(item => item.CompanyId == companyId && item.VehicleId == id, token)
                    || await _db.AssetMovements.AnyAsync(item => item.CompanyId == companyId && item.AssetType == AssetTypes.Vehicle && item.AssetId == id, token))
                    return "The vehicle has operational history or assignments.";
                break;
            case AppUser:
                if (await _db.LoginIdentities.AnyAsync(item => item.CompanyId == companyId && item.AppUserId == id, token)
                    || await _db.TaskItems.AnyAsync(item => item.CompanyId == companyId && (item.AssignedToUserId == id || item.AssignedByUserId == id), token)
                    || await _db.IssueReports.AnyAsync(item => item.CompanyId == companyId && (item.ReportedByUserId == id || item.AssignedToUserId == id || item.ResolvedByUserId == id), token)
                    || await _db.DailyVehicleReadinessReports.AnyAsync(item => item.CompanyId == companyId && item.PerformedByUserId == id, token))
                    return "The staff profile has login or operational history.";
                break;
            case EquipmentItem:
                if (await _db.VehicleEquipmentAssignments.AnyAsync(item => item.CompanyId == companyId && item.EquipmentItemId == id, token)
                    || await _db.DailyVehicleEquipmentChecks.AnyAsync(item => item.CompanyId == companyId && item.EquipmentItemId == id, token)
                    || await _db.AssetMovements.AnyAsync(item => item.CompanyId == companyId && item.AssetType == AssetTypes.Equipment && item.AssetId == id, token))
                    return "The equipment has assignments, checks, or movement history.";
                break;
            case StockItem:
                if (await _db.AssetMovements.AnyAsync(item => item.CompanyId == companyId && item.AssetType == AssetTypes.Stock && item.AssetId == id, token))
                    return "The stock item has movement history.";
                break;
            case MedicationItem:
                if (await _db.AssetMovements.AnyAsync(item => item.CompanyId == companyId && item.AssetType == AssetTypes.Medication && item.AssetId == id, token))
                    return "The medication item has movement history.";
                break;
            case OperationalArea:
                if (await _db.OperationalAreas.AnyAsync(item => item.CompanyId == companyId && item.ParentOperationalAreaId == id, token)
                    || await _db.StorageLocations.AnyAsync(item => item.CompanyId == companyId && item.OperationalAreaId == id, token)
                    || await _db.Vehicles.AnyAsync(item => item.CompanyId == companyId && item.CurrentOperationalAreaId == id, token)
                    || await _db.EquipmentItems.AnyAsync(item => item.CompanyId == companyId && item.CurrentOperationalAreaId == id, token)
                    || await _db.StockItems.AnyAsync(item => item.CompanyId == companyId && item.CurrentOperationalAreaId == id, token)
                    || await _db.MedicationItems.AnyAsync(item => item.CompanyId == companyId && item.CurrentOperationalAreaId == id, token)
                    || await _db.AppUsers.AnyAsync(item => item.CompanyId == companyId && item.AssignedOperationalAreaId == id, token))
                    return "The operational area is referenced by company records.";
                break;
            case StorageLocation storage:
                if (await _db.StockItems.AnyAsync(item => item.CompanyId == companyId && item.Location == storage.Name, token)
                    || await _db.MedicationItems.AnyAsync(item => item.CompanyId == companyId && item.StorageLocation == storage.Name, token))
                    return "The storage location is referenced by stock or medication.";
                break;
            case ChecklistTemplate checklist:
                if (checklist.IsPublished || !string.Equals(checklist.Status, "Draft", StringComparison.OrdinalIgnoreCase)
                    || await _db.ChecklistPublishScopes.AnyAsync(item => item.CompanyId == companyId && item.ChecklistTemplateId == id, token)
                    || await _db.DailyVehicleReadinessReports.AnyAsync(item => item.CompanyId == companyId && item.ChecklistTemplateId == id, token))
                    return "The checklist was published or used and must be retired through checklist management.";
                break;
        }
        return null;
    }

    private async Task<ImportBatch> LoadBatchAsync(AppUser user, int batchId, CancellationToken cancellationToken)
    {
        return await _db.ImportBatches.Include(item => item.SourceAssetFile).Include(item => item.ColumnMappings)
            .SingleOrDefaultAsync(item => item.Id == batchId && item.CompanyId == user.CompanyId
                && item.SourceAssetFile != null && item.SourceAssetFile.CompanyId == user.CompanyId, cancellationToken)
            ?? throw new InvalidOperationException("The import batch was not found for this company.");
    }

    private async Task RequirePrepareAsync(AppUser user, CancellationToken cancellationToken)
    {
        var access = await _batches.CanPrepareAsync(user, cancellationToken);
        if (!access.Allowed) throw new UnauthorizedAccessException(access.Message);
    }

    private void AddAudit(AppUser user, string action, string entityType, int? entityId, string details)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = user.CompanyId, AppUserId = user.Id, Action = action, EntityType = entityType,
            EntityId = entityId, Details = details, CreatedAtUtc = DateTime.UtcNow
        });
    }

    private static string HeadingSignature(IEnumerable<ImportColumnMapping> mappings)
    {
        var headings = string.Join("|", mappings.OrderBy(item => item.SourceColumnIndex).Select(item => item.NormalizedSourceHeading));
        return string.IsNullOrWhiteSpace(headings) ? string.Empty : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(headings)));
    }

    private static IReadOnlyDictionary<string, object?> SnapshotEntity(object entity)
    {
        if (entity is ChecklistTemplate checklist)
            return new Dictionary<string, object?> { ["Id"] = checklist.Id, ["Name"] = checklist.Name, ["Version"] = checklist.Version, ["Status"] = checklist.Status, ["Sections"] = checklist.Sections.Count };
        return entity.GetType().GetProperties()
            .Where(property => property.CanRead && IsScalar(property.PropertyType))
            .ToDictionary(property => property.Name, property => property.GetValue(entity));
    }

    private static bool IsScalar(Type type) => type.IsPrimitive || type.IsEnum || type == typeof(string)
        || type == typeof(DateTime) || type == typeof(DateTime?) || type == typeof(int?) || type == typeof(bool?);

    private static string StateToken(string json) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));

    private static void RestoreScalarSnapshot(object entity, string json)
    {
        var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOptions)
            ?? throw new InvalidOperationException("The recorded previous field state is invalid.");
        foreach (var (name, value) in values)
        {
            if (name is "Id" or "CompanyId" or "CreatedAtUtc") continue;
            var property = entity.GetType().GetProperty(name);
            if (property?.CanWrite != true || !IsScalar(property.PropertyType)) continue;
            property.SetValue(entity, ConvertElement(value, property.PropertyType));
        }
    }

    private static object? ConvertElement(JsonElement value, Type targetType)
    {
        if (value.ValueKind == JsonValueKind.Null) return null;
        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (type == typeof(string)) return value.GetString();
        if (type == typeof(int)) return value.GetInt32();
        if (type == typeof(bool)) return value.GetBoolean();
        if (type == typeof(DateTime)) return value.GetDateTime();
        if (type.IsEnum) return Enum.Parse(type, value.GetString()!, true);
        return JsonSerializer.Deserialize(value.GetRawText(), targetType, JsonOptions);
    }
}
