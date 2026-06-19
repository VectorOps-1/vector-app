using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public sealed record ReadinessDraftCleanupResult(int RemovedRules, int ReorderedRules);

public class ReadinessEngineService
{
    public const string ProductDefaultSourceType = "AcuityOps product default";

    private readonly VectorDbContext _db;

    public ReadinessEngineService(VectorDbContext db)
    {
        _db = db;
    }

    public async Task<ReadinessEngineVersion?> LoadDraftVersionAsync(int companyId)
    {
        return await _db.ReadinessEngineVersions
            .Include(version => version.Rules)
            .Where(version =>
                version.CompanyId == companyId &&
                version.Status == ReadinessEngineStatuses.Draft)
            .OrderByDescending(version => version.CreatedAtUtc)
            .FirstOrDefaultAsync();
    }

    public async Task<ReadinessEngineVersion> CreateDraftVersionAsync(AppUser currentUser)
    {
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
        var now = DateTime.UtcNow;

        draft = new ReadinessEngineVersion
        {
            CompanyId = currentUser.CompanyId,
            Name = "Draft readiness engine",
            VersionNumber = published is null ? "1.0" : NextVersionNumber(published.VersionNumber),
            Status = ReadinessEngineStatuses.Draft,
            SourceReadinessEngineVersionId = published?.Id,
            CreatedByUserId = currentUser.Id,
            Notes = published is null
                ? "Blank readiness engine draft created by senior management."
                : "Draft created from the active published readiness engine.",
            CreatedAtUtc = now
        };

        _db.ReadinessEngineVersions.Add(draft);
        await _db.SaveChangesAsync();

        if (published is not null)
        {
            foreach (var rule in published.Rules.OrderBy(rule => rule.SortOrder).ThenBy(rule => rule.Id))
            {
                var draftRule = CloneRule(rule, currentUser.CompanyId);
                draftRule.ReadinessEngineVersionId = draft.Id;
                _db.ReadinessEngineRules.Add(draftRule);
            }
        }

        AuditTrailService.Record(
            _db,
            currentUser.CompanyId,
            currentUser.Id,
            "Readiness engine draft created",
            "ReadinessEngineVersion",
            draft.Id,
            published is null
                ? $"Blank readiness engine draft {draft.VersionNumber} created."
                : $"Draft readiness engine {draft.VersionNumber} created from published version {published.VersionNumber}.");
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

    public async Task<int> ApplyProductDefaultRulesAsync(AppUser currentUser, int versionId)
    {
        var version = await _db.ReadinessEngineVersions
            .Include(item => item.Rules)
            .FirstOrDefaultAsync(item =>
                item.Id == versionId &&
                item.CompanyId == currentUser.CompanyId &&
                item.Status == ReadinessEngineStatuses.Draft);

        if (version is null ||
            version.Rules.Any(rule => string.Equals(rule.SourceType, ProductDefaultSourceType, StringComparison.OrdinalIgnoreCase)))
        {
            return 0;
        }

        var fingerprints = version.Rules
            .Select(BuildFingerprint)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var nextOrder = version.Rules.Count == 0 ? 10 : version.Rules.Max(rule => rule.SortOrder) + 10;
        var addedCount = 0;
        var now = DateTime.UtcNow;

        foreach (var rule in ProductDefaultRules())
        {
            rule.CompanyId = currentUser.CompanyId;
            rule.ReadinessEngineVersionId = version.Id;
            rule.SourceType = ProductDefaultSourceType;
            rule.SourceEntityType = "ProductDefault";
            rule.IsAutoPopulated = false;
            rule.SortOrder = nextOrder;
            rule.CreatedAtUtc = now;

            if (!fingerprints.Add(BuildFingerprint(rule)))
            {
                continue;
            }

            nextOrder += 10;
            addedCount++;
            version.Rules.Add(rule);
        }

        if (addedCount == 0)
        {
            return 0;
        }

        version.UpdatedAtUtc = now;
        AuditTrailService.Record(
            _db,
            currentUser.CompanyId,
            currentUser.Id,
            "Readiness engine product defaults applied",
            "ReadinessEngineVersion",
            version.Id,
            $"{addedCount} AcuityOps product default scoring rule(s) were intentionally added to draft {version.VersionNumber}.",
            now);

        await _db.SaveChangesAsync();
        return addedCount;
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

        var vehicleRows = await _db.Vehicles
            .AsNoTracking()
            .Where(vehicle => vehicle.CompanyId == currentUser.CompanyId && vehicle.Status != "Deleted")
            .ToListAsync();

        var vehicleTypes = vehicleRows
            .Select(VehicleTaxonomyService.DisplayClassification)
            .Where(type => !string.IsNullOrWhiteSpace(type))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var vehicleType in vehicleTypes)
        {
            AddSuggested(CreateRule("Vehicle", "Vehicle register", vehicleType, "Next service date", "Service overdue", "Vehicle function / subtype", vehicleType, null, ReadinessRuleSeverity.Major, 20, false, true, "VehicleClassification", null, ReadinessRuleScope.AssignedToActiveVehicle));
            AddSuggested(CreateRule("Vehicle", "Vehicle status", vehicleType, "Status", "Out of service / unavailable", "Vehicle function / subtype", vehicleType, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, "VehicleClassification", null, ReadinessRuleScope.AssignedToActiveVehicle));
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

    public async Task<ReadinessDraftCleanupResult> CleanDraftRulesAsync(AppUser currentUser, int versionId)
    {
        var version = await _db.ReadinessEngineVersions
            .Include(item => item.Rules)
            .FirstOrDefaultAsync(item =>
                item.Id == versionId &&
                item.CompanyId == currentUser.CompanyId &&
                item.Status == ReadinessEngineStatuses.Draft);

        if (version is null)
        {
            return new ReadinessDraftCleanupResult(0, 0);
        }

        var rulesToRemove = version.Rules
            .GroupBy(BuildFingerprint, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .SelectMany(group =>
            {
                var keep = group
                    .OrderBy(RuleRetentionRank)
                    .ThenByDescending(rule => rule.IsActive)
                    .ThenByDescending(rule => rule.ManualImpactPercent.HasValue)
                    .ThenByDescending(rule => !string.IsNullOrWhiteSpace(rule.Notes))
                    .ThenByDescending(rule => rule.UpdatedAtUtc ?? rule.CreatedAtUtc)
                    .ThenBy(rule => rule.SortOrder)
                    .ThenBy(rule => rule.Id)
                    .First();

                return group.Where(rule => rule.Id != keep.Id);
            })
            .ToList();
        var duplicateRuleCount = rulesToRemove.Count;
        var inactiveGeneratedSuggestions = version.Rules
            .Except(rulesToRemove)
            .Where(IsInactiveGeneratedSuggestion)
            .ToList();

        rulesToRemove.AddRange(inactiveGeneratedSuggestions);

        foreach (var rule in rulesToRemove)
        {
            version.Rules.Remove(rule);
            _db.ReadinessEngineRules.Remove(rule);
        }

        var reorderedCount = 0;
        var order = 10;
        foreach (var rule in version.Rules
            .Except(rulesToRemove)
            .OrderBy(rule => rule.SortOrder)
            .ThenBy(rule => rule.AssetType)
            .ThenBy(rule => rule.ItemName)
            .ThenBy(rule => rule.Id))
        {
            if (rule.SortOrder != order)
            {
                reorderedCount++;
                rule.SortOrder = order;
            }

            order += 10;
        }

        if (rulesToRemove.Count == 0 && reorderedCount == 0)
        {
            return new ReadinessDraftCleanupResult(0, 0);
        }

        var now = DateTime.UtcNow;
        version.UpdatedAtUtc = now;
        AuditTrailService.Record(
            _db,
            currentUser.CompanyId,
            currentUser.Id,
            "Readiness engine draft cleaned",
            "ReadinessEngineVersion",
            version.Id,
            $"Removed {duplicateRuleCount} duplicate draft rule(s), removed {inactiveGeneratedSuggestions.Count} inactive generated suggestion(s), and reordered {reorderedCount} retained rule(s).",
            now);

        await _db.SaveChangesAsync();
        return new ReadinessDraftCleanupResult(rulesToRemove.Count, reorderedCount);
    }

    public async Task PublishDraftAsync(AppUser currentUser, int versionId)
    {
        var draft = await _db.ReadinessEngineVersions
            .Include(version => version.Rules)
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
            $"{currentUser.FullName} published {draft.Name} {draft.VersionNumber} with {draft.Rules.Count} rule(s). {publishedVersions.Count} previous published version(s) archived.",
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
            $"{currentUser.FullName} requested scoring change for rule {rule.Id} ({request.AssetType} / {request.ItemName} / {request.TriggerValue}). Current: {request.CurrentSeverity} / -{request.CurrentImpactPercent}% / {(request.CurrentActive == true ? "active" : "inactive")}. Proposed: {request.ProposedSeverity} / -{request.ProposedImpactPercent ?? request.CurrentImpactPercent ?? 0}% / {(request.ProposedActive == false ? "inactive" : "active")}. Reason: {request.Reason}");
        await _db.SaveChangesAsync();
    }

    public async Task ReviewChangeRequestAsync(AppUser currentUser, int requestId, bool approve, string? decisionNote)
    {
        var request = await _db.ReadinessScoringChangeRequests
            .Include(item => item.ReadinessEngineRule)
            .Include(item => item.RequestedByUser)
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
            $"{currentUser.FullName} {(approve ? "approved" : "rejected")} scoring request {request.Id} from {request.RequestedByUser?.FullName ?? "operational manager"} for {request.AssetType} / {request.ItemName} / {request.TriggerValue}. Current: {request.CurrentSeverity} / -{request.CurrentImpactPercent}% / {(request.CurrentActive == false ? "inactive" : "active")}. Proposed: {request.ProposedSeverity} / -{request.ProposedImpactPercent ?? request.CurrentImpactPercent ?? 0}% / {(request.ProposedActive == false ? "inactive" : "active")}. Note: {(string.IsNullOrWhiteSpace(decisionNote) ? request.Reason : decisionNote)}",
            now);

        if (approve && request.ReadinessEngineRule is not null)
        {
            AuditTrailService.Record(
                _db,
                currentUser.CompanyId,
                currentUser.Id,
                "Readiness engine rule updated from approved request",
                "ReadinessEngineRule",
                request.ReadinessEngineRule.Id,
                $"Rule updated from approved scoring request {request.Id}: {request.AssetType} / {request.ItemName} / {request.TriggerValue}. New setting: {request.ReadinessEngineRule.Severity} / -{request.ReadinessEngineRule.ManualImpactPercent ?? request.ReadinessEngineRule.DefaultImpactPercent}% / {(request.ReadinessEngineRule.IsActive ? "active" : "inactive")}.",
                now);
        }

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

    private static IReadOnlyList<ReadinessEngineRule> ProductDefaultRules()
    {
        return
        [
            CreateRule("Checklist Completion", "Daily check", "Daily vehicle readiness", "Completion", "Missing completed check", "All", null, null, ReadinessRuleSeverity.Critical, 40, false, true, ProductDefaultSourceType, null, ReadinessRuleScope.AssignedToActiveVehicle),
            CreateRule("Vehicle", "Vehicle register", "Vehicle", "Status", "Out of service / unavailable", "All", null, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, ProductDefaultSourceType, null, ReadinessRuleScope.AssignedToActiveVehicle),
            CreateRule("Vehicle", "Vehicle register", "Next service date", "NextServiceDate", "Service overdue", "All", null, null, ReadinessRuleSeverity.Major, 20, false, true, ProductDefaultSourceType, null, ReadinessRuleScope.AssignedToActiveVehicle),
            CreateRule("Equipment", "Equipment check", "Required equipment", "PresentStatus", "Missing", "All", null, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, ProductDefaultSourceType, null, ReadinessRuleScope.AssignedToActiveVehicle),
            CreateRule("Equipment", "Equipment check", "Required equipment", "Operational", "No", "All", null, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, ProductDefaultSourceType, null, ReadinessRuleScope.AssignedToActiveVehicle),
            CreateRule("Equipment", "Equipment check", "Battery", "BatteryStatus", "Low", "All", null, null, ReadinessRuleSeverity.Major, 20, false, true, ProductDefaultSourceType, null, ReadinessRuleScope.AssignedToActiveVehicle),
            CreateRule("Equipment", "Equipment check", "Battery", "BatteryStatus", "Flat / failed", "All", null, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, ProductDefaultSourceType, null, ReadinessRuleScope.AssignedToActiveVehicle),
            CreateRule("Equipment", "Equipment register", "Next service date", "NextServiceDate", "Service overdue", "All", null, null, ReadinessRuleSeverity.Major, 20, false, true, ProductDefaultSourceType, null, ReadinessRuleScope.AssignedToActiveVehicle),
            CreateRule("Equipment", "Equipment register", "S/N / Asset ID", "SerialOrAssetId", "S/N mismatch or wrong location", "All", null, null, ReadinessRuleSeverity.Moderate, 10, false, true, ProductDefaultSourceType, null, ReadinessRuleScope.AssignedToActiveVehicle),
            CreateRule("Issue Report", "Issue reports", "Open issue", "Severity", "Critical unresolved issue", "All", null, null, ReadinessRuleSeverity.Critical, 40, false, true, ProductDefaultSourceType, null, ReadinessRuleScope.ActiveShift),
            CreateRule("Stock", "Stock register", "Required stock", "Quantity", "Below minimum", "All", null, null, ReadinessRuleSeverity.Major, 20, false, true, ProductDefaultSourceType, null, ReadinessRuleScope.RequiredStockMinimum),
            CreateRule("Stock", "Stock register", "Required stock", "ExpiryDate", "Expired", "All", null, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, ProductDefaultSourceType, null, ReadinessRuleScope.RequiredStockMinimum),
            CreateRule("Medication", "Medication register", "Required medication", "Quantity", "Below minimum", "All", null, null, ReadinessRuleSeverity.Critical, 40, false, true, ProductDefaultSourceType, null, ReadinessRuleScope.RequiredStockMinimum),
            CreateRule("Medication", "Medication register", "Required medication", "ExpiryDate", "Expired", "All", null, null, ReadinessRuleSeverity.HardBlocker, 100, true, true, ProductDefaultSourceType, null, ReadinessRuleScope.RequiredStockMinimum)
        ];
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
            NormalizeFingerprintPart(rule.AssetType),
            NormalizeFingerprintPart(rule.Section),
            NormalizeFingerprintPart(rule.ItemName),
            NormalizeFingerprintPart(rule.FieldKey),
            NormalizeFingerprintPart(rule.TriggerValue),
            NormalizeFingerprintPart(rule.AppliesTo),
            NormalizeFingerprintPart(rule.ReadinessScope),
            NormalizeFingerprintPart(rule.TargetVehicleType),
            rule.OperationalAreaId?.ToString() ?? string.Empty
        });
    }

    private static string NormalizeFingerprintPart(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();
    }

    private static int RuleRetentionRank(ReadinessEngineRule rule)
    {
        var sourceType = rule.SourceType ?? string.Empty;
        if (string.Equals(sourceType, "Custom", StringComparison.OrdinalIgnoreCase) && !rule.IsAutoPopulated)
        {
            return 0;
        }

        if (!rule.IsAutoPopulated &&
            !string.Equals(sourceType, ProductDefaultSourceType, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(sourceType, "Auto-populated", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (!rule.IsAutoPopulated &&
            string.Equals(sourceType, ProductDefaultSourceType, StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (!rule.IsAutoPopulated)
        {
            return 3;
        }

        return 4;
    }

    private static bool IsInactiveGeneratedSuggestion(ReadinessEngineRule rule)
    {
        var sourceType = rule.SourceType ?? string.Empty;
        return !rule.IsActive &&
            (rule.IsAutoPopulated || string.Equals(sourceType, "Auto-populated", StringComparison.OrdinalIgnoreCase)) &&
            !rule.ManualImpactPercent.HasValue &&
            string.IsNullOrWhiteSpace(rule.Notes);
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
