using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class AppUserAccessPermission
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    [MaxLength(120)]
    public string PermissionKey { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Status { get; set; } = "Allowed";

    public int UpdatedByUserId { get; set; }
    public AppUser? UpdatedByUser { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
