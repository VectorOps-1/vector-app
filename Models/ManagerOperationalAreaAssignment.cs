using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class ManagerOperationalAreaAssignment
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int ManagerUserId { get; set; }
    public AppUser? ManagerUser { get; set; }

    public int OperationalAreaId { get; set; }
    public OperationalArea? OperationalArea { get; set; }

    public int? AssignedByUserId { get; set; }
    public AppUser? AssignedByUser { get; set; }

    [MaxLength(80)]
    public string Status { get; set; } = "Active";

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
