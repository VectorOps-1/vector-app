using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class IssueReport
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int ReportedByUserId { get; set; }
    public AppUser? ReportedByUser { get; set; }

    public int AssignedToUserId { get; set; }
    public AppUser? AssignedToUser { get; set; }

    public int? ResolvedByUserId { get; set; }
    public AppUser? ResolvedByUser { get; set; }

    [MaxLength(80)]
    public string ManagerLevel { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Module { get; set; } = "General";

    [MaxLength(160)]
    public string IssueType { get; set; } = string.Empty;

    [MaxLength(260)]
    public string? RelatedItem { get; set; }

    [MaxLength(260)]
    public string? Location { get; set; }

    [MaxLength(80)]
    public string? Severity { get; set; }

    [MaxLength(120)]
    public string? OperationalStatus { get; set; }

    [MaxLength(1200)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(80)]
    public string NotificationMethod { get; set; } = "In-app notification";

    [MaxLength(1200)]
    public string? EvidenceFileNames { get; set; }

    [MaxLength(80)]
    public string Status { get; set; } = "Open";

    [MaxLength(80)]
    public string? ResolutionOutcome { get; set; }

    [MaxLength(1200)]
    public string? ActionTaken { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAtUtc { get; set; }

    public ICollection<IssueReportEvent> Events { get; set; } = new List<IssueReportEvent>();
}
