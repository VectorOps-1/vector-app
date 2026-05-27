using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class AppUser
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int AppRoleId { get; set; }
    public AppRole? AppRole { get; set; }

    [MaxLength(160)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(180)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Status { get; set; } = "Active";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }

    public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
    public ICollection<TaskItem> CreatedTasks { get; set; } = new List<TaskItem>();
    public ICollection<TaskEvent> TaskEvents { get; set; } = new List<TaskEvent>();
    public ICollection<IssueReport> ReportedIssueReports { get; set; } = new List<IssueReport>();
    public ICollection<IssueReport> AssignedIssueReports { get; set; } = new List<IssueReport>();
    public ICollection<IssueReport> ResolvedIssueReports { get; set; } = new List<IssueReport>();
    public ICollection<IssueReportEvent> IssueReportEvents { get; set; } = new List<IssueReportEvent>();
    public ICollection<MedicationItem> CreatedMedicationItems { get; set; } = new List<MedicationItem>();
    public ICollection<MedicationItem> LastAllocatedMedicationItems { get; set; } = new List<MedicationItem>();
    public ICollection<StockItem> CreatedStockItems { get; set; } = new List<StockItem>();
    public ICollection<StockItem> LastMovedStockItems { get; set; } = new List<StockItem>();
    public ICollection<Vehicle> LastMovedVehicles { get; set; } = new List<Vehicle>();
    public ICollection<EquipmentItem> LastMovedEquipmentItems { get; set; } = new List<EquipmentItem>();
    public ICollection<AssetMovement> AssetMovements { get; set; } = new List<AssetMovement>();
    public ICollection<DailyVehicleReadinessReport> PerformedVehicleReadinessReports { get; set; } = new List<DailyVehicleReadinessReport>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
