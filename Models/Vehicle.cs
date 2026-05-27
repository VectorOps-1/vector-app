using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class Vehicle
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [MaxLength(120)]
    public string RegistrationNumber { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Callsign { get; set; } = string.Empty;

    [MaxLength(120)]
    public string VehicleType { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? QualificationLevel { get; set; }

    [MaxLength(120)]
    public string? SchematicType { get; set; }

    public DateTime? NextServiceDate { get; set; }

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

    public ICollection<VehicleEquipmentAssignment> EquipmentAssignments { get; set; } = new List<VehicleEquipmentAssignment>();
    public ICollection<DailyVehicleReadinessReport> ReadinessReports { get; set; } = new List<DailyVehicleReadinessReport>();
}
