using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class StockItem
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }

    public int? LastMovedByUserId { get; set; }
    public AppUser? LastMovedByUser { get; set; }

    [MaxLength(180)]
    public string ItemName { get; set; } = string.Empty;

    [MaxLength(160)]
    public string? ItemType { get; set; }

    [MaxLength(160)]
    public string? StockCategory { get; set; }

    [MaxLength(160)]
    public string? BatchNumber { get; set; }

    public int Quantity { get; set; }
    public int? MinimumQuantity { get; set; }

    [MaxLength(80)]
    public string? Unit { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public bool IsReadinessCritical { get; set; }

    [MaxLength(260)]
    public string? Location { get; set; }

    public int? CurrentOperationalAreaId { get; set; }
    public OperationalArea? CurrentOperationalArea { get; set; }

    [MaxLength(80)]
    public string Status { get; set; } = "Active";

    [MaxLength(120)]
    public string? LastMovementType { get; set; }

    public DateTime? LastMovementAtUtc { get; set; }

    [MaxLength(1200)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
