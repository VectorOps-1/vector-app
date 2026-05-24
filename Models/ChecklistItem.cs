using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class ChecklistItem
{
    public int Id { get; set; }

    public int ChecklistSectionId { get; set; }
    public ChecklistSection? ChecklistSection { get; set; }

    [MaxLength(240)]
    public string Prompt { get; set; } = string.Empty;

    [MaxLength(80)]
    public string ResponseType { get; set; } = "PassFail";

    public bool RequiresCommentOnFail { get; set; }
    public int DisplayOrder { get; set; }
}
