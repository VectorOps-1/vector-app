using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class UploadedFile
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int ChecklistTemplateId { get; set; }
    public ChecklistTemplate? ChecklistTemplate { get; set; }

    [MaxLength(260)]
    public string OriginalFileName { get; set; } = string.Empty;

    [MaxLength(120)]
    public string ContentType { get; set; } = string.Empty;

    [MaxLength(520)]
    public string StoragePath { get; set; } = string.Empty;

    public long SizeBytes { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}
