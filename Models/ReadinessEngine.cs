using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public static class ReadinessEngineStatuses
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Archived = "Archived";
}

public static class ReadinessRuleSeverity
{
    public const string NoImpact = "No impact";
    public const string Minor = "Minor";
    public const string Moderate = "Moderate";
    public const string Major = "Major";
    public const string Critical = "Critical";
    public const string HardBlocker = "Hard blocker";

    public static readonly string[] Options =
    [
        NoImpact,
        Minor,
        Moderate,
        Major,
        Critical,
        HardBlocker
    ];

    public static int SuggestedImpactPercent(string? severity)
    {
        return severity switch
        {
            NoImpact => 0,
            Minor => 2,
            Moderate => 10,
            Major => 20,
            Critical => 40,
            HardBlocker => 100,
            _ => 10
        };
    }

    public static string FromImpactPercent(int impactPercent)
    {
        return impactPercent switch
        {
            <= 0 => NoImpact,
            >= 100 => HardBlocker,
            >= 40 => Critical,
            >= 20 => Major,
            >= 10 => Moderate,
            _ => Minor
        };
    }
}

public static class ReadinessRuleScope
{
    public const string ActiveShift = "Active shift";
    public const string AssignedToActiveVehicle = "Assigned to active vehicle";
    public const string AssignedToOperationalBase = "Assigned to operational base";
    public const string RequiredStockMinimum = "Required stock minimum";
    public const string StoreroomOnly = "Storeroom only";
    public const string ReserveBackup = "Reserve / backup";
    public const string OutOfService = "Out of service";
    public const string TrainingOnly = "Training only";
    public const string ReferenceOnly = "Reference only";

    public static readonly string[] Options =
    [
        ActiveShift,
        AssignedToActiveVehicle,
        AssignedToOperationalBase,
        RequiredStockMinimum,
        StoreroomOnly,
        ReserveBackup,
        OutOfService,
        TrainingOnly,
        ReferenceOnly
    ];

    public static bool AffectsShiftReadiness(string? scope)
    {
        return string.IsNullOrWhiteSpace(scope) ||
            string.Equals(scope, ActiveShift, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(scope, AssignedToActiveVehicle, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(scope, AssignedToOperationalBase, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(scope, RequiredStockMinimum, StringComparison.OrdinalIgnoreCase);
    }
}

public class ReadinessEngineVersion
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = "Readiness engine";

    [MaxLength(40)]
    public string VersionNumber { get; set; } = "1.0";

    [MaxLength(40)]
    public string Status { get; set; } = ReadinessEngineStatuses.Draft;

    public int? SourceReadinessEngineVersionId { get; set; }
    public ReadinessEngineVersion? SourceReadinessEngineVersion { get; set; }

    public int? CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }

    public int? PublishedByUserId { get; set; }
    public AppUser? PublishedByUser { get; set; }

    [MaxLength(1200)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }

    public ICollection<ReadinessEngineRule> Rules { get; set; } = new List<ReadinessEngineRule>();
}

public class ReadinessEngineRule
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int ReadinessEngineVersionId { get; set; }
    public ReadinessEngineVersion? ReadinessEngineVersion { get; set; }

    [MaxLength(80)]
    public string AssetType { get; set; } = "Vehicle";

    [MaxLength(120)]
    public string Section { get; set; } = "General";

    [MaxLength(180)]
    public string ItemName { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? FieldKey { get; set; }

    [MaxLength(180)]
    public string TriggerValue { get; set; } = string.Empty;

    [MaxLength(180)]
    public string AppliesTo { get; set; } = "All";

    [MaxLength(80)]
    public string ReadinessScope { get; set; } = ReadinessRuleScope.ActiveShift;

    [MaxLength(120)]
    public string? TargetVehicleType { get; set; }

    public int? OperationalAreaId { get; set; }
    public OperationalArea? OperationalArea { get; set; }

    public int? ChecklistTemplateId { get; set; }
    public ChecklistTemplate? ChecklistTemplate { get; set; }

    [MaxLength(40)]
    public string Severity { get; set; } = ReadinessRuleSeverity.Moderate;

    public int DefaultImpactPercent { get; set; }
    public int? ManualImpactPercent { get; set; }
    public bool IsHardBlocker { get; set; }
    public bool ManagerAlert { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public bool IsAutoPopulated { get; set; }

    [MaxLength(80)]
    public string SourceType { get; set; } = "Custom";

    [MaxLength(80)]
    public string? SourceEntityType { get; set; }

    public int? SourceEntityId { get; set; }

    [MaxLength(1200)]
    public string? Notes { get; set; }

    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}

public class ReadinessScoringChangeRequest
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int RequestedByUserId { get; set; }
    public AppUser? RequestedByUser { get; set; }

    public int? ReviewedByUserId { get; set; }
    public AppUser? ReviewedByUser { get; set; }

    public int? ReadinessEngineRuleId { get; set; }
    public ReadinessEngineRule? ReadinessEngineRule { get; set; }

    [MaxLength(40)]
    public string Status { get; set; } = "Pending";

    [MaxLength(80)]
    public string AssetType { get; set; } = string.Empty;

    [MaxLength(180)]
    public string ItemName { get; set; } = string.Empty;

    [MaxLength(180)]
    public string TriggerValue { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? CurrentSeverity { get; set; }

    [MaxLength(40)]
    public string ProposedSeverity { get; set; } = ReadinessRuleSeverity.Moderate;

    public int? CurrentImpactPercent { get; set; }
    public int? ProposedImpactPercent { get; set; }
    public bool? CurrentActive { get; set; }
    public bool? ProposedActive { get; set; }

    [MaxLength(1200)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(1200)]
    public string? SeniorDecisionNote { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
}
