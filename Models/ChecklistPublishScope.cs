using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class ChecklistPublishScope
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int ChecklistTemplateId { get; set; }
    public ChecklistTemplate? ChecklistTemplate { get; set; }

    [MaxLength(80)]
    public string ScopeType { get; set; } = "AllAreas";

    public int? OperationalAreaId { get; set; }
    public OperationalArea? OperationalArea { get; set; }

    public int? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public int PublishedByUserId { get; set; }
    public AppUser? PublishedByUser { get; set; }

    [MaxLength(1200)]
    public string? PublishNote { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime PublishedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RetiredAtUtc { get; set; }
}
