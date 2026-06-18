using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public class ReadinessEngineService
{
    private readonly VectorDbContext _db;

    public ReadinessEngineService(VectorDbContext db)
    {
        _db = db;
    }

    public async Task<ReadinessEngineVersion> EnsureDraftVersionAsync(AppUser currentUser)
    {
        await EnsureDefaultPublishedEngineAsync(_db, currentUser.CompanyId, currentUser.Id);

        var draft = await _db.ReadinessEngineVersions
            .Include(version => version.Rules)
            .Where(version =>
                version.CompanyId == currentUser.CompanyId &&
                version.Status == ReadinessEngineStatuses.Draft)
            .OrderByDescending(version => version.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (draft is not null)
        {
            return draft;
        }

        var published = await LoadPublishedVersionAsync(currentUser.CompanyId);
        if (published is null)
        {
            throw new InvalidOperationException("No readiness engine version is available.");
        }

        draft = new ReadinessEngineVersion
        {
            CompanyId = currentUser.CompanyId,
            Name = "Draft readiness engine",
            VersionNumber = NextVersionNumber(published.VersionNumber),
            Status = ReadinessEngineStatuses.Draft,
            SourceReadinessEngineVersionId = published.Id,
            CreatedByUserId = currentUser.Id,
            Notes = "Draft created from the active published readiness engine.",
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.ReadinessEngineVersions.Add(draft);
        await _db.SaveChangesAsync();

        foreach (var rule in published.Rules.OrderBy(rule => rule.SortOrder).ThenBy(rule => rule.Id))
        {
            var draftRule = CloneRule(rule, currentUser.CompanyId);
            draftRule.ReadinessEngineVersionId = draft.Id;
            _db.ReadinessEngineRules.Add(draftRule);
        }

        AuditTrailService.Record(
            _db,
            currentUser.CompanyId,
            currentUser.Id,
            "Readiness engine draft created",
            "ReadinessEngineVersion",
            draft.Id,
            $"Draft readiness engine {draft.VersionNumber} created from published version {published.VersionNumber}.");
        await _db.SaveChangesAsync();
        return draft;
    }

    public async Task<ReadinessEngineVersion?> LoadPublishedVersionAsync(int companyId)
    {
        return await _db.ReadinessEngineVersions
            .Include(version => version.Rules)
            .Where(version =>
                version.CompanyId == companyId &&
                version.Status == ReadinessEngineStatuses.Published)
            .OrderByDescending(version => version.PublishedAtUtc ?? version.CreatedAtUtc)
            .FirstOrDefaultAsync();
    }

    public async Task<int> AutoPopulateSuggestedRulesAsync(AppUser currentUser, int versionId)
    {
        var version = await _db.ReadinessEngineVersions
            .Include(item => item.Rules)
            .FirstOrDefaultAsync(item =>
                item.Id == versionId &&
                item.CompanyId == currentUser.CompanyId &&
                item.Status == ReadinessEngineStatuses.Draft);

        if (version is null)
        {
            return 0;
        }

        var beforeCount = version.Rules.Count;
        var fingerprints = version.Rules
            .Select(BuildFingerprint)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var nextOrder = version.Rules.Count == 0 ? 10 : version.Rules.Max(rule => rule.SortOrder) + 10;

        void AddSuggested(ReadinessEngineRule rule)
        {
            rule.CompanyId = currentUser.CompanyId;
            rule.ReadinessEngineVersionId = version.Id;
            rule.IsAutoPopulated = true;
            rule.SourceType = "Auto-populated";
            rule.IsActive = false;
            rule.SortOrder = nextOrder;
            rule.CreatedAtUtc = DateTime.UtcNow;

            var fingerprint = BuildFingerprint(rule);
            if (!fingerprints.Add(fingerprint))
            {
                return;
            }

            nextOrder += 10;
            version.Rules.Add(rule);
        }

        var vehicleTypes = await _db.Vehicles
            .AsNoTracking()
            .Where(vehicle => vehicle.CompanyId == currentUser.CompanyId && vehicle.Status != "Deleted")
            .Select(vehicle => vehicle.VehicleType)
            .Distinct()
            .ToListAsync();

        foreach (var vehicleType in vehicleTypes.Where(type => !string.IsNullOrWhiteSpace(type)))
        {
            AddSuggested(CreateRule("Vehicle", "Vehicle register", vehicleType, "Next service date", "Service overdue", "Vehicle type", vehicleType, null, ReadinessRuleSeverity.Major, 20, false, true, "VehicleType", null, ReadinessRuleScope.AssignedToActiveVehicle));
            AddSuggested(CreateRule("Vehicle", "Vehicle status", vehicleType, "Status", "Out of service / unavailable", "Vehicle type", vehicleType, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, "VehicleType", null, ReadinessRuleScope.AssignedToActiveVehicle));
        }

        var equipmentGroups = await _db.EquipmentItems
            .AsNoTracking()
            .Where(item => item.CompanyId == currentUser.CompanyId && item.Status != "Deleted")
            .Select(item => new
            {
                Label = string.IsNullOrWhiteSpace(item.EquipmentType) ? item.Name : item.EquipmentType,
                item.BatteryRequired
            })
            .Distinct()
            .ToListAsync();

        foreach (var item in equipmentGroups.Where(item => !string.IsNullOrWhiteSpace(item.Label)))
        {
            AddSuggested(CreateRule("Equipment", "Carried equipment", item.Label, "PresentStatus", "Missing", "All", null, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, "EquipmentRegister", null, ReadinessRuleScope.AssignedToActiveVehicle));
            AddSuggested(CreateRule("Equipment", "Carried equipment", item.Label, "Operational", "No", "All", null, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, "EquipmentRegister", null, ReadinessRuleScope.AssignedToActiveVehicle));
            AddSuggested(CreateRule("Equipment", "Equipment register", item.Label, "NextServiceDate", "Service overdue", "All", null, null, ReadinessRuleSeverity.Major, 20, false, true, "EquipmentRegister", null, ReadinessRuleScope.AssignedToActiveVehicle));
            AddSuggested(CreateRule("Equipment", "Equipment register", item.Label, "SerialOrAssetId", "S/N mismatch or wrong location", "All", null, null, ReadinessRuleSeverity.Moderate, 10, false, true, "EquipmentRegister", null, ReadinessRuleScope.AssignedToActiveVehicle));

            if (item.BatteryRequired)
            {
                AddSuggested(CreateRule("Equipment", "Carried equipment", item.Label, "BatteryStatus", "Low", "All", null, null, ReadinessRuleSeverity.Major, 20, false, true, "EquipmentRegister", null, ReadinessRuleScope.AssignedToActiveVehicle));
                AddSuggested(CreateRule("Equipment", "Carried equipment", item.Label, "BatteryStatus", "Flat / failed", "All", null, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, "EquipmentRegister", null, ReadinessRuleScope.AssignedToActiveVehicle));
            }
        }

        var stockNames = await _db.StockItems
            .AsNoTracking()
            .Where(item => item.CompanyId == currentUser.CompanyId && item.Status != "Deleted")
            .Select(item => item.ItemName)
            .Distinct()
            .ToListAsync();

        foreach (var itemName in stockNames.Where(name => !string.IsNullOrWhiteSpace(name)))
        {
            AddSuggested(CreateRule("Stock", "Stock register", itemName, "Quantity", "Below minimum", "All", null, null, ReadinessRuleSeverity.Major, 20, false, true, "StockRegister", null, ReadinessRuleScope.RequiredStockMinimum));
            AddSuggested(CreateRule("Stock", "Stock register", itemName, "ExpiryDate", "Expired", "All", null, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, "StockRegister", null, ReadinessRuleScope.RequiredStockMinimum));
            AddSuggested(CreateRule("Stock", "Stock register", itemName, "Location", "Wrong location or batch discrepancy", "All", null, null, ReadinessRuleSeverity.Moderate, 10, false, true, "StockRegister", null, ReadinessRuleScope.RequiredStockMinimum));
        }

        var medicationNames = await _db.MedicationItems
            .AsNoTracking()
            .Where(item => item.CompanyId == currentUser.CompanyId && item.Status != "Deleted")
            .Select(item => item.Name)
            .Distinct()
            .ToListAsync();

        foreach (var itemName in medicationNames.Where(name => !string.IsNullOrWhiteSpace(name)))
        {
            AddSuggested(CreateRule("Medication", "Medication register", itemName, "Quantity", "Below minimum", "All", null, null, ReadinessRuleSeverity.Critical, 40, false, true, "MedicationRegister", null, ReadinessRuleScope.RequiredStockMinimum));
            AddSuggested(CreateRule("Medication", "Medication register", itemName, "ExpiryDate", "Expired", "All", null, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, "MedicationRegister", null, ReadinessRuleScope.RequiredStockMinimum));
            AddSuggested(CreateRule("Medication", "Medication register", itemName, "Location", "Wrong location or batch discrepancy", "All", null, null, ReadinessRuleSeverity.Moderate, 10, false, true, "MedicationRegister", null, ReadinessRuleScope.RequiredStockMinimum));
        }

        var addedCount = version.Rules.Count - beforeCount;
        if (addedCount > 0)
        {
            AuditTrailService.Record(
                _db,
                currentUser.CompanyId,
                currentUser.Id,
                "Readiness engine rules auto-populated",
                "ReadinessEngineVersion",
                version.Id,
                $"{addedCount} suggested scoring rule(s) added from active registers.");
        }

        await _db.SaveChangesAsync();
        return addedCount;
    }

    public async Task PublishDraftAsync(AppUser currentUser, int versionId)
    {
        var draft = await _db.ReadinessEngineVersions
            .FirstOrDefaultAsync(version =>
                version.Id == versionId &&
                version.CompanyId == currentUser.CompanyId &&
                version.Status == ReadinessEngineStatuses.Draft);

        if (draft is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var publishedVersions = await _db.ReadinessEngineVersions
            .Where(version =>
                version.CompanyId == currentUser.CompanyId &&
                version.Status == ReadinessEngineStatuses.Published)
            .ToListAsync();

        foreach (var published in publishedVersions)
        {
            published.Status = ReadinessEngineStatuses.Archived;
            published.UpdatedAtUtc = now;
        }

        draft.Status = ReadinessEngineStatuses.Published;
        draft.PublishedByUserId = currentUser.Id;
        draft.PublishedAtUtc = now;
        draft.UpdatedAtUtc = now;

        AuditTrailService.Record(
            _db,
            currentUser.CompanyId,
            currentUser.Id,
            "Readiness engine published",
            "ReadinessEngineVersion",
            draft.Id,
            $"{draft.Name} {draft.VersionNumber} published.",
            now);

        await _db.SaveChangesAsync();
    }

    public async Task CreateChangeRequestAsync(
        AppUser currentUser,
        int ruleId,
        string proposedSeverity,
        int? proposedImpactPercent,
        bool? proposedActive,
        string reason)
    {
        var rule = await _db.ReadinessEngineRules
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == ruleId && item.CompanyId == currentUser.CompanyId);

        if (rule is null)
        {
            return;
        }

        var request = new ReadinessScoringChangeRequest
        {
            CompanyId = currentUser.CompanyId,
            RequestedByUserId = currentUser.Id,
            ReadinessEngineRuleId = rule.Id,
            AssetType = rule.AssetType,
            ItemName = rule.ItemName,
            TriggerValue = rule.TriggerValue,
            CurrentSeverity = rule.Severity,
            ProposedSeverity = string.IsNullOrWhiteSpace(proposedSeverity) ? rule.Severity : proposedSeverity,
            CurrentImpactPercent = rule.ManualImpactPercent ?? rule.DefaultImpactPercent,
            ProposedImpactPercent = proposedImpactPercent,
            CurrentActive = rule.IsActive,
            ProposedActive = proposedActive,
            Reason = reason.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.ReadinessScoringChangeRequests.Add(request);
        await _db.SaveChangesAsync();

        AuditTrailService.Record(
            _db,
            currentUser.CompanyId,
            currentUser.Id,
            "Readiness scoring request created",
            "ReadinessScoringChangeRequest",
            request.Id,
            $"{currentUser.FullName} requested scoring change for {request.AssetType} / {request.ItemName}: {request.Reason}");
        await _db.SaveChangesAsync();
    }

    public async Task ReviewChangeRequestAsync(AppUser currentUser, int requestId, bool approve, string? decisionNote)
    {
        var request = await _db.ReadinessScoringChangeRequests
            .Include(item => item.ReadinessEngineRule)
            .FirstOrDefaultAsync(item =>
                item.Id == requestId &&
                item.CompanyId == currentUser.CompanyId &&
                item.Status == "Pending");

        if (request is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        request.Status = approve ? "Approved" : "Rejected";
        request.ReviewedByUserId = currentUser.Id;
        request.ReviewedAtUtc = now;
        request.SeniorDecisionNote = decisionNote;

        if (approve && request.ReadinessEngineRule is not null)
        {
            request.ReadinessEngineRule.Severity = request.ProposedSeverity;
            if (request.ProposedImpactPercent.HasValue)
            {
                request.ReadinessEngineRule.ManualImpactPercent = request.ProposedImpactPercent;
            }
            if (request.ProposedActive.HasValue)
            {
                request.ReadinessEngineRule.IsActive = request.ProposedActive.Value;
            }

            request.ReadinessEngineRule.IsHardBlocker = string.Equals(request.ProposedSeverity, ReadinessRuleSeverity.HardBlocker, StringComparison.OrdinalIgnoreCase);
            request.ReadinessEngineRule.UpdatedAtUtc = now;
        }

        AuditTrailService.Record(
            _db,
            currentUser.CompanyId,
            currentUser.Id,
            approve ? "Readiness scoring request approved" : "Readiness scoring request rejected",
            "ReadinessScoringChangeRequest",
            request.Id,
            string.IsNullOrWhiteSpace(decisionNote) ? request.Reason : decisionNote,
            now);

        await _db.SaveChangesAsync();
    }

    public async Task DeleteChangeRequestAsync(AppUser currentUser, int requestId, string? decisionNote)
    {
        var request = await _db.ReadinessScoringChangeRequests
            .FirstOrDefaultAsync(item =>
                item.Id == requestId &&
                item.CompanyId == currentUser.CompanyId &&
                item.Status == "Pending");

        if (request is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        request.Status = "Deleted";
        request.ReviewedByUserId = currentUser.Id;
        request.ReviewedAtUtc = now;
        request.SeniorDecisionNote = string.IsNullOrWhiteSpace(decisionNote)
            ? "Deleted by senior management."
            : decisionNote.Trim();

        AuditTrailService.Record(
            _db,
            currentUser.CompanyId,
            currentUser.Id,
            "Readiness scoring request deleted",
            "ReadinessScoringChangeRequest",
            request.Id,
            request.SeniorDecisionNote,
            now);

        await _db.SaveChangesAsync();
    }

    public static async Task EnsureDefaultPublishedEngineAsync(VectorDbContext db, int companyId, int? createdByUserId)
    {
        var version = await db.ReadinessEngineVersions
            .Include(item => item.Rules)
            .Where(item => item.CompanyId == companyId && item.Status == ReadinessEngineStatuses.Published)
            .OrderByDescending(item => item.PublishedAtUtc ?? item.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (version is null)
        {
            version = new ReadinessEngineVersion
            {
                CompanyId = companyId,
                Name = "Default AcuityOps Engine",
                VersionNumber = "1.0",
                Status = ReadinessEngineStatuses.Published,
                CreatedByUserId = createdByUserId,
                PublishedByUserId = createdByUserId,
                Notes = "Default readiness scoring rules supplied by AcuityOps.",
                CreatedAtUtc = DateTime.UtcNow,
                PublishedAtUtc = DateTime.UtcNow
            };

            db.ReadinessEngineVersions.Add(version);
            AuditTrailService.Record(
                db,
                companyId,
                createdByUserId,
                "Default readiness engine created",
                "ReadinessEngineVersion",
                null,
                "Default AcuityOps readiness scoring engine created.");
            await db.SaveChangesAsync();
        }

        var fingerprints = version.Rules
            .Select(BuildFingerprint)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var order = version.Rules.Count == 0 ? 10 : version.Rules.Max(rule => rule.SortOrder) + 10;

        foreach (var rule in DefaultRules(companyId))
        {
            if (!fingerprints.Add(BuildFingerprint(rule)))
            {
                continue;
            }

            rule.CompanyId = companyId;
            rule.SortOrder = order;
            rule.ReadinessEngineVersionId = version.Id;
            order += 10;
            db.ReadinessEngineRules.Add(rule);
        }

        await db.SaveChangesAsync();
    }

    private static IEnumerable<ReadinessEngineRule> DefaultRules(int companyId)
    {
        yield return CreateRule("Checklist Completion", "Daily readiness", "Daily vehicle readiness", "Completion", "Missing completed check", "All", null, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, "Default", null);
        yield return CreateRule("Checklist Completion", "Daily readiness", "Required field", "Completion", "Omitted required field", "All", null, null, ReadinessRuleSeverity.Major, 20, false, true, "Default", null);
        yield return CreateRule("Vehicle", "Vehicle status", "Vehicle", "Status", "Out of service / unavailable", "All", null, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, "Default", null);
        yield return CreateRule("Vehicle", "Operational checks", "Lights", "LightsStatus", "Failed / not operational", "All", null, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, "Default", null);
        yield return CreateRule("Vehicle", "Operational checks", "Sirens", "SirensStatus", "Failed / not operational", "All", null, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, "Default", null);
        yield return CreateRule("Vehicle", "Operational checks", "Warning lights", "WarningLightsStatus", "Failed / not operational", "All", null, null, ReadinessRuleSeverity.Major, 20, false, true, "Default", null);
        yield return CreateRule("Vehicle", "Operational checks", "Tyres", "TyresStatus", "Unsafe / failed", "All", null, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, "Default", null);
        yield return CreateRule("Vehicle", "Operational checks", "Ops radio", "RadioConnectivityStatus", "Disconnected / failed", "All", null, null, ReadinessRuleSeverity.Major, 20, false, true, "Default", null);
        yield return CreateRule("Vehicle", "Vehicle register", "Next service date", "NextServiceDate", "Service overdue", "All", null, null, ReadinessRuleSeverity.Major, 20, false, true, "Default", null);
        yield return CreateRule("Vehicle", "Schematic / damage", "Damage", "DamageStatus", "Minor cosmetic", "All", null, null, ReadinessRuleSeverity.Minor, 2, false, false, "Default", null);
        yield return CreateRule("Vehicle", "Schematic / damage", "Damage", "DamageStatus", "Operationally unsafe", "All", null, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, "Default", null);

        yield return CreateRule("Equipment", "Carried equipment", "Required equipment", "PresentStatus", "Missing", "All", null, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, "Default", null);
        yield return CreateRule("Equipment", "Carried equipment", "Required equipment", "Operational", "No", "All", null, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, "Default", null);
        yield return CreateRule("Equipment", "Carried equipment", "Battery", "BatteryStatus", "Low", "All", null, null, ReadinessRuleSeverity.Major, 20, false, true, "Default", null);
        yield return CreateRule("Equipment", "Carried equipment", "Battery", "BatteryStatus", "Charging", "All", null, null, ReadinessRuleSeverity.Minor, 3, false, false, "Default", null);
        yield return CreateRule("Equipment", "Carried equipment", "Battery", "BatteryStatus", "Flat / failed", "All", null, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, "Default", null);
        yield return CreateRule("Equipment", "Equipment register", "Service date", "NextServiceDate", "Service overdue", "All", null, null, ReadinessRuleSeverity.Major, 20, false, true, "Default", null);
        yield return CreateRule("Equipment", "Equipment register", "S/N / ID", "SerialOrAssetId", "S/N mismatch or wrong location", "All", null, null, ReadinessRuleSeverity.Moderate, 10, false, true, "Default", null);

        yield return CreateRule("Stock", "Stock register", "Required stock", "Quantity", "Below minimum", "All", null, null, ReadinessRuleSeverity.Major, 20, false, true, "Default", null, ReadinessRuleScope.RequiredStockMinimum);
        yield return CreateRule("Stock", "Stock register", "Required stock", "Quantity", "Missing", "All", null, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, "Default", null, ReadinessRuleScope.RequiredStockMinimum);
        yield return CreateRule("Stock", "Stock register", "Disposable stock", "ExpiryDate", "Expired", "All", null, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, "Default", null, ReadinessRuleScope.RequiredStockMinimum);

        yield return CreateRule("Medication", "Medication register", "Required medication", "Quantity", "Below minimum", "All", null, null, ReadinessRuleSeverity.Critical, 40, false, true, "Default", null, ReadinessRuleScope.RequiredStockMinimum);
        yield return CreateRule("Medication", "Medication register", "Required medication", "Quantity", "Missing", "All", null, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, "Default", null, ReadinessRuleScope.RequiredStockMinimum);
        yield return CreateRule("Medication", "Medication register", "Medication expiry", "ExpiryDate", "Expired", "All", null, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, "Default", null, ReadinessRuleScope.RequiredStockMinimum);

        yield return CreateRule("Issue Report", "Issue reports", "Open issue", "Severity", "Critical unresolved issue", "All", null, null, ReadinessRuleSeverity.Major, 20, false, true, "Default", null);
    }

    private static ReadinessEngineRule CreateRule(
        string assetType,
        string section,
        string itemName,
        string fieldKey,
        string triggerValue,
        string appliesTo,
        string? targetVehicleType,
        int? operationalAreaId,
        string severity,
        int defaultImpactPercent,
        bool isHardBlocker,
        bool managerAlert,
        string sourceType,
        int? sourceEntityId,
        string readinessScope = ReadinessRuleScope.ActiveShift)
    {
        return new ReadinessEngineRule
        {
            AssetType = assetType,
            Section = section,
            ItemName = itemName,
            FieldKey = fieldKey,
            TriggerValue = triggerValue,
            AppliesTo = appliesTo,
            ReadinessScope = ResolveDefaultScope(assetType, readinessScope),
            TargetVehicleType = targetVehicleType,
            OperationalAreaId = operationalAreaId,
            ChecklistTemplateId = null,
            Severity = severity,
            DefaultImpactPercent = defaultImpactPercent,
            ManualImpactPercent = null,
            IsHardBlocker = isHardBlocker || string.Equals(severity, ReadinessRuleSeverity.HardBlocker, StringComparison.OrdinalIgnoreCase),
            ManagerAlert = managerAlert,
            IsActive = true,
            SourceType = sourceType,
            SourceEntityId = sourceEntityId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static ReadinessEngineRule CloneRule(ReadinessEngineRule source, int companyId)
    {
        return new ReadinessEngineRule
        {
            CompanyId = companyId,
            AssetType = source.AssetType,
            Section = source.Section,
            ItemName = source.ItemName,
            FieldKey = source.FieldKey,
            TriggerValue = source.TriggerValue,
            AppliesTo = source.AppliesTo,
            ReadinessScope = source.ReadinessScope,
            TargetVehicleType = source.TargetVehicleType,
            OperationalAreaId = source.OperationalAreaId,
            ChecklistTemplateId = source.ChecklistTemplateId,
            Severity = source.Severity,
            DefaultImpactPercent = source.DefaultImpactPercent,
            ManualImpactPercent = source.ManualImpactPercent,
            IsHardBlocker = source.IsHardBlocker,
            ManagerAlert = source.ManagerAlert,
            IsActive = source.IsActive,
            IsAutoPopulated = source.IsAutoPopulated,
            SourceType = source.SourceType,
            SourceEntityType = source.SourceEntityType,
            SourceEntityId = source.SourceEntityId,
            Notes = source.Notes,
            SortOrder = source.SortOrder,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static string ResolveDefaultScope(string assetType, string readinessScope)
    {
        if (!string.Equals(readinessScope, ReadinessRuleScope.ActiveShift, StringComparison.OrdinalIgnoreCase))
        {
            return readinessScope;
        }

        if (string.Equals(assetType, "Vehicle", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(assetType, "Equipment", StringComparison.OrdinalIgnoreCase))
        {
            return ReadinessRuleScope.AssignedToActiveVehicle;
        }

        if (string.Equals(assetType, "Stock", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(assetType, "Medication", StringComparison.OrdinalIgnoreCase))
        {
            return ReadinessRuleScope.RequiredStockMinimum;
        }

        return ReadinessRuleScope.ActiveShift;
    }

    private static string BuildFingerprint(ReadinessEngineRule rule)
    {
        return string.Join("|", new[]
        {
            rule.AssetType,
            rule.Section,
            rule.ItemName,
            rule.FieldKey ?? string.Empty,
            rule.TriggerValue,
            rule.AppliesTo,
            rule.ReadinessScope,
            rule.TargetVehicleType ?? string.Empty,
            rule.OperationalAreaId?.ToString() ?? string.Empty
        }).ToUpperInvariant();
    }

    private static string NextVersionNumber(string versionNumber)
    {
        if (decimal.TryParse(versionNumber, out var numericVersion))
        {
            return (numericVersion + 0.1m).ToString("0.0");
        }

        return "1.1";
    }
}
