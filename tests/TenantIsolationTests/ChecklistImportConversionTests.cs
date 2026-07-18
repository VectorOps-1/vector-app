using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Data.Sqlite;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

internal static class ChecklistImportConversionTests
{
    public static async Task RunAllAsync()
    {
        await using var fixture = await TenantFixture.CreateAsync();
        var company = await fixture.Db.Companies.SingleAsync(item => item.Id == fixture.TenantA.CompanyId);
        company.SubscriptionTier = SubscriptionTiers.Pro;
        await fixture.Db.SaveChangesAsync();
        var actor = await fixture.Db.AppUsers.Include(user => user.AppRole)
            .SingleAsync(user => user.Id == fixture.TenantA.SeniorUserId);

        await VerifyExplicitColumnsAsync(fixture, actor);
        await VerifyMatrixAsync(fixture, actor);
        await VerifyOneSheetPerSectionAsync(fixture, actor);
        await VerifySectionedSheetAsync(fixture, actor);
        await VerifyTenantBoundaryAsync(fixture, actor);
        await VerifyAdditiveMigrationAsync();
    }

    private static async Task VerifyExplicitColumnsAsync(TenantFixture fixture, AppUser actor)
    {
        var reader = new StaticChecklistReader(new Dictionary<string, ImportTabularData>
        {
            ["Checklist"] = Data("Checklist",
                ["Section", "Item", "Input Type", "Required", "Readiness", "Register Source"],
                [
                    ["Vehicle", "Current kilometres", "Number", "Yes", "No", "Vehicle Register"],
                    ["Vehicle", "Warning lights", "Pass/Fail", "Yes", "Yes", null]
                ])
        });
        var prepared = await PrepareAsync(fixture, actor, reader, ChecklistImportLayouts.ExplicitColumns, "Imported Daily Check", "Checklist");
        Ensure(prepared.Draft.Sections.Count == 1 && prepared.Draft.Sections[0].Items.Count == 2,
            "Explicit checklist columns did not create the expected structure.");
        var template = await prepared.Service.CommitDraftAsync(actor, prepared.Batch.Id);
        var replay = await prepared.Service.CommitDraftAsync(actor, prepared.Batch.Id);
        Ensure(replay.Id == template.Id, "Checklist import commit was not idempotent.");
        await AssertDraftOnlyAsync(fixture, template.Id, "Imported Daily Check", 1, 2);
    }

    private static async Task VerifyMatrixAsync(TenantFixture fixture, AppUser actor)
    {
        var reader = new StaticChecklistReader(new Dictionary<string, ImportTabularData>
        {
            ["Vehicle"] = Data("Vehicle", ["Item", "Status", "Notes"], [["Lights", null, null], ["Sirens", null, null]])
        });
        var prepared = await PrepareAsync(fixture, actor, reader, ChecklistImportLayouts.Matrix, "Imported Matrix", "Vehicle");
        Ensure(prepared.Draft.Sections.Single().Items.All(item => item.Columns.Count == 2),
            "Matrix columns were not retained as checklist columns.");
        var template = await prepared.Service.CommitDraftAsync(actor, prepared.Batch.Id);
        await AssertDraftOnlyAsync(fixture, template.Id, "Imported Matrix", 1, 2);
    }

    private static async Task VerifyOneSheetPerSectionAsync(TenantFixture fixture, AppUser actor)
    {
        var reader = new StaticChecklistReader(new Dictionary<string, ImportTabularData>
        {
            ["Vehicle"] = Data("Vehicle", ["Item", "Status"], [["Tyres", null]]),
            ["Equipment"] = Data("Equipment", ["Item", "Present", "Operational"], [["Monitor Defibrillator", null, null]])
        });
        var prepared = await PrepareAsync(fixture, actor, reader, ChecklistImportLayouts.OneSheetPerSection, "Imported Multi-Sheet", null);
        Ensure(prepared.Draft.Sections.Select(section => section.Name).ToHashSet().SetEquals(["Vehicle", "Equipment"]),
            "Worksheet names were not retained as section names.");
        var template = await prepared.Service.CommitDraftAsync(actor, prepared.Batch.Id);
        await AssertDraftOnlyAsync(fixture, template.Id, "Imported Multi-Sheet", 2, 2);
    }

    private static async Task VerifySectionedSheetAsync(TenantFixture fixture, AppUser actor)
    {
        var reader = new StaticChecklistReader(new Dictionary<string, ImportTabularData>
        {
            ["Audit"] = Data("Audit", ["Label", "Response"],
                [["Vehicle", null], ["Body condition", "Select"], ["Equipment", null], ["Monitor present", "Select"]])
        });
        var prepared = await PrepareAsync(fixture, actor, reader, ChecklistImportLayouts.SectionedSheet, "Imported Sectioned Audit", "Audit");
        Ensure(prepared.Draft.Sections.Count == 2 && prepared.Draft.Sections.Sum(section => section.Items.Count) == 2,
            "Sectioned worksheet did not preserve section boundaries.");
        var template = await prepared.Service.CommitDraftAsync(actor, prepared.Batch.Id);
        await AssertDraftOnlyAsync(fixture, template.Id, "Imported Sectioned Audit", 2, 2);
    }

    private static async Task VerifyTenantBoundaryAsync(TenantFixture fixture, AppUser actor)
    {
        var reader = new StaticChecklistReader(new Dictionary<string, ImportTabularData>
        {
            ["Checklist"] = Data("Checklist", ["Item", "Status"], [["Tenant-owned item", null]])
        });
        var prepared = await PrepareAsync(fixture, actor, reader, ChecklistImportLayouts.Matrix, "Tenant A Checklist", "Checklist");
        var foreignActor = await fixture.Db.AppUsers.Include(user => user.AppRole)
            .SingleAsync(user => user.Id == fixture.TenantB.SeniorUserId);
        var foreignCompany = await fixture.Db.Companies.SingleAsync(item => item.Id == fixture.TenantB.CompanyId);
        foreignCompany.SubscriptionTier = SubscriptionTiers.Pro;
        await fixture.Db.SaveChangesAsync();
        await EnsureThrowsAsync<InvalidOperationException>(
            () => prepared.Service.CommitDraftAsync(foreignActor, prepared.Batch.Id),
            "A second tenant committed another company's checklist import.");
    }

    private static async Task VerifyAdditiveMigrationAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<VectorDbContext>().UseSqlite(connection).Options;
        await using var db = new VectorDbContext(options);
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE ImportBatches (Id INTEGER NOT NULL PRIMARY KEY);");
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE ChecklistTemplates (Id INTEGER NOT NULL PRIMARY KEY, CompanyId INTEGER NOT NULL);");
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE __EFMigrationsHistory (MigrationId TEXT NOT NULL PRIMARY KEY, ProductVersion TEXT NOT NULL);");

        const string migrationId = "20260718134500_AddChecklistImportDraftLinkage";
        var migrations = db.GetService<IMigrationsAssembly>().Migrations.Keys.OrderBy(item => item).ToList();
        foreach (var prior in migrations.Where(item => string.CompareOrdinal(item, migrationId) < 0))
        {
            await using var history = connection.CreateCommand();
            history.CommandText = "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ($id, '8.0.0');";
            history.Parameters.AddWithValue("$id", prior);
            await history.ExecuteNonQueryAsync();
        }

        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(migrationId);
        Ensure(await HasColumnAsync(connection, "ImportBatches", "ProposedRecordName"), "Checklist import name column was not added.");
        Ensure(await HasColumnAsync(connection, "ChecklistTemplates", "SourceImportBatchId"), "Checklist import linkage column was not added.");
        Ensure(await HasIndexAsync(connection, "ChecklistTemplates", "IX_ChecklistTemplates_CompanyId_SourceImportBatchId"),
            "Checklist import linkage index was not added.");

        var previous = migrations.Last(item => string.CompareOrdinal(item, migrationId) < 0);
        await migrator.MigrateAsync(previous);
        Ensure(!await HasColumnAsync(connection, "ImportBatches", "ProposedRecordName"), "Checklist import name column did not roll back.");
        Ensure(!await HasColumnAsync(connection, "ChecklistTemplates", "SourceImportBatchId"), "Checklist import linkage column did not roll back.");
    }

    private static async Task<bool> HasColumnAsync(SqliteConnection connection, string table, string column)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info('{table}');";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) if (string.Equals(reader.GetString(1), column, StringComparison.Ordinal)) return true;
        return false;
    }

    private static async Task<bool> HasIndexAsync(SqliteConnection connection, string table, string index)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA index_list('{table}');";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) if (string.Equals(reader.GetString(1), index, StringComparison.Ordinal)) return true;
        return false;
    }

    private static async Task AssertDraftOnlyAsync(
        TenantFixture fixture,
        int templateId,
        string expectedName,
        int expectedSections,
        int expectedItems)
    {
        var template = await fixture.Db.ChecklistTemplates
            .Include(item => item.Sections).ThenInclude(section => section.Items).ThenInclude(item => item.ColumnDefinitions)
            .SingleAsync(item => item.Id == templateId);
        Ensure(template.Name == expectedName, "The client-selected checklist name was not saved.");
        Ensure(template.SourceImportBatchId is not null && template.SourceType == "Imported", "Checklist import linkage was not saved.");
        Ensure(template.Status == "Draft" && !template.IsPublished, "Imported checklist became live without publishing.");
        Ensure(template.TargetVehicleType == "Unassigned", "Imported checklist received a hidden target.");
        Ensure(!await fixture.Db.ChecklistPublishScopes.AnyAsync(scope => scope.ChecklistTemplateId == templateId),
            "Imported checklist created a hidden publish scope.");
        Ensure(template.Sections.Count == expectedSections, "Imported checklist section count is incorrect.");
        Ensure(template.Sections.Sum(section => section.Items.Count) == expectedItems, "Imported checklist item count is incorrect.");
        Ensure(template.Sections.SelectMany(section => section.Items).All(item => item.ColumnDefinitions.Count > 0),
            "Imported checklist contains an item that cannot be edited/rendered as a column-backed field.");
    }

    private static async Task<PreparedChecklist> PrepareAsync(
        TenantFixture fixture,
        AppUser actor,
        StaticChecklistReader reader,
        string layout,
        string name,
        string? worksheet)
    {
        var file = new AssetFile
        {
            CompanyId = actor.CompanyId,
            UploadedByUserId = actor.Id,
            LinkedEntityType = SetupUploadService.RegisterUploadEntityType,
            Category = ImportTargetTypes.Checklist,
            OriginalFileName = $"{name}.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            StorageProvider = "test",
            StoragePath = $"test/company-{actor.CompanyId}/{Guid.NewGuid():N}.xlsx",
            SizeBytes = 100
        };
        fixture.Db.AssetFiles.Add(file);
        var profiles = reader.Worksheets.Select(data =>
            new ImportWorksheetProfile(data.Worksheet, data.Rows.Count + 1, data.Columns.Count, data.Rows.Sum(row => row.Values.Count))).ToList();
        var profile = new ImportSourceProfile(1, new string('C', 64), "XLSX", profiles.Count,
            profiles.Sum(item => item.RowCount), profiles.Sum(item => item.NonEmptyCellCount), profiles);
        var batches = new ImportBatchService(fixture.Db, new UserActionPermissionService(fixture.Db));
        var batch = await batches.CreateUploadedBatchAsync(actor, file, ImportTargetTypes.Checklist, profile, DateTime.UtcNow);
        await fixture.Db.SaveChangesAsync();
        var service = new ChecklistImportConversionService(
            fixture.Db, batches, reader, new UserActionPermissionService(fixture.Db));
        var draft = await service.PrepareAsync(actor, batch.Id, name, layout, worksheet, 1);
        return new PreparedChecklist(batch, draft, service);
    }

    private static ImportTabularData Data(string worksheet, string[] headings, string?[][] values)
    {
        var columns = headings.Select((heading, index) => new ImportSourceColumn(index, heading, [])).ToList();
        var rows = values.Select((row, rowIndex) => new ImportSourceRow(rowIndex + 2,
            row.Select((value, index) => (index, value)).ToDictionary(cell => cell.index, cell => cell.value))).ToList();
        return new ImportTabularData(worksheet, 1, columns, rows);
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

    private sealed record PreparedChecklist(ImportBatch Batch, ChecklistImportDraft Draft, ChecklistImportConversionService Service);

    private sealed class StaticChecklistReader : IImportTabularReader
    {
        private readonly IReadOnlyDictionary<string, ImportTabularData> _data;
        public StaticChecklistReader(IReadOnlyDictionary<string, ImportTabularData> data) => _data = data;
        public IReadOnlyList<ImportTabularData> Worksheets => _data.Values.ToList();
        public Task<ImportTabularData> ReadAsync(AssetFile sourceFile, string? worksheet, int headerRowNumber, CancellationToken cancellationToken = default)
        {
            var key = string.IsNullOrWhiteSpace(worksheet) ? _data.Keys.First() : worksheet;
            return Task.FromResult(_data.TryGetValue(key, out var value)
                ? value
                : throw new InvalidOperationException($"Worksheet '{key}' was not found."));
        }
    }
}
