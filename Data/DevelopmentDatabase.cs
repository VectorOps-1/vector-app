using System.Data;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Models;

namespace vector_app_local.Data;

public static class DevelopmentDatabase
{
    private const string EfProductVersion = "8.0.5";

    public static async Task InitialiseAsync(VectorDbContext db)
    {
        if (db.Database.IsSqlite())
        {
            await InitialiseSqliteAsync(db);
        }
        else
        {
            await db.Database.MigrateAsync();
        }

        await SeedPrototypeDataAsync(db);
    }

    public static async Task RepairSqliteDevelopmentSchemaAsync(VectorDbContext db)
    {
        if (db.Database.IsSqlite())
        {
            await EnsureSqliteDevelopmentSchemaAsync(db);
        }
    }

    private static async Task InitialiseSqliteAsync(VectorDbContext db)
    {
        if (await HasSqliteTableAsync(db, "Companies"))
        {
            if (await HasSqliteTableAsync(db, "__EFMigrationsHistory"))
            {
                await db.Database.MigrateAsync();
            }

            await EnsureSqliteDevelopmentSchemaAsync(db);
            return;
        }

        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        await MarkCurrentMigrationsAppliedAsync(db);
    }

    private static async Task<bool> HasSqliteTableAsync(VectorDbContext db, string tableName)
    {
        var connection = db.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;
        if (shouldCloseConnection)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "$name";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<bool> HasSqliteColumnAsync(VectorDbContext db, string tableName, string columnName)
    {
        var connection = db.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;
        if (shouldCloseConnection)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"""PRAGMA table_info("{tableName}");""";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (string.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task EnsureSqliteDevelopmentSchemaAsync(VectorDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "MedicationItems" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_MedicationItems" PRIMARY KEY AUTOINCREMENT,
                "CompanyId" INTEGER NOT NULL,
                "CreatedByUserId" INTEGER NOT NULL,
                "LastAllocatedByUserId" INTEGER NULL,
                "Name" TEXT NOT NULL,
                "MedicationCode" TEXT NULL,
                "MedicationType" TEXT NULL,
                "Schedule" TEXT NULL,
                "BatchNumber" TEXT NULL,
                "StorageLocation" TEXT NULL,
                "CurrentOperationalAreaId" INTEGER NULL,
                "Status" TEXT NOT NULL,
                "Quantity" INTEGER NULL,
                "ExpiryDate" TEXT NULL,
                "LastAllocationLocation" TEXT NULL,
                "LastAllocatedAtUtc" TEXT NULL,
                "Notes" TEXT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                "UpdatedAtUtc" TEXT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "StockItems" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_StockItems" PRIMARY KEY AUTOINCREMENT,
                "CompanyId" INTEGER NOT NULL,
                "CreatedByUserId" INTEGER NOT NULL,
                "LastMovedByUserId" INTEGER NULL,
                "ItemName" TEXT NOT NULL,
                "ItemType" TEXT NULL,
                "BatchNumber" TEXT NULL,
                "Quantity" INTEGER NOT NULL,
                "Location" TEXT NULL,
                "CurrentOperationalAreaId" INTEGER NULL,
                "Status" TEXT NOT NULL,
                "LastMovementType" TEXT NULL,
                "LastMovementAtUtc" TEXT NULL,
                "Notes" TEXT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                "UpdatedAtUtc" TEXT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "StockOrders" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_StockOrders" PRIMARY KEY AUTOINCREMENT,
                "CompanyId" INTEGER NOT NULL,
                "RequestedByUserId" INTEGER NOT NULL,
                "ApprovedBySeniorUserId" INTEGER NULL,
                "RegisterEntryAuthorisedUserId" INTEGER NULL,
                "SupplierName" TEXT NOT NULL,
                "SupplierEmail" TEXT NOT NULL,
                "DeliveryAddress" TEXT NULL,
                "DeliveryInstructions" TEXT NULL,
                "OrderNotes" TEXT NULL,
                "Status" TEXT NOT NULL,
                "EmailSubject" TEXT NOT NULL,
                "EmailBody" TEXT NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                "ApprovedAtUtc" TEXT NULL,
                "EmailSentAtUtc" TEXT NULL,
                "SupplierConfirmedAtUtc" TEXT NULL,
                "RegisterEnteredAtUtc" TEXT NULL,
                "AllocatedAtUtc" TEXT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "StockOrderLines" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_StockOrderLines" PRIMARY KEY AUTOINCREMENT,
                "StockOrderId" INTEGER NOT NULL,
                "ItemName" TEXT NOT NULL,
                "ItemType" TEXT NULL,
                "QuantityRequested" INTEGER NOT NULL,
                "QuantityConfirmed" INTEGER NULL,
                "BatchNumber" TEXT NULL,
                "ExpiryDate" TEXT NULL,
                "RegisterLocation" TEXT NULL,
                "QuantityAllocated" INTEGER NULL,
                "AllocationLocation" TEXT NULL,
                "Notes" TEXT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "OperationalAreas" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_OperationalAreas" PRIMARY KEY AUTOINCREMENT,
                "CompanyId" INTEGER NOT NULL,
                "Name" TEXT NOT NULL,
                "AreaType" TEXT NOT NULL,
                "Address" TEXT NULL,
                "Status" TEXT NOT NULL,
                "Notes" TEXT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                "UpdatedAtUtc" TEXT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ChecklistTemplates" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ChecklistTemplates" PRIMARY KEY AUTOINCREMENT,
                "CompanyId" INTEGER NOT NULL DEFAULT 1,
                "ClientName" TEXT NOT NULL,
                "Name" TEXT NOT NULL,
                "ChecklistType" TEXT NOT NULL DEFAULT 'Vehicle',
                "TargetVehicleType" TEXT NOT NULL DEFAULT 'All Vehicles',
                "Version" TEXT NOT NULL,
                "Status" TEXT NOT NULL DEFAULT 'Draft',
                "IsPublished" INTEGER NOT NULL DEFAULT 0,
                "PublishedAtUtc" TEXT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                "UpdatedAtUtc" TEXT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ChecklistSections" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ChecklistSections" PRIMARY KEY AUTOINCREMENT,
                "ChecklistTemplateId" INTEGER NOT NULL,
                "Name" TEXT NOT NULL,
                "DisplayOrder" INTEGER NOT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ChecklistItems" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ChecklistItems" PRIMARY KEY AUTOINCREMENT,
                "ChecklistSectionId" INTEGER NOT NULL,
                "Prompt" TEXT NOT NULL,
                "ResponseType" TEXT NOT NULL,
                "RequiresCommentOnFail" INTEGER NOT NULL,
                "DisplayOrder" INTEGER NOT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "AssetMovements" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_AssetMovements" PRIMARY KEY AUTOINCREMENT,
                "CompanyId" INTEGER NOT NULL,
                "AssetType" TEXT NOT NULL,
                "AssetId" INTEGER NOT NULL,
                "AssetLabel" TEXT NOT NULL,
                "FromOperationalAreaId" INTEGER NULL,
                "ToOperationalAreaId" INTEGER NOT NULL,
                "FromLocationText" TEXT NULL,
                "ToLocationText" TEXT NULL,
                "QuantityMoved" INTEGER NULL,
                "MovementReason" TEXT NULL,
                "MovedByUserId" INTEGER NOT NULL,
                "TaskItemId" INTEGER NULL,
                "CreatedAtUtc" TEXT NOT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ManagerOperationalAreaAssignments" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ManagerOperationalAreaAssignments" PRIMARY KEY AUTOINCREMENT,
                "CompanyId" INTEGER NOT NULL,
                "ManagerUserId" INTEGER NOT NULL,
                "OperationalAreaId" INTEGER NOT NULL,
                "AssignedByUserId" INTEGER NULL,
                "Status" TEXT NOT NULL,
                "AssignedAtUtc" TEXT NOT NULL,
                "UpdatedAtUtc" TEXT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "AssetFiles" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_AssetFiles" PRIMARY KEY AUTOINCREMENT,
                "CompanyId" INTEGER NOT NULL,
                "UploadedByUserId" INTEGER NOT NULL,
                "LinkedEntityType" TEXT NOT NULL,
                "LinkedEntityId" INTEGER NOT NULL,
                "Category" TEXT NOT NULL,
                "OriginalFileName" TEXT NOT NULL,
                "ContentType" TEXT NOT NULL,
                "StorageProvider" TEXT NOT NULL,
                "StoragePath" TEXT NOT NULL,
                "SizeBytes" INTEGER NOT NULL,
                "Notes" TEXT NULL,
                "UploadedAtUtc" TEXT NOT NULL
            );
            """);

        await EnsureSqliteColumnAsync(db, "Vehicles", "CurrentOperationalAreaId", """ALTER TABLE "Vehicles" ADD "CurrentOperationalAreaId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "Vehicles", "CurrentLocationDetail", """ALTER TABLE "Vehicles" ADD "CurrentLocationDetail" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "Vehicles", "LastMovedByUserId", """ALTER TABLE "Vehicles" ADD "LastMovedByUserId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "Vehicles", "LastMovedAtUtc", """ALTER TABLE "Vehicles" ADD "LastMovedAtUtc" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "EquipmentItems", "CurrentOperationalAreaId", """ALTER TABLE "EquipmentItems" ADD "CurrentOperationalAreaId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "EquipmentItems", "CurrentLocationDetail", """ALTER TABLE "EquipmentItems" ADD "CurrentLocationDetail" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "EquipmentItems", "LastMovedByUserId", """ALTER TABLE "EquipmentItems" ADD "LastMovedByUserId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "EquipmentItems", "LastMovedAtUtc", """ALTER TABLE "EquipmentItems" ADD "LastMovedAtUtc" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "Companies", "AllowSameAsPreviousVehicleInspection", """ALTER TABLE "Companies" ADD "AllowSameAsPreviousVehicleInspection" INTEGER NOT NULL DEFAULT 1;""");
        await EnsureSqliteColumnAsync(db, "Companies", "AllowSameAsPreviousEquipmentCheck", """ALTER TABLE "Companies" ADD "AllowSameAsPreviousEquipmentCheck" INTEGER NOT NULL DEFAULT 1;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleReadinessReports", "ShiftStartedAtUtc", """ALTER TABLE "DailyVehicleReadinessReports" ADD "ShiftStartedAtUtc" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleReadinessReports", "ShiftEndsAtUtc", """ALTER TABLE "DailyVehicleReadinessReports" ADD "ShiftEndsAtUtc" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleReadinessReports", "DraftExpiresAtUtc", """ALTER TABLE "DailyVehicleReadinessReports" ADD "DraftExpiresAtUtc" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleReadinessReports", "LastSavedAtUtc", """ALTER TABLE "DailyVehicleReadinessReports" ADD "LastSavedAtUtc" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleReadinessReports", "WorkflowStatus", """ALTER TABLE "DailyVehicleReadinessReports" ADD "WorkflowStatus" TEXT NOT NULL DEFAULT 'Draft';""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleReadinessReports", "LastSavedSection", """ALTER TABLE "DailyVehicleReadinessReports" ADD "LastSavedSection" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleReadinessReports", "VehicleSameAsPreviousShiftUsed", """ALTER TABLE "DailyVehicleReadinessReports" ADD "VehicleSameAsPreviousShiftUsed" INTEGER NOT NULL DEFAULT 0;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleReadinessReports", "EquipmentSameAsPreviousShiftUsed", """ALTER TABLE "DailyVehicleReadinessReports" ADD "EquipmentSameAsPreviousShiftUsed" INTEGER NOT NULL DEFAULT 0;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleReadinessReports", "VehicleSameAsPreviousSourceReportId", """ALTER TABLE "DailyVehicleReadinessReports" ADD "VehicleSameAsPreviousSourceReportId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleReadinessReports", "EquipmentSameAsPreviousSourceReportId", """ALTER TABLE "DailyVehicleReadinessReports" ADD "EquipmentSameAsPreviousSourceReportId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleReadinessReports", "VehicleSameAsPreviousAppliedAtUtc", """ALTER TABLE "DailyVehicleReadinessReports" ADD "VehicleSameAsPreviousAppliedAtUtc" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleReadinessReports", "EquipmentSameAsPreviousAppliedAtUtc", """ALTER TABLE "DailyVehicleReadinessReports" ADD "EquipmentSameAsPreviousAppliedAtUtc" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleReadinessReports", "VehicleSameAsPreviousCopiedSummary", """ALTER TABLE "DailyVehicleReadinessReports" ADD "VehicleSameAsPreviousCopiedSummary" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleReadinessReports", "EquipmentSameAsPreviousCopiedSummary", """ALTER TABLE "DailyVehicleReadinessReports" ADD "EquipmentSameAsPreviousCopiedSummary" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleEquipmentChecks", "SameAsPreviousShiftUsed", """ALTER TABLE "DailyVehicleEquipmentChecks" ADD "SameAsPreviousShiftUsed" INTEGER NOT NULL DEFAULT 0;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleEquipmentChecks", "CopiedFromDailyVehicleEquipmentCheckId", """ALTER TABLE "DailyVehicleEquipmentChecks" ADD "CopiedFromDailyVehicleEquipmentCheckId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleEquipmentChecks", "SameAsPreviousAppliedAtUtc", """ALTER TABLE "DailyVehicleEquipmentChecks" ADD "SameAsPreviousAppliedAtUtc" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleEquipmentChecks", "IsOperational", """ALTER TABLE "DailyVehicleEquipmentChecks" ADD "IsOperational" INTEGER NOT NULL DEFAULT 1;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleEquipmentChecks", "IssueNotes", """ALTER TABLE "DailyVehicleEquipmentChecks" ADD "IssueNotes" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "ChecklistTemplates", "CompanyId", """ALTER TABLE "ChecklistTemplates" ADD "CompanyId" INTEGER NOT NULL DEFAULT 1;""");
        await EnsureSqliteColumnAsync(db, "ChecklistTemplates", "ChecklistType", """ALTER TABLE "ChecklistTemplates" ADD "ChecklistType" TEXT NOT NULL DEFAULT 'Vehicle';""");
        await EnsureSqliteColumnAsync(db, "ChecklistTemplates", "TargetVehicleType", """ALTER TABLE "ChecklistTemplates" ADD "TargetVehicleType" TEXT NOT NULL DEFAULT 'All Vehicles';""");
        await EnsureSqliteColumnAsync(db, "ChecklistTemplates", "Status", """ALTER TABLE "ChecklistTemplates" ADD "Status" TEXT NOT NULL DEFAULT 'Draft';""");
        await EnsureSqliteColumnAsync(db, "ChecklistTemplates", "IsPublished", """ALTER TABLE "ChecklistTemplates" ADD "IsPublished" INTEGER NOT NULL DEFAULT 0;""");
        await EnsureSqliteColumnAsync(db, "ChecklistTemplates", "PublishedAtUtc", """ALTER TABLE "ChecklistTemplates" ADD "PublishedAtUtc" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "ChecklistTemplates", "UpdatedAtUtc", """ALTER TABLE "ChecklistTemplates" ADD "UpdatedAtUtc" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "MedicationItems", "LastAllocatedByUserId", """ALTER TABLE "MedicationItems" ADD "LastAllocatedByUserId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "MedicationItems", "LastAllocatedAtUtc", """ALTER TABLE "MedicationItems" ADD "LastAllocatedAtUtc" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "MedicationItems", "LastAllocationLocation", """ALTER TABLE "MedicationItems" ADD "LastAllocationLocation" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "MedicationItems", "Schedule", """ALTER TABLE "MedicationItems" ADD "Schedule" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "StockItems", "CurrentOperationalAreaId", """ALTER TABLE "StockItems" ADD "CurrentOperationalAreaId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "MedicationItems", "CurrentOperationalAreaId", """ALTER TABLE "MedicationItems" ADD "CurrentOperationalAreaId" INTEGER NULL;""");

        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_MedicationItems_CompanyId" ON "MedicationItems" ("CompanyId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_MedicationItems_CreatedByUserId" ON "MedicationItems" ("CreatedByUserId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_MedicationItems_LastAllocatedByUserId" ON "MedicationItems" ("LastAllocatedByUserId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_MedicationItems_CurrentOperationalAreaId" ON "MedicationItems" ("CurrentOperationalAreaId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_StockItems_CompanyId_ItemName_BatchNumber_Location" ON "StockItems" ("CompanyId", "ItemName", "BatchNumber", "Location");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_StockItems_CreatedByUserId" ON "StockItems" ("CreatedByUserId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_StockItems_LastMovedByUserId" ON "StockItems" ("LastMovedByUserId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_StockItems_CurrentOperationalAreaId" ON "StockItems" ("CurrentOperationalAreaId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_StockOrderLines_StockOrderId" ON "StockOrderLines" ("StockOrderId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_StockOrders_ApprovedBySeniorUserId" ON "StockOrders" ("ApprovedBySeniorUserId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_StockOrders_CompanyId_Status_CreatedAtUtc" ON "StockOrders" ("CompanyId", "Status", "CreatedAtUtc");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_StockOrders_RegisterEntryAuthorisedUserId" ON "StockOrders" ("RegisterEntryAuthorisedUserId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_StockOrders_RequestedByUserId" ON "StockOrders" ("RequestedByUserId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_DailyVehicleReadinessReports_CompanyId_WorkflowStatus_DraftExpiresAtUtc" ON "DailyVehicleReadinessReports" ("CompanyId", "WorkflowStatus", "DraftExpiresAtUtc");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_DailyVehicleReadinessReports_VehicleSameAsPreviousSourceReportId" ON "DailyVehicleReadinessReports" ("VehicleSameAsPreviousSourceReportId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_DailyVehicleReadinessReports_EquipmentSameAsPreviousSourceReportId" ON "DailyVehicleReadinessReports" ("EquipmentSameAsPreviousSourceReportId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_DailyVehicleEquipmentChecks_CopiedFromDailyVehicleEquipmentCheckId" ON "DailyVehicleEquipmentChecks" ("CopiedFromDailyVehicleEquipmentCheckId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_ChecklistTemplates_CompanyId_ChecklistType_TargetVehicleType_Name" ON "ChecklistTemplates" ("CompanyId", "ChecklistType", "TargetVehicleType", "Name");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_OperationalAreas_CompanyId_Name" ON "OperationalAreas" ("CompanyId", "Name");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_ManagerOperationalAreaAssignments_CompanyId_ManagerUserId_OperationalAreaId" ON "ManagerOperationalAreaAssignments" ("CompanyId", "ManagerUserId", "OperationalAreaId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_ManagerOperationalAreaAssignments_ManagerUserId" ON "ManagerOperationalAreaAssignments" ("ManagerUserId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_ManagerOperationalAreaAssignments_OperationalAreaId" ON "ManagerOperationalAreaAssignments" ("OperationalAreaId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_ManagerOperationalAreaAssignments_AssignedByUserId" ON "ManagerOperationalAreaAssignments" ("AssignedByUserId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_AssetMovements_CompanyId_AssetType_AssetId_CreatedAtUtc" ON "AssetMovements" ("CompanyId", "AssetType", "AssetId", "CreatedAtUtc");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_AssetFiles_CompanyId_LinkedEntityType_LinkedEntityId_Category" ON "AssetFiles" ("CompanyId", "LinkedEntityType", "LinkedEntityId", "Category");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_AssetFiles_UploadedByUserId" ON "AssetFiles" ("UploadedByUserId");""");
    }

    private static async Task EnsureSqliteColumnAsync(VectorDbContext db, string tableName, string columnName, string alterSql)
    {
        if (!await HasSqliteTableAsync(db, tableName) || await HasSqliteColumnAsync(db, tableName, columnName))
        {
            return;
        }

        await db.Database.ExecuteSqlRawAsync(alterSql);
    }

    private static async Task MarkCurrentMigrationsAppliedAsync(VectorDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );
            """);

        foreach (var migrationId in db.Database.GetMigrations())
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                VALUES ({0}, {1});
                """,
                migrationId,
                EfProductVersion);
        }
    }

    private static async Task SeedPrototypeDataAsync(VectorDbContext db)
    {
        var company = await db.Companies.FirstOrDefaultAsync(company => company.Id == 1);
        if (company is null)
        {
            company = new Company
            {
                Id = 1,
                Name = "Client Business Name",
                Status = "Active",
                SubscriptionTier = SubscriptionTiers.Base,
                AllowSameAsPreviousVehicleInspection = true,
                AllowSameAsPreviousEquipmentCheck = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.Companies.Add(company);
        }

        var staffRole = await EnsureRoleAsync(db, 1, "Staff");
        var opsRole = await EnsureRoleAsync(db, 2, "Operational Management");
        var seniorRole = await EnsureRoleAsync(db, 3, "Senior Management");
        var ownerRole = await EnsureRoleAsync(db, 4, "Company Owner");

        await db.SaveChangesAsync();

        await EnsureUserAsync(db, 1, company.Id, staffRole.Id, "Test Staff User", "staff@test.local");
        await EnsureUserAsync(db, 2, company.Id, opsRole.Id, "Test Operational Manager", "ops@test.local");
        await EnsureUserAsync(db, 3, company.Id, seniorRole.Id, "Test Senior Manager", "senior@test.local");
        await EnsureUserAsync(db, 4, company.Id, ownerRole.Id, "Test Company Owner", "owner@test.local");
        await EnsureOperationalAreaAsync(db, company.Id, "Main Base", "Base");
        await EnsureOperationalAreaAsync(db, company.Id, "Secondary Base", "Base");
        await EnsureOperationalAreaAsync(db, company.Id, "Main Store", "Store");
        await db.SaveChangesAsync();
        await EnsurePrototypeManagerAreaAssignmentAsync(db, company.Id, "ops@test.local", "Main Base");
        await EnsurePrototypeVehicleEquipmentAsync(db, company.Id);
        await EnsurePrototypeVehicleChecklistTemplatesAsync(db, company);

        await db.SaveChangesAsync();
    }

    private static async Task<AppRole> EnsureRoleAsync(VectorDbContext db, int id, string name)
    {
        var role = await db.AppRoles.FirstOrDefaultAsync(role => role.Name == name);
        if (role is not null)
        {
            return role;
        }

        role = new AppRole
        {
            Id = id,
            Name = name
        };
        db.AppRoles.Add(role);
        return role;
    }

    private static async Task EnsureUserAsync(VectorDbContext db, int id, int companyId, int roleId, string fullName, string email)
    {
        if (await db.AppUsers.AnyAsync(user => user.Email == email))
        {
            return;
        }

        db.AppUsers.Add(new AppUser
        {
            Id = id,
            CompanyId = companyId,
            AppRoleId = roleId,
            FullName = fullName,
            Email = email,
            Status = "Active",
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    private static async Task EnsureOperationalAreaAsync(VectorDbContext db, int companyId, string name, string areaType)
    {
        if (await db.OperationalAreas.AnyAsync(area => area.CompanyId == companyId && area.Name == name))
        {
            return;
        }

        db.OperationalAreas.Add(new OperationalArea
        {
            CompanyId = companyId,
            Name = name,
            AreaType = areaType,
            Status = "Active",
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    private static async Task EnsurePrototypeManagerAreaAssignmentAsync(
        VectorDbContext db,
        int companyId,
        string managerEmail,
        string areaName)
    {
        var manager = await db.AppUsers.FirstOrDefaultAsync(user =>
            user.CompanyId == companyId &&
            user.Email == managerEmail);
        var area = await db.OperationalAreas.FirstOrDefaultAsync(item =>
            item.CompanyId == companyId &&
            item.Name == areaName);

        if (manager is null || area is null)
        {
            return;
        }

        var assignment = await db.ManagerOperationalAreaAssignments.FirstOrDefaultAsync(item =>
            item.CompanyId == companyId &&
            item.ManagerUserId == manager.Id &&
            item.OperationalAreaId == area.Id);

        if (assignment is not null)
        {
            assignment.Status = "Active";
            assignment.UpdatedAtUtc = DateTime.UtcNow;
            return;
        }

        db.ManagerOperationalAreaAssignments.Add(new ManagerOperationalAreaAssignment
        {
            CompanyId = companyId,
            ManagerUserId = manager.Id,
            OperationalAreaId = area.Id,
            Status = "Active",
            AssignedAtUtc = DateTime.UtcNow
        });
    }

    private static async Task EnsurePrototypeVehicleChecklistTemplatesAsync(VectorDbContext db, Company company)
    {
        var templates = new[]
        {
            ("Daily Vehicle Readiness", "Ambulance"),
            ("Daily Vehicle Readiness", "ICU Ambulance"),
            ("Daily Vehicle Readiness", "Response Vehicle"),
            ("Daily Vehicle Readiness", "Rescue Vehicle")
        };

        foreach (var (name, targetVehicleType) in templates)
        {
            await EnsureVehicleChecklistTemplateAsync(db, company, name, targetVehicleType);
        }
    }

    private static async Task EnsureVehicleChecklistTemplateAsync(
        VectorDbContext db,
        Company company,
        string name,
        string targetVehicleType)
    {
        var template = await db.ChecklistTemplates.FirstOrDefaultAsync(item =>
                item.CompanyId == company.Id &&
                item.ChecklistType == "Vehicle" &&
                item.Name == name &&
                item.TargetVehicleType == targetVehicleType);

        if (template is null)
        {
            template = new ChecklistTemplate
            {
                CompanyId = company.Id,
                ClientName = company.Name,
                Name = name,
                ChecklistType = "Vehicle",
                TargetVehicleType = targetVehicleType,
                Version = "1.0",
                Status = "Published",
                IsPublished = true,
                CreatedAtUtc = DateTime.UtcNow,
                PublishedAtUtc = DateTime.UtcNow
            };

            db.ChecklistTemplates.Add(template);
            await db.SaveChangesAsync();
        }

        if (await db.ChecklistSections.AnyAsync(section => section.ChecklistTemplateId == template.Id))
        {
            return;
        }

        var sections = new[]
        {
            ("Vehicle Details", 10, new[] { "Registration number", "Vehicle / callsign", "Vehicle type", "Next service date" }),
            ("Same as previous shift", 20, new[] { "Allow vehicle inspection reuse", "Allow equipment check reuse" }),
            ("Operational Checks", 30, new[] { "Current kilometres", "Fuel level", "Vehicle condition", "Lights", "Sirens", "Warning lights", "Tyres", "Ops radio connectivity" }),
            ("Vehicle Schematic", 40, new[] { "Vehicle schematic", "Damage type", "Damage severity", "Damage notes" }),
            ("Carried Equipment", 50, new[] { "Name/item", "S/N / ID", "Next Service", "Battery", "Operational?", "Issues / errors" }),
            ("Notes / Issue", 60, new[] { "Inspection notes" })
        };

        foreach (var (sectionName, sectionOrder, prompts) in sections)
        {
            var section = new ChecklistSection
            {
                ChecklistTemplateId = template.Id,
                Name = sectionName,
                DisplayOrder = sectionOrder
            };

            for (var index = 0; index < prompts.Length; index++)
            {
                section.Items.Add(new ChecklistItem
                {
                    Prompt = prompts[index],
                    ResponseType = sectionName == "Carried Equipment" ? "EquipmentColumn" : "ChecklistField",
                    RequiresCommentOnFail = sectionName is "Operational Checks" or "Vehicle Schematic",
                    DisplayOrder = index + 1
                });
            }

            db.ChecklistSections.Add(section);
        }
    }

    private static async Task EnsurePrototypeVehicleEquipmentAsync(VectorDbContext db, int companyId)
    {
        var vehicle = await EnsureVehicleAsync(
            db,
            companyId,
            "AMB-101",
            "Medic 1",
            "Operational Ambulance",
            "ALS",
            "Operational Ambulance",
            DateTime.UtcNow.AddMonths(2));

        var mainBase = await db.OperationalAreas.FirstOrDefaultAsync(area =>
            area.CompanyId == companyId &&
            area.Name == "Main Base");
        if (mainBase is not null && vehicle.CurrentOperationalAreaId is null)
        {
            vehicle.CurrentOperationalAreaId = mainBase.Id;
            vehicle.CurrentLocationDetail = "Daily operations";
            vehicle.UpdatedAtUtc = DateTime.UtcNow;
        }

        var equipmentRows = new[]
        {
            new PrototypeEquipmentRow("LP15", "Monitor/Defibrillator", "LP15", "LP15-TEST-001", DateTime.UtcNow.AddMonths(5), true, 10),
            new PrototypeEquipmentRow("Syringe driver 1", "Infusion Pump", "Syringe Driver", "SYR-001", DateTime.UtcNow.AddMonths(4), true, 20),
            new PrototypeEquipmentRow("Syringe driver 2", "Infusion Pump", "Syringe Driver", "SYR-002", DateTime.UtcNow.AddMonths(4), true, 30),
            new PrototypeEquipmentRow("Ventilator Oxylog", "Ventilator", "Oxylog", "OXY-001", DateTime.UtcNow.AddMonths(3), true, 40),
            new PrototypeEquipmentRow("LUCAS", "Mechanical CPR", "LUCAS", "LUCAS-001", DateTime.UtcNow.AddMonths(6), true, 50)
        };

        foreach (var row in equipmentRows)
        {
            var item = await EnsureEquipmentItemAsync(db, companyId, row);
            await EnsureVehicleEquipmentAssignmentAsync(db, companyId, vehicle.Id, item.Id, row);
        }
    }

    private static async Task<Vehicle> EnsureVehicleAsync(
        VectorDbContext db,
        int companyId,
        string registrationNumber,
        string callsign,
        string vehicleType,
        string qualificationLevel,
        string schematicType,
        DateTime nextServiceDate)
    {
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(item =>
            item.CompanyId == companyId &&
            item.RegistrationNumber == registrationNumber);

        if (vehicle is not null)
        {
            vehicle.Callsign = string.IsNullOrWhiteSpace(vehicle.Callsign) ? callsign : vehicle.Callsign;
            vehicle.VehicleType = string.IsNullOrWhiteSpace(vehicle.VehicleType) ? vehicleType : vehicle.VehicleType;
            vehicle.QualificationLevel ??= qualificationLevel;
            vehicle.SchematicType ??= schematicType;
            vehicle.NextServiceDate ??= nextServiceDate;
            return vehicle;
        }

        vehicle = new Vehicle
        {
            CompanyId = companyId,
            RegistrationNumber = registrationNumber,
            Callsign = callsign,
            VehicleType = vehicleType,
            QualificationLevel = qualificationLevel,
            SchematicType = schematicType,
            NextServiceDate = nextServiceDate,
            Status = "Active",
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        return vehicle;
    }

    private static async Task<EquipmentItem> EnsureEquipmentItemAsync(VectorDbContext db, int companyId, PrototypeEquipmentRow row)
    {
        var item = await db.EquipmentItems.FirstOrDefaultAsync(equipment =>
            equipment.CompanyId == companyId &&
            equipment.SerialOrAssetId == row.SerialOrAssetId);

        if (item is not null)
        {
            item.Name = string.IsNullOrWhiteSpace(item.Name) ? row.Name : item.Name;
            item.EquipmentType ??= row.EquipmentType;
            item.Model ??= row.Model;
            item.NextServiceDate ??= row.NextServiceDate;
            item.BatteryRequired = row.BatteryRequired;
            item.Status = string.IsNullOrWhiteSpace(item.Status) ? "Active" : item.Status;
            return item;
        }

        item = new EquipmentItem
        {
            CompanyId = companyId,
            Name = row.Name,
            EquipmentType = row.EquipmentType,
            Model = row.Model,
            SerialOrAssetId = row.SerialOrAssetId,
            NextServiceDate = row.NextServiceDate,
            BatteryRequired = row.BatteryRequired,
            Status = "Active",
            CreatedAtUtc = DateTime.UtcNow
        };

        db.EquipmentItems.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    private static async Task EnsureVehicleEquipmentAssignmentAsync(
        VectorDbContext db,
        int companyId,
        int vehicleId,
        int equipmentItemId,
        PrototypeEquipmentRow row)
    {
        if (await db.VehicleEquipmentAssignments.AnyAsync(assignment =>
            assignment.CompanyId == companyId &&
            assignment.VehicleId == vehicleId &&
            assignment.EquipmentItemId == equipmentItemId))
        {
            return;
        }

        db.VehicleEquipmentAssignments.Add(new VehicleEquipmentAssignment
        {
            CompanyId = companyId,
            VehicleId = vehicleId,
            EquipmentItemId = equipmentItemId,
            ExpectedEquipmentName = row.Name,
            ExpectedEquipmentType = row.EquipmentType,
            ExpectedModel = row.Model,
            ExpectedQuantity = 1,
            RequiredForReadiness = true,
            RequiresBatteryCheck = row.BatteryRequired,
            SortOrder = row.SortOrder,
            Status = "Active",
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    private record PrototypeEquipmentRow(
        string Name,
        string EquipmentType,
        string Model,
        string SerialOrAssetId,
        DateTime NextServiceDate,
        bool BatteryRequired,
        int SortOrder);
}
