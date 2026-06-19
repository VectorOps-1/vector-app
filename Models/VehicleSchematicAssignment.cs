using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class VehicleSchematicAssignment
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [MaxLength(120)]
    public string SchematicKey { get; set; } = string.Empty;

    [MaxLength(40)]
    public string ScopeType { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? VehicleFunction { get; set; }

    [MaxLength(120)]
    public string? VehicleSubtype { get; set; }

    public int? OperationalAreaId { get; set; }
    public OperationalArea? OperationalArea { get; set; }

    public int? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public int? CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
