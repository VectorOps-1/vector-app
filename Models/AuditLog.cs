using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class AuditLog
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int? AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    [MaxLength(120)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(120)]
    public string EntityType { get; set; } = string.Empty;

    public int? EntityId { get; set; }

    [MaxLength(1200)]
    public string? Details { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
