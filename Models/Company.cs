using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class Company
{
    public int Id { get; set; }

    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Status { get; set; } = "Active";

    [MaxLength(40)]
    public string SubscriptionTier { get; set; } = SubscriptionTiers.Base;

    [MaxLength(260)]
    public string? LogoStoragePath { get; set; }

    public bool AllowSameAsPreviousVehicleInspection { get; set; } = true;
    public bool AllowSameAsPreviousEquipmentCheck { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<IssueReport> IssueReports { get; set; } = new List<IssueReport>();
    public ICollection<OperationalArea> OperationalAreas { get; set; } = new List<OperationalArea>();
    public ICollection<AssetMovement> AssetMovements { get; set; } = new List<AssetMovement>();
    public ICollection<MedicationItem> MedicationItems { get; set; } = new List<MedicationItem>();
    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
    public ICollection<StockOrder> StockOrders { get; set; } = new List<StockOrder>();
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<EquipmentItem> EquipmentItems { get; set; } = new List<EquipmentItem>();
    public ICollection<VehicleEquipmentAssignment> VehicleEquipmentAssignments { get; set; } = new List<VehicleEquipmentAssignment>();
    public ICollection<DailyVehicleReadinessReport> DailyVehicleReadinessReports { get; set; } = new List<DailyVehicleReadinessReport>();
    public ICollection<DailyVehicleEquipmentCheck> DailyVehicleEquipmentChecks { get; set; } = new List<DailyVehicleEquipmentCheck>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
