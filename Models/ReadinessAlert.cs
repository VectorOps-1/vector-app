using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public static class ReadinessAlertStatuses
{
    public const string Open = "Open";
    public const string Acknowledged = "Acknowledged";
    public const string Resolved = "Resolved";
    public const string Deleted = "Deleted";
}

public class ReadinessAlert
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int? ReadinessEngineRuleId { get; set; }
    public ReadinessEngineRule? ReadinessEngineRule { get; set; }

    public int DailyVehicleReadinessReportId { get; set; }
    public DailyVehicleReadinessReport? DailyVehicleReadinessReport { get; set; }

    public int? DailyVehicleEquipmentCheckId { get; set; }
    public DailyVehicleEquipmentCheck? DailyVehicleEquipmentCheck { get; set; }

    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public int TriggeredByUserId { get; set; }
    public AppUser? TriggeredByUser { get; set; }

    public int? AssignedToUserId { get; set; }
    public AppUser? AssignedToUser { get; set; }

    public int? ReviewedByUserId { get; set; }
    public AppUser? ReviewedByUser { get; set; }

    [MaxLength(80)]
    public string AssetType { get; set; } = string.Empty;

    [MaxLength(120)]
    public string SourceArea { get; set; } = string.Empty;

    [MaxLength(180)]
    public string ItemName { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? FieldKey { get; set; }

    [MaxLength(180)]
    public string TriggerValue { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Severity { get; set; } = ReadinessRuleSeverity.Moderate;

    public int ImpactPercent { get; set; }
    public bool IsHardBlocker { get; set; }

    [MaxLength(80)]
    public string Status { get; set; } = ReadinessAlertStatuses.Open;

    [MaxLength(220)]
    public string VehicleLabel { get; set; } = string.Empty;

    [MaxLength(1200)]
    public string AlertSummary { get; set; } = string.Empty;

    [MaxLength(1200)]
    public string? SourceValue { get; set; }

    [MaxLength(1200)]
    public string? ReviewNote { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}
