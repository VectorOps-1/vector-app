using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class ChecklistSection
{
    public int Id { get; set; }

    public int ChecklistTemplateId { get; set; }
    public ChecklistTemplate? ChecklistTemplate { get; set; }

    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public ICollection<ChecklistItem> Items { get; set; } = new List<ChecklistItem>();
}
