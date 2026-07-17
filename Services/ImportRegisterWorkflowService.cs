using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public static class ImportRowStatuses
{
    public const string Valid = "Valid";
    public const string Invalid = "Invalid";
    public const string Duplicate = "Duplicate";
    public const string Excluded = "Excluded";
    public const string Committed = "Committed";
}

public static class ImportRowDecisions
{
    public const string Create = "Create";
    public const string Update = "Update";
    public const string Skip = "Skip";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Create, Update, Skip
    };
}

public sealed record ImportMappingInput(int SourceColumnIndex, string? TargetFieldKey, bool IsIgnored);
public sealed record ImportRowCorrection(int SourceRowNumber, IReadOnlyDictionary<string, string?> Values, bool IsIncluded, string? Decision);
public sealed record ImportCommitResult(int Created, int Updated, int Skipped, int Excluded);
public sealed record ImportDuplicateCandidate(int EntityId, string Label);

public sealed class ImportRegisterWorkflowService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly VectorDbContext _db;
    private readonly ImportBatchService _batches;
    private readonly IImportFieldRegistry _fields;
    private readonly IImportTabularReader _reader;

    public ImportRegisterWorkflowService(
        VectorDbContext db,
        ImportBatchService batches,
        IImportFieldRegistry fields,
        IImportTabularReader reader)
    {
        _db = db;
        _batches = batches;
        _fields = fields;
        _reader = reader;
    }

    public async Task<ImportBatch?> LoadAsync(AppUser user, int batchId, CancellationToken cancellationToken = default)
    {
        return await _db.ImportBatches
            .Include(batch => batch.SourceAssetFile)
            .Include(batch => batch.CreatedByUser)
            .Include(batch => batch.ColumnMappings.OrderBy(mapping => mapping.DisplayOrder))
            .Include(batch => batch.RowResults.OrderBy(row => row.SourceRowNumber))
            .SingleOrDefaultAsync(batch =>
                batch.Id == batchId &&
                batch.CompanyId == user.CompanyId &&
                batch.SourceAssetFile != null &&
                batch.SourceAssetFile.CompanyId == user.CompanyId,
                cancellationToken);
    }

    public async Task SelectSourceAsync(
        AppUser user,
        int batchId,
        string? worksheet,
        int headerRowNumber,
        CancellationToken cancellationToken = default)
    {
        var batch = await RequireBatchAsync(user, batchId, cancellationToken);
        EnsureMutable(batch);
        var data = await _reader.ReadAsync(batch.SourceAssetFile!, worksheet, headerRowNumber, cancellationToken);
        var target = _fields.FindTarget(batch.TargetType)
            ?? throw new InvalidOperationException("The import target field contract was not found.");

        _db.ImportColumnMappings.RemoveRange(batch.ColumnMappings);
        _db.ImportRowResults.RemoveRange(batch.RowResults);
        batch.ColumnMappings.Clear();
        batch.RowResults.Clear();

        foreach (var column in data.Columns)
        {
            var suggestion = FindSuggestion(target, column.Heading);
            batch.ColumnMappings.Add(new ImportColumnMapping
            {
                CompanyId = user.CompanyId,
                SourceColumnIndex = column.Index,
                SourceHeading = column.Heading,
                NormalizedSourceHeading = Normalize(column.Heading),
                TargetFieldKey = suggestion?.Key,
                SuggestionReason = suggestion is null ? null : "Exact normalized heading or configured alias match.",
                IsIgnored = false,
                IsUserConfirmed = false,
                DisplayOrder = column.Index
            });
        }

        batch.SelectedWorksheet = data.Worksheet;
        batch.HeaderRowNumber = data.HeaderRowNumber;
        batch.LayoutMode = "RegisterRows";
        batch.Status = ImportBatchStatuses.Mapping;
        Touch(batch);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveMappingsAsync(
        AppUser user,
        int batchId,
        IReadOnlyCollection<ImportMappingInput> inputs,
        CancellationToken cancellationToken = default)
    {
        var batch = await RequireBatchAsync(user, batchId, cancellationToken);
        EnsureMutable(batch);
        var target = _fields.FindTarget(batch.TargetType)
            ?? throw new InvalidOperationException("The import target field contract was not found.");
        var validKeys = target.Fields.Select(field => field.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in batch.ColumnMappings)
        {
            var input = inputs.SingleOrDefault(candidate => candidate.SourceColumnIndex == mapping.SourceColumnIndex)
                ?? throw new InvalidOperationException($"Column '{mapping.SourceHeading}' must be mapped or ignored.");
            var key = input.TargetFieldKey?.Trim();
            if (!input.IsIgnored && (string.IsNullOrWhiteSpace(key) || !validKeys.Contains(key)))
            {
                throw new InvalidOperationException($"Column '{mapping.SourceHeading}' must use a valid target field or Ignore.");
            }

            mapping.TargetFieldKey = input.IsIgnored ? null : key;
            mapping.IsIgnored = input.IsIgnored;
            mapping.IsUserConfirmed = true;
            mapping.ConversionRule = input.IsIgnored ? null : _fields.FindField(key!)?.DataType;
        }

        var duplicateTargets = batch.ColumnMappings
            .Where(mapping => !mapping.IsIgnored && !string.IsNullOrWhiteSpace(mapping.TargetFieldKey))
            .GroupBy(mapping => mapping.TargetFieldKey!, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        if (duplicateTargets.Count > 0)
        {
            throw new InvalidOperationException($"Each target field may be mapped once. Repeated: {string.Join(", ", duplicateTargets)}.");
        }

        var mappedKeys = batch.ColumnMappings
            .Where(mapping => !mapping.IsIgnored && mapping.IsUserConfirmed)
            .Select(mapping => mapping.TargetFieldKey!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingRequired = target.Fields
            .Where(field => field.IsRequired && !mappedKeys.Contains(field.Key))
            .Select(field => field.Label)
            .ToList();
        if (missingRequired.Count > 0)
        {
            throw new InvalidOperationException($"Map required fields before validation: {string.Join(", ", missingRequired)}.");
        }

        batch.Status = ImportBatchStatuses.Mapping;
        Touch(batch);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ValidateAsync(AppUser user, int batchId, CancellationToken cancellationToken = default)
    {
        var batch = await RequireBatchAsync(user, batchId, cancellationToken);
        EnsureMutable(batch);
        if (batch.HeaderRowNumber is null || string.IsNullOrWhiteSpace(batch.SelectedWorksheet))
        {
            throw new InvalidOperationException("Choose the worksheet and header row first.");
        }
        if (batch.ColumnMappings.Count == 0 || batch.ColumnMappings.Any(mapping => !mapping.IsUserConfirmed))
        {
            throw new InvalidOperationException("Confirm every source-column mapping before validation.");
        }

        var data = await _reader.ReadAsync(
            batch.SourceAssetFile!, batch.SelectedWorksheet, batch.HeaderRowNumber.Value, cancellationToken);
        var previous = batch.RowResults.ToDictionary(row => row.SourceRowNumber);
        _db.ImportRowResults.RemoveRange(batch.RowResults);
        batch.RowResults.Clear();

        foreach (var sourceRow in data.Rows)
        {
            previous.TryGetValue(sourceRow.SourceRowNumber, out var prior);
            var original = MapSourceValues(sourceRow, batch.ColumnMappings);
            var effective = prior?.CorrectedPayloadJson is null
                ? original
                : DeserializeDictionary(prior.CorrectedPayloadJson);
            var validation = await ValidateRowAsync(user.CompanyId, batch.TargetType, effective, cancellationToken);
            batch.RowResults.Add(new ImportRowResult
            {
                CompanyId = user.CompanyId,
                SourceRowNumber = sourceRow.SourceRowNumber,
                OriginalPayloadJson = JsonSerializer.Serialize(original, JsonOptions),
                CorrectedPayloadJson = JsonSerializer.Serialize(validation.Values, JsonOptions),
                ValidationStatus = prior?.IsIncluded == false
                    ? ImportRowStatuses.Excluded
                    : validation.Status,
                FieldErrorsJson = SerializeOrNull(validation.Errors),
                WarningsJson = SerializeOrNull(validation.Warnings),
                DuplicateCandidatesJson = SerializeOrNull(validation.Duplicates),
                RowDecision = NormalizeDecision(prior?.RowDecision, validation.Duplicates.Count),
                IsIncluded = prior?.IsIncluded ?? true
            });
        }

        MarkSourceDuplicates(batch);

        UpdateBatchCounts(batch);
        batch.ValidatedByUserId = user.Id;
        batch.ValidatedAtUtc = DateTime.UtcNow;
        batch.Status = DetermineBatchStatus(batch);
        Touch(batch);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task CorrectRowAsync(
        AppUser user,
        int batchId,
        ImportRowCorrection correction,
        CancellationToken cancellationToken = default)
    {
        var batch = await RequireBatchAsync(user, batchId, cancellationToken);
        EnsureMutable(batch);
        var row = batch.RowResults.SingleOrDefault(item => item.SourceRowNumber == correction.SourceRowNumber)
            ?? throw new InvalidOperationException("The selected source row was not found.");
        if (!string.IsNullOrWhiteSpace(correction.Decision) && !ImportRowDecisions.All.Contains(correction.Decision))
        {
            throw new InvalidOperationException("Choose Create, Update existing, or Skip.");
        }

        row.CorrectedPayloadJson = JsonSerializer.Serialize(correction.Values, JsonOptions);
        row.IsIncluded = correction.IsIncluded;
        row.RowDecision = correction.IsIncluded ? correction.Decision : ImportRowDecisions.Skip;
        Touch(batch);
        await _db.SaveChangesAsync(cancellationToken);
        await ValidateAsync(user, batchId, cancellationToken);
    }

    public async Task<ImportCommitResult> CommitAsync(AppUser user, int batchId, CancellationToken cancellationToken = default)
    {
        var access = await _batches.CanCommitAsync(user, cancellationToken);
        if (!access.Allowed)
        {
            throw new UnauthorizedAccessException(access.Message);
        }

        var batch = await RequireBatchAsync(user, batchId, cancellationToken);
        if (string.Equals(batch.Status, ImportBatchStatuses.Committed, StringComparison.OrdinalIgnoreCase))
        {
            return SummarizeCommitted(batch);
        }

        await ValidateAsync(user, batchId, cancellationToken);
        batch = await RequireBatchAsync(user, batchId, cancellationToken);
        if (!string.Equals(batch.Status, ImportBatchStatuses.ReadyToCommit, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Resolve or exclude invalid rows and decide every duplicate before committing.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var created = 0;
        var updated = 0;
        var skipped = 0;
        var excluded = 0;

        foreach (var row in batch.RowResults.OrderBy(item => item.SourceRowNumber))
        {
            if (!row.IsIncluded)
            {
                excluded++;
                continue;
            }
            if (string.Equals(row.RowDecision, ImportRowDecisions.Skip, StringComparison.OrdinalIgnoreCase))
            {
                row.ValidationStatus = ImportRowStatuses.Committed;
                skipped++;
                continue;
            }

            var values = row.CorrectedPayloadJson is null
                ? DeserializeDictionary(row.OriginalPayloadJson)
                : DeserializeDictionary(row.CorrectedPayloadJson);
            var validation = await ValidateRowAsync(user.CompanyId, batch.TargetType, values, cancellationToken);
            if (validation.Errors.Count > 0)
            {
                throw new InvalidOperationException($"Source row {row.SourceRowNumber} changed or is no longer valid. Validate the batch again.");
            }

            var candidateId = validation.Duplicates.Count == 1 ? validation.Duplicates[0].EntityId : (int?)null;
            if (string.Equals(row.RowDecision, ImportRowDecisions.Update, StringComparison.OrdinalIgnoreCase) && candidateId is null)
            {
                throw new InvalidOperationException($"Source row {row.SourceRowNumber} no longer resolves to one update target.");
            }
            if (string.Equals(row.RowDecision, ImportRowDecisions.Create, StringComparison.OrdinalIgnoreCase) && candidateId is not null)
            {
                throw new InvalidOperationException($"Source row {row.SourceRowNumber} now matches an existing record. Review the duplicate decision.");
            }

            var write = await WriteDomainRecordAsync(user, batch.TargetType, values, candidateId, cancellationToken);
            _db.ImportEntityChanges.Add(new ImportEntityChange
            {
                CompanyId = user.CompanyId,
                ImportBatchId = batch.Id,
                ImportRowResultId = row.Id,
                EntityType = batch.TargetType,
                EntityId = write.EntityId,
                Action = write.Action,
                BeforeValuesJson = write.BeforeJson,
                AfterValuesJson = write.AfterJson,
                EntityStateToken = write.StateToken,
                IsRollbackEligible = true,
                RollbackStatus = "Eligible"
            });
            row.ValidationStatus = ImportRowStatuses.Committed;
            if (write.Action == ImportRowDecisions.Create) created++; else updated++;
        }

        batch.Status = ImportBatchStatuses.Committed;
        batch.CommittedByUserId = user.Id;
        batch.CommittedAtUtc = DateTime.UtcNow;
        batch.FailureCode = null;
        batch.FailureSummary = null;
        Touch(batch);
        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = user.CompanyId,
            AppUserId = user.Id,
            Action = "Import committed",
            EntityType = nameof(ImportBatch),
            EntityId = batch.Id,
            Details = $"{batch.TargetType} import committed: {created} created, {updated} updated, {skipped} skipped, {excluded} excluded.",
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new ImportCommitResult(created, updated, skipped, excluded);
    }

    private async Task<ImportBatch> RequireBatchAsync(AppUser user, int batchId, CancellationToken cancellationToken)
    {
        var access = await _batches.CanPrepareAsync(user, cancellationToken);
        if (!access.Allowed) throw new UnauthorizedAccessException(access.Message);
        return await LoadAsync(user, batchId, cancellationToken)
            ?? throw new InvalidOperationException("The import batch was not found for the current company.");
    }

    private static void EnsureMutable(ImportBatch batch)
    {
        if (string.Equals(batch.Status, ImportBatchStatuses.Committed, StringComparison.OrdinalIgnoreCase)
            || string.Equals(batch.Status, ImportBatchStatuses.RolledBack, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("A committed or rolled-back import batch cannot be changed.");
        }
    }

    private ImportFieldDefinition? FindSuggestion(ImportTargetDefinition target, string heading)
    {
        var normalized = Normalize(heading);
        return target.Fields.FirstOrDefault(field =>
            Normalize(field.Label) == normalized ||
            Normalize(field.TargetProperty) == normalized ||
            field.Aliases.Any(alias => Normalize(alias) == normalized));
    }

    private static Dictionary<string, string?> MapSourceValues(
        ImportSourceRow row,
        IEnumerable<ImportColumnMapping> mappings)
    {
        return mappings
            .Where(mapping => !mapping.IsIgnored && !string.IsNullOrWhiteSpace(mapping.TargetFieldKey))
            .ToDictionary(
                mapping => mapping.TargetFieldKey!,
                mapping => row.Values.TryGetValue(mapping.SourceColumnIndex, out var value) ? value : null,
                StringComparer.OrdinalIgnoreCase);
    }

    private async Task<RowValidation> ValidateRowAsync(
        int companyId,
        string targetType,
        IReadOnlyDictionary<string, string?> source,
        CancellationToken cancellationToken)
    {
        var target = _fields.FindTarget(targetType)
            ?? throw new InvalidOperationException("The import target field contract was not found.");
        var normalized = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var warnings = new List<string>();

        foreach (var field in target.Fields)
        {
            source.TryGetValue(field.Key, out var raw);
            raw = Collapse(raw);
            if (string.IsNullOrWhiteSpace(raw))
            {
                normalized[field.Key] = null;
                if (field.IsRequired) errors[field.Key] = $"{field.Label} is required.";
                continue;
            }
            if (field.MaxLength is int max && raw.Length > max)
            {
                errors[field.Key] = $"{field.Label} exceeds {max} characters.";
                continue;
            }

            var conversion = await ConvertValueAsync(companyId, targetType, field, raw, cancellationToken);
            normalized[field.Key] = conversion.Value;
            if (conversion.Error is not null) errors[field.Key] = conversion.Error;
        }

        await ValidateCrossFieldsAsync(companyId, targetType, normalized, errors, cancellationToken);
        var duplicates = errors.Count == 0
            ? await FindDuplicatesAsync(companyId, targetType, normalized, cancellationToken)
            : [];
        if (targetType == ImportTargetTypes.Vehicle && errors.Count == 0)
        {
            var callsign = Get(normalized, "vehicle.callsign");
            var registration = Get(normalized, "vehicle.registration_number");
            if (!string.IsNullOrWhiteSpace(callsign))
            {
                var vehicles = await _db.Vehicles.AsNoTracking().Where(item => item.CompanyId == companyId).ToListAsync(cancellationToken);
                if (vehicles.Any(item => Normalize(item.Callsign) == Normalize(callsign)
                    && Normalize(item.RegistrationNumber) != Normalize(registration)))
                    errors["vehicle.callsign"] = "Callsign is already assigned to a different vehicle in this company.";
            }
        }
        if (targetType == ImportTargetTypes.OperationalArea && errors.Count == 0 && duplicates.Count == 1)
        {
            var parentId = GetInt(normalized, "area.parent");
            if (parentId is not null && await WouldCreateAreaCycleAsync(companyId, duplicates[0].EntityId, parentId.Value, cancellationToken))
                errors["area.parent"] = "The selected parent would create an operational-area cycle.";
        }
        if (duplicates.Count > 1) errors["duplicate"] = "The row matches conflicting existing records and cannot be updated automatically.";
        var status = errors.Count > 0 ? ImportRowStatuses.Invalid
            : duplicates.Count > 0 ? ImportRowStatuses.Duplicate
            : ImportRowStatuses.Valid;
        return new RowValidation(status, normalized, errors, warnings, duplicates);
    }

    private async Task<ValueConversion> ConvertValueAsync(
        int companyId,
        string targetType,
        ImportFieldDefinition field,
        string raw,
        CancellationToken cancellationToken)
    {
        switch (field.DataType)
        {
            case "integer":
                return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer) && integer >= 0
                    ? new(integer.ToString(CultureInfo.InvariantCulture), null)
                    : new(null, $"{field.Label} must be a whole number of zero or more.");
            case "boolean":
                return TryBoolean(raw, out var boolean)
                    ? new(boolean ? "true" : "false", null)
                    : new(null, $"{field.Label} must be Yes/No, True/False, or 1/0.");
            case "date":
                return TryDate(raw, out var date)
                    ? new(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), null)
                    : new(null, $"{field.Label} is not a valid date or Excel serial date.");
            case "email":
                try
                {
                    var address = new System.Net.Mail.MailAddress(raw);
                    return new(address.Address.Trim().ToLowerInvariant(), null);
                }
                catch (FormatException)
                {
                    return new(null, $"{field.Label} is not a valid email address.");
                }
            case "tenant-reference":
                var area = await ResolveAreaAsync(companyId, raw, cancellationToken);
                return area is null
                    ? new(null, $"{field.Label} does not exactly match an operational area in this company.")
                    : new(area.Id.ToString(CultureInfo.InvariantCulture), null);
            case "tenant-option":
                return await ResolveOptionAsync(companyId, targetType, field, raw, cancellationToken);
            case "status":
                return new(ToTitleCase(raw), null);
            default:
                return new(raw, null);
        }
    }

    private async Task<ValueConversion> ResolveOptionAsync(
        int companyId,
        string targetType,
        ImportFieldDefinition field,
        string raw,
        CancellationToken cancellationToken)
    {
        if (field.OptionSource == "vehicle-functions")
        {
            var match = await _db.VehicleFunctionSetups.AsNoTracking().SingleOrDefaultAsync(item =>
                item.CompanyId == companyId && item.Status == "Active" && item.Name.ToLower() == raw.ToLower(), cancellationToken);
            return match is null ? new(null, "Vehicle function must exactly match an active company option.") : new(match.Name, null);
        }
        if (field.OptionSource == "vehicle-subtypes")
        {
            var match = await _db.VehicleSubtypeSetups.AsNoTracking().SingleOrDefaultAsync(item =>
                item.CompanyId == companyId && item.Status == "Active" && item.Name.ToLower() == raw.ToLower(), cancellationToken);
            return match is null ? new(null, "Vehicle subtype must exactly match an active company option.") : new(match.Name, null);
        }
        if (field.OptionSource == "staff-qualifications")
        {
            var match = await _db.StaffQualificationSetups.AsNoTracking().SingleOrDefaultAsync(item =>
                item.CompanyId == companyId && item.Status == "Active" && item.Name.ToLower() == raw.ToLower(), cancellationToken);
            return match is null ? new(null, "Qualification must exactly match an active company option.") : new(match.Name, null);
        }
        if (field.OptionSource == "area-types")
        {
            var company = await _db.Companies.AsNoTracking().SingleAsync(item => item.Id == companyId, cancellationToken);
            var allowed = new[] { "Region", "Base", "Operational Area" };
            var match = allowed.FirstOrDefault(item => string.Equals(item, raw, StringComparison.OrdinalIgnoreCase));
            return match is null ? new(null, $"Area type is not valid for {company.OperationalStructureMode ?? "the current structure"}.") : new(match, null);
        }
        return new(raw, null);
    }

    private async Task<List<ImportDuplicateCandidate>> FindDuplicatesAsync(
        int companyId,
        string targetType,
        IReadOnlyDictionary<string, string?> values,
        CancellationToken cancellationToken)
    {
        var candidates = new Dictionary<int, string>();
        switch (targetType)
        {
            case ImportTargetTypes.Vehicle:
                var registration = Get(values, "vehicle.registration_number");
                foreach (var item in await _db.Vehicles.AsNoTracking().Where(item => item.CompanyId == companyId).ToListAsync(cancellationToken))
                    if (Normalize(item.RegistrationNumber) == Normalize(registration)) candidates[item.Id] = $"{item.RegistrationNumber} / {item.Callsign}";
                break;
            case ImportTargetTypes.Staff:
                var staff = await _db.AppUsers.AsNoTracking().Where(item => item.CompanyId == companyId).ToListAsync(cancellationToken);
                foreach (var item in staff)
                    if (MatchesAny(values, ("staff.email", item.Email), ("staff.staff_id", item.StaffIdentifier), ("staff.practitioner_number", item.PractitionerNumber), ("staff.national_id", item.NationalId)))
                        candidates[item.Id] = $"{item.FullName} / {item.StaffIdentifier ?? item.Email}";
                break;
            case ImportTargetTypes.Equipment:
                var serial = Get(values, "equipment.serial_asset_id");
                if (!string.IsNullOrWhiteSpace(serial))
                    foreach (var item in await _db.EquipmentItems.AsNoTracking().Where(item => item.CompanyId == companyId).ToListAsync(cancellationToken))
                        if (Normalize(item.SerialOrAssetId) == Normalize(serial)) candidates[item.Id] = $"{item.Name} / {item.SerialOrAssetId}";
                break;
            case ImportTargetTypes.Stock:
                foreach (var item in await _db.StockItems.AsNoTracking().Where(item => item.CompanyId == companyId).ToListAsync(cancellationToken))
                    if (Normalize(item.ItemName) == Normalize(Get(values, "stock.item_name"))
                        && Normalize(item.BatchNumber) == Normalize(Get(values, "stock.batch"))
                        && item.CurrentOperationalAreaId == GetInt(values, "stock.operational_area"))
                        candidates[item.Id] = $"{item.ItemName} / {item.BatchNumber ?? "No batch"}";
                break;
            case ImportTargetTypes.Medication:
                foreach (var item in await _db.MedicationItems.AsNoTracking().Where(item => item.CompanyId == companyId).ToListAsync(cancellationToken))
                    if (MedicationMatches(item, values)) candidates[item.Id] = $"{item.Name} / {item.BatchNumber ?? "No batch"}";
                break;
            case ImportTargetTypes.OperationalArea:
                foreach (var item in await _db.OperationalAreas.AsNoTracking().Where(item => item.CompanyId == companyId).ToListAsync(cancellationToken))
                    if (Normalize(item.Name) == Normalize(Get(values, "area.name"))
                        && Normalize(item.AreaType) == Normalize(Get(values, "area.type"))
                        && item.ParentOperationalAreaId == GetInt(values, "area.parent"))
                        candidates[item.Id] = item.Name;
                break;
            case ImportTargetTypes.StorageLocation:
                foreach (var item in await _db.StorageLocations.AsNoTracking().Where(item => item.CompanyId == companyId).ToListAsync(cancellationToken))
                    if (Normalize(item.Name) == Normalize(Get(values, "storage.name"))
                        && item.OperationalAreaId == GetInt(values, "storage.operational_area"))
                        candidates[item.Id] = item.Name;
                break;
        }
        return candidates.Select(item => new ImportDuplicateCandidate(item.Key, item.Value)).ToList();
    }

    private async Task<DomainWriteResult> WriteDomainRecordAsync(
        AppUser actor,
        string targetType,
        IReadOnlyDictionary<string, string?> values,
        int? existingId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        object entity;
        string? before = null;
        if (existingId is not null)
        {
            entity = await LoadEntityAsync(actor.CompanyId, targetType, existingId.Value, cancellationToken);
            before = JsonSerializer.Serialize(SnapshotEntity(entity), JsonOptions);
        }
        else
        {
            entity = await CreateEntityAsync(actor, targetType, now, cancellationToken);
        }

        ApplyValues(entity, values, actor, now);
        await _db.SaveChangesAsync(cancellationToken);
        var id = (int)(entity.GetType().GetProperty("Id")?.GetValue(entity) ?? 0);
        var after = JsonSerializer.Serialize(SnapshotEntity(entity), JsonOptions);
        return new DomainWriteResult(
            id,
            existingId is null ? ImportRowDecisions.Create : ImportRowDecisions.Update,
            before,
            after,
            ComputeStateToken(after));
    }

    private async Task<object> LoadEntityAsync(int companyId, string targetType, int id, CancellationToken cancellationToken) => targetType switch
    {
        ImportTargetTypes.Vehicle => await _db.Vehicles.SingleAsync(item => item.Id == id && item.CompanyId == companyId, cancellationToken),
        ImportTargetTypes.Staff => await _db.AppUsers.SingleAsync(item => item.Id == id && item.CompanyId == companyId, cancellationToken),
        ImportTargetTypes.Equipment => await _db.EquipmentItems.SingleAsync(item => item.Id == id && item.CompanyId == companyId, cancellationToken),
        ImportTargetTypes.Stock => await _db.StockItems.SingleAsync(item => item.Id == id && item.CompanyId == companyId, cancellationToken),
        ImportTargetTypes.Medication => await _db.MedicationItems.SingleAsync(item => item.Id == id && item.CompanyId == companyId, cancellationToken),
        ImportTargetTypes.OperationalArea => await _db.OperationalAreas.SingleAsync(item => item.Id == id && item.CompanyId == companyId, cancellationToken),
        ImportTargetTypes.StorageLocation => await _db.StorageLocations.SingleAsync(item => item.Id == id && item.CompanyId == companyId, cancellationToken),
        _ => throw new InvalidOperationException("The selected register target cannot be committed.")
    };

    private async Task<object> CreateEntityAsync(AppUser actor, string targetType, DateTime now, CancellationToken cancellationToken)
    {
        object entity = targetType switch
        {
            ImportTargetTypes.Vehicle => new Vehicle { CompanyId = actor.CompanyId, CreatedAtUtc = now },
            ImportTargetTypes.Staff => new AppUser
            {
                CompanyId = actor.CompanyId,
                AppRoleId = await _db.AppRoles.Where(role => role.Name == "Staff").Select(role => role.Id).SingleOrDefaultAsync(cancellationToken),
                CreatedAtUtc = now
            },
            ImportTargetTypes.Equipment => new EquipmentItem { CompanyId = actor.CompanyId, CreatedAtUtc = now },
            ImportTargetTypes.Stock => new StockItem { CompanyId = actor.CompanyId, CreatedByUserId = actor.Id, CreatedAtUtc = now },
            ImportTargetTypes.Medication => new MedicationItem { CompanyId = actor.CompanyId, CreatedByUserId = actor.Id, CreatedAtUtc = now },
            ImportTargetTypes.OperationalArea => new OperationalArea { CompanyId = actor.CompanyId, CreatedAtUtc = now },
            ImportTargetTypes.StorageLocation => new StorageLocation { CompanyId = actor.CompanyId, CreatedAtUtc = now },
            _ => throw new InvalidOperationException("The selected register target cannot be committed.")
        };
        if (entity is AppUser user && user.AppRoleId == 0)
            throw new InvalidOperationException("The Staff role is not configured. Staff imports cannot create roles or login access.");
        _db.Add(entity);
        return entity;
    }

    private static void ApplyValues(object entity, IReadOnlyDictionary<string, string?> values, AppUser actor, DateTime now)
    {
        switch (entity)
        {
            case Vehicle item:
                item.RegistrationNumber = Required(values, "vehicle.registration_number");
                SetIfPresent(values, "vehicle.callsign", value => item.Callsign = value);
                SetIfPresent(values, "vehicle.function", value => { item.VehicleFunction = value; item.VehicleType = value; });
                SetIfPresent(values, "vehicle.subtype", value => item.VehicleSubtype = value);
                SetIfPresent(values, "vehicle.qualification_level", value => item.QualificationLevel = value);
                SetIfPresent(values, "vehicle.vin", value => item.VinNumber = value);
                SetIfPresent(values, "vehicle.chassis_number", value => item.ChassisNumber = value);
                SetIfPresent(values, "vehicle.licence_number", value => item.LicenseNumber = value);
                SetDate(values, "vehicle.licence_expiry", value => item.LicenseDiscExpiryDate = value);
                SetDate(values, "vehicle.last_service", value => item.LastServiceDate = value);
                SetDate(values, "vehicle.next_service", value => item.NextServiceDate = value);
                SetIfPresent(values, "vehicle.status", value => item.Status = value);
                SetNullableInt(values, "vehicle.operational_area", value => item.CurrentOperationalAreaId = value);
                SetIfPresent(values, "vehicle.location_detail", value => item.CurrentLocationDetail = value);
                SetIfPresent(values, "vehicle.notes", value => item.Notes = value);
                item.UpdatedAtUtc = now;
                break;
            case AppUser item:
                item.FullName = Required(values, "staff.full_name");
                SetIfPresent(values, "staff.email", value => item.Email = value);
                SetIfPresent(values, "staff.staff_id", value => item.StaffIdentifier = value);
                SetIfPresent(values, "staff.national_id", value => item.NationalId = value);
                SetIfPresent(values, "staff.cell_number", value => item.CellNumber = value);
                SetIfPresent(values, "staff.qualification", value => item.QualificationFunction = value);
                SetIfPresent(values, "staff.practitioner_number", value => item.PractitionerNumber = value);
                SetDate(values, "staff.licence_expiry", value => item.AnnualLicenseExpiryDate = value);
                SetIfPresent(values, "staff.cpd_status", value => item.CpdComplianceStatus = value);
                SetDate(values, "staff.cpd_expiry", value => item.CpdComplianceExpiryDate = value);
                SetNullableInt(values, "staff.operational_area", value => item.AssignedOperationalAreaId = value);
                SetIfPresent(values, "staff.status", value => item.Status = value);
                break;
            case EquipmentItem item:
                item.Name = Required(values, "equipment.name");
                SetIfPresent(values, "equipment.type", value => item.EquipmentType = value);
                SetIfPresent(values, "equipment.model", value => item.Model = value);
                SetIfPresent(values, "equipment.serial_asset_id", value => item.SerialOrAssetId = value);
                SetDate(values, "equipment.next_service", value => item.NextServiceDate = value);
                SetBoolean(values, "equipment.battery_required", value => item.BatteryRequired = value);
                SetIfPresent(values, "equipment.status", value => item.Status = value);
                SetNullableInt(values, "equipment.operational_area", value => item.CurrentOperationalAreaId = value);
                SetIfPresent(values, "equipment.location_detail", value => item.CurrentLocationDetail = value);
                SetIfPresent(values, "equipment.notes", value => item.Notes = value);
                item.UpdatedAtUtc = now;
                break;
            case StockItem item:
                item.ItemName = Required(values, "stock.item_name");
                item.Quantity = GetInt(values, "stock.quantity") ?? 0;
                SetIfPresent(values, "stock.item_type", value => item.ItemType = value);
                SetIfPresent(values, "stock.category", value => item.StockCategory = value);
                SetIfPresent(values, "stock.batch", value => item.BatchNumber = value);
                SetNullableInt(values, "stock.minimum_quantity", value => item.MinimumQuantity = value);
                SetIfPresent(values, "stock.unit", value => item.Unit = value);
                SetDate(values, "stock.expiry", value => item.ExpiryDate = value);
                SetBoolean(values, "stock.readiness_critical", value => item.IsReadinessCritical = value);
                SetIfPresent(values, "stock.location", value => item.Location = value);
                SetNullableInt(values, "stock.operational_area", value => item.CurrentOperationalAreaId = value);
                SetIfPresent(values, "stock.status", value => item.Status = value);
                SetIfPresent(values, "stock.notes", value => item.Notes = value);
                item.UpdatedAtUtc = now;
                break;
            case MedicationItem item:
                item.Name = Required(values, "medication.name");
                SetIfPresent(values, "medication.code", value => item.MedicationCode = value);
                SetIfPresent(values, "medication.type", value => item.MedicationType = value);
                SetIfPresent(values, "medication.schedule", value => item.Schedule = value);
                SetIfPresent(values, "medication.batch", value => item.BatchNumber = value);
                SetIfPresent(values, "medication.storage_location", value => item.StorageLocation = value);
                SetNullableInt(values, "medication.operational_area", value => item.CurrentOperationalAreaId = value);
                SetIfPresent(values, "medication.status", value => item.Status = value);
                SetNullableInt(values, "medication.quantity", value => item.Quantity = value);
                SetDate(values, "medication.expiry", value => item.ExpiryDate = value);
                SetIfPresent(values, "medication.notes", value => item.Notes = value);
                item.UpdatedAtUtc = now;
                break;
            case OperationalArea item:
                item.Name = Required(values, "area.name");
                item.AreaType = Required(values, "area.type");
                SetNullableInt(values, "area.parent", value => item.ParentOperationalAreaId = value);
                SetIfPresent(values, "area.address", value => item.Address = value);
                SetIfPresent(values, "area.status", value => item.Status = value);
                SetIfPresent(values, "area.notes", value => item.Notes = value);
                item.UpdatedAtUtc = now;
                break;
            case StorageLocation item:
                item.Name = Required(values, "storage.name");
                item.OperationalAreaId = GetInt(values, "storage.operational_area") ?? throw new InvalidOperationException("Storage operational area is required.");
                SetIfPresent(values, "storage.type", value => item.StorageType = value);
                SetIfPresent(values, "storage.status", value => item.Status = value);
                SetIfPresent(values, "storage.notes", value => item.Notes = value);
                item.UpdatedAtUtc = now;
                break;
        }
    }

    private async Task ValidateCrossFieldsAsync(
        int companyId,
        string targetType,
        IReadOnlyDictionary<string, string?> values,
        IDictionary<string, string> errors,
        CancellationToken cancellationToken)
    {
        if (targetType == ImportTargetTypes.Vehicle
            && !string.IsNullOrWhiteSpace(Get(values, "vehicle.subtype"))
            && string.IsNullOrWhiteSpace(Get(values, "vehicle.function")))
            errors["vehicle.function"] = "Vehicle function is required when subtype is supplied.";

        if (targetType == ImportTargetTypes.Vehicle
            && !string.IsNullOrWhiteSpace(Get(values, "vehicle.subtype"))
            && !string.IsNullOrWhiteSpace(Get(values, "vehicle.function")))
        {
            var function = Get(values, "vehicle.function")!;
            var subtype = Get(values, "vehicle.subtype")!;
            var validPair = await _db.VehicleSubtypeSetups.AsNoTracking().AnyAsync(item =>
                item.CompanyId == companyId && item.Status == "Active" &&
                item.Name.ToLower() == subtype.ToLower() &&
                item.VehicleFunctionSetup != null && item.VehicleFunctionSetup.CompanyId == companyId &&
                item.VehicleFunctionSetup.Name.ToLower() == function.ToLower(), cancellationToken);
            if (!validPair) errors["vehicle.subtype"] = "Vehicle subtype does not belong to the selected function.";
        }
    }

    private async Task<bool> WouldCreateAreaCycleAsync(int companyId, int areaId, int parentId, CancellationToken cancellationToken)
    {
        if (areaId == parentId) return true;
        var areas = await _db.OperationalAreas.AsNoTracking()
            .Where(area => area.CompanyId == companyId)
            .Select(area => new { area.Id, area.ParentOperationalAreaId })
            .ToListAsync(cancellationToken);
        var parents = areas.ToDictionary(area => area.Id, area => area.ParentOperationalAreaId);
        var seen = new HashSet<int>();
        var current = (int?)parentId;
        while (current is not null && seen.Add(current.Value))
        {
            if (current.Value == areaId) return true;
            current = parents.GetValueOrDefault(current.Value);
        }
        return false;
    }

    private static void MarkSourceDuplicates(ImportBatch batch)
    {
        var rows = batch.RowResults.Where(row => row.IsIncluded && row.ValidationStatus != ImportRowStatuses.Invalid).ToList();
        foreach (var group in rows.GroupBy(row => BuildSourceDuplicateKey(batch.TargetType, DeserializeDictionary(row.CorrectedPayloadJson ?? row.OriginalPayloadJson))))
        {
            if (string.IsNullOrWhiteSpace(group.Key) || group.Count() < 2) continue;
            foreach (var row in group)
            {
                var errors = string.IsNullOrWhiteSpace(row.FieldErrorsJson)
                    ? new Dictionary<string, string>()
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(row.FieldErrorsJson, JsonOptions) ?? [];
                errors["duplicate"] = $"Duplicate source key also appears on rows {string.Join(", ", group.Select(item => item.SourceRowNumber))}. Exclude duplicate source rows before commit.";
                row.FieldErrorsJson = JsonSerializer.Serialize(errors, JsonOptions);
                row.ValidationStatus = ImportRowStatuses.Invalid;
                row.RowDecision = null;
            }
        }
    }

    private static string BuildSourceDuplicateKey(string targetType, IReadOnlyDictionary<string, string?> values) => targetType switch
    {
        ImportTargetTypes.Vehicle => Normalize(Get(values, "vehicle.registration_number")),
        ImportTargetTypes.Staff => Normalize(Get(values, "staff.email")),
        ImportTargetTypes.Equipment => Normalize(Get(values, "equipment.serial_asset_id")),
        ImportTargetTypes.Stock => $"{Normalize(Get(values, "stock.item_name"))}|{Normalize(Get(values, "stock.batch"))}|{Get(values, "stock.operational_area")}",
        ImportTargetTypes.Medication => $"{Normalize(Get(values, "medication.code") ?? Get(values, "medication.name"))}|{Normalize(Get(values, "medication.batch"))}|{Get(values, "medication.operational_area")}",
        ImportTargetTypes.OperationalArea => $"{Normalize(Get(values, "area.name"))}|{Normalize(Get(values, "area.type"))}|{Get(values, "area.parent")}",
        ImportTargetTypes.StorageLocation => $"{Normalize(Get(values, "storage.name"))}|{Get(values, "storage.operational_area")}",
        _ => string.Empty
    };

    private async Task<OperationalArea?> ResolveAreaAsync(int companyId, string name, CancellationToken cancellationToken)
    {
        if (int.TryParse(name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            return await _db.OperationalAreas.AsNoTracking()
                .SingleOrDefaultAsync(area => area.CompanyId == companyId && area.Id == id, cancellationToken);
        }
        var normalized = Normalize(name);
        var areas = await _db.OperationalAreas.AsNoTracking().Where(area => area.CompanyId == companyId).ToListAsync(cancellationToken);
        return areas.SingleOrDefault(area => Normalize(area.Name) == normalized);
    }

    private static bool MedicationMatches(MedicationItem item, IReadOnlyDictionary<string, string?> values)
    {
        var code = Get(values, "medication.code");
        var batch = Get(values, "medication.batch");
        if (!string.IsNullOrWhiteSpace(code))
            return Normalize(item.MedicationCode) == Normalize(code) && Normalize(item.BatchNumber) == Normalize(batch);
        return Normalize(item.Name) == Normalize(Get(values, "medication.name"))
            && Normalize(item.BatchNumber) == Normalize(batch)
            && item.CurrentOperationalAreaId == GetInt(values, "medication.operational_area");
    }

    private static bool MatchesAny(IReadOnlyDictionary<string, string?> values, params (string Key, string? Existing)[] candidates) =>
        candidates.Any(candidate => !string.IsNullOrWhiteSpace(Get(values, candidate.Key))
            && Normalize(Get(values, candidate.Key)) == Normalize(candidate.Existing));

    private static string? NormalizeDecision(string? decision, int duplicateCount)
    {
        if (decision is not null && ImportRowDecisions.All.Contains(decision)) return decision;
        return duplicateCount == 0 ? ImportRowDecisions.Create : null;
    }

    private static string DetermineBatchStatus(ImportBatch batch)
    {
        if (batch.RowResults.Any(row => row.IsIncluded && row.ValidationStatus == ImportRowStatuses.Invalid))
            return ImportBatchStatuses.CorrectionRequired;
        if (batch.RowResults.Any(row => row.IsIncluded && row.ValidationStatus == ImportRowStatuses.Duplicate && string.IsNullOrWhiteSpace(row.RowDecision)))
            return ImportBatchStatuses.CorrectionRequired;
        return ImportBatchStatuses.ReadyToCommit;
    }

    private static void UpdateBatchCounts(ImportBatch batch)
    {
        batch.IncludedRowCount = batch.RowResults.Count(row => row.IsIncluded);
        batch.ValidRowCount = batch.RowResults.Count(row => row.IsIncluded && row.ValidationStatus is ImportRowStatuses.Valid or ImportRowStatuses.Duplicate);
        batch.InvalidRowCount = batch.RowResults.Count(row => row.IsIncluded && row.ValidationStatus == ImportRowStatuses.Invalid);
        batch.WarningRowCount = batch.RowResults.Count(row => !string.IsNullOrWhiteSpace(row.WarningsJson));
    }

    private static ImportCommitResult SummarizeCommitted(ImportBatch batch)
    {
        var created = batch.EntityChanges.Count(change => change.Action == ImportRowDecisions.Create);
        var updated = batch.EntityChanges.Count(change => change.Action == ImportRowDecisions.Update);
        var excluded = batch.RowResults.Count(row => !row.IsIncluded);
        var skipped = batch.RowResults.Count(row => row.IsIncluded && row.RowDecision == ImportRowDecisions.Skip);
        return new ImportCommitResult(created, updated, skipped, excluded);
    }

    private static Dictionary<string, string?> DeserializeDictionary(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, string?>>(json, JsonOptions)
        ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    private static string? SerializeOrNull<T>(T value)
    {
        if (value is ICollection<string> strings && strings.Count == 0) return null;
        if (value is ICollection<ImportDuplicateCandidate> duplicates && duplicates.Count == 0) return null;
        if (value is IDictionary<string, string> dictionary && dictionary.Count == 0) return null;
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private static string Normalize(string? value) => Regex.Replace(value?.Trim().ToLowerInvariant() ?? string.Empty, "[^a-z0-9]+", string.Empty);
    private static string? Collapse(string? value) => string.IsNullOrWhiteSpace(value) ? null : Regex.Replace(value.Trim(), @"\s+", " ");
    private static string ToTitleCase(string value) => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.Trim().ToLowerInvariant());
    private static string? Get(IReadOnlyDictionary<string, string?> values, string key) => values.TryGetValue(key, out var value) ? value : null;
    private static string Required(IReadOnlyDictionary<string, string?> values, string key) => Get(values, key) ?? throw new InvalidOperationException($"Required value '{key}' is missing.");
    private static int? GetInt(IReadOnlyDictionary<string, string?> values, string key) => int.TryParse(Get(values, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
    private static void SetIfPresent(IReadOnlyDictionary<string, string?> values, string key, Action<string> setter) { if (!string.IsNullOrWhiteSpace(Get(values, key))) setter(Get(values, key)!); }
    private static void SetNullableInt(IReadOnlyDictionary<string, string?> values, string key, Action<int?> setter) { if (!string.IsNullOrWhiteSpace(Get(values, key))) setter(GetInt(values, key)); }
    private static void SetDate(IReadOnlyDictionary<string, string?> values, string key, Action<DateTime?> setter) { if (DateTime.TryParse(Get(values, key), CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)) setter(value); }
    private static void SetBoolean(IReadOnlyDictionary<string, string?> values, string key, Action<bool> setter) { if (bool.TryParse(Get(values, key), out var value)) setter(value); }
    private static bool TryBoolean(string raw, out bool value)
    {
        if (new[] { "yes", "true", "1", "y" }.Contains(raw.Trim(), StringComparer.OrdinalIgnoreCase)) { value = true; return true; }
        if (new[] { "no", "false", "0", "n" }.Contains(raw.Trim(), StringComparer.OrdinalIgnoreCase)) { value = false; return true; }
        value = false; return false;
    }
    private static bool TryDate(string raw, out DateTime value)
    {
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out value)) return true;
        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial) && serial is >= 1 and <= 2958465)
        {
            try { value = DateTime.FromOADate(serial); return true; } catch (ArgumentException) { }
        }
        value = default; return false;
    }
    private static IReadOnlyDictionary<string, object?> SnapshotEntity(object entity)
    {
        return entity.GetType().GetProperties()
            .Where(property => property.CanRead
                && (property.PropertyType.IsPrimitive
                    || property.PropertyType.IsEnum
                    || property.PropertyType == typeof(string)
                    || property.PropertyType == typeof(DateTime)
                    || property.PropertyType == typeof(DateTime?)
                    || property.PropertyType == typeof(int?)
                    || property.PropertyType == typeof(bool?)))
            .ToDictionary(property => property.Name, property => property.GetValue(entity));
    }
    private static string ComputeStateToken(string json) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json)));
    private static void Touch(ImportBatch batch) { batch.UpdatedAtUtc = DateTime.UtcNow; batch.ConcurrencyToken = Guid.NewGuid().ToString("D"); }

    private sealed record ValueConversion(string? Value, string? Error);
    private sealed record RowValidation(
        string Status,
        IReadOnlyDictionary<string, string?> Values,
        IReadOnlyDictionary<string, string> Errors,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<ImportDuplicateCandidate> Duplicates);
    private sealed record DomainWriteResult(int EntityId, string Action, string? BeforeJson, string AfterJson, string StateToken);
}
