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
    public DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();
    public DbSet<StaffQualificationSetup> StaffQualificationSetups => Set<StaffQualificationSetup>();
    public DbSet<VehicleFunctionSetup> VehicleFunctionSetups => Set<VehicleFunctionSetup>();
    public DbSet<VehicleSubtypeSetup> VehicleSubtypeSetups => Set<VehicleSubtypeSetup>();
    public DbSet<ManagerOperationalAreaAssignment> ManagerOperationalAreaAssignments => Set<ManagerOperationalAreaAssignment>();
    public DbSet<AssetMovement> AssetMovements => Set<AssetMovement>();
    public DbSet<MedicationItem> MedicationItems => Set<MedicationItem>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockOrder> StockOrders => Set<StockOrder>();
    public DbSet<StockOrderLine> StockOrderLines => Set<StockOrderLine>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleSchematicAssignment> VehicleSchematicAssignments => Set<VehicleSchematicAssignment>();
    public DbSet<EquipmentItem> EquipmentItems => Set<EquipmentItem>();
    public DbSet<VehicleEquipmentAssignment> VehicleEquipmentAssignments => Set<VehicleEquipmentAssignment>();
    public DbSet<DailyVehicleReadinessReport> DailyVehicleReadinessReports => Set<DailyVehicleReadinessReport>();
    public DbSet<DailyVehicleEquipmentCheck> DailyVehicleEquipmentChecks => Set<DailyVehicleEquipmentCheck>();
    public DbSet<AssetFile> AssetFiles => Set<AssetFile>();
    public DbSet<AppUserAccessPermission> AppUserAccessPermissions => Set<AppUserAccessPermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<ChecklistTemplate> ChecklistTemplates => Set<ChecklistTemplate>();
    public DbSet<ChecklistSection> ChecklistSections => Set<ChecklistSection>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();
    public DbSet<ChecklistColumnDefinition> ChecklistColumnDefinitions => Set<ChecklistColumnDefinition>();
    public DbSet<ChecklistPublishScope> ChecklistPublishScopes => Set<ChecklistPublishScope>();
    public DbSet<ChecklistVarianceAlert> ChecklistVarianceAlerts => Set<ChecklistVarianceAlert>();
    public DbSet<ReadinessAlert> ReadinessAlerts => Set<ReadinessAlert>();
    public DbSet<ReadinessEngineVersion> ReadinessEngineVersions => Set<ReadinessEngineVersion>();
    public DbSet<ReadinessEngineRule> ReadinessEngineRules => Set<ReadinessEngineRule>();
    public DbSet<ReadinessScoringChangeRequest> ReadinessScoringChangeRequests => Set<ReadinessScoringChangeRequest>();
    public DbSet<CatalogueItem> CatalogueItems => Set<CatalogueItem>();
    public DbSet<UploadedFile> UploadedFiles => Set<UploadedFile>();
    public DbSet<CustomDropdownOption> CustomDropdownOptions => Set<CustomDropdownOption>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<ImportColumnMapping> ImportColumnMappings => Set<ImportColumnMapping>();
    public DbSet<ImportRowResult> ImportRowResults => Set<ImportRowResult>();
    public DbSet<ImportEntityChange> ImportEntityChanges => Set<ImportEntityChange>();

    public override int SaveChanges()
    {
        PrepareAuditLogs();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        PrepareAuditLogs();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppRole>()
            .HasIndex(role => role.Name)
            .IsUnique();

        modelBuilder.Entity<Company>()
            .HasIndex(company => company.WorkspaceSlug)
            .IsUnique()
            .HasFilter("WorkspaceSlug IS NOT NULL");

        modelBuilder.Entity<AppUser>()
            .HasIndex(user => new { user.CompanyId, user.Email })
            .IsUnique();

        modelBuilder.Entity<AppUser>()
            .HasIndex(user => new { user.CompanyId, user.StaffIdentifier });

        modelBuilder.Entity<AppUser>()
            .HasIndex(user => new { user.CompanyId, user.QualificationFunction });

        modelBuilder.Entity<AppUser>()
            .HasIndex(user => new { user.CompanyId, user.PractitionerNumber });

        modelBuilder.Entity<AppUser>()
            .HasIndex(user => new { user.CompanyId, user.AnnualLicenseExpiryDate });

        modelBuilder.Entity<AppUser>()
            .HasIndex(user => new { user.CompanyId, user.CpdComplianceStatus });

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

        modelBuilder.Entity<AppUser>()
            .HasOne(user => user.AssignedOperationalArea)
            .WithMany()
            .HasForeignKey(user => user.AssignedOperationalAreaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppUserAccessPermission>()
            .HasIndex(permission => new { permission.CompanyId, permission.AppUserId, permission.PermissionKey })
            .IsUnique();

        modelBuilder.Entity<AppUserAccessPermission>()
            .HasOne(permission => permission.Company)
            .WithMany(company => company.AppUserAccessPermissions)
            .HasForeignKey(permission => permission.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ImportBatch>()
            .HasIndex(batch => new { batch.CompanyId, batch.Status, batch.CreatedAtUtc });

        modelBuilder.Entity<ImportBatch>()
            .HasIndex(batch => batch.SourceAssetFileId)
            .IsUnique();

        modelBuilder.Entity<ImportBatch>()
            .Property(batch => batch.ConcurrencyToken)
            .IsConcurrencyToken();

        modelBuilder.Entity<ImportBatch>()
            .HasOne(batch => batch.Company)
            .WithMany(company => company.ImportBatches)
            .HasForeignKey(batch => batch.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ImportBatch>()
            .HasOne(batch => batch.SourceAssetFile)
            .WithMany()
            .HasForeignKey(batch => batch.SourceAssetFileId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ImportBatch>()
            .HasOne(batch => batch.CreatedByUser)
            .WithMany()
            .HasForeignKey(batch => batch.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ImportBatch>()
            .HasOne(batch => batch.ValidatedByUser)
            .WithMany()
            .HasForeignKey(batch => batch.ValidatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ImportBatch>()
            .HasOne(batch => batch.CommittedByUser)
            .WithMany()
            .HasForeignKey(batch => batch.CommittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ImportBatch>()
            .HasOne(batch => batch.RolledBackByUser)
            .WithMany()
            .HasForeignKey(batch => batch.RolledBackByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ImportColumnMapping>()
            .HasIndex(mapping => new { mapping.CompanyId, mapping.ImportBatchId, mapping.SourceColumnIndex })
            .IsUnique();

        modelBuilder.Entity<ImportColumnMapping>()
            .HasOne(mapping => mapping.Company)
            .WithMany(company => company.ImportColumnMappings)
            .HasForeignKey(mapping => mapping.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ImportColumnMapping>()
            .HasOne(mapping => mapping.ImportBatch)
            .WithMany(batch => batch.ColumnMappings)
            .HasForeignKey(mapping => mapping.ImportBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ImportRowResult>()
            .HasIndex(row => new { row.CompanyId, row.ImportBatchId, row.SourceRowNumber })
            .IsUnique();

        modelBuilder.Entity<ImportRowResult>()
            .HasOne(row => row.Company)
            .WithMany(company => company.ImportRowResults)
            .HasForeignKey(row => row.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ImportRowResult>()
            .HasOne(row => row.ImportBatch)
            .WithMany(batch => batch.RowResults)
            .HasForeignKey(row => row.ImportBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ImportEntityChange>()
            .HasIndex(change => new { change.CompanyId, change.ImportBatchId, change.EntityType, change.EntityId });

        modelBuilder.Entity<ImportEntityChange>()
            .HasOne(change => change.Company)
            .WithMany(company => company.ImportEntityChanges)
            .HasForeignKey(change => change.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ImportEntityChange>()
            .HasOne(change => change.ImportBatch)
            .WithMany(batch => batch.EntityChanges)
            .HasForeignKey(change => change.ImportBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ImportEntityChange>()
            .HasOne(change => change.ImportRowResult)
            .WithMany()
            .HasForeignKey(change => change.ImportRowResultId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ImportEntityChange>()
            .HasOne(change => change.RolledBackByUser)
            .WithMany()
            .HasForeignKey(change => change.RolledBackByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppUserAccessPermission>()
            .HasOne(permission => permission.AppUser)
            .WithMany(user => user.AccessPermissions)
            .HasForeignKey(permission => permission.AppUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppUserAccessPermission>()
            .HasOne(permission => permission.UpdatedByUser)
            .WithMany(user => user.UpdatedAccessPermissions)
            .HasForeignKey(permission => permission.UpdatedByUserId)
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
            .HasIndex(taskEvent => new { taskEvent.CompanyId, taskEvent.TaskItemId, taskEvent.CreatedAtUtc });

        modelBuilder.Entity<TaskEvent>()
            .HasOne(taskEvent => taskEvent.Company)
            .WithMany(company => company.TaskEvents)
            .HasForeignKey(taskEvent => taskEvent.CompanyId)
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
            .HasIndex(area => new { area.CompanyId, area.AreaType, area.ParentOperationalAreaId });

        modelBuilder.Entity<OperationalArea>()
            .HasOne(area => area.Company)
            .WithMany(company => company.OperationalAreas)
            .HasForeignKey(area => area.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OperationalArea>()
            .HasOne(area => area.ParentOperationalArea)
            .WithMany(area => area.ChildOperationalAreas)
            .HasForeignKey(area => area.ParentOperationalAreaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StorageLocation>()
            .HasIndex(location => new { location.CompanyId, location.OperationalAreaId, location.Name })
            .IsUnique();

        modelBuilder.Entity<StorageLocation>()
            .HasOne(location => location.Company)
            .WithMany(company => company.StorageLocations)
            .HasForeignKey(location => location.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StorageLocation>()
            .HasOne(location => location.OperationalArea)
            .WithMany(area => area.StorageLocations)
            .HasForeignKey(location => location.OperationalAreaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StaffQualificationSetup>()
            .HasIndex(qualification => new { qualification.CompanyId, qualification.Name })
            .IsUnique();

        modelBuilder.Entity<StaffQualificationSetup>()
            .HasIndex(qualification => new { qualification.CompanyId, qualification.Status, qualification.SortOrder });

        modelBuilder.Entity<StaffQualificationSetup>()
            .HasOne(qualification => qualification.Company)
            .WithMany(company => company.StaffQualificationSetups)
            .HasForeignKey(qualification => qualification.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VehicleFunctionSetup>()
            .HasIndex(vehicleFunction => new { vehicleFunction.CompanyId, vehicleFunction.Name })
            .IsUnique();

        modelBuilder.Entity<VehicleFunctionSetup>()
            .HasIndex(vehicleFunction => new { vehicleFunction.CompanyId, vehicleFunction.Status, vehicleFunction.SortOrder });

        modelBuilder.Entity<VehicleFunctionSetup>()
            .HasOne(vehicleFunction => vehicleFunction.Company)
            .WithMany(company => company.VehicleFunctionSetups)
            .HasForeignKey(vehicleFunction => vehicleFunction.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VehicleSubtypeSetup>()
            .HasIndex(vehicleSubtype => new { vehicleSubtype.CompanyId, vehicleSubtype.VehicleFunctionSetupId, vehicleSubtype.Name })
            .IsUnique();

        modelBuilder.Entity<VehicleSubtypeSetup>()
            .HasIndex(vehicleSubtype => new { vehicleSubtype.CompanyId, vehicleSubtype.Status, vehicleSubtype.SortOrder });

        modelBuilder.Entity<VehicleSubtypeSetup>()
            .HasOne(vehicleSubtype => vehicleSubtype.Company)
            .WithMany(company => company.VehicleSubtypeSetups)
            .HasForeignKey(vehicleSubtype => vehicleSubtype.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VehicleSubtypeSetup>()
            .HasOne(vehicleSubtype => vehicleSubtype.VehicleFunctionSetup)
            .WithMany(vehicleFunction => vehicleFunction.Subtypes)
            .HasForeignKey(vehicleSubtype => vehicleSubtype.VehicleFunctionSetupId)
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
            .HasIndex(stock => new { stock.CompanyId, stock.StockCategory, stock.ItemName });

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

        modelBuilder.Entity<VehicleSchematicAssignment>()
            .HasIndex(assignment => new { assignment.CompanyId, assignment.ScopeType, assignment.VehicleFunction, assignment.VehicleSubtype });

        modelBuilder.Entity<VehicleSchematicAssignment>()
            .HasIndex(assignment => new { assignment.CompanyId, assignment.SchematicKey });

        modelBuilder.Entity<VehicleSchematicAssignment>()
            .HasIndex(assignment => new { assignment.CompanyId, assignment.ScopeType, assignment.OperationalAreaId });

        modelBuilder.Entity<VehicleSchematicAssignment>()
            .HasIndex(assignment => new { assignment.CompanyId, assignment.ScopeType, assignment.VehicleId });

        modelBuilder.Entity<VehicleSchematicAssignment>()
            .HasOne(assignment => assignment.Company)
            .WithMany()
            .HasForeignKey(assignment => assignment.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VehicleSchematicAssignment>()
            .HasOne(assignment => assignment.CreatedByUser)
            .WithMany()
            .HasForeignKey(assignment => assignment.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VehicleSchematicAssignment>()
            .HasOne(assignment => assignment.OperationalArea)
            .WithMany()
            .HasForeignKey(assignment => assignment.OperationalAreaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VehicleSchematicAssignment>()
            .HasOne(assignment => assignment.Vehicle)
            .WithMany()
            .HasForeignKey(assignment => assignment.VehicleId)
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
            .HasOne(report => report.ChecklistTemplate)
            .WithMany()
            .HasForeignKey(report => report.ChecklistTemplateId)
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

        modelBuilder.Entity<DailyVehicleEquipmentCheck>()
            .HasOne(check => check.ChecklistItem)
            .WithMany()
            .HasForeignKey(check => check.ChecklistItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChecklistTemplate>()
            .HasIndex(template => new { template.CompanyId, template.ChecklistType, template.TargetVehicleType, template.Name });

        modelBuilder.Entity<ChecklistTemplate>()
            .HasOne(template => template.Company)
            .WithMany(company => company.ChecklistTemplates)
            .HasForeignKey(template => template.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChecklistTemplate>()
            .HasOne(template => template.ParentChecklistTemplate)
            .WithMany(template => template.ChildTemplates)
            .HasForeignKey(template => template.ParentChecklistTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChecklistTemplate>()
            .HasOne(template => template.CreatedByUser)
            .WithMany()
            .HasForeignKey(template => template.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChecklistTemplate>()
            .HasOne(template => template.PublishedByUser)
            .WithMany()
            .HasForeignKey(template => template.PublishedByUserId)
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

        modelBuilder.Entity<ChecklistItem>()
            .HasIndex(item => item.ParentChecklistItemId);

        modelBuilder.Entity<ChecklistItem>()
            .HasOne(item => item.ParentChecklistItem)
            .WithMany(item => item.SubItems)
            .HasForeignKey(item => item.ParentChecklistItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChecklistItem>()
            .HasOne(item => item.CatalogueItem)
            .WithMany()
            .HasForeignKey(item => item.CatalogueItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChecklistColumnDefinition>()
            .HasIndex(column => new { column.ChecklistItemId, column.SortOrder });

        modelBuilder.Entity<ChecklistColumnDefinition>()
            .HasOne(column => column.ChecklistItem)
            .WithMany(item => item.ColumnDefinitions)
            .HasForeignKey(column => column.ChecklistItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChecklistPublishScope>()
            .HasIndex(scope => new { scope.CompanyId, scope.ChecklistTemplateId, scope.ScopeType, scope.IsActive });

        modelBuilder.Entity<ChecklistPublishScope>()
            .HasOne(scope => scope.Company)
            .WithMany(company => company.ChecklistPublishScopes)
            .HasForeignKey(scope => scope.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChecklistPublishScope>()
            .HasOne(scope => scope.ChecklistTemplate)
            .WithMany(template => template.PublishScopes)
            .HasForeignKey(scope => scope.ChecklistTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChecklistPublishScope>()
            .HasOne(scope => scope.OperationalArea)
            .WithMany()
            .HasForeignKey(scope => scope.OperationalAreaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChecklistPublishScope>()
            .HasOne(scope => scope.Vehicle)
            .WithMany()
            .HasForeignKey(scope => scope.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChecklistPublishScope>()
            .HasOne(scope => scope.PublishedByUser)
            .WithMany()
            .HasForeignKey(scope => scope.PublishedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CatalogueItem>()
            .HasIndex(item => new { item.CompanyId, item.CatalogueType, item.Category, item.ItemName });

        modelBuilder.Entity<CatalogueItem>()
            .HasOne(item => item.Company)
            .WithMany(company => company.CatalogueItems)
            .HasForeignKey(item => item.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CustomDropdownOption>()
            .HasIndex(option => new { option.CompanyId, option.DropdownKey, option.Value });

        modelBuilder.Entity<CustomDropdownOption>()
            .HasOne(option => option.Company)
            .WithMany(company => company.CustomDropdownOptions)
            .HasForeignKey(option => option.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CustomDropdownOption>()
            .HasOne(option => option.CreatedByUser)
            .WithMany()
            .HasForeignKey(option => option.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChecklistVarianceAlert>()
            .HasIndex(alert => new { alert.CompanyId, alert.AssignedToUserId, alert.Status, alert.CreatedAtUtc });

        modelBuilder.Entity<ChecklistVarianceAlert>()
            .HasOne(alert => alert.Company)
            .WithMany(company => company.ChecklistVarianceAlerts)
            .HasForeignKey(alert => alert.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChecklistVarianceAlert>()
            .HasOne(alert => alert.DailyVehicleReadinessReport)
            .WithMany(report => report.VarianceAlerts)
            .HasForeignKey(alert => alert.DailyVehicleReadinessReportId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChecklistVarianceAlert>()
            .HasOne(alert => alert.DailyVehicleEquipmentCheck)
            .WithMany(check => check.VarianceAlerts)
            .HasForeignKey(alert => alert.DailyVehicleEquipmentCheckId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChecklistVarianceAlert>()
            .HasOne(alert => alert.Vehicle)
            .WithMany()
            .HasForeignKey(alert => alert.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChecklistVarianceAlert>()
            .HasOne(alert => alert.DetectedForUser)
            .WithMany(user => user.DetectedChecklistVarianceAlerts)
            .HasForeignKey(alert => alert.DetectedForUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChecklistVarianceAlert>()
            .HasOne(alert => alert.AssignedToUser)
            .WithMany(user => user.AssignedChecklistVarianceAlerts)
            .HasForeignKey(alert => alert.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChecklistVarianceAlert>()
            .HasOne(alert => alert.ReviewedByUser)
            .WithMany(user => user.ReviewedChecklistVarianceAlerts)
            .HasForeignKey(alert => alert.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReadinessAlert>()
            .HasIndex(alert => new { alert.CompanyId, alert.AssignedToUserId, alert.Status, alert.CreatedAtUtc });

        modelBuilder.Entity<ReadinessAlert>()
            .HasIndex(alert => new { alert.CompanyId, alert.Status, alert.CreatedAtUtc });

        modelBuilder.Entity<ReadinessAlert>()
            .HasOne(alert => alert.Company)
            .WithMany(company => company.ReadinessAlerts)
            .HasForeignKey(alert => alert.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReadinessAlert>()
            .HasOne(alert => alert.ReadinessEngineRule)
            .WithMany()
            .HasForeignKey(alert => alert.ReadinessEngineRuleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReadinessAlert>()
            .HasOne(alert => alert.DailyVehicleReadinessReport)
            .WithMany(report => report.ReadinessAlerts)
            .HasForeignKey(alert => alert.DailyVehicleReadinessReportId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReadinessAlert>()
            .HasOne(alert => alert.DailyVehicleEquipmentCheck)
            .WithMany(check => check.ReadinessAlerts)
            .HasForeignKey(alert => alert.DailyVehicleEquipmentCheckId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReadinessAlert>()
            .HasOne(alert => alert.Vehicle)
            .WithMany()
            .HasForeignKey(alert => alert.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReadinessAlert>()
            .HasOne(alert => alert.TriggeredByUser)
            .WithMany(user => user.TriggeredReadinessAlerts)
            .HasForeignKey(alert => alert.TriggeredByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReadinessAlert>()
            .HasOne(alert => alert.AssignedToUser)
            .WithMany(user => user.AssignedReadinessAlerts)
            .HasForeignKey(alert => alert.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReadinessAlert>()
            .HasOne(alert => alert.ReviewedByUser)
            .WithMany(user => user.ReviewedReadinessAlerts)
            .HasForeignKey(alert => alert.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReadinessEngineVersion>()
            .HasIndex(version => new { version.CompanyId, version.Status, version.CreatedAtUtc });

        modelBuilder.Entity<ReadinessEngineVersion>()
            .HasOne(version => version.Company)
            .WithMany(company => company.ReadinessEngineVersions)
            .HasForeignKey(version => version.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReadinessEngineVersion>()
            .HasOne(version => version.SourceReadinessEngineVersion)
            .WithMany()
            .HasForeignKey(version => version.SourceReadinessEngineVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReadinessEngineVersion>()
            .HasOne(version => version.CreatedByUser)
            .WithMany()
            .HasForeignKey(version => version.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReadinessEngineVersion>()
            .HasOne(version => version.PublishedByUser)
            .WithMany()
            .HasForeignKey(version => version.PublishedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReadinessEngineRule>()
            .HasIndex(rule => new { rule.CompanyId, rule.AssetType, rule.Section, rule.ItemName, rule.TriggerValue });

        modelBuilder.Entity<ReadinessEngineRule>()
            .HasIndex(rule => new { rule.ReadinessEngineVersionId, rule.SortOrder });

        modelBuilder.Entity<ReadinessEngineRule>()
            .Property(rule => rule.ReadinessScope)
            .HasMaxLength(80);

        modelBuilder.Entity<ReadinessEngineRule>()
            .HasOne(rule => rule.Company)
            .WithMany(company => company.ReadinessEngineRules)
            .HasForeignKey(rule => rule.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReadinessEngineRule>()
            .HasOne(rule => rule.ReadinessEngineVersion)
            .WithMany(version => version.Rules)
            .HasForeignKey(rule => rule.ReadinessEngineVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReadinessEngineRule>()
            .HasOne(rule => rule.OperationalArea)
            .WithMany()
            .HasForeignKey(rule => rule.OperationalAreaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReadinessEngineRule>()
            .HasOne(rule => rule.ChecklistTemplate)
            .WithMany()
            .HasForeignKey(rule => rule.ChecklistTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReadinessScoringChangeRequest>()
            .HasIndex(request => new { request.CompanyId, request.Status, request.CreatedAtUtc });

        modelBuilder.Entity<ReadinessScoringChangeRequest>()
            .HasOne(request => request.Company)
            .WithMany(company => company.ReadinessScoringChangeRequests)
            .HasForeignKey(request => request.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReadinessScoringChangeRequest>()
            .HasOne(request => request.RequestedByUser)
            .WithMany()
            .HasForeignKey(request => request.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReadinessScoringChangeRequest>()
            .HasOne(request => request.ReviewedByUser)
            .WithMany()
            .HasForeignKey(request => request.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReadinessScoringChangeRequest>()
            .HasOne(request => request.ReadinessEngineRule)
            .WithMany()
            .HasForeignKey(request => request.ReadinessEngineRuleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UploadedFile>()
            .HasIndex(file => new { file.CompanyId, file.ChecklistTemplateId, file.UploadedAtUtc });

        modelBuilder.Entity<UploadedFile>()
            .HasOne(file => file.Company)
            .WithMany(company => company.UploadedFiles)
            .HasForeignKey(file => file.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

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

    private void PrepareAuditLogs()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditLog>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
        {
            entry.Entity.Action = CleanAuditText(entry.Entity.Action, 120, "Unknown action");
            entry.Entity.EntityType = CleanAuditText(entry.Entity.EntityType, 120, "Unknown entity");
            entry.Entity.Details = CleanOptionalAuditText(entry.Entity.Details, 1200);

            if (entry.Entity.CreatedAtUtc == default)
            {
                entry.Entity.CreatedAtUtc = now;
            }
        }
    }

    private static string CleanAuditText(string? value, int maxLength, string fallback)
    {
        var cleaned = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return cleaned.Length <= maxLength ? cleaned : cleaned[..maxLength];
    }

    private static string? CleanOptionalAuditText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var cleaned = value.Trim();
        return cleaned.Length <= maxLength ? cleaned : cleaned[..maxLength];
    }
}
