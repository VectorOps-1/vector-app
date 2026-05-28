using Microsoft.EntityFrameworkCore;
using vector_app_local.Models;

namespace vector_app_local.Data;

public class VectorDbContext : DbContext
{
    public VectorDbContext(DbContextOptions<VectorDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<AppRole> AppRoles => Set<AppRole>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<TaskEvent> TaskEvents => Set<TaskEvent>();
    public DbSet<IssueReport> IssueReports => Set<IssueReport>();
    public DbSet<IssueReportEvent> IssueReportEvents => Set<IssueReportEvent>();
    public DbSet<OperationalArea> OperationalAreas => Set<OperationalArea>();
    public DbSet<ManagerOperationalAreaAssignment> ManagerOperationalAreaAssignments => Set<ManagerOperationalAreaAssignment>();
    public DbSet<AssetMovement> AssetMovements => Set<AssetMovement>();
    public DbSet<MedicationItem> MedicationItems => Set<MedicationItem>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockOrder> StockOrders => Set<StockOrder>();
    public DbSet<StockOrderLine> StockOrderLines => Set<StockOrderLine>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<EquipmentItem> EquipmentItems => Set<EquipmentItem>();
    public DbSet<VehicleEquipmentAssignment> VehicleEquipmentAssignments => Set<VehicleEquipmentAssignment>();
    public DbSet<DailyVehicleReadinessReport> DailyVehicleReadinessReports => Set<DailyVehicleReadinessReport>();
    public DbSet<DailyVehicleEquipmentCheck> DailyVehicleEquipmentChecks => Set<DailyVehicleEquipmentCheck>();
    public DbSet<AssetFile> AssetFiles => Set<AssetFile>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<ChecklistTemplate> ChecklistTemplates => Set<ChecklistTemplate>();
    public DbSet<ChecklistSection> ChecklistSections => Set<ChecklistSection>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();
    public DbSet<UploadedFile> UploadedFiles => Set<UploadedFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppRole>()
            .HasIndex(role => role.Name)
            .IsUnique();

        modelBuilder.Entity<AppUser>()
            .HasIndex(user => new { user.CompanyId, user.Email })
            .IsUnique();

        modelBuilder.Entity<AppUser>()
            .HasOne(user => user.Company)
            .WithMany(company => company.Users)
            .HasForeignKey(user => user.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppUser>()
            .HasOne(user => user.AppRole)
            .WithMany(role => role.Users)
            .HasForeignKey(user => user.AppRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TaskItem>()
            .HasOne(task => task.Company)
            .WithMany(company => company.Tasks)
            .HasForeignKey(task => task.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TaskItem>()
            .HasOne(task => task.AssignedToUser)
            .WithMany(user => user.AssignedTasks)
            .HasForeignKey(task => task.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TaskItem>()
            .HasOne(task => task.AssignedByUser)
            .WithMany(user => user.CreatedTasks)
            .HasForeignKey(task => task.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TaskEvent>()
            .HasOne(taskEvent => taskEvent.TaskItem)
            .WithMany(task => task.Events)
            .HasForeignKey(taskEvent => taskEvent.TaskItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TaskEvent>()
            .HasOne(taskEvent => taskEvent.PerformedByUser)
            .WithMany(user => user.TaskEvents)
            .HasForeignKey(taskEvent => taskEvent.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<IssueReport>()
            .HasOne(issue => issue.Company)
            .WithMany(company => company.IssueReports)
            .HasForeignKey(issue => issue.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<IssueReport>()
            .HasOne(issue => issue.ReportedByUser)
            .WithMany(user => user.ReportedIssueReports)
            .HasForeignKey(issue => issue.ReportedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<IssueReport>()
            .HasOne(issue => issue.AssignedToUser)
            .WithMany(user => user.AssignedIssueReports)
            .HasForeignKey(issue => issue.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<IssueReport>()
            .HasOne(issue => issue.ResolvedByUser)
            .WithMany(user => user.ResolvedIssueReports)
            .HasForeignKey(issue => issue.ResolvedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<IssueReportEvent>()
            .HasOne(issueEvent => issueEvent.IssueReport)
            .WithMany(issue => issue.Events)
            .HasForeignKey(issueEvent => issueEvent.IssueReportId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<IssueReportEvent>()
            .HasOne(issueEvent => issueEvent.PerformedByUser)
            .WithMany(user => user.IssueReportEvents)
            .HasForeignKey(issueEvent => issueEvent.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OperationalArea>()
            .HasIndex(area => new { area.CompanyId, area.Name })
            .IsUnique();

        modelBuilder.Entity<OperationalArea>()
            .HasOne(area => area.Company)
            .WithMany(company => company.OperationalAreas)
            .HasForeignKey(area => area.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ManagerOperationalAreaAssignment>()
            .HasIndex(assignment => new { assignment.CompanyId, assignment.ManagerUserId, assignment.OperationalAreaId })
            .IsUnique();

        modelBuilder.Entity<ManagerOperationalAreaAssignment>()
            .HasOne(assignment => assignment.Company)
            .WithMany(company => company.ManagerOperationalAreaAssignments)
            .HasForeignKey(assignment => assignment.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ManagerOperationalAreaAssignment>()
            .HasOne(assignment => assignment.ManagerUser)
            .WithMany(user => user.OperationalAreaAssignments)
            .HasForeignKey(assignment => assignment.ManagerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ManagerOperationalAreaAssignment>()
            .HasOne(assignment => assignment.OperationalArea)
            .WithMany(area => area.ManagerAssignments)
            .HasForeignKey(assignment => assignment.OperationalAreaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ManagerOperationalAreaAssignment>()
            .HasOne(assignment => assignment.AssignedByUser)
            .WithMany(user => user.CreatedOperationalAreaAssignments)
            .HasForeignKey(assignment => assignment.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AssetMovement>()
            .HasIndex(movement => new { movement.CompanyId, movement.AssetType, movement.AssetId, movement.CreatedAtUtc });

        modelBuilder.Entity<AssetMovement>()
            .HasOne(movement => movement.Company)
            .WithMany(company => company.AssetMovements)
            .HasForeignKey(movement => movement.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AssetMovement>()
            .HasOne(movement => movement.FromOperationalArea)
            .WithMany(area => area.SourceMovements)
            .HasForeignKey(movement => movement.FromOperationalAreaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AssetMovement>()
            .HasOne(movement => movement.ToOperationalArea)
            .WithMany(area => area.DestinationMovements)
            .HasForeignKey(movement => movement.ToOperationalAreaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AssetMovement>()
            .HasOne(movement => movement.MovedByUser)
            .WithMany(user => user.AssetMovements)
            .HasForeignKey(movement => movement.MovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AssetMovement>()
            .HasOne(movement => movement.TaskItem)
            .WithMany()
            .HasForeignKey(movement => movement.TaskItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MedicationItem>()
            .HasOne(medication => medication.Company)
            .WithMany(company => company.MedicationItems)
            .HasForeignKey(medication => medication.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MedicationItem>()
            .HasOne(medication => medication.CreatedByUser)
            .WithMany(user => user.CreatedMedicationItems)
            .HasForeignKey(medication => medication.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MedicationItem>()
            .HasOne(medication => medication.LastAllocatedByUser)
            .WithMany(user => user.LastAllocatedMedicationItems)
            .HasForeignKey(medication => medication.LastAllocatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MedicationItem>()
            .HasOne(medication => medication.CurrentOperationalArea)
            .WithMany()
            .HasForeignKey(medication => medication.CurrentOperationalAreaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockItem>()
            .HasIndex(stock => new { stock.CompanyId, stock.ItemName, stock.BatchNumber, stock.Location });

        modelBuilder.Entity<StockItem>()
            .HasOne(stock => stock.Company)
            .WithMany(company => company.StockItems)
            .HasForeignKey(stock => stock.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockItem>()
            .HasOne(stock => stock.CreatedByUser)
            .WithMany(user => user.CreatedStockItems)
            .HasForeignKey(stock => stock.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockItem>()
            .HasOne(stock => stock.LastMovedByUser)
            .WithMany(user => user.LastMovedStockItems)
            .HasForeignKey(stock => stock.LastMovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockItem>()
            .HasOne(stock => stock.CurrentOperationalArea)
            .WithMany()
            .HasForeignKey(stock => stock.CurrentOperationalAreaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockOrder>()
            .HasIndex(order => new { order.CompanyId, order.Status, order.CreatedAtUtc });

        modelBuilder.Entity<StockOrder>()
            .HasOne(order => order.Company)
            .WithMany(company => company.StockOrders)
            .HasForeignKey(order => order.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockOrder>()
            .HasOne(order => order.RequestedByUser)
            .WithMany()
            .HasForeignKey(order => order.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockOrder>()
            .HasOne(order => order.ApprovedBySeniorUser)
            .WithMany()
            .HasForeignKey(order => order.ApprovedBySeniorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockOrder>()
            .HasOne(order => order.RegisterEntryAuthorisedUser)
            .WithMany()
            .HasForeignKey(order => order.RegisterEntryAuthorisedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockOrderLine>()
            .HasOne(line => line.StockOrder)
            .WithMany(order => order.Lines)
            .HasForeignKey(line => line.StockOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Vehicle>()
            .HasIndex(vehicle => new { vehicle.CompanyId, vehicle.RegistrationNumber })
            .IsUnique();

        modelBuilder.Entity<Vehicle>()
            .HasOne(vehicle => vehicle.Company)
            .WithMany(company => company.Vehicles)
            .HasForeignKey(vehicle => vehicle.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Vehicle>()
            .HasOne(vehicle => vehicle.CurrentOperationalArea)
            .WithMany()
            .HasForeignKey(vehicle => vehicle.CurrentOperationalAreaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Vehicle>()
            .HasOne(vehicle => vehicle.LastMovedByUser)
            .WithMany(user => user.LastMovedVehicles)
            .HasForeignKey(vehicle => vehicle.LastMovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EquipmentItem>()
            .HasIndex(equipment => new { equipment.CompanyId, equipment.SerialOrAssetId });

        modelBuilder.Entity<EquipmentItem>()
            .HasOne(equipment => equipment.Company)
            .WithMany(company => company.EquipmentItems)
            .HasForeignKey(equipment => equipment.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EquipmentItem>()
            .HasOne(equipment => equipment.CurrentOperationalArea)
            .WithMany()
            .HasForeignKey(equipment => equipment.CurrentOperationalAreaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EquipmentItem>()
            .HasOne(equipment => equipment.LastMovedByUser)
            .WithMany(user => user.LastMovedEquipmentItems)
            .HasForeignKey(equipment => equipment.LastMovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VehicleEquipmentAssignment>()
            .HasIndex(assignment => new { assignment.CompanyId, assignment.VehicleId, assignment.SortOrder });

        modelBuilder.Entity<VehicleEquipmentAssignment>()
            .HasIndex(assignment => new { assignment.CompanyId, assignment.VehicleType, assignment.QualificationLevel });

        modelBuilder.Entity<VehicleEquipmentAssignment>()
            .HasOne(assignment => assignment.Company)
            .WithMany(company => company.VehicleEquipmentAssignments)
            .HasForeignKey(assignment => assignment.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VehicleEquipmentAssignment>()
            .HasOne(assignment => assignment.Vehicle)
            .WithMany(vehicle => vehicle.EquipmentAssignments)
            .HasForeignKey(assignment => assignment.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VehicleEquipmentAssignment>()
            .HasOne(assignment => assignment.EquipmentItem)
            .WithMany(equipment => equipment.VehicleAssignments)
            .HasForeignKey(assignment => assignment.EquipmentItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DailyVehicleReadinessReport>()
            .HasIndex(report => new { report.CompanyId, report.VehicleId, report.InspectionDateUtc });

        modelBuilder.Entity<DailyVehicleReadinessReport>()
            .HasIndex(report => new { report.CompanyId, report.ReadinessStatus, report.InspectionDateUtc });

        modelBuilder.Entity<DailyVehicleReadinessReport>()
            .HasIndex(report => new { report.CompanyId, report.WorkflowStatus, report.DraftExpiresAtUtc });

        modelBuilder.Entity<DailyVehicleReadinessReport>()
            .HasOne(report => report.Company)
            .WithMany(company => company.DailyVehicleReadinessReports)
            .HasForeignKey(report => report.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DailyVehicleReadinessReport>()
            .HasOne(report => report.Vehicle)
            .WithMany(vehicle => vehicle.ReadinessReports)
            .HasForeignKey(report => report.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DailyVehicleReadinessReport>()
            .HasOne(report => report.PerformedByUser)
            .WithMany(user => user.PerformedVehicleReadinessReports)
            .HasForeignKey(report => report.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DailyVehicleReadinessReport>()
            .HasOne(report => report.VehicleSameAsPreviousSourceReport)
            .WithMany()
            .HasForeignKey(report => report.VehicleSameAsPreviousSourceReportId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DailyVehicleReadinessReport>()
            .HasOne(report => report.EquipmentSameAsPreviousSourceReport)
            .WithMany()
            .HasForeignKey(report => report.EquipmentSameAsPreviousSourceReportId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DailyVehicleEquipmentCheck>()
            .HasIndex(check => new { check.CompanyId, check.DailyVehicleReadinessReportId, check.SortOrder });

        modelBuilder.Entity<DailyVehicleEquipmentCheck>()
            .HasOne(check => check.Company)
            .WithMany(company => company.DailyVehicleEquipmentChecks)
            .HasForeignKey(check => check.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DailyVehicleEquipmentCheck>()
            .HasOne(check => check.DailyVehicleReadinessReport)
            .WithMany(report => report.EquipmentChecks)
            .HasForeignKey(check => check.DailyVehicleReadinessReportId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DailyVehicleEquipmentCheck>()
            .HasOne(check => check.VehicleEquipmentAssignment)
            .WithMany(assignment => assignment.DailyEquipmentChecks)
            .HasForeignKey(check => check.VehicleEquipmentAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DailyVehicleEquipmentCheck>()
            .HasOne(check => check.EquipmentItem)
            .WithMany(equipment => equipment.DailyEquipmentChecks)
            .HasForeignKey(check => check.EquipmentItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DailyVehicleEquipmentCheck>()
            .HasOne(check => check.CopiedFromDailyVehicleEquipmentCheck)
            .WithMany()
            .HasForeignKey(check => check.CopiedFromDailyVehicleEquipmentCheckId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChecklistTemplate>()
            .HasIndex(template => new { template.CompanyId, template.ChecklistType, template.TargetVehicleType, template.Name });

        modelBuilder.Entity<ChecklistTemplate>()
            .HasOne(template => template.Company)
            .WithMany(company => company.ChecklistTemplates)
            .HasForeignKey(template => template.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChecklistSection>()
            .HasOne(section => section.ChecklistTemplate)
            .WithMany(template => template.Sections)
            .HasForeignKey(section => section.ChecklistTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChecklistItem>()
            .HasOne(item => item.ChecklistSection)
            .WithMany(section => section.Items)
            .HasForeignKey(item => item.ChecklistSectionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UploadedFile>()
            .HasOne(file => file.ChecklistTemplate)
            .WithMany(template => template.UploadedFiles)
            .HasForeignKey(file => file.ChecklistTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AssetFile>()
            .HasIndex(file => new { file.CompanyId, file.LinkedEntityType, file.LinkedEntityId, file.Category });

        modelBuilder.Entity<AssetFile>()
            .HasOne(file => file.Company)
            .WithMany(company => company.AssetFiles)
            .HasForeignKey(file => file.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AssetFile>()
            .HasOne(file => file.UploadedByUser)
            .WithMany(user => user.UploadedAssetFiles)
            .HasForeignKey(file => file.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AuditLog>()
            .HasOne(auditLog => auditLog.Company)
            .WithMany(company => company.AuditLogs)
            .HasForeignKey(auditLog => auditLog.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AuditLog>()
            .HasOne(auditLog => auditLog.AppUser)
            .WithMany(user => user.AuditLogs)
            .HasForeignKey(auditLog => auditLog.AppUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
