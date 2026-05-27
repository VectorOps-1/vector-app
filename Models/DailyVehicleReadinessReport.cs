using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class DailyVehicleReadinessReport
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public int PerformedByUserId { get; set; }
    public AppUser? PerformedByUser { get; set; }

    public DateTime InspectionDateUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(80)]
    public string? ShiftName { get; set; }

    [MaxLength(120)]
    public string VehicleRegistrationNumber { get; set; } = string.Empty;

    [MaxLength(120)]
    public string CallsignAtCheck { get; set; } = string.Empty;

    [MaxLength(120)]
    public string VehicleTypeAtCheck { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? QualificationLevelAtCheck { get; set; }

    [MaxLength(120)]
    public string? SchematicTypeAtCheck { get; set; }

    public DateTime? VehicleNextServiceDateAtCheck { get; set; }

    public bool SameAsPreviousShiftUsed { get; set; }

    [MaxLength(80)]
    public string? LightsStatus { get; set; }

    [MaxLength(80)]
    public string? SirensStatus { get; set; }

    [MaxLength(80)]
    public string? WarningLightsStatus { get; set; }

    [MaxLength(80)]
    public string? TyresStatus { get; set; }

    [MaxLength(80)]
    public string? RadioConnectivityStatus { get; set; }

    [MaxLength(1200)]
    public string? OperationalNotes { get; set; }

    [MaxLength(1200)]
    public string? DamageNotes { get; set; }

    [MaxLength(1200)]
    public string? SchematicNotes { get; set; }

    [MaxLength(1200)]
    public string? GeneralNotes { get; set; }

    [MaxLength(80)]
    public string ReadinessStatus { get; set; } = "Pending";

    public int CriticalIssueCount { get; set; }
    public int WarningIssueCount { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<DailyVehicleEquipmentCheck> EquipmentChecks { get; set; } = new List<DailyVehicleEquipmentCheck>();
}
