using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

internal static class PilotEntitlementTests
{
    public static async Task RunAllAsync()
    {
        await VerifyLifecycleAndTenantIsolationAsync();
        await VerifyMigrationAsync();
    }

    private static async Task VerifyLifecycleAndTenantIsolationAsync()
    {
        await using var fixture = await TenantFixture.CreateAsync();
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));
        var service = new PilotEntitlementService(fixture.Db, clock);
        var session = new TestSession();
        session.SetInt32(CurrentUserService.CompanyIdSessionKey, fixture.TenantA.CompanyId);
        var httpContext = new DefaultHttpContext { Session = session };
        var featureAccess = new FeatureAccessService(new HttpContextAccessor { HttpContext = httpContext }, fixture.Db, clock);
        var seniorA = await fixture.Db.AppUsers.Include(user => user.AppRole)
            .SingleAsync(user => user.Id == fixture.TenantA.SeniorUserId);
        var staffA = await fixture.Db.AppUsers.Include(user => user.AppRole)
            .SingleAsync(user => user.Id == fixture.TenantA.StaffUserId);

        var companyA = await fixture.Db.Companies.SingleAsync(company => company.Id == fixture.TenantA.CompanyId);
        companyA.SubscriptionTier = SubscriptionTiers.Premium;
        await fixture.Db.SaveChangesAsync();

        Ensure(!await featureAccess.CanUseFeatureAsync(VectorFeatures.AiImportIntelligence),
            "Legacy SubscriptionTier granted Premium access without an explicit entitlement.");
        Ensure(await fixture.Db.PilotEntitlements.CountAsync() == 0,
            "Reading feature access created an entitlement record.");

        var unauthorized = await service.ActivateAsync(staffA, clock.GetUtcNow().UtcDateTime.Date, null);
        Ensure(!unauthorized.Success, "Staff activated a company entitlement.");

        var start = clock.GetUtcNow().UtcDateTime.Date;
        var activated = await service.ActivateAsync(seniorA, start, "Pilot approval");
        Ensure(activated.Success, "Senior manager could not activate the pilot entitlement.");
        Ensure(activated.Entitlement?.ExpiresAtUtc == start.AddYears(1), "Pilot activation was not exactly twelve months.");
        Ensure(await featureAccess.CanUseFeatureAsync(VectorFeatures.AiImportIntelligence),
            "Active Premium entitlement did not grant the Premium feature.");
        Ensure(!(await PilotEntitlementAccess.LoadAsync(fixture.Db, fixture.TenantB.CompanyId, clock.GetUtcNow().UtcDateTime)).IsFullAccess,
            "Premium entitlement leaked to another tenant.");

        var extendedExpiry = start.AddYears(1).AddMonths(2);
        var extended = await service.ExtendAsync(seniorA, extendedExpiry, "Controlled extension");
        Ensure(extended.Success && extended.Entitlement?.ExpiresAtUtc == extendedExpiry,
            "Manual entitlement extension failed.");

        var tenantRecordCount = await CountTenantRecordsAsync(fixture.Db, companyA.Id);
        var revoked = await service.RevokeAsync(seniorA, "Controlled revocation test");
        Ensure(revoked.Success, "Entitlement revocation failed.");
        var revokedAccess = await featureAccess.GetFeatureAccessAsync(VectorFeatures.AiImportIntelligence);
        Ensure(revokedAccess.IsReadOnlyExport, "Revoked Premium access did not enter read-only/export mode.");
        Ensure(tenantRecordCount == await CountTenantRecordsAsync(fixture.Db, companyA.Id),
            "Revocation altered tenant-owned records.");

        var history = await service.GetHistoryAsync(companyA.Id);
        Ensure(history.Count == 3, "Entitlement lifecycle history is incomplete.");
        Ensure(await service.GetHistoryAsync(fixture.TenantB.CompanyId) is { Count: 0 },
            "Entitlement history leaked to another tenant.");
        Ensure(await fixture.Db.AuditLogs.CountAsync(log => log.CompanyId == companyA.Id && log.EntityType == nameof(PilotEntitlement)) == 3,
            "Entitlement lifecycle was not fully audit logged.");

        clock.SetUtcNow(new DateTimeOffset(extendedExpiry.AddDays(1), TimeSpan.Zero));
        var expiredSnapshot = PilotEntitlementAccess.Snapshot(new PilotEntitlement
        {
            CompanyId = companyA.Id,
            Tier = SubscriptionTiers.Premium,
            Status = PilotEntitlementStatuses.Active,
            StartsAtUtc = start,
            ExpiresAtUtc = extendedExpiry
        }, companyA.Id, clock.GetUtcNow().UtcDateTime);
        Ensure(expiredSnapshot.IsReadOnlyExport && expiredSnapshot.EffectiveStatus == PilotEntitlementStatuses.Expired,
            "Expired entitlement did not preserve read-only/export access.");
    }

    private static async Task<int> CountTenantRecordsAsync(VectorDbContext db, int companyId)
        => await db.AppUsers.CountAsync(item => item.CompanyId == companyId)
           + await db.Vehicles.CountAsync(item => item.CompanyId == companyId)
           + await db.EquipmentItems.CountAsync(item => item.CompanyId == companyId)
           + await db.StockItems.CountAsync(item => item.CompanyId == companyId)
           + await db.MedicationItems.CountAsync(item => item.CompanyId == companyId)
           + await db.ChecklistTemplates.CountAsync(item => item.CompanyId == companyId);

    private static async Task VerifyMigrationAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<VectorDbContext>().UseSqlite(connection).Options;
        await using var db = new VectorDbContext(options);
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE Companies (Id INTEGER NOT NULL PRIMARY KEY);");
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE AppUsers (Id INTEGER NOT NULL PRIMARY KEY);");
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE __EFMigrationsHistory (MigrationId TEXT NOT NULL PRIMARY KEY, ProductVersion TEXT NOT NULL);");

        const string migrationId = "20260814150000_AddPilotEntitlementLifecycle";
        var migrations = db.GetService<IMigrationsAssembly>().Migrations.Keys.OrderBy(item => item).ToList();
        foreach (var prior in migrations.Where(item => string.CompareOrdinal(item, migrationId) < 0))
        {
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ({0}, '8.0.5');", prior);
        }

        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(migrationId);
        Ensure(await TableExistsAsync(connection, "PilotEntitlements"), "Entitlement table was not created.");
        Ensure(await TableExistsAsync(connection, "PilotEntitlementEvents"), "Entitlement event table was not created.");
        var previous = migrations.Last(item => string.CompareOrdinal(item, migrationId) < 0);
        await migrator.MigrateAsync(previous);
        Ensure(!await TableExistsAsync(connection, "PilotEntitlements"), "Entitlement migration did not roll back.");
        Ensure(!await TableExistsAsync(connection, "PilotEntitlementEvents"), "Entitlement event migration did not roll back.");
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection connection, string table)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name;";
        command.Parameters.AddWithValue("$name", table);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;
        public TestTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => _utcNow;
        public void SetUtcNow(DateTimeOffset utcNow) => _utcNow = utcNow;
    }
}
