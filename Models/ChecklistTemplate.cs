using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class ChecklistTemplate
{
    public int Id { get; set; }

    [MaxLength(160)]
    public string ClientName { get; set; } = string.Empty;

    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Version { get; set; } = "1.0";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ChecklistSection> Sections { get; set; } = new List<ChecklistSection>();
    public ICollection<UploadedFile> UploadedFiles { get; set; } = new List<UploadedFile>();
}
