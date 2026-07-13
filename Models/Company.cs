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

    [MaxLength(120)]
    public string? StaffIdFormat { get; set; }

    public bool StaffPractitionerNumberRequired { get; set; }

    public bool StaffAnnualLicenseExpiryRequired { get; set; }

    public bool StaffCpdTrackingRequired { get; set; }

    [MaxLength(1000)]
    public string? StaffDefaultProfileFields { get; set; }

    [MaxLength(80)]
    public string? OperationalManagerScopeBehavior { get; set; }

    [MaxLength(4000)]
    public string? CompanyOwnerDefaultPermissionKeys { get; set; }

    [MaxLength(4000)]
    public string? SeniorManagerDefaultPermissionKeys { get; set; }

    [MaxLength(4000)]
    public string? OperationalManagerDefaultPermissionKeys { get; set; }

    [MaxLength(4000)]
    public string? StaffDefaultPermissionKeys { get; set; }

    public bool AccessModelDefaultsConfigured { get; set; }

    [MaxLength(80)]
    public string? VehicleRegisterSetupChoice { get; set; }

    [MaxLength(80)]
    public string? EquipmentRegisterSetupChoice { get; set; }

    [MaxLength(80)]
    public string? StockRegisterSetupChoice { get; set; }

    [MaxLength(80)]
    public string? MedicationRegisterSetupChoice { get; set; }

    [MaxLength(80)]
    public string? StaffRegisterSetupChoice { get; set; }

    [MaxLength(80)]
    public string? StorageLocationSetupChoice { get; set; }

    [MaxLength(1000)]
    public string? AssetRegisterSetupNotes { get; set; }

    public bool AssetRegisterSetupConfigured { get; set; }

    [MaxLength(80)]
    public string? DailyChecklistSetupChoice { get; set; }

    [MaxLength(500)]
    public string? DailyChecklistPublishScopeKeys { get; set; }

    [MaxLength(80)]
    public string? FullAuditChecklistSetupChoice { get; set; }

    [MaxLength(1000)]
    public string? ChecklistSetupNotes { get; set; }

    public bool ChecklistSetupConfigured { get; set; }

    [MaxLength(80)]
    public string? ReadinessScoringSetupChoice { get; set; }

    public bool ReadinessScoringActivated { get; set; }

    public bool RequireSeniorApprovalForScoringChanges { get; set; } = true;

    [MaxLength(1000)]
    public string? ReadinessEngineSetupNotes { get; set; }

    public bool ReadinessEngineSetupConfigured { get; set; }

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
    public ICollection<StaffQualificationSetup> StaffQualificationSetups { get; set; } = new List<StaffQualificationSetup>();
    public ICollection<VehicleFunctionSetup> VehicleFunctionSetups { get; set; } = new List<VehicleFunctionSetup>();
    public ICollection<VehicleSubtypeSetup> VehicleSubtypeSetups { get; set; } = new List<VehicleSubtypeSetup>();
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
    public ICollection<ImportBatch> ImportBatches { get; set; } = new List<ImportBatch>();
    public ICollection<ImportColumnMapping> ImportColumnMappings { get; set; } = new List<ImportColumnMapping>();
    public ICollection<ImportRowResult> ImportRowResults { get; set; } = new List<ImportRowResult>();
    public ICollection<ImportEntityChange> ImportEntityChanges { get; set; } = new List<ImportEntityChange>();
}
