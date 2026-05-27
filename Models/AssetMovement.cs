using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class AssetMovement
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [MaxLength(80)]
    public string AssetType { get; set; } = string.Empty;

    public int AssetId { get; set; }

    [MaxLength(260)]
    public string AssetLabel { get; set; } = string.Empty;

    public int? FromOperationalAreaId { get; set; }
    public OperationalArea? FromOperationalArea { get; set; }

    public int ToOperationalAreaId { get; set; }
    public OperationalArea? ToOperationalArea { get; set; }

    [MaxLength(260)]
    public string? FromLocationText { get; set; }

    [MaxLength(260)]
    public string? ToLocationText { get; set; }

    public int? QuantityMoved { get; set; }

    [MaxLength(1200)]
    public string? MovementReason { get; set; }

    public int MovedByUserId { get; set; }
    public AppUser? MovedByUser { get; set; }

    public int? TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
