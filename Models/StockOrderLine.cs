using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class StockOrderLine
{
    public int Id { get; set; }

    public int StockOrderId { get; set; }
    public StockOrder? StockOrder { get; set; }

    [MaxLength(180)]
    public string ItemName { get; set; } = string.Empty;

    [MaxLength(160)]
    public string? ItemType { get; set; }

    public int QuantityRequested { get; set; }
    public int? QuantityConfirmed { get; set; }

    [MaxLength(160)]
    public string? BatchNumber { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [MaxLength(260)]
    public string? RegisterLocation { get; set; }

    public int? QuantityAllocated { get; set; }

    [MaxLength(260)]
    public string? AllocationLocation { get; set; }

    [MaxLength(1200)]
    public string? Notes { get; set; }
}
