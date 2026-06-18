using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public class ReadinessEngineScoringService
{
    private readonly VectorDbContext _db;

    public ReadinessEngineScoringService(VectorDbContext db)
    {
        _db = db;
    }

    public async Task<ReadinessDashboardScore> ScoreDashboardAsync(
        int companyId,
        IReadOnlyList<Vehicle> vehicles,
        IReadOnlyDictionary<int, DailyVehicleReadinessReport> latestReports,
        IReadOnlyList<IssueReport> openIssues)
    {
        var engine = await _db.ReadinessEngineVersions
            .AsNoTracking()
            .Include(version => version.Rules)
            .Where(version => version.CompanyId == companyId && version.Status == ReadinessEngineStatuses.Published)
            .OrderByDescending(version => version.PublishedAtUtc ?? version.CreatedAtUtc)
            .FirstOrDefaultAsync();

        var rules = engine?.Rules
            .Where(rule => rule.IsActive)
            .OrderBy(rule => rule.SortOrder)
            .ToList() ?? [];

        if (rules.Count == 0)
        {
            await ReadinessEngineService.EnsureDefaultPublishedEngineAsync(_db, companyId, null);
            rules = await _db.ReadinessEngineRules
                .AsNoTracking()
                .Where(rule =>
                    rule.CompanyId == companyId &&
                    rule.IsActive &&
                    rule.ReadinessEngineVersion != null &&
                    rule.ReadinessEngineVersion.Status == ReadinessEngineStatuses.Published)
                .OrderBy(rule => rule.SortOrder)
                .ToListAsync();
        }

        var scores = vehicles
            .Select(vehicle => ScoreVehicle(vehicle, latestReports.GetValueOrDefault(vehicle.Id), openIssues, rules))
            .ToList();

        var totalVehicles = scores.Count;
        var checkedVehicles = scores.Count(score => score.CheckedThisShift);
        var readyVehicles = scores.Count(score => score.IsReady);
        var unavailableVehicles = scores.Count(score => score.IsHardBlocked);
        var missingChecks = scores.Count(score => score.MissingCheck);
        var equipmentWarnings = scores.Count(score => score.HasEquipmentAlert);
        var scorePercent = totalVehicles == 0 ? 0 : (int)Math.Round(scores.Average(score => score.ScorePercent));

        return new ReadinessDashboardScore
        {
            TotalVehicles = totalVehicles,
            CheckedVehicles = checkedVehicles,
            ReadyVehicles = readyVehicles,
            UnavailableVehicles = unavailableVehicles,
            MissingChecks = missingChecks,
            EquipmentWarnings = equipmentWarnings,
            OpenIssues = openIssues.Count,
            ScorePercent = scorePercent,
            ScoreClass = scorePercent >= 90 ? "score-green" : scorePercent >= 75 ? "score-teal" : scorePercent >= 50 ? "score-amber" : "score-red"
        };
    }

    private static VehicleScore ScoreVehicle(
        Vehicle vehicle,
        DailyVehicleReadinessReport? report,
        IReadOnlyList<IssueReport> openIssues,
        IReadOnlyList<ReadinessEngineRule> rules)
    {
        var result = new VehicleScore();

        void Apply(string assetType, string itemName, string? fieldKey, string triggerValue)
        {
            var rule = FindRule(rules, vehicle, assetType, itemName, fieldKey, triggerValue);
            if (rule is null)
            {
                if (HasNonScoringMatch(rules, vehicle, assetType, itemName, fieldKey, triggerValue))
                {
                    return;
                }

                result.ScorePercent -= FallbackImpact(triggerValue);
                return;
            }

            var impact = Math.Clamp(rule.ManualImpactPercent ?? rule.DefaultImpactPercent, 0, 100);
            result.ScorePercent -= impact;
            result.IsHardBlocked = result.IsHardBlocked || rule.IsHardBlocker ||
                string.Equals(rule.Severity, ReadinessRuleSeverity.HardBlocker, StringComparison.OrdinalIgnoreCase);

            if (string.Equals(assetType, "Equipment", StringComparison.OrdinalIgnoreCase))
            {
                result.HasEquipmentAlert = true;
            }
        }

        result.CheckedThisShift = IsReadinessCheckComplete(report);
        result.MissingCheck = !result.CheckedThisShift;

        if (result.MissingCheck)
        {
            Apply("Checklist Completion", "Daily vehicle readiness", "Completion", "Missing completed check");
        }

        if (IsUnavailable(vehicle.Status))
        {
            Apply("Vehicle", "Vehicle", "Status", "Out of service / unavailable");
        }

        if (vehicle.NextServiceDate.HasValue && vehicle.NextServiceDate.Value.Date < DateTime.UtcNow.Date)
        {
            Apply("Vehicle", "Next service date", "NextServiceDate", "Service overdue");
        }

        if (report is not null)
        {
            ApplyOperationalCheckIfNeeded(report.LightsStatus, "Lights", "LightsStatus", Apply);
            ApplyOperationalCheckIfNeeded(report.SirensStatus, "Sirens", "SirensStatus", Apply);
            ApplyOperationalCheckIfNeeded(report.WarningLightsStatus, "Warning lights", "WarningLightsStatus", Apply);
            ApplyOperationalCheckIfNeeded(report.TyresStatus, "Tyres", "TyresStatus", Apply);
            ApplyOperationalCheckIfNeeded(report.RadioConnectivityStatus, "Ops radio", "RadioConnectivityStatus", Apply);

            if (IsUnavailable(report.ReadinessStatus) || report.CriticalIssueCount > 0)
            {
                Apply("Issue Report", "Open issue", "Severity", "Critical unresolved issue");
            }

            if (report.EquipmentChecks.Count == 0)
            {
                Apply("Equipment", "Required equipment", "PresentStatus", "Missing");
            }

            foreach (var check in report.EquipmentChecks)
            {
                if (IsProblemStatus(check.PresentStatus, "Present", "N/A", "Not applicable"))
                {
                    Apply("Equipment", check.EquipmentName, "PresentStatus", "Missing");
                }

                if (!check.IsOperational)
                {
                    Apply("Equipment", check.EquipmentName, "Operational", "No");
                }

                if (IsProblemStatus(check.BatteryStatus, "Full", "Acceptable", "Charging", "N/A", "Not applicable"))
                {
                    var batteryTrigger = ContainsAny(check.BatteryStatus, "flat", "fail", "empty") ? "Flat / failed" : "Low";
                    Apply("Equipment", check.EquipmentName, "BatteryStatus", batteryTrigger);
                }

                if (check.NextServiceDateAtCheck.HasValue && check.NextServiceDateAtCheck.Value.Date < DateTime.UtcNow.Date)
                {
                    Apply("Equipment", check.EquipmentName, "NextServiceDate", "Service overdue");
                }

                if (!string.IsNullOrWhiteSpace(check.IssueNotes) ||
                    IsProblemStatus(check.DamageStatus, "No damage", "None", "Good", "N/A", "Not applicable") ||
                    IsProblemStatus(check.ReadinessImpact, "None", "N/A", "Not applicable"))
                {
                    result.HasEquipmentAlert = true;
                }
            }
        }

        var issueCount = openIssues.Count(issue =>
            MatchesIssue(issue, vehicle.RegistrationNumber) ||
            MatchesIssue(issue, vehicle.Callsign));
        for (var i = 0; i < issueCount; i++)
        {
            Apply("Issue Report", "Open issue", "Severity", "Critical unresolved issue");
        }

        result.ScorePercent = Math.Clamp(result.ScorePercent, 0, 100);
        result.IsReady = !result.IsHardBlocked && result.ScorePercent >= 90;
        return result;
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

    private static bool HasNonScoringMatch(
        IReadOnlyList<ReadinessEngineRule> rules,
        Vehicle vehicle,
        string assetType,
        string itemName,
        string? fieldKey,
        string triggerValue)
    {
        return rules.Any(rule =>
            !ReadinessRuleScope.AffectsShiftReadiness(rule.ReadinessScope) &&
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

    private static void ApplyOperationalCheckIfNeeded(
        string? value,
        string itemName,
        string fieldKey,
        Action<string, string, string?, string> apply)
    {
        if (IsProblemStatus(value, "Operational", "Pass", "Passed", "OK", "Good", "N/A", "Not applicable"))
        {
            apply("Vehicle", itemName, fieldKey, "Failed / not operational");
        }
    }

    private static int FallbackImpact(string triggerValue)
    {
        if (ContainsAny(triggerValue, "missing", "failed", "out of service", "unavailable", "expired"))
        {
            return 100;
        }

        if (ContainsAny(triggerValue, "critical"))
        {
            return 40;
        }

        if (ContainsAny(triggerValue, "low", "overdue", "below minimum"))
        {
            return 20;
        }

        return 10;
    }

    private static bool IsReadinessCheckComplete(DailyVehicleReadinessReport? report)
    {
        return report is not null &&
            !string.Equals(report.WorkflowStatus, "Draft", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(report.LastSavedSection, "Equipment", StringComparison.OrdinalIgnoreCase) &&
            report.EquipmentChecks.Any();
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

    private static bool MatchesIssue(IssueReport issue, string? needle)
    {
        return Matches(issue.RelatedItem, needle) ||
            Matches(issue.Location, needle) ||
            Matches(issue.Description, needle);
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

    private sealed class VehicleScore
    {
        public int ScorePercent { get; set; } = 100;
        public bool CheckedThisShift { get; set; }
        public bool MissingCheck { get; set; }
        public bool IsHardBlocked { get; set; }
        public bool IsReady { get; set; }
        public bool HasEquipmentAlert { get; set; }
    }
}

public sealed class ReadinessDashboardScore
{
    public int TotalVehicles { get; set; }
    public int CheckedVehicles { get; set; }
    public int ReadyVehicles { get; set; }
    public int UnavailableVehicles { get; set; }
    public int MissingChecks { get; set; }
    public int OpenIssues { get; set; }
    public int EquipmentWarnings { get; set; }
    public int ScorePercent { get; set; }
    public string ScoreClass { get; set; } = "score-red";
}
