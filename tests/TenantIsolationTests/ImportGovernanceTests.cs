using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

internal static class ImportGovernanceTests
{
    public static async Task RunAllAsync()
    {
        await using var fixture = await TenantFixture.CreateAsync();
        foreach (var company in await fixture.Db.Companies.Where(item => item.Id == fixture.TenantA.CompanyId || item.Id == fixture.TenantB.CompanyId).ToListAsync())
            company.SubscriptionTier = SubscriptionTiers.Pro;
        await fixture.Db.SaveChangesAsync();
        var actor = await fixture.Db.AppUsers.Include(user => user.AppRole).SingleAsync(user => user.Id == fixture.TenantA.SeniorUserId);
        var foreignActor = await fixture.Db.AppUsers.Include(user => user.AppRole).SingleAsync(user => user.Id == fixture.TenantB.SeniorUserId);
        var batches = new ImportBatchService(fixture.Db, new UserActionPermissionService(fixture.Db));
        var governance = new ImportGovernanceService(fixture.Db, batches);

        var mappingBatch = await PrepareAsync(fixture, actor, ImportTargetTypes.Vehicle,
            ("Registration", "MAP-101"), ("Call sign", "M01"));
        await governance.SaveMappingProfileAsync(actor, mappingBatch.Id, "Vehicle register mapping");
        var profile = await fixture.Db.ImportMappingProfiles.SingleAsync(item => item.CompanyId == actor.CompanyId);
        foreach (var mapping in mappingBatch.ColumnMappings) { mapping.TargetFieldKey = null; mapping.IsIgnored = true; mapping.IsUserConfirmed = true; }
        await fixture.Db.SaveChangesAsync();
        await governance.ReuseMappingProfileAsync(actor, mappingBatch.Id, profile.Id);
        var reused = await fixture.Db.ImportColumnMappings.Where(item => item.ImportBatchId == mappingBatch.Id).ToListAsync();
        Ensure(reused.All(item => !item.IsUserConfirmed), "Reused mappings bypassed explicit confirmation.");
        Ensure(reused.Any(item => item.TargetFieldKey == "vehicle.registration_number"), "Saved mapping did not restore the expected target field.");
        await EnsureThrowsAsync<InvalidOperationException>(() => governance.ReuseMappingProfileAsync(foreignActor, mappingBatch.Id, profile.Id),
            "A second tenant reused another company's mapping profile.");

        var cleanBatch = await PrepareAsync(fixture, actor, ImportTargetTypes.Stock,
            ("Item", "Rollback Gauze"), ("Quantity", "10"), ("Batch number", "ROLL-1"));
        var cleanWorkflow = Workflow(fixture, new StaticReader(("Item", "Rollback Gauze"), ("Quantity", "10"), ("Batch number", "ROLL-1")));
        await cleanWorkflow.CommitAsync(actor, cleanBatch.Id);
        var cleanResult = await governance.RollbackAsync(actor, cleanBatch.Id);
        Ensure(cleanResult.Reversed == 1 && cleanResult.Blocked == 0, "An unchanged import was not rolled back.");
        Ensure(!await fixture.Db.StockItems.AnyAsync(item => item.CompanyId == actor.CompanyId && item.BatchNumber == "ROLL-1"),
            "Rolled-back imported stock still exists.");

        var changedBatch = await PrepareAsync(fixture, actor, ImportTargetTypes.Stock,
            ("Item", "Changed Gauze"), ("Quantity", "8"), ("Batch number", "ROLL-2"));
        var changedWorkflow = Workflow(fixture, new StaticReader(("Item", "Changed Gauze"), ("Quantity", "8"), ("Batch number", "ROLL-2")));
        await changedWorkflow.CommitAsync(actor, changedBatch.Id);
        var changed = await fixture.Db.StockItems.SingleAsync(item => item.CompanyId == actor.CompanyId && item.BatchNumber == "ROLL-2");
        changed.Notes = "Used after import";
        changed.UpdatedAtUtc = DateTime.UtcNow.AddMinutes(1);
        await fixture.Db.SaveChangesAsync();
        var blocked = await governance.RollbackAsync(actor, changedBatch.Id);
        Ensure(blocked.Blocked == 1 && await fixture.Db.StockItems.AnyAsync(item => item.Id == changed.Id),
            "Rollback removed a record changed after import.");

        var companyA = await fixture.Db.Companies.SingleAsync(item => item.Id == actor.CompanyId);
        companyA.SubscriptionTier = SubscriptionTiers.Base;
        await fixture.Db.SaveChangesAsync();
        Ensure(!(await batches.CanPrepareAsync(actor)).Allowed, "Base retained access to guided import tools.");
        Ensure(await fixture.Db.StockItems.AnyAsync(item => item.Id == changed.Id), "Downgrade removed imported domain data.");

        await VerifyMigrationAsync();
    }

    private static async Task<ImportBatch> PrepareAsync(TenantFixture fixture, AppUser actor, string target, params (string Heading, string? Value)[] cells)
    {
        var reader = new StaticReader(cells);
        var batches = new ImportBatchService(fixture.Db, new UserActionPermissionService(fixture.Db));
        var file = new AssetFile
        {
            CompanyId = actor.CompanyId, UploadedByUserId = actor.Id, LinkedEntityType = SetupUploadService.RegisterUploadEntityType,
            Category = target, OriginalFileName = $"{target}.csv", ContentType = "text/csv", StorageProvider = "test",
            StoragePath = $"test/company-{actor.CompanyId}/{Guid.NewGuid():N}.csv", SizeBytes = 100
        };
        fixture.Db.AssetFiles.Add(file);
        var profile = new ImportSourceProfile(1, new string('G', 64), "CSV", 1, 2, cells.Length * 2,
            [new ImportWorksheetProfile("Sheet 1", 2, cells.Length, cells.Length * 2)]);
        var batch = await batches.CreateUploadedBatchAsync(actor, file, target, profile, DateTime.UtcNow);
        await fixture.Db.SaveChangesAsync();
        var workflow = Workflow(fixture, reader);
        await workflow.SelectSourceAsync(actor, batch.Id, "Sheet 1", 1);
        batch = (await workflow.LoadAsync(actor, batch.Id))!;
        await workflow.SaveMappingsAsync(actor, batch.Id, batch.ColumnMappings.Select(mapping =>
            new ImportMappingInput(mapping.SourceColumnIndex, mapping.TargetFieldKey, mapping.TargetFieldKey is null)).ToList());
        await workflow.ValidateAsync(actor, batch.Id);
        return (await workflow.LoadAsync(actor, batch.Id))!;
    }

    private static ImportRegisterWorkflowService Workflow(TenantFixture fixture, IImportTabularReader reader)
    {
        var batches = new ImportBatchService(fixture.Db, new UserActionPermissionService(fixture.Db));
        return new ImportRegisterWorkflowService(fixture.Db, batches, new ImportFieldRegistry(), reader);
    }

    private static async Task VerifyMigrationAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<VectorDbContext>().UseSqlite(connection).Options;
        await using var db = new VectorDbContext(options);
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE Companies (Id INTEGER NOT NULL PRIMARY KEY);");
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE AppUsers (Id INTEGER NOT NULL PRIMARY KEY);");
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE __EFMigrationsHistory (MigrationId TEXT NOT NULL PRIMARY KEY, ProductVersion TEXT NOT NULL);");
        const string migrationId = "20260718152000_AddImportMappingProfiles";
        var migrations = db.GetService<IMigrationsAssembly>().Migrations.Keys.OrderBy(item => item).ToList();
        foreach (var prior in migrations.Where(item => string.CompareOrdinal(item, migrationId) < 0))
            await db.Database.ExecuteSqlRawAsync("INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ({0}, '8.0.0');", prior);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(migrationId);
        Ensure(await TableExistsAsync(connection, "ImportMappingProfiles"), "Mapping profile table was not created.");
        var previous = migrations.Last(item => string.CompareOrdinal(item, migrationId) < 0);
        await migrator.MigrateAsync(previous);
        Ensure(!await TableExistsAsync(connection, "ImportMappingProfiles"), "Mapping profile migration did not roll back.");
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection connection, string table)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name;";
        command.Parameters.AddWithValue("$name", table);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    private static async Task EnsureThrowsAsync<TException>(Func<Task> action, string message) where TException : Exception
    {
        try { await action(); }
        catch (TException) { return; }
        throw new InvalidOperationException(message);
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class StaticReader : IImportTabularReader
    {
        private readonly (string Heading, string? Value)[] _cells;
        public StaticReader(params (string Heading, string? Value)[] cells) => _cells = cells;
        public Task<ImportTabularData> ReadAsync(AssetFile sourceFile, string? worksheet, int headerRowNumber, CancellationToken cancellationToken = default)
        {
            var columns = _cells.Select((cell, index) => new ImportSourceColumn(index, cell.Heading, cell.Value is null ? [] : [cell.Value])).ToList();
            var rows = _cells.Length == 0 ? new List<ImportSourceRow>() :
                [new ImportSourceRow(2, _cells.Select((cell, index) => (index, cell.Value)).ToDictionary(item => item.index, item => item.Value))];
            return Task.FromResult(new ImportTabularData("Sheet 1", 1, columns, rows));
        }
    }
}
