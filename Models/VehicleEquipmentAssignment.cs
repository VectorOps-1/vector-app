using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class VehicleEquipmentAssignment
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public int? EquipmentItemId { get; set; }
    public EquipmentItem? EquipmentItem { get; set; }

    [MaxLength(120)]
    public string? VehicleType { get; set; }

    [MaxLength(120)]
    public string? QualificationLevel { get; set; }

    [MaxLength(180)]
    public string ExpectedEquipmentName { get; set; } = string.Empty;

    [MaxLength(160)]
    public string? ExpectedEquipmentType { get; set; }

    [MaxLength(160)]
    public string? ExpectedModel { get; set; }

    public int ExpectedQuantity { get; set; } = 1;

    public bool RequiredForReadiness { get; set; } = true;

    public bool RequiresBatteryCheck { get; set; }

    [MaxLength(160)]
    public string? DefaultLocation { get; set; }

    public int SortOrder { get; set; }

    [MaxLength(80)]
    public string Status { get; set; } = "Active";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<DailyVehicleEquipmentCheck> DailyEquipmentChecks { get; set; } = new List<DailyVehicleEquipmentCheck>();
}
