using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class TaskItem
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int AssignedToUserId { get; set; }
    public AppUser? AssignedToUser { get; set; }

    public int AssignedByUserId { get; set; }
    public AppUser? AssignedByUser { get; set; }

    [MaxLength(160)]
    public string ActionType { get; set; } = string.Empty;

    [MaxLength(260)]
    public string? RelatedItemReference { get; set; }

    [MaxLength(1200)]
    public string? InstructionMessage { get; set; }

    [MaxLength(80)]
    public string Status { get; set; } = "Open";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? OpenedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    public ICollection<TaskEvent> Events { get; set; } = new List<TaskEvent>();
}
