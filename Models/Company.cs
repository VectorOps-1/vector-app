using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class Company
{
    public int Id { get; set; }

    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Status { get; set; } = "Active";

    [MaxLength(260)]
    public string? LogoStoragePath { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<IssueReport> IssueReports { get; set; } = new List<IssueReport>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
