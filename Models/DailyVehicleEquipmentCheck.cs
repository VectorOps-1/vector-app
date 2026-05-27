using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class DailyVehicleEquipmentCheck
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int DailyVehicleReadinessReportId { get; set; }
    public DailyVehicleReadinessReport? DailyVehicleReadinessReport { get; set; }

    public int? VehicleEquipmentAssignmentId { get; set; }
    public VehicleEquipmentAssignment? VehicleEquipmentAssignment { get; set; }

    public int? EquipmentItemId { get; set; }
    public EquipmentItem? EquipmentItem { get; set; }

    [MaxLength(180)]
    public string EquipmentName { get; set; } = string.Empty;

    [MaxLength(160)]
    public string? EquipmentType { get; set; }

    [MaxLength(160)]
    public string? Model { get; set; }

    [MaxLength(160)]
    public string? SerialOrAssetId { get; set; }

    public DateTime? NextServiceDateAtCheck { get; set; }

    [MaxLength(80)]
    public string PresentStatus { get; set; } = "Present";

    [MaxLength(80)]
    public string? DamageStatus { get; set; }

    [MaxLength(80)]
    public string? BatteryStatus { get; set; }

    [MaxLength(80)]
    public string ReadinessImpact { get; set; } = "None";

    [MaxLength(1200)]
    public string? Notes { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
