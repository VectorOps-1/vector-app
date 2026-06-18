using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class ChecklistVarianceAlert
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int DailyVehicleReadinessReportId { get; set; }
    public DailyVehicleReadinessReport? DailyVehicleReadinessReport { get; set; }

    public int? DailyVehicleEquipmentCheckId { get; set; }
    public DailyVehicleEquipmentCheck? DailyVehicleEquipmentCheck { get; set; }

    public int? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public int DetectedForUserId { get; set; }
    public AppUser? DetectedForUser { get; set; }

    public int? AssignedToUserId { get; set; }
    public AppUser? AssignedToUser { get; set; }

    public int? ReviewedByUserId { get; set; }
    public AppUser? ReviewedByUser { get; set; }

    [MaxLength(80)]
    public string AlertType { get; set; } = "EquipmentVariance";

    [MaxLength(120)]
    public string FieldKey { get; set; } = string.Empty;

    [MaxLength(220)]
    public string? AssetLabel { get; set; }

    [MaxLength(1200)]
    public string? PreviousValue { get; set; }

    [MaxLength(1200)]
    public string? NewValue { get; set; }

    [MaxLength(1200)]
    public string? RegisterValue { get; set; }

    [MaxLength(80)]
    public string Severity { get; set; } = "Review";

    [MaxLength(80)]
    public string Status { get; set; } = "Open";

    public bool RequiresRegisterUpdate { get; set; }
    public DateTime? RegisterUpdatedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }

    [MaxLength(1200)]
    public string? ReviewNote { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
