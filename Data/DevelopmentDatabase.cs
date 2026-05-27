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

        await EnsureSqliteColumnAsync(db, "Vehicles", "CurrentOperationalAreaId", """ALTER TABLE "Vehicles" ADD "CurrentOperationalAreaId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "Vehicles", "CurrentLocationDetail", """ALTER TABLE "Vehicles" ADD "CurrentLocationDetail" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "Vehicles", "LastMovedByUserId", """ALTER TABLE "Vehicles" ADD "LastMovedByUserId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "Vehicles", "LastMovedAtUtc", """ALTER TABLE "Vehicles" ADD "LastMovedAtUtc" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "EquipmentItems", "CurrentOperationalAreaId", """ALTER TABLE "EquipmentItems" ADD "CurrentOperationalAreaId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "EquipmentItems", "CurrentLocationDetail", """ALTER TABLE "EquipmentItems" ADD "CurrentLocationDetail" TEXT NULL;""");
        await EnsureSqliteColumnAsync(db, "EquipmentItems", "LastMovedByUserId", """ALTER TABLE "EquipmentItems" ADD "LastMovedByUserId" INTEGER NULL;""");
        await EnsureSqliteColumnAsync(db, "EquipmentItems", "LastMovedAtUtc", """ALTER TABLE "EquipmentItems" ADD "LastMovedAtUtc" TEXT NULL;""");
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
        await db.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_OperationalAreas_CompanyId_Name" ON "OperationalAreas" ("CompanyId", "Name");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_AssetMovements_CompanyId_AssetType_AssetId_CreatedAtUtc" ON "AssetMovements" ("CompanyId", "AssetType", "AssetId", "CreatedAtUtc");""");
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
}
