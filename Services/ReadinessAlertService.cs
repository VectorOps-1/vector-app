using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public class ReadinessAlertService
{
    private readonly VectorDbContext _db;

    public ReadinessAlertService(VectorDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAlertsForReportAsync(int reportId, int performedByUserId)
    {
        var report = await _db.DailyVehicleReadinessReports
            .Include(item => item.Vehicle)
                .ThenInclude(vehicle => vehicle!.CurrentOperationalArea)
            .Include(item => item.PerformedByUser)
                .ThenInclude(user => user!.AppRole)
            .Include(item => item.EquipmentChecks)
                .ThenInclude(check => check.EquipmentItem)
            .FirstOrDefaultAsync(item => item.Id == reportId);

        if (report?.Vehicle is null)
        {
            return 0;
        }

        var rules = await LoadManagerAlertRulesAsync(report.CompanyId);
        if (rules.Count == 0)
        {
            await ReadinessEngineService.EnsureDefaultPublishedEngineAsync(_db, report.CompanyId, null);
            rules = await LoadManagerAlertRulesAsync(report.CompanyId);
        }

        if (rules.Count == 0)
        {
            return 0;
        }

        var assignedToUserId = await ResolveSuperiorUserIdAsync(report);
        var createdCount = 0;

        async Task AddAlertAsync(
            string assetType,
            string sourceArea,
            string itemName,
            string? fieldKey,
            string triggerValue,
            string? sourceValue,
            DailyVehicleEquipmentCheck? equipmentCheck = null)
        {
            createdCount += await AddAlertIfRuleMatchesAsync(
                report,
                rules,
                assignedToUserId,
                assetType,
                sourceArea,
                itemName,
                fieldKey,
                triggerValue,
                sourceValue,
                equipmentCheck);
        }

        async Task ApplyOperationalCheckIfNeededAsync(string? value, string itemName, string fieldKey)
        {
            if (IsProblemStatus(value, "Operational", "Pass", "Passed", "OK", "Good", "N/A", "Not applicable"))
            {
                await AddAlertAsync("Vehicle", "Vehicle inspection", itemName, fieldKey, "Failed / not operational", value);
            }
        }

        if (IsUnavailable(report.Vehicle.Status))
        {
            await AddAlertAsync("Vehicle", "Vehicle register", "Vehicle", "Status", "Out of service / unavailable", report.Vehicle.Status);
        }

        if (report.Vehicle.NextServiceDate.HasValue && report.Vehicle.NextServiceDate.Value.Date < DateTime.UtcNow.Date)
        {
            await AddAlertAsync("Vehicle", "Vehicle register", "Next service date", "NextServiceDate", "Service overdue", report.Vehicle.NextServiceDate.Value.ToString("yyyy-MM-dd"));
        }

        await ApplyOperationalCheckIfNeededAsync(report.LightsStatus, "Lights", "LightsStatus");
        await ApplyOperationalCheckIfNeededAsync(report.SirensStatus, "Sirens", "SirensStatus");
        await ApplyOperationalCheckIfNeededAsync(report.WarningLightsStatus, "Warning lights", "WarningLightsStatus");
        await ApplyOperationalCheckIfNeededAsync(report.TyresStatus, "Tyres", "TyresStatus");
        await ApplyOperationalCheckIfNeededAsync(report.RadioConnectivityStatus, "Ops radio", "RadioConnectivityStatus");

        if (IsUnavailable(report.ReadinessStatus) || report.CriticalIssueCount > 0)
        {
            await AddAlertAsync("Issue Report", "Readiness result", "Open issue", "Severity", "Critical unresolved issue", report.ReadinessStatus);
        }

        if (report.EquipmentChecks.Count == 0)
        {
            await AddAlertAsync("Equipment", "Equipment check", "Required equipment", "PresentStatus", "Missing", "No equipment rows captured");
        }

        foreach (var check in report.EquipmentChecks)
        {
            if (IsProblemStatus(check.PresentStatus, "Present", "N/A", "Not applicable"))
            {
                await AddAlertAsync("Equipment", "Equipment check", check.EquipmentName, "PresentStatus", "Missing", check.PresentStatus, check);
            }

            if (!check.IsOperational)
            {
                await AddAlertAsync("Equipment", "Equipment check", check.EquipmentName, "Operational", "No", "No", check);
            }

            if (IsProblemStatus(check.BatteryStatus, "Full", "Acceptable", "Charging", "N/A", "Not applicable"))
            {
                var batteryTrigger = ContainsAny(check.BatteryStatus, "flat", "fail", "empty") ? "Flat / failed" : "Low";
                await AddAlertAsync("Equipment", "Equipment check", check.EquipmentName, "BatteryStatus", batteryTrigger, check.BatteryStatus, check);
            }

            if (check.NextServiceDateAtCheck.HasValue && check.NextServiceDateAtCheck.Value.Date < DateTime.UtcNow.Date)
            {
                await AddAlertAsync("Equipment", "Equipment check", check.EquipmentName, "NextServiceDate", "Service overdue", check.NextServiceDateAtCheck.Value.ToString("yyyy-MM-dd"), check);
            }

            if (!string.IsNullOrWhiteSpace(check.IssueNotes))
            {
                await AddAlertAsync("Equipment", "Equipment check", check.EquipmentName, "IssueNotes", "Issue reported", check.IssueNotes, check);
            }

            if (IsProblemStatus(check.DamageStatus, "No damage", "None", "Good", "N/A", "Not applicable"))
            {
                await AddAlertAsync("Equipment", "Equipment check", check.EquipmentName, "DamageStatus", "Damage reported", check.DamageStatus, check);
            }

            if (IsProblemStatus(check.ReadinessImpact, "None", "N/A", "Not applicable"))
            {
                await AddAlertAsync("Equipment", "Equipment check", check.EquipmentName, "ReadinessImpact", "Any issue", check.ReadinessImpact, check);
            }
        }

        if (createdCount > 0)
        {
            await _db.SaveChangesAsync();
        }

        return createdCount;
    }

    public async Task<bool> AcknowledgeAlertAsync(int alertId, AppUser reviewer, string? reviewNote)
    {
        return await UpdateAlertStatusAsync(alertId, reviewer, ReadinessAlertStatuses.Acknowledged, "Readiness alert acknowledged", reviewNote);
    }

    public async Task<bool> ResolveAlertAsync(int alertId, AppUser reviewer, string? reviewNote)
    {
        return await UpdateAlertStatusAsync(alertId, reviewer, ReadinessAlertStatuses.Resolved, "Readiness alert resolved", reviewNote);
    }

    public async Task<bool> DeleteAlertAsync(int alertId, AppUser reviewer, string? reviewNote)
    {
        return await UpdateAlertStatusAsync(alertId, reviewer, ReadinessAlertStatuses.Deleted, "Readiness alert deleted", reviewNote);
    }

    private async Task<List<ReadinessEngineRule>> LoadManagerAlertRulesAsync(int companyId)
    {
        return await _db.ReadinessEngineRules
            .AsNoTracking()
            .Include(rule => rule.ReadinessEngineVersion)
            .Where(rule =>
                rule.CompanyId == companyId &&
                rule.IsActive &&
                rule.ManagerAlert &&
                rule.ReadinessEngineVersion != null &&
                rule.ReadinessEngineVersion.Status == ReadinessEngineStatuses.Published)
            .OrderBy(rule => rule.SortOrder)
            .ToListAsync();
    }

    private async Task<int> AddAlertIfRuleMatchesAsync(
        DailyVehicleReadinessReport report,
        IReadOnlyList<ReadinessEngineRule> rules,
        int? assignedToUserId,
        string assetType,
        string sourceArea,
        string itemName,
        string? fieldKey,
        string triggerValue,
        string? sourceValue,
        DailyVehicleEquipmentCheck? equipmentCheck)
    {
        var rule = FindRule(rules, report.Vehicle!, assetType, itemName, fieldKey, triggerValue);
        if (rule is null)
        {
            return 0;
        }

        var normalizedSourceValue = NormalizeOptional(sourceValue);
        var duplicateExists = await _db.ReadinessAlerts.AnyAsync(alert =>
            alert.CompanyId == report.CompanyId &&
            alert.ReadinessEngineRuleId == rule.Id &&
            alert.DailyVehicleReadinessReportId == report.Id &&
            alert.ItemName == itemName &&
            alert.FieldKey == fieldKey &&
            alert.TriggerValue == triggerValue &&
            alert.SourceValue == normalizedSourceValue &&
            (alert.Status == ReadinessAlertStatuses.Open || alert.Status == ReadinessAlertStatuses.Acknowledged));

        if (duplicateExists)
        {
            return 0;
        }

        var impact = Math.Clamp(rule.ManualImpactPercent ?? rule.DefaultImpactPercent, 0, 100);
        var isHardBlocker = rule.IsHardBlocker ||
            string.Equals(rule.Severity, ReadinessRuleSeverity.HardBlocker, StringComparison.OrdinalIgnoreCase);
        var vehicleLabel = BuildVehicleLabel(report);

        _db.ReadinessAlerts.Add(new ReadinessAlert
        {
            CompanyId = report.CompanyId,
            ReadinessEngineRuleId = rule.Id,
            DailyVehicleReadinessReportId = report.Id,
            DailyVehicleEquipmentCheckId = equipmentCheck?.Id,
            VehicleId = report.VehicleId,
            TriggeredByUserId = report.PerformedByUserId,
            AssignedToUserId = assignedToUserId,
            AssetType = assetType,
            SourceArea = sourceArea,
            ItemName = itemName,
            FieldKey = NormalizeOptional(fieldKey),
            TriggerValue = triggerValue,
            Severity = rule.Severity,
            ImpactPercent = impact,
            IsHardBlocker = isHardBlocker,
            Status = ReadinessAlertStatuses.Open,
            VehicleLabel = vehicleLabel,
            SourceValue = normalizedSourceValue,
            AlertSummary = BuildAlertSummary(vehicleLabel, itemName, fieldKey, triggerValue, normalizedSourceValue, rule.Severity, impact),
            CreatedAtUtc = DateTime.UtcNow
        });

        return 1;
    }

    private async Task<bool> UpdateAlertStatusAsync(int alertId, AppUser reviewer, string status, string auditAction, string? reviewNote)
    {
        var alert = await _db.ReadinessAlerts
            .FirstOrDefaultAsync(item =>
                item.Id == alertId &&
                item.CompanyId == reviewer.CompanyId &&
                (item.Status == ReadinessAlertStatuses.Open || item.Status == ReadinessAlertStatuses.Acknowledged));

        if (alert is null || !CanReviewAlert(alert, reviewer))
        {
            return false;
        }

        if (status == ReadinessAlertStatuses.Acknowledged && alert.Status != ReadinessAlertStatuses.Open)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        alert.Status = status;
        alert.ReviewedByUserId = reviewer.Id;
        alert.ReviewNote = NormalizeOptional(reviewNote);

        if (status == ReadinessAlertStatuses.Acknowledged)
        {
            alert.AcknowledgedAtUtc = now;
        }
        else if (status == ReadinessAlertStatuses.Resolved)
        {
            alert.ResolvedAtUtc = now;
        }
        else if (status == ReadinessAlertStatuses.Deleted)
        {
            alert.DeletedAtUtc = now;
        }

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = reviewer.CompanyId,
            AppUserId = reviewer.Id,
            Action = auditAction,
            EntityType = "ReadinessAlert",
            EntityId = alert.Id,
            Details = $"{reviewer.FullName} set readiness alert {alert.Id} to {status}: {alert.AlertSummary}",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();
        return true;
    }

    private static bool CanReviewAlert(ReadinessAlert alert, AppUser reviewer)
    {
        return CurrentUserService.IsSeniorAccessRole(reviewer.AppRole?.Name) ||
            alert.AssignedToUserId == reviewer.Id;
    }

    private async Task<int?> ResolveSuperiorUserIdAsync(DailyVehicleReadinessReport report)
    {
        var user = report.PerformedByUser;
        var roleName = user?.AppRole?.Name;
        if (user is null)
        {
            return null;
        }

        if (string.Equals(roleName, "Staff", StringComparison.OrdinalIgnoreCase))
        {
            var areaId = user.AssignedOperationalAreaId ?? report.Vehicle?.CurrentOperationalAreaId;
            if (areaId is not null)
            {
                var assignedManager = await _db.ManagerOperationalAreaAssignments
                    .AsNoTracking()
                    .Include(item => item.ManagerUser)
                        .ThenInclude(manager => manager!.AppRole)
                    .Where(item =>
                        item.CompanyId == report.CompanyId &&
                        item.OperationalAreaId == areaId &&
                        item.Status == "Active" &&
                        item.ManagerUser != null &&
                        item.ManagerUser.Status == "Active")
                    .Select(item => item.ManagerUser)
                    .FirstOrDefaultAsync();

                if (assignedManager is not null)
                {
                    return assignedManager.Id;
                }
            }
        }

        if (!CurrentUserService.IsSeniorAccessRole(roleName))
        {
            return await _db.AppUsers
                .AsNoTracking()
                .Include(item => item.AppRole)
                .Where(item =>
                    item.CompanyId == report.CompanyId &&
                    item.Status == "Active" &&
                    item.AppRole != null &&
                    (item.AppRole.Name == "Senior Management" || item.AppRole.Name == "Company Owner"))
                .OrderByDescending(item => item.AppRole!.Name == "Company Owner")
                .Select(item => (int?)item.Id)
                .FirstOrDefaultAsync();
        }

        return await _db.AppUsers
            .AsNoTracking()
            .Include(item => item.AppRole)
            .Where(item =>
                item.CompanyId == report.CompanyId &&
                item.Status == "Active" &&
                item.Id != user.Id &&
                item.AppRole != null &&
                item.AppRole.Name == "Company Owner")
            .Select(item => (int?)item.Id)
            .FirstOrDefaultAsync();
    }

    private static ReadinessEngineRule? FindRule(
        IReadOnlyList<ReadinessEngineRule> rules,
        Vehicle vehicle,
        string assetType,
        string itemName,
        string? fieldKey,
        string triggerValue)
    {
        return rules.FirstOrDefault(rule =>
            ReadinessRuleScope.AffectsShiftReadiness(rule.ReadinessScope) &&
            MatchesRule(rule, vehicle, assetType, itemName, fieldKey, triggerValue));
    }

    private static bool MatchesRule(
        ReadinessEngineRule rule,
        Vehicle vehicle,
        string assetType,
        string itemName,
        string? fieldKey,
        string triggerValue)
    {
        return Matches(rule.AssetType, assetType) &&
            MatchesTarget(rule, vehicle) &&
            (string.IsNullOrWhiteSpace(rule.FieldKey) || Matches(rule.FieldKey, fieldKey)) &&
            (Matches(rule.TriggerValue, triggerValue) || triggerValue.Contains(rule.TriggerValue, StringComparison.OrdinalIgnoreCase)) &&
            (Matches(rule.ItemName, itemName) || IsGenericRuleItem(rule.ItemName, assetType));
    }

    private static bool MatchesTarget(ReadinessEngineRule rule, Vehicle vehicle)
    {
        if (!string.IsNullOrWhiteSpace(rule.TargetVehicleType) &&
            !Matches(rule.TargetVehicleType, vehicle.VehicleType))
        {
            return false;
        }

        if (rule.OperationalAreaId.HasValue &&
            rule.OperationalAreaId != vehicle.CurrentOperationalAreaId)
        {
            return false;
        }

        return string.Equals(rule.AppliesTo, "All", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(rule.AppliesTo) ||
            Matches(rule.AppliesTo, vehicle.VehicleType) ||
            Matches(rule.AppliesTo, vehicle.Callsign) ||
            Matches(rule.AppliesTo, vehicle.RegistrationNumber);
    }

    private static bool IsGenericRuleItem(string itemName, string assetType)
    {
        if (string.Equals(assetType, "Equipment", StringComparison.OrdinalIgnoreCase))
        {
            return itemName.Contains("required equipment", StringComparison.OrdinalIgnoreCase) ||
                itemName.Contains("battery", StringComparison.OrdinalIgnoreCase) ||
                itemName.Contains("service date", StringComparison.OrdinalIgnoreCase) ||
                itemName.Contains("s/n", StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(assetType, "Vehicle", StringComparison.OrdinalIgnoreCase))
        {
            return itemName.Contains("vehicle", StringComparison.OrdinalIgnoreCase) ||
                itemName.Contains("service", StringComparison.OrdinalIgnoreCase);
        }

        return itemName.Contains("open issue", StringComparison.OrdinalIgnoreCase) ||
            itemName.Contains("required", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildVehicleLabel(DailyVehicleReadinessReport report)
    {
        if (!string.IsNullOrWhiteSpace(report.CallsignAtCheck) &&
            !string.IsNullOrWhiteSpace(report.VehicleRegistrationNumber))
        {
            return $"{report.CallsignAtCheck} / {report.VehicleRegistrationNumber}";
        }

        return NormalizeOptional(report.CallsignAtCheck) ??
            NormalizeOptional(report.VehicleRegistrationNumber) ??
            $"Vehicle {report.VehicleId}";
    }

    private static string BuildAlertSummary(
        string vehicleLabel,
        string itemName,
        string? fieldKey,
        string triggerValue,
        string? sourceValue,
        string severity,
        int impactPercent)
    {
        var fieldText = string.IsNullOrWhiteSpace(fieldKey) ? "rule" : fieldKey;
        var valueText = string.IsNullOrWhiteSpace(sourceValue) ? triggerValue : sourceValue;
        return $"{vehicleLabel}: {itemName} triggered {triggerValue} from {fieldText} value '{valueText}'. Severity {severity}, score impact {impactPercent}%.";
    }

    private static bool IsProblemStatus(string? value, params string[] acceptedValues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return !acceptedValues.Any(accepted => string.Equals(value, accepted, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsUnavailable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Contains("out of service", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("unavailable", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("inactive", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("not ready", StringComparison.OrdinalIgnoreCase);
    }

    private static bool Matches(string? left, string? right)
    {
        return !string.IsNullOrWhiteSpace(left) &&
            !string.IsNullOrWhiteSpace(right) &&
            string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsAny(string? value, params string[] needles)
    {
        return !string.IsNullOrWhiteSpace(value) &&
            needles.Any(needle => value.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
