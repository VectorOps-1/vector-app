using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class TaskEvent
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }

    public int PerformedByUserId { get; set; }
    public AppUser? PerformedByUser { get; set; }

    [MaxLength(80)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(1200)]
    public string? Notes { get; set; }

    [MaxLength(520)]
    public string? EvidenceStoragePath { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
