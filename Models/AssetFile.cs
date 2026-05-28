using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class AssetFile
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int UploadedByUserId { get; set; }
    public AppUser? UploadedByUser { get; set; }

    [MaxLength(80)]
    public string LinkedEntityType { get; set; } = string.Empty;

    public int LinkedEntityId { get; set; }

    [MaxLength(120)]
    public string Category { get; set; } = "General";

    [MaxLength(260)]
    public string OriginalFileName { get; set; } = string.Empty;

    [MaxLength(120)]
    public string ContentType { get; set; } = string.Empty;

    [MaxLength(40)]
    public string StorageProvider { get; set; } = string.Empty;

    [MaxLength(520)]
    public string StoragePath { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    [MaxLength(1200)]
    public string? Notes { get; set; }

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}
