using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class StorageLocation
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int OperationalAreaId { get; set; }
    public OperationalArea? OperationalArea { get; set; }

    [MaxLength(180)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(120)]
    public string StorageType { get; set; } = "General store";

    [MaxLength(80)]
    public string Status { get; set; } = "Active";

    [MaxLength(1200)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
