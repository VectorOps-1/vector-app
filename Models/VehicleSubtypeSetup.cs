using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class VehicleSubtypeSetup
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int VehicleFunctionSetupId { get; set; }
    public VehicleFunctionSetup? VehicleFunctionSetup { get; set; }

    [MaxLength(140)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Status { get; set; } = "Active";

    public int SortOrder { get; set; }

    [MaxLength(1200)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
