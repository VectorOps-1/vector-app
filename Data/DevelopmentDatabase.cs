using System.Data;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Models;
using vector_app_local.Services;

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

        await BackfillVehicleTaxonomyAsync(db);
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
                "StockCategory" TEXT NULL,
                "BatchNumber" TEXT NULL,
                "Quantity" INTEGER NOT NULL,
                "MinimumQuantity" INTEGER NULL,
                "Unit" TEXT NULL,
                "ExpiryDate" TEXT NULL,
                "IsReadinessCritical" INTEGER NOT NULL DEFAULT 0,
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
                "SourceType" TEXT NOT NULL DEFAULT 'Built',
                "ParentChecklistTemplateId" INTEGER NULL,
                "CreatedByUserId" INTEGER NULL,
                "PublishedByUserId" INTEGER NULL,
                "IsPublished" INTEGER NOT NULL DEFAULT 0,
                "PublishedAtUtc" TEXT NULL,
                "PublishScopeSummary" TEXT NULL,
                "PublishNotes" TEXT NULL,
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
                "ParentChecklistItemId" INTEGER NULL,
                "Prompt" TEXT NOT NULL,
                "ResponseType" TEXT NOT NULL,
                "ItemKind" TEXT NOT NULL DEFAULT 'Field',
                "CatalogueItemId" INTEGER NULL,
                "EquipmentType" TEXT NULL,
                "Model" TEXT NULL,
                "FieldKey" TEXT NULL,
                "RequiresCommentOnFail" INTEGER NOT NULL,
                "IsRequired" INTEGER NOT NULL DEFAULT 1,
                "IsReadinessCritical" INTEGER NOT NULL DEFAULT 0,
                "AllowsSameAsPrevious" INTEGER NOT NULL DEFAULT 1,
                "DefaultLocation" TEXT NULL,
                "DisplayOrder" INTEGER NOT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "CatalogueItems" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_CatalogueItems" PRIMARY KEY AUTOINCREMENT,
                "CompanyId" INTEGER NOT NULL,
                "CatalogueType" TEXT NOT NULL,
                "Category" TEXT NOT NULL,
                "Subcategory" TEXT NULL,
                "ItemName" TEXT NOT NULL,
                "Variant" TEXT NULL,
                "Manufacturer" TEXT NULL,
                "Model" TEXT NULL,
                "Size" TEXT NULL,
                "Unit" TEXT NULL,
                "ServiceRequired" INTEGER NOT NULL DEFAULT 0,
                "BatteryRequired" INTEGER NOT NULL DEFAULT 0,
                "SerialRequired" INTEGER NOT NULL DEFAULT 0,
                "BatchRequired" INTEGER NOT NULL DEFAULT 0,
                "ExpiryRequired" INTEGER NOT NULL DEFAULT 0,
                "ReadinessCritical" INTEGER NOT NULL DEFAULT 0,
                "DefaultChecklistColumns" TEXT NULL,
                "Notes" TEXT NULL,
                "Status" TEXT NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                "UpdatedAtUtc" TEXT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ChecklistColumnDefinitions" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ChecklistColumnDefinitions" PRIMARY KEY AUTOINCREMENT,
                "ChecklistItemId" INTEGER NOT NULL,
                "Heading" TEXT NOT NULL,
                "FieldKey" TEXT NOT NULL,
                "ResponseType" TEXT NOT NULL,
                "RegisterSource" TEXT NULL,
                "IsRequired" INTEGER NOT NULL DEFAULT 0,
                "IsEditable" INTEGER NOT NULL DEFAULT 1,
                "AllowsNotApplicable" INTEGER NOT NULL DEFAULT 1,
                "PullsFromRegister" INTEGER NOT NULL DEFAULT 0,
                "AffectsReadiness" INTEGER NOT NULL DEFAULT 0,
                "SameAsPreviousEligible" INTEGER NOT NULL DEFAULT 1,
                "RequiresNoteWhenNotNormal" INTEGER NOT NULL DEFAULT 0,
                "ReadinessImpact" TEXT NULL,
                "DropdownOptionsJson" TEXT NULL,
                "SortOrder" INTEGER NOT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ChecklistPublishScopes" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ChecklistPublishScopes" PRIMARY KEY AUTOINCREMENT,
                "ChecklistTemplateId" INTEGER NOT NULL,
                "ScopeType" TEXT NOT NULL,
                "OperationalAreaId" INTEGER NULL,
                "VehicleId" INTEGER NULL,
                "PublishedByUserId" INTEGER NOT NULL,
                "PublishNote" TEXT NULL,
                "IsActive" INTEGER NOT NULL DEFAULT 1,
                "PublishedAtUtc" TEXT NOT NULL,
                "RetiredAtUtc" TEXT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ChecklistVarianceAlerts" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ChecklistVarianceAlerts" PRIMARY KEY AUTOINCREMENT,
                "CompanyId" INTEGER NOT NULL,
                "DailyVehicleReadinessReportId" INTEGER NOT NULL,
                "DailyVehicleEquipmentCheckId" INTEGER NULL,
                "VehicleId" INTEGER NULL,
                "DetectedForUserId" INTEGER NOT NULL,
                "AssignedToUserId" INTEGER NULL,
                "ReviewedByUserId" INTEGER NULL,
                "AlertType" TEXT NOT NULL,
                "FieldKey" TEXT NOT NULL,
                "AssetLabel" TEXT NULL,
                "PreviousValue" TEXT NULL,
                "NewValue" TEXT NULL,
                "RegisterValue" TEXT NULL,
                "Severity" TEXT NOT NULL,
                "Status" TEXT NOT NULL,
                "RequiresRegisterUpdate" INTEGER NOT NULL DEFAULT 0,
                "RegisterUpdatedAtUtc" TEXT NULL,
                "ReviewedAtUtc" TEXT NULL,
                "ReviewNote" TEXT NULL,
                "CreatedAtUtc" TEXT NOT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ReadinessAlerts" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ReadinessAlerts" PRIMARY KEY AUTOINCREMENT,
                "CompanyId" INTEGER NOT NULL,
                "ReadinessEngineRuleId" INTEGER NULL,
                "DailyVehicleReadinessReportId" INTEGER NOT NULL,
                "DailyVehicleEquipmentCheckId" INTEGER NULL,
                "VehicleId" INTEGER NOT NULL,
                "TriggeredByUserId" INTEGER NOT NULL,
                "AssignedToUserId" INTEGER NULL,
                "ReviewedByUserId" INTEGER NULL,
                "AssetType" TEXT NOT NULL,
                "SourceArea" TEXT NOT NULL,
                "ItemName" TEXT NOT NULL,
                "FieldKey" TEXT NULL,
                "TriggerValue" TEXT NOT NULL,
                "Severity" TEXT NOT NULL,
                "ImpactPercent" INTEGER NOT NULL,
                "IsHardBlocker" INTEGER NOT NULL DEFAULT 0,
                "Status" TEXT NOT NULL,
                "VehicleLabel" TEXT NOT NULL,
                "AlertSummary" TEXT NOT NULL,
                "SourceValue" TEXT NULL,
                "ReviewNote" TEXT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                "AcknowledgedAtUtc" TEXT NULL,
                "ResolvedAtUtc" TEXT NULL,
                "DeletedAtUtc" TEXT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ReadinessEngineVersions" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ReadinessEngineVersions" PRIMARY KEY AUTOINCREMENT,
                "CompanyId" INTEGER NOT NULL,
                "Name" TEXT NOT NULL,
                "VersionNumber" TEXT NOT NULL,
                "Status" TEXT NOT NULL,
                "SourceReadinessEngineVersionId" INTEGER NULL,
                "CreatedByUserId" INTEGER NULL,
                "PublishedByUserId" INTEGER NULL,
                "Notes" TEXT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                "UpdatedAtUtc" TEXT NULL,
                "PublishedAtUtc" TEXT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ReadinessEngineRules" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ReadinessEngineRules" PRIMARY KEY AUTOINCREMENT,
                "CompanyId" INTEGER NOT NULL,
                "ReadinessEngineVersionId" INTEGER NOT NULL,
                "AssetType" TEXT NOT NULL,
                "Section" TEXT NOT NULL,
                "ItemName" TEXT NOT NULL,
                "FieldKey" TEXT NULL,
                "TriggerValue" TEXT NOT NULL,
                "AppliesTo" TEXT NOT NULL,
                "ReadinessScope" TEXT NOT NULL DEFAULT 'Active shift',
                "TargetVehicleType" TEXT NULL,
                "OperationalAreaId" INTEGER NULL,
                "ChecklistTemplateId" INTEGER NULL,
                "Severity" TEXT NOT NULL,
                "DefaultImpactPercent" INTEGER NOT NULL,
                "ManualImpactPercent" INTEGER NULL,
                "IsHardBlocker" INTEGER NOT NULL DEFAULT 0,
                "ManagerAlert" INTEGER NOT NULL DEFAULT 1,
                "IsActive" INTEGER NOT NULL DEFAULT 1,
                "IsAutoPopulated" INTEGER NOT NULL DEFAULT 0,
                "SourceType" TEXT NOT NULL,
                "SourceEntityType" TEXT NULL,
                "SourceEntityId" INTEGER NULL,
                "Notes" TEXT NULL,
                "SortOrder" INTEGER NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                "UpdatedAtUtc" TEXT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ReadinessScoringChangeRequests" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ReadinessScoringChangeRequests" PRIMARY KEY AUTOINCREMENT,
                "CompanyId" INTEGER NOT NULL,
                "RequestedByUserId" INTEGER NOT NULL,
                "ReviewedByUserId" INTEGER NULL,
                "ReadinessEngineRuleId" INTEGER NULL,
                "Status" TEXT NOT NULL,
                "AssetType" TEXT NOT NULL,
                "ItemName" TEXT NOT NULL,
                "TriggerValue" TEXT NOT NULL,
                "CurrentSeverity" TEXT NULL,
                "ProposedSeverity" TEXT NOT NULL,
                "CurrentImpactPercent" INTEGER NULL,
                "ProposedImpactPercent" INTEGER NULL,
                "CurrentActive" INTEGER NULL,
                "ProposedActive" INTEGER NULL,
                "Reason" TEXT NOT NULL,
                "SeniorDecisionNote" TEXT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                "ReviewedAtUtc" TEXT NULL
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

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "AppUserAccessPermissions" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_AppUserAccessPermissions" PRIMARY KEY AUTOINCREMENT,
                "CompanyId" INTEGER NOT NULL,
                "AppUserId" INTEGER NOT NULL,
                "PermissionKey" TEXT NOT NULL,
                "Status" TEXT NOT NULL,
                "UpdatedByUserId" INTEGER NOT NULL,
                "UpdatedAtUtc" TEXT NOT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "VehicleSchematicAssignments" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_VehicleSchematicAssignments" PRIMARY KEY AUTOINCREMENT,
                "CompanyId" INTEGER NOT NULL,
                "SchematicKey" TEXT NOT NULL,
                "ScopeType" TEXT NOT NULL,
                "VehicleFunction" TEXT NULL,
                "VehicleSubtype" TEXT NULL,
                "CreatedByUserId" INTEGER NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                "UpdatedAtUtc" TEXT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "CustomDropdownOptions" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_CustomDropdownOptions" PRIMARY KEY AUTOINCREMENT,
                "CompanyId" INTEGER NOT NULL,
                "CreatedByUserId" INTEGER NOT NULL,
                "DropdownKey" TEXT NOT NULL,
                "Value" TEXT NOT NULL,
                "Status" TEXT NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                "UpdatedAtUtc" TEXT NULL
            );
            """);

        await EnsureSqliteColumnAsync(db, "Vehicles", "CurrentOperationalAreaId", """ALTER TABLE "Vehicles" ADD "CurrentOperationalAreaId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "Vehicles", "CurrentLocationDetail", """ALTER TABLE "Vehicles" ADD "CurrentLocationDetail" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "Vehicles", "LastMovedByUserId", """ALTER TABLE "Vehicles" ADD "LastMovedByUserId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "Vehicles", "LastMovedAtUtc", """ALTER TABLE "Vehicles" ADD "LastMovedAtUtc" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "Vehicles", "VehicleFunction", """ALTER TABLE "Vehicles" ADD "VehicleFunction" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "Vehicles", "VehicleSubtype", """ALTER TABLE "Vehicles" ADD "VehicleSubtype" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "Vehicles", "VinNumber", """ALTER TABLE "Vehicles" ADD "VinNumber" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "Vehicles", "ChassisNumber", """ALTER TABLE "Vehicles" ADD "ChassisNumber" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "Vehicles", "LicenseNumber", """ALTER TABLE "Vehicles" ADD "LicenseNumber" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "Vehicles", "LicenseDiscExpiryDate", """ALTER TABLE "Vehicles" ADD "LicenseDiscExpiryDate" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "Vehicles", "LastServiceDate", """ALTER TABLE "Vehicles" ADD "LastServiceDate" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "EquipmentItems", "CurrentOperationalAreaId", """ALTER TABLE "EquipmentItems" ADD "CurrentOperationalAreaId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "EquipmentItems", "CurrentLocationDetail", """ALTER TABLE "EquipmentItems" ADD "CurrentLocationDetail" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "EquipmentItems", "LastMovedByUserId", """ALTER TABLE "EquipmentItems" ADD "LastMovedByUserId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "EquipmentItems", "LastMovedAtUtc", """ALTER TABLE "EquipmentItems" ADD "LastMovedAtUtc" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "AppUsers", "StaffIdentifier", """ALTER TABLE "AppUsers" ADD "StaffIdentifier" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "AppUsers", "NationalId", """ALTER TABLE "AppUsers" ADD "NationalId" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "AppUsers", "CellNumber", """ALTER TABLE "AppUsers" ADD "CellNumber" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "AppUsers", "QualificationFunction", """ALTER TABLE "AppUsers" ADD "QualificationFunction" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "AppUsers", "PractitionerNumber", """ALTER TABLE "AppUsers" ADD "PractitionerNumber" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "AppUsers", "AnnualLicenseExpiryDate", """ALTER TABLE "AppUsers" ADD "AnnualLicenseExpiryDate" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "AppUsers", "CpdComplianceStatus", """ALTER TABLE "AppUsers" ADD "CpdComplianceStatus" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "AppUsers", "CpdComplianceExpiryDate", """ALTER TABLE "AppUsers" ADD "CpdComplianceExpiryDate" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "AppUsers", "AssignedOperationalAreaId", """ALTER TABLE "AppUsers" ADD "AssignedOperationalAreaId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "Companies", "WorkspaceSlug", """ALTER TABLE "Companies" ADD "WorkspaceSlug" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "Companies", "WorkspaceAccessCode", """ALTER TABLE "Companies" ADD "WorkspaceAccessCode" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "Companies", "AllowSameAsPreviousVehicleInspection", """ALTER TABLE "Companies" ADD "AllowSameAsPreviousVehicleInspection" INTEGER NOT NULL DEFAULT 1;""");
        await EnsureSqliteColumnAsync(db, "Companies", "AllowSameAsPreviousEquipmentCheck", """ALTER TABLE "Companies" ADD "AllowSameAsPreviousEquipmentCheck" INTEGER NOT NULL DEFAULT 1;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleReadinessReports", "ShiftStartedAtUtc", """ALTER TABLE "DailyVehicleReadinessReports" ADD "ShiftStartedAtUtc" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleReadinessReports", "ChecklistTemplateId", """ALTER TABLE "DailyVehicleReadinessReports" ADD "ChecklistTemplateId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleReadinessReports", "ChecklistTemplateVersion", """ALTER TABLE "DailyVehicleReadinessReports" ADD "ChecklistTemplateVersion" TEXT NULL;""");
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
        await EnsureSqliteColumnAsync(db, "DailyVehicleReadinessReports", "SchematicMarkData", """ALTER TABLE "DailyVehicleReadinessReports" ADD "SchematicMarkData" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleEquipmentChecks", "SameAsPreviousShiftUsed", """ALTER TABLE "DailyVehicleEquipmentChecks" ADD "SameAsPreviousShiftUsed" INTEGER NOT NULL DEFAULT 0;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleEquipmentChecks", "ChecklistItemId", """ALTER TABLE "DailyVehicleEquipmentChecks" ADD "ChecklistItemId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleEquipmentChecks", "CopiedFromDailyVehicleEquipmentCheckId", """ALTER TABLE "DailyVehicleEquipmentChecks" ADD "CopiedFromDailyVehicleEquipmentCheckId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleEquipmentChecks", "SameAsPreviousAppliedAtUtc", """ALTER TABLE "DailyVehicleEquipmentChecks" ADD "SameAsPreviousAppliedAtUtc" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleEquipmentChecks", "IsOperational", """ALTER TABLE "DailyVehicleEquipmentChecks" ADD "IsOperational" INTEGER NOT NULL DEFAULT 1;""");
        await EnsureSqliteColumnAsync(db, "DailyVehicleEquipmentChecks", "IssueNotes", """ALTER TABLE "DailyVehicleEquipmentChecks" ADD "IssueNotes" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "ChecklistTemplates", "CompanyId", """ALTER TABLE "ChecklistTemplates" ADD "CompanyId" INTEGER NOT NULL DEFAULT 1;""");
        await EnsureSqliteColumnAsync(db, "ChecklistTemplates", "ChecklistType", """ALTER TABLE "ChecklistTemplates" ADD "ChecklistType" TEXT NOT NULL DEFAULT 'Vehicle';""");
        await EnsureSqliteColumnAsync(db, "ChecklistTemplates", "TargetVehicleType", """ALTER TABLE "ChecklistTemplates" ADD "TargetVehicleType" TEXT NOT NULL DEFAULT 'All Vehicles';""");
        await EnsureSqliteColumnAsync(db, "ChecklistTemplates", "Status", """ALTER TABLE "ChecklistTemplates" ADD "Status" TEXT NOT NULL DEFAULT 'Draft';""");
        await EnsureSqliteColumnAsync(db, "ChecklistTemplates", "SourceType", """ALTER TABLE "ChecklistTemplates" ADD "SourceType" TEXT NOT NULL DEFAULT 'Built';""");
        await EnsureSqliteColumnAsync(db, "ChecklistTemplates", "ParentChecklistTemplateId", """ALTER TABLE "ChecklistTemplates" ADD "ParentChecklistTemplateId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "ChecklistTemplates", "CreatedByUserId", """ALTER TABLE "ChecklistTemplates" ADD "CreatedByUserId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "ChecklistTemplates", "PublishedByUserId", """ALTER TABLE "ChecklistTemplates" ADD "PublishedByUserId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "ChecklistTemplates", "IsPublished", """ALTER TABLE "ChecklistTemplates" ADD "IsPublished" INTEGER NOT NULL DEFAULT 0;""");
        await EnsureSqliteColumnAsync(db, "ChecklistTemplates", "PublishedAtUtc", """ALTER TABLE "ChecklistTemplates" ADD "PublishedAtUtc" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "ChecklistTemplates", "PublishScopeSummary", """ALTER TABLE "ChecklistTemplates" ADD "PublishScopeSummary" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "ChecklistTemplates", "PublishNotes", """ALTER TABLE "ChecklistTemplates" ADD "PublishNotes" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "ChecklistTemplates", "UpdatedAtUtc", """ALTER TABLE "ChecklistTemplates" ADD "UpdatedAtUtc" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "ChecklistItems", "ItemKind", """ALTER TABLE "ChecklistItems" ADD "ItemKind" TEXT NOT NULL DEFAULT 'Field';""");
        await EnsureSqliteColumnAsync(db, "ChecklistItems", "ParentChecklistItemId", """ALTER TABLE "ChecklistItems" ADD "ParentChecklistItemId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "ChecklistItems", "CatalogueItemId", """ALTER TABLE "ChecklistItems" ADD "CatalogueItemId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "ChecklistItems", "EquipmentType", """ALTER TABLE "ChecklistItems" ADD "EquipmentType" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "ChecklistItems", "Model", """ALTER TABLE "ChecklistItems" ADD "Model" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "ChecklistItems", "FieldKey", """ALTER TABLE "ChecklistItems" ADD "FieldKey" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "ChecklistItems", "IsRequired", """ALTER TABLE "ChecklistItems" ADD "IsRequired" INTEGER NOT NULL DEFAULT 1;""");
        await EnsureSqliteColumnAsync(db, "ChecklistItems", "IsReadinessCritical", """ALTER TABLE "ChecklistItems" ADD "IsReadinessCritical" INTEGER NOT NULL DEFAULT 0;""");
        await EnsureSqliteColumnAsync(db, "ChecklistItems", "AllowsSameAsPrevious", """ALTER TABLE "ChecklistItems" ADD "AllowsSameAsPrevious" INTEGER NOT NULL DEFAULT 1;""");
        await EnsureSqliteColumnAsync(db, "ChecklistItems", "DefaultLocation", """ALTER TABLE "ChecklistItems" ADD "DefaultLocation" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "MedicationItems", "LastAllocatedByUserId", """ALTER TABLE "MedicationItems" ADD "LastAllocatedByUserId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "MedicationItems", "LastAllocatedAtUtc", """ALTER TABLE "MedicationItems" ADD "LastAllocatedAtUtc" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "MedicationItems", "LastAllocationLocation", """ALTER TABLE "MedicationItems" ADD "LastAllocationLocation" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "MedicationItems", "Schedule", """ALTER TABLE "MedicationItems" ADD "Schedule" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "StockItems", "CurrentOperationalAreaId", """ALTER TABLE "StockItems" ADD "CurrentOperationalAreaId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "StockItems", "StockCategory", """ALTER TABLE "StockItems" ADD "StockCategory" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "StockItems", "MinimumQuantity", """ALTER TABLE "StockItems" ADD "MinimumQuantity" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "StockItems", "Unit", """ALTER TABLE "StockItems" ADD "Unit" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "StockItems", "ExpiryDate", """ALTER TABLE "StockItems" ADD "ExpiryDate" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "StockItems", "IsReadinessCritical", """ALTER TABLE "StockItems" ADD "IsReadinessCritical" INTEGER NOT NULL DEFAULT 0;""");
        await EnsureSqliteColumnAsync(db, "MedicationItems", "CurrentOperationalAreaId", """ALTER TABLE "MedicationItems" ADD "CurrentOperationalAreaId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "ReadinessEngineRules", "ReadinessScope", """ALTER TABLE "ReadinessEngineRules" ADD "ReadinessScope" TEXT NOT NULL DEFAULT 'Active shift';""");

        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_MedicationItems_CompanyId" ON "MedicationItems" ("CompanyId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_AppUsers_CompanyId_StaffIdentifier" ON "AppUsers" ("CompanyId", "StaffIdentifier");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_AppUsers_CompanyId_QualificationFunction" ON "AppUsers" ("CompanyId", "QualificationFunction");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_AppUsers_CompanyId_PractitionerNumber" ON "AppUsers" ("CompanyId", "PractitionerNumber");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_AppUsers_CompanyId_AnnualLicenseExpiryDate" ON "AppUsers" ("CompanyId", "AnnualLicenseExpiryDate");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_AppUsers_CompanyId_CpdComplianceStatus" ON "AppUsers" ("CompanyId", "CpdComplianceStatus");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_AppUsers_AssignedOperationalAreaId" ON "AppUsers" ("AssignedOperationalAreaId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_MedicationItems_CreatedByUserId" ON "MedicationItems" ("CreatedByUserId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_MedicationItems_LastAllocatedByUserId" ON "MedicationItems" ("LastAllocatedByUserId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_MedicationItems_CurrentOperationalAreaId" ON "MedicationItems" ("CurrentOperationalAreaId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_StockItems_CompanyId_ItemName_BatchNumber_Location" ON "StockItems" ("CompanyId", "ItemName", "BatchNumber", "Location");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_StockItems_CompanyId_StockCategory_ItemName" ON "StockItems" ("CompanyId", "StockCategory", "ItemName");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_StockItems_CreatedByUserId" ON "StockItems" ("CreatedByUserId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_StockItems_LastMovedByUserId" ON "StockItems" ("LastMovedByUserId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_StockItems_CurrentOperationalAreaId" ON "StockItems" ("CurrentOperationalAreaId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_StockOrderLines_StockOrderId" ON "StockOrderLines" ("StockOrderId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_StockOrders_ApprovedBySeniorUserId" ON "StockOrders" ("ApprovedBySeniorUserId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_StockOrders_CompanyId_Status_CreatedAtUtc" ON "StockOrders" ("CompanyId", "Status", "CreatedAtUtc");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_StockOrders_RegisterEntryAuthorisedUserId" ON "StockOrders" ("RegisterEntryAuthorisedUserId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_StockOrders_RequestedByUserId" ON "StockOrders" ("RequestedByUserId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_DailyVehicleReadinessReports_CompanyId_WorkflowStatus_DraftExpiresAtUtc" ON "DailyVehicleReadinessReports" ("CompanyId", "WorkflowStatus", "DraftExpiresAtUtc");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_DailyVehicleReadinessReports_ChecklistTemplateId" ON "DailyVehicleReadinessReports" ("ChecklistTemplateId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_DailyVehicleReadinessReports_VehicleSameAsPreviousSourceReportId" ON "DailyVehicleReadinessReports" ("VehicleSameAsPreviousSourceReportId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_DailyVehicleReadinessReports_EquipmentSameAsPreviousSourceReportId" ON "DailyVehicleReadinessReports" ("EquipmentSameAsPreviousSourceReportId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_DailyVehicleEquipmentChecks_ChecklistItemId" ON "DailyVehicleEquipmentChecks" ("ChecklistItemId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_DailyVehicleEquipmentChecks_CopiedFromDailyVehicleEquipmentCheckId" ON "DailyVehicleEquipmentChecks" ("CopiedFromDailyVehicleEquipmentCheckId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_ChecklistTemplates_CompanyId_ChecklistType_TargetVehicleType_Name" ON "ChecklistTemplates" ("CompanyId", "ChecklistType", "TargetVehicleType", "Name");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_ChecklistItems_ParentChecklistItemId" ON "ChecklistItems" ("ParentChecklistItemId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_ChecklistColumnDefinitions_ChecklistItemId_SortOrder" ON "ChecklistColumnDefinitions" ("ChecklistItemId", "SortOrder");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_ChecklistPublishScopes_ChecklistTemplateId_ScopeType_IsActive" ON "ChecklistPublishScopes" ("ChecklistTemplateId", "ScopeType", "IsActive");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_CatalogueItems_CompanyId_CatalogueType_Category_ItemName" ON "CatalogueItems" ("CompanyId", "CatalogueType", "Category", "ItemName");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_ChecklistVarianceAlerts_CompanyId_AssignedToUserId_Status_CreatedAtUtc" ON "ChecklistVarianceAlerts" ("CompanyId", "AssignedToUserId", "Status", "CreatedAtUtc");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_ReadinessAlerts_CompanyId_AssignedToUserId_Status_CreatedAtUtc" ON "ReadinessAlerts" ("CompanyId", "AssignedToUserId", "Status", "CreatedAtUtc");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_ReadinessAlerts_CompanyId_Status_CreatedAtUtc" ON "ReadinessAlerts" ("CompanyId", "Status", "CreatedAtUtc");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_ReadinessAlerts_DailyVehicleReadinessReportId" ON "ReadinessAlerts" ("DailyVehicleReadinessReportId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_ReadinessAlerts_DailyVehicleEquipmentCheckId" ON "ReadinessAlerts" ("DailyVehicleEquipmentCheckId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_ReadinessAlerts_ReadinessEngineRuleId" ON "ReadinessAlerts" ("ReadinessEngineRuleId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_ReadinessEngineVersions_CompanyId_Status_CreatedAtUtc" ON "ReadinessEngineVersions" ("CompanyId", "Status", "CreatedAtUtc");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_ReadinessEngineRules_CompanyId_AssetType_Section_ItemName_TriggerValue" ON "ReadinessEngineRules" ("CompanyId", "AssetType", "Section", "ItemName", "TriggerValue");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_ReadinessEngineRules_ReadinessEngineVersionId_SortOrder" ON "ReadinessEngineRules" ("ReadinessEngineVersionId", "SortOrder");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_ReadinessScoringChangeRequests_CompanyId_Status_CreatedAtUtc" ON "ReadinessScoringChangeRequests" ("CompanyId", "Status", "CreatedAtUtc");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_OperationalAreas_CompanyId_Name" ON "OperationalAreas" ("CompanyId", "Name");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_ManagerOperationalAreaAssignments_CompanyId_ManagerUserId_OperationalAreaId" ON "ManagerOperationalAreaAssignments" ("CompanyId", "ManagerUserId", "OperationalAreaId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_ManagerOperationalAreaAssignments_ManagerUserId" ON "ManagerOperationalAreaAssignments" ("ManagerUserId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_ManagerOperationalAreaAssignments_OperationalAreaId" ON "ManagerOperationalAreaAssignments" ("OperationalAreaId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_ManagerOperationalAreaAssignments_AssignedByUserId" ON "ManagerOperationalAreaAssignments" ("AssignedByUserId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_AssetMovements_CompanyId_AssetType_AssetId_CreatedAtUtc" ON "AssetMovements" ("CompanyId", "AssetType", "AssetId", "CreatedAtUtc");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_AssetFiles_CompanyId_LinkedEntityType_LinkedEntityId_Category" ON "AssetFiles" ("CompanyId", "LinkedEntityType", "LinkedEntityId", "Category");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_AssetFiles_UploadedByUserId" ON "AssetFiles" ("UploadedByUserId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_AppUserAccessPermissions_CompanyId_AppUserId_PermissionKey" ON "AppUserAccessPermissions" ("CompanyId", "AppUserId", "PermissionKey");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_AppUserAccessPermissions_AppUserId" ON "AppUserAccessPermissions" ("AppUserId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_AppUserAccessPermissions_UpdatedByUserId" ON "AppUserAccessPermissions" ("UpdatedByUserId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_VehicleSchematicAssignments_CompanyId_ScopeType_VehicleFunction_VehicleSubtype" ON "VehicleSchematicAssignments" ("CompanyId", "ScopeType", "VehicleFunction", "VehicleSubtype");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_VehicleSchematicAssignments_CompanyId_SchematicKey" ON "VehicleSchematicAssignments" ("CompanyId", "SchematicKey");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_VehicleSchematicAssignments_CreatedByUserId" ON "VehicleSchematicAssignments" ("CreatedByUserId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_CustomDropdownOptions_CompanyId_DropdownKey_Value" ON "CustomDropdownOptions" ("CompanyId", "DropdownKey", "Value");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_CustomDropdownOptions_CreatedByUserId" ON "CustomDropdownOptions" ("CreatedByUserId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_Companies_WorkspaceSlug" ON "Companies" ("WorkspaceSlug") WHERE "WorkspaceSlug" IS NOT NULL;""");
    }

    private static async Task BackfillVehicleTaxonomyAsync(VectorDbContext db)
    {
        var vehicles = await db.Vehicles
            .Where(vehicle =>
                vehicle.VehicleFunction == null ||
                vehicle.VehicleFunction == "" ||
                vehicle.VehicleSubtype == null ||
                vehicle.VehicleSubtype == "")
            .ToListAsync();

        var now = DateTime.UtcNow;
        var changed = false;
        foreach (var vehicle in vehicles)
        {
            if (!VehicleTaxonomyService.Backfill(vehicle))
            {
                continue;
            }

            vehicle.UpdatedAtUtc = now;
            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureDefaultVehicleSchematicAssignmentsAsync(VectorDbContext db)
    {
        var companyIds = await db.Companies
            .AsNoTracking()
            .Select(company => company.Id)
            .ToListAsync();

        foreach (var companyId in companyIds)
        {
            await EnsureVehicleSchematicAssignmentAsync(
                db,
                companyId,
                VehicleSchematicAssignmentService.FunctionScope,
                VehicleTaxonomyService.AmbulanceFunction,
                null,
                "toyota-quantum-hiace-high-roof");

            await EnsureVehicleSchematicAssignmentAsync(
                db,
                companyId,
                VehicleSchematicAssignmentService.FunctionScope,
                VehicleTaxonomyService.ResponseVehicleFunction,
                null,
                "pickup-rv");
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureVehicleSchematicAssignmentAsync(
        VectorDbContext db,
        int companyId,
        string scopeType,
        string? vehicleFunction,
        string? vehicleSubtype,
        string schematicKey)
    {
        if (await db.VehicleSchematicAssignments.AnyAsync(assignment =>
            assignment.CompanyId == companyId &&
            assignment.ScopeType == scopeType &&
            assignment.VehicleFunction == vehicleFunction &&
            assignment.VehicleSubtype == vehicleSubtype))
        {
            return;
        }

        db.VehicleSchematicAssignments.Add(new VehicleSchematicAssignment
        {
            CompanyId = companyId,
            ScopeType = scopeType,
            VehicleFunction = vehicleFunction,
            VehicleSubtype = vehicleSubtype,
            SchematicKey = schematicKey,
            CreatedAtUtc = DateTime.UtcNow
        });
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
}
