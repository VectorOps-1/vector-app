using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class IssueReportEvent
{
    public int Id { get; set; }

    public int IssueReportId { get; set; }
    public IssueReport? IssueReport { get; set; }

    public int PerformedByUserId { get; set; }
    public AppUser? PerformedByUser { get; set; }

    [MaxLength(80)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(1200)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
