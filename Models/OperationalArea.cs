using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class OperationalArea
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [MaxLength(180)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(120)]
    public string AreaType { get; set; } = "Base";

    [MaxLength(360)]
    public string? Address { get; set; }

    [MaxLength(80)]
    public string Status { get; set; } = "Active";

    [MaxLength(1200)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<AssetMovement> SourceMovements { get; set; } = new List<AssetMovement>();
    public ICollection<AssetMovement> DestinationMovements { get; set; } = new List<AssetMovement>();
    public ICollection<ManagerOperationalAreaAssignment> ManagerAssignments { get; set; } = new List<ManagerOperationalAreaAssignment>();
}
