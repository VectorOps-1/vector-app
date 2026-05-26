using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class AppRole
{
    public int Id { get; set; }

    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(240)]
    public string? Description { get; set; }

    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
}
