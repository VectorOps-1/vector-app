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
    public DbSet<MedicationItem> MedicationItems => Set<MedicationItem>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<EquipmentItem> EquipmentItems => Set<EquipmentItem>();
    public DbSet<VehicleEquipmentAssignment> VehicleEquipmentAssignments => Set<VehicleEquipmentAssignment>();
    public DbSet<DailyVehicleReadinessReport> DailyVehicleReadinessReports => Set<DailyVehicleReadinessReport>();
    public DbSet<DailyVehicleEquipmentCheck> DailyVehicleEquipmentChecks => Set<DailyVehicleEquipmentCheck>();
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

        modelBuilder.Entity<Vehicle>()
            .HasIndex(vehicle => new { vehicle.CompanyId, vehicle.RegistrationNumber })
            .IsUnique();

        modelBuilder.Entity<Vehicle>()
            .HasOne(vehicle => vehicle.Company)
            .WithMany(company => company.Vehicles)
            .HasForeignKey(vehicle => vehicle.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EquipmentItem>()
            .HasIndex(equipment => new { equipment.CompanyId, equipment.SerialOrAssetId });

        modelBuilder.Entity<EquipmentItem>()
            .HasOne(equipment => equipment.Company)
            .WithMany(company => company.EquipmentItems)
            .HasForeignKey(equipment => equipment.CompanyId)
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
