using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class MedicationItem
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }

    public int? LastAllocatedByUserId { get; set; }
    public AppUser? LastAllocatedByUser { get; set; }

    [MaxLength(180)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? MedicationCode { get; set; }

    [MaxLength(160)]
    public string? MedicationType { get; set; }

    [MaxLength(80)]
    public string? Schedule { get; set; }

    [MaxLength(160)]
    public string? BatchNumber { get; set; }

    [MaxLength(260)]
    public string? StorageLocation { get; set; }

    public int? CurrentOperationalAreaId { get; set; }
    public OperationalArea? CurrentOperationalArea { get; set; }

    [MaxLength(80)]
    public string Status { get; set; } = "Active";

    public int? Quantity { get; set; }
    public DateTime? ExpiryDate { get; set; }

    [MaxLength(260)]
    public string? LastAllocationLocation { get; set; }

    public DateTime? LastAllocatedAtUtc { get; set; }

    [MaxLength(1200)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
