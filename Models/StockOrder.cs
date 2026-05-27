using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class StockOrder
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int RequestedByUserId { get; set; }
    public AppUser? RequestedByUser { get; set; }

    public int? ApprovedBySeniorUserId { get; set; }
    public AppUser? ApprovedBySeniorUser { get; set; }

    public int? RegisterEntryAuthorisedUserId { get; set; }
    public AppUser? RegisterEntryAuthorisedUser { get; set; }

    [MaxLength(180)]
    public string SupplierName { get; set; } = string.Empty;

    [MaxLength(180)]
    public string SupplierEmail { get; set; } = string.Empty;

    [MaxLength(360)]
    public string? DeliveryAddress { get; set; }

    [MaxLength(1200)]
    public string? DeliveryInstructions { get; set; }

    [MaxLength(1200)]
    public string? OrderNotes { get; set; }

    [MaxLength(80)]
    public string Status { get; set; } = StockOrderStatuses.PendingSeniorApproval;

    [MaxLength(220)]
    public string EmailSubject { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string EmailBody { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? EmailSentAtUtc { get; set; }
    public DateTime? SupplierConfirmedAtUtc { get; set; }
    public DateTime? RegisterEnteredAtUtc { get; set; }
    public DateTime? AllocatedAtUtc { get; set; }

    public ICollection<StockOrderLine> Lines { get; set; } = new List<StockOrderLine>();
}
