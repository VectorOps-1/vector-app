using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class ChecklistTemplate
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [MaxLength(160)]
    public string ClientName { get; set; } = string.Empty;

    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    public string ChecklistType { get; set; } = "Vehicle";

    [MaxLength(120)]
    public string TargetVehicleType { get; set; } = "All Vehicles";

    [MaxLength(40)]
    public string Version { get; set; } = "1.0";

    [MaxLength(80)]
    public string Status { get; set; } = "Draft";

    [MaxLength(80)]
    public string SourceType { get; set; } = "Built";

    public int? SourceImportBatchId { get; set; }

    public int? ParentChecklistTemplateId { get; set; }
    public ChecklistTemplate? ParentChecklistTemplate { get; set; }

    public int? CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }

    public int? PublishedByUserId { get; set; }
    public AppUser? PublishedByUser { get; set; }

    public bool IsPublished { get; set; }
    public DateTime? PublishedAtUtc { get; set; }

    [MaxLength(260)]
    public string? PublishScopeSummary { get; set; }

    [MaxLength(1200)]
    public string? PublishNotes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ChecklistSection> Sections { get; set; } = new List<ChecklistSection>();
    public ICollection<UploadedFile> UploadedFiles { get; set; } = new List<UploadedFile>();
    public ICollection<ChecklistPublishScope> PublishScopes { get; set; } = new List<ChecklistPublishScope>();
    public ICollection<ChecklistTemplate> ChildTemplates { get; set; } = new List<ChecklistTemplate>();
}
