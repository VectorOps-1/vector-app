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
    public string? StaffIdentifier { get; set; }

    [MaxLength(120)]
    public string? NationalId { get; set; }

    [MaxLength(80)]
    public string? CellNumber { get; set; }

    [MaxLength(120)]
    public string? QualificationFunction { get; set; }

    [MaxLength(120)]
    public string? PractitionerNumber { get; set; }

    public DateTime? AnnualLicenseExpiryDate { get; set; }

    [MaxLength(80)]
    public string? CpdComplianceStatus { get; set; }

    public DateTime? CpdComplianceExpiryDate { get; set; }

    public int? AssignedOperationalAreaId { get; set; }
    public OperationalArea? AssignedOperationalArea { get; set; }

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
    public ICollection<ChecklistVarianceAlert> DetectedChecklistVarianceAlerts { get; set; } = new List<ChecklistVarianceAlert>();
    public ICollection<ChecklistVarianceAlert> AssignedChecklistVarianceAlerts { get; set; } = new List<ChecklistVarianceAlert>();
    public ICollection<ChecklistVarianceAlert> ReviewedChecklistVarianceAlerts { get; set; } = new List<ChecklistVarianceAlert>();
    public ICollection<ReadinessAlert> TriggeredReadinessAlerts { get; set; } = new List<ReadinessAlert>();
    public ICollection<ReadinessAlert> AssignedReadinessAlerts { get; set; } = new List<ReadinessAlert>();
    public ICollection<ReadinessAlert> ReviewedReadinessAlerts { get; set; } = new List<ReadinessAlert>();
    public ICollection<AssetFile> UploadedAssetFiles { get; set; } = new List<AssetFile>();
    public ICollection<AppUserAccessPermission> AccessPermissions { get; set; } = new List<AppUserAccessPermission>();
    public ICollection<AppUserAccessPermission> UpdatedAccessPermissions { get; set; } = new List<AppUserAccessPermission>();
    public ICollection<ManagerOperationalAreaAssignment> OperationalAreaAssignments { get; set; } = new List<ManagerOperationalAreaAssignment>();
    public ICollection<ManagerOperationalAreaAssignment> CreatedOperationalAreaAssignments { get; set; } = new List<ManagerOperationalAreaAssignment>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
