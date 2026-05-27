using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class EquipmentItem
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [MaxLength(180)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(160)]
    public string? EquipmentType { get; set; }

    [MaxLength(160)]
    public string? Model { get; set; }

    [MaxLength(160)]
    public string? SerialOrAssetId { get; set; }

    public DateTime? NextServiceDate { get; set; }

    public bool BatteryRequired { get; set; }

    [MaxLength(80)]
    public string Status { get; set; } = "Active";

    public int? CurrentOperationalAreaId { get; set; }
    public OperationalArea? CurrentOperationalArea { get; set; }

    [MaxLength(260)]
    public string? CurrentLocationDetail { get; set; }

    public int? LastMovedByUserId { get; set; }
    public AppUser? LastMovedByUser { get; set; }

    public DateTime? LastMovedAtUtc { get; set; }

    [MaxLength(1200)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<VehicleEquipmentAssignment> VehicleAssignments { get; set; } = new List<VehicleEquipmentAssignment>();
    public ICollection<DailyVehicleEquipmentCheck> DailyEquipmentChecks { get; set; } = new List<DailyVehicleEquipmentCheck>();
}
