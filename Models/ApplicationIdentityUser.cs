using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class ApplicationIdentityUser : IdentityUser
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    public bool IsLoginEnabled { get; set; }
    public bool MustChangePassword { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(80)]
    public string AuthenticationProvider { get; set; } = "LocalIdentity";
}
