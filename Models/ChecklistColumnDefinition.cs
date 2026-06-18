using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class ChecklistColumnDefinition
{
    public int Id { get; set; }

    public int ChecklistItemId { get; set; }
    public ChecklistItem? ChecklistItem { get; set; }

    [MaxLength(120)]
    public string Heading { get; set; } = string.Empty;

    [MaxLength(120)]
    public string FieldKey { get; set; } = string.Empty;

    [MaxLength(80)]
    public string ResponseType { get; set; } = "Text";

    [MaxLength(160)]
    public string? RegisterSource { get; set; }

    public bool IsRequired { get; set; }
    public bool IsEditable { get; set; } = true;
    public bool AllowsNotApplicable { get; set; } = true;
    public bool PullsFromRegister { get; set; }
    public bool AffectsReadiness { get; set; }
    public bool SameAsPreviousEligible { get; set; } = true;
    public bool RequiresNoteWhenNotNormal { get; set; }

    [MaxLength(80)]
    public string? ReadinessImpact { get; set; }

    [MaxLength(1200)]
    public string? DropdownOptionsJson { get; set; }

    public int SortOrder { get; set; }
}
