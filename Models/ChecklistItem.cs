using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class ChecklistItem
{
    public int Id { get; set; }

    public int ChecklistSectionId { get; set; }
    public ChecklistSection? ChecklistSection { get; set; }

    public int? ParentChecklistItemId { get; set; }
    public ChecklistItem? ParentChecklistItem { get; set; }
    public ICollection<ChecklistItem> SubItems { get; set; } = new List<ChecklistItem>();

    [MaxLength(240)]
    public string Prompt { get; set; } = string.Empty;

    [MaxLength(80)]
    public string ResponseType { get; set; } = "PassFail";

    [MaxLength(80)]
    public string ItemKind { get; set; } = "Field";

    public int? CatalogueItemId { get; set; }
    public CatalogueItem? CatalogueItem { get; set; }

    [MaxLength(160)]
    public string? EquipmentType { get; set; }

    [MaxLength(160)]
    public string? Model { get; set; }

    [MaxLength(120)]
    public string? FieldKey { get; set; }

    public bool RequiresCommentOnFail { get; set; }
    public bool IsRequired { get; set; } = true;
    public bool IsReadinessCritical { get; set; }
    public bool AllowsSameAsPrevious { get; set; } = true;

    [MaxLength(160)]
    public string? DefaultLocation { get; set; }

    public int DisplayOrder { get; set; }

    public ICollection<ChecklistColumnDefinition> ColumnDefinitions { get; set; } = new List<ChecklistColumnDefinition>();
}
