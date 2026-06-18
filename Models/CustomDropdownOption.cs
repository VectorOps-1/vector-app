using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class CustomDropdownOption
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }

    [MaxLength(120)]
    public string DropdownKey { get; set; } = string.Empty;

    [MaxLength(160)]
    public string Value { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Status { get; set; } = "Active";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
