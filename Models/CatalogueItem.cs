using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class CatalogueItem
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [MaxLength(80)]
    public string CatalogueType { get; set; } = "ServiceableEquipment";

    [MaxLength(160)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(160)]
    public string? Subcategory { get; set; }

    [MaxLength(220)]
    public string ItemName { get; set; } = string.Empty;

    [MaxLength(220)]
    public string? Variant { get; set; }

    [MaxLength(160)]
    public string? Manufacturer { get; set; }

    [MaxLength(160)]
    public string? Model { get; set; }

    [MaxLength(80)]
    public string? Size { get; set; }

    [MaxLength(80)]
    public string? Unit { get; set; }

    public bool ServiceRequired { get; set; }
    public bool BatteryRequired { get; set; }
    public bool SerialRequired { get; set; }
    public bool BatchRequired { get; set; }
    public bool ExpiryRequired { get; set; }
    public bool ReadinessCritical { get; set; }

    [MaxLength(600)]
    public string? DefaultChecklistColumns { get; set; }

    [MaxLength(1200)]
    public string? Notes { get; set; }

    [MaxLength(80)]
    public string Status { get; set; } = "Active";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
