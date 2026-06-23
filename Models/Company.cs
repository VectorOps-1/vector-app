using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class Company
{
    public int Id { get; set; }

    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(160)]
    public string? TradingName { get; set; }

    [MaxLength(160)]
    public string? ContactEmail { get; set; }

    [MaxLength(60)]
    public string? ContactPhone { get; set; }

    [MaxLength(80)]
    public string? Country { get; set; }

    [MaxLength(120)]
    public string? Region { get; set; }

    [MaxLength(80)]
    public string? Timezone { get; set; }

    [MaxLength(80)]
    public string? OperationalStructureMode { get; set; }

    [MaxLength(80)]
    public string BrandingStatus { get; set; } = "Incomplete";

    [MaxLength(80)]
    public string? SetupWizardCurrentStepKey { get; set; }

    [MaxLength(1000)]
    public string? SetupWizardCompletedStepKeys { get; set; }

    public DateTime? SetupWizardUpdatedAtUtc { get; set; }

    [MaxLength(80)]
    public string Status { get; set; } = "Active";

    [MaxLength(40)]
    public string SubscriptionTier { get; set; } = SubscriptionTiers.Base;

    [MaxLength(120)]
    public string? WorkspaceSlug { get; set; }

    [MaxLength(120)]
    public string? WorkspaceAccessCode { get; set; }

    [MaxLength(260)]
    public string? LogoStoragePath { get; set; }

    public bool LogoRemoved { get; set; }

    public bool AllowSameAsPreviousVehicleInspection { get; set; } = true;
    public bool AllowSameAsPreviousEquipmentCheck { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<TaskEvent> TaskEvents { get; set; } = new List<TaskEvent>();
    public ICollection<IssueReport> IssueReports { get; set; } = new List<IssueReport>();
    public ICollection<OperationalArea> OperationalAreas { get; set; } = new List<OperationalArea>();
    public ICollection<StorageLocation> StorageLocations { get; set; } = new List<StorageLocation>();
    public ICollection<AssetMovement> AssetMovements { get; set; } = new List<AssetMovement>();
    public ICollection<MedicationItem> MedicationItems { get; set; } = new List<MedicationItem>();
    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
    public ICollection<StockOrder> StockOrders { get; set; } = new List<StockOrder>();
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<EquipmentItem> EquipmentItems { get; set; } = new List<EquipmentItem>();
    public ICollection<VehicleEquipmentAssignment> VehicleEquipmentAssignments { get; set; } = new List<VehicleEquipmentAssignment>();
    public ICollection<DailyVehicleReadinessReport> DailyVehicleReadinessReports { get; set; } = new List<DailyVehicleReadinessReport>();
    public ICollection<DailyVehicleEquipmentCheck> DailyVehicleEquipmentChecks { get; set; } = new List<DailyVehicleEquipmentCheck>();
    public ICollection<ChecklistTemplate> ChecklistTemplates { get; set; } = new List<ChecklistTemplate>();
    public ICollection<ChecklistPublishScope> ChecklistPublishScopes { get; set; } = new List<ChecklistPublishScope>();
    public ICollection<UploadedFile> UploadedFiles { get; set; } = new List<UploadedFile>();
    public ICollection<CatalogueItem> CatalogueItems { get; set; } = new List<CatalogueItem>();
    public ICollection<CustomDropdownOption> CustomDropdownOptions { get; set; } = new List<CustomDropdownOption>();
    public ICollection<ChecklistVarianceAlert> ChecklistVarianceAlerts { get; set; } = new List<ChecklistVarianceAlert>();
    public ICollection<ReadinessAlert> ReadinessAlerts { get; set; } = new List<ReadinessAlert>();
    public ICollection<ReadinessEngineVersion> ReadinessEngineVersions { get; set; } = new List<ReadinessEngineVersion>();
    public ICollection<ReadinessEngineRule> ReadinessEngineRules { get; set; } = new List<ReadinessEngineRule>();
    public ICollection<ReadinessScoringChangeRequest> ReadinessScoringChangeRequests { get; set; } = new List<ReadinessScoringChangeRequest>();
    public ICollection<AssetFile> AssetFiles { get; set; } = new List<AssetFile>();
    public ICollection<AppUserAccessPermission> AppUserAccessPermissions { get; set; } = new List<AppUserAccessPermission>();
    public ICollection<ManagerOperationalAreaAssignment> ManagerOperationalAreaAssignments { get; set; } = new List<ManagerOperationalAreaAssignment>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
